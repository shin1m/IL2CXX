using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace IL2CXX
{
    using static MethodKey;

    public partial class Transpiler
    {
        struct TypedReferenceTag { }
        class Stack : IEnumerable<Stack>
        {
            private readonly Transpiler transpiler;
            public readonly Stack Pop;
            public readonly Dictionary<string, int> Indices;
            public readonly Type Type;
            public readonly string VariableType;
            public readonly string Variable;

            public Stack(Transpiler transpiler)
            {
                this.transpiler = transpiler;
                Indices = new Dictionary<string, int>();
            }
            private Stack(Stack pop, Type type)
            {
                transpiler = pop.transpiler;
                Pop = pop;
                Indices = new Dictionary<string, int>(Pop.Indices);
                Type = type;
                string prefix;
                if (Type.IsByRef || Type.IsPointer || Type == transpiler.typeofIntPtr || Type == transpiler.typeofUIntPtr)
                {
                    VariableType = "void*";
                    prefix = "p";
                }
                else if (transpiler.primitives.ContainsKey(Type))
                {
                    if (Type == transpiler.typeofInt64 || Type == transpiler.typeofUInt64)
                    {
                        VariableType = "int64_t";
                        prefix = "j";
                    }
                    else if (Type == transpiler.typeofSingle || Type == transpiler.typeofDouble)
                    {
                        VariableType = "double";
                        prefix = "f";
                    }
                    else
                    {
                        VariableType = "int32_t";
                        prefix = "i";
                    }
                }
                else if (Type.IsEnum)
                {
                    var underlying = Type.GetEnumUnderlyingType();
                    if (underlying == transpiler.typeofInt64 || underlying == transpiler.typeofUInt64)
                    {
                        VariableType = "int64_t";
                        prefix = "j";
                    }
                    else
                    {
                        VariableType = "int32_t";
                        prefix = "i";
                    }
                }
                else if (!Type.IsValueType)
                {
                    VariableType = "t__object* RECYCLONE__SPILL";
                    prefix = "o";
                }
                else
                {
                    var t = transpiler.Escape(Type);
                    VariableType = $"{t}::t_stacked{(transpiler.ToBeSpilled(Type) ? " RECYCLONE__SPILL" : string.Empty)}";
                    prefix = $"v{t}__";
                }
                Indices.TryGetValue(VariableType, out var index);
                Variable = prefix + index;
                Indices[VariableType] = ++index;
                transpiler.definedIndices.TryGetValue(VariableType, out var defined);
                if (index > defined.Index) transpiler.definedIndices[VariableType] = (prefix, index);
            }
            public Stack Push(Type type) => new(this, type);
            public IEnumerator<Stack> GetEnumerator()
            {
                for (var x = this; x != null; x = x.Pop) yield return x;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public string AsSigned => VariableType.EndsWith("*") ? $"reinterpret_cast<intptr_t>({Variable})" : Variable;
            public string AsUnsigned => VariableType switch
            {
                "int32_t" => $"static_cast<uint32_t>({Variable})",
                "int64_t" => $"static_cast<uint64_t>({Variable})",
                _ => VariableType.EndsWith("*") ? $"reinterpret_cast<uintptr_t>({Variable})" : Variable
            };
            public string Assign(string value) => $"{Variable} = {(VariableType.EndsWith("*") ? $"reinterpret_cast<{VariableType}>({value})" : value)}";
        }
        class Instruction
        {
            public OpCode OpCode;
            public Func<int, Stack, (int, Stack)> Estimate;
            public Func<int, Stack, int> Generate;
        }

        private static readonly OpCode[] opcodes1 = new OpCode[256];
        private static readonly OpCode[] opcodes2 = new OpCode[256];
        private static readonly Regex unsafeCharacters = new(@"(\W|_)", RegexOptions.Compiled);
        private static string Escape(string name) => unsafeCharacters.Replace(name, m => string.Join(string.Empty, m.Value.Select(x => $"_{(int)x:x}")));
        private static MethodInfo FinalizeOf(Type x) => x.GetMethod(nameof(Finalize), declaredAndInstance);

        static Transpiler()
        {
            foreach (var x in typeof(OpCodes).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public))
            {
                var opcode = (OpCode)x.GetValue(null);
                (opcode.Size == 1 ? opcodes1 : opcodes2)[opcode.Value & 0xff] = opcode;
            }
        }

        private readonly IBuiltin builtin;
        private readonly Action<string> log;
        public readonly PlatformID Target;
        public readonly bool Is64Bit;
        public readonly bool CheckNull;
        public readonly bool CheckRange;
        public IEnumerable<Type> Bundle = Enumerable.Empty<Type>();
        public Func<Type, bool> GenerateReflection = _ => false;
        private readonly Func<Type, Type> getType;
        public Type TypeOf<T>() => getType(typeof(T));
        public readonly Type typeofObject;
        public readonly Type typeofRuntimeAssembly;
        public readonly Type typeofRuntimeFieldInfo;
        public readonly Type typeofRuntimeConstructorInfo;
        public readonly Type typeofRuntimeMethodInfo;
        public readonly Type typeofRuntimePropertyInfo;
        public readonly Type typeofRuntimeType;
        public readonly Type typeofBoolean;
        public readonly Type typeofByte;
        public readonly Type typeofSByte;
        public readonly Type typeofInt16;
        public readonly Type typeofUInt16;
        public readonly Type typeofInt32;
        public readonly Type typeofUInt32;
        public readonly Type typeofInt64;
        public readonly Type typeofUInt64;
        public readonly Type typeofVoidPointer;
        public readonly Type typeofChar;
        public readonly Type typeofDouble;
        public readonly Type typeofSingle;
        public readonly Type typeofVoid;
        public readonly Type typeofIntPtr;
        public readonly Type typeofUIntPtr;
        public readonly Type typeofString;
        public readonly Type typeofStringBuilder;
        public readonly Type typeofException;
        public readonly Type typeofTypedReference;
        public readonly Type typeofTypedReferenceTag;
        public readonly Type typeofDelegate;
        public readonly Type typeofMulticastDelegate;
        public readonly Type typeofSafeHandle;
        public readonly Type typeofOutAttribute;
        public readonly Type typeofDllImportAttribute;
        public readonly Type typeofFieldOffsetAttribute;
        public readonly Type typeofMarshalAsAttribute;
        public readonly Type typeofThreadStaticAttribute;
        public readonly Type typeofRuntimeFieldHandle;
        public readonly Type typeofRuntimeMethodHandle;
        public readonly Type typeofRuntimeTypeHandle;
        public readonly Type typeofSZArrayHelper;
        public readonly Type typeofUtilities;
        private readonly IReadOnlyDictionary<Type, string> builtinTypes;
        private readonly IReadOnlyDictionary<Type, string> primitives;
        private readonly Type typedReferenceByRefType;
        private readonly IReadOnlyDictionary<(string, string), Type> typeOfAdd;
        private readonly IReadOnlyDictionary<(string, string), Type> typeOfDiv_Un;
        private readonly IReadOnlyDictionary<(string, string), Type> typeOfShl;
        private readonly MethodInfo finalizeOfObject;
        private readonly Instruction[] instructions1 = new Instruction[256];
        private readonly Instruction[] instructions2 = new Instruction[256];
        private readonly Dictionary<string, Type> genericParameters = new();
        private readonly HashSet<string> identifiers = new();
        private readonly Dictionary<Type, string> typeToIdentifier = new();
        private readonly Dictionary<MethodKey, string> methodToIdentifier = new();
        private readonly Dictionary<PropertyInfo, string> propertyToIdentifier = new();
        private readonly HashSet<MethodBase> ldftnMethods = new();

        private Type MakeByRefType(Type type) => type == typeofTypedReference ? typedReferenceByRefType : type.MakeByRefType();
        private Type MakePointerType(Type type) => (type == typeofTypedReference ? typeofTypedReferenceTag : type).MakePointerType();
        private Type GetElementType(Type type)
        {
            var t = type.GetElementType();
            return t == typeofTypedReferenceTag ? typeofTypedReference : t;
        }
        private byte ParseU1(ref int index) => bytes[index++];
        private sbyte ParseI1(ref int index) => (sbyte)bytes[index++];
        private ushort ParseU2(ref int index)
        {
            var x = BitConverter.ToUInt16(bytes, index);
            index += 2;
            return x;
        }
        private int ParseI4(ref int index)
        {
            var x = BitConverter.ToInt32(bytes, index);
            index += 4;
            return x;
        }
        private long ParseI8(ref int index)
        {
            var x = BitConverter.ToInt64(bytes, index);
            index += 8;
            return x;
        }
        private float ParseR4(ref int index)
        {
            var x = BitConverter.ToSingle(bytes, index);
            index += 4;
            return x;
        }
        private double ParseR8(ref int index)
        {
            var x = BitConverter.ToDouble(bytes, index);
            index += 8;
            return x;
        }
        private delegate int ParseBranchTarget(ref int index);
        private int ParseBranchTargetI1(ref int index)
        {
            var offset = ParseI1(ref index);
            return index + offset;
        }
        private int ParseBranchTargetI4(ref int index)
        {
            var offset = ParseI4(ref index);
            return index + offset;
        }
        private static readonly Type typeofEcmaModule = Type.GetType("System.Reflection.TypeLoading.Ecma.EcmaModule, System.Reflection.MetadataLoadContext", true);
        private static readonly PropertyInfo ecmaModuleReader = typeofEcmaModule.GetProperty("Reader", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MetadataReader GetMetadataReader(Module module) => (MetadataReader)ecmaModuleReader.GetValue(module);
        private MetadataReader GetMetadataReader() => GetMetadataReader(method.Module);
        private string ParseString(ref int index) =>
            //method.Module.ResolveString(ParseI4(ref index));
            GetMetadataReader().GetUserString((UserStringHandle)MetadataTokens.Handle(ParseI4(ref index)));
        private Type[] GetGenericArguments() => method.IsGenericMethod ? method.GetGenericArguments() : null;
        private static readonly Type typeofEcmaResolver = Type.GetType("System.Reflection.TypeLoading.Ecma.EcmaResolver, System.Reflection.MetadataLoadContext", true);
        private static readonly MethodBase ecmaResolverResolveType = typeofEcmaResolver.GetMethod("ResolveTypeDefRefOrSpec");
        private static readonly MethodBase ecmaResolverResolveMethod = typeofEcmaResolver.GetMethod("ResolveMethod").MakeGenericMethod(typeof(MethodBase));
        private static readonly Type typeofRoType = Type.GetType("System.Reflection.TypeLoading.RoType, System.Reflection.MetadataLoadContext", true);
        private static readonly Type typeofTypeContext = Type.GetType("System.Reflection.TypeLoading.TypeContext, System.Reflection.MetadataLoadContext", true);
        private object NewTypeContext(Type[] tas, Type[] mas)
        {
            Array rogas(Type[] gas)
            {
                if (gas == null) gas = Array.Empty<Type>();
                var ro = Array.CreateInstance(typeofRoType, gas.Length);
                Array.Copy(gas, ro, gas.Length);
                return ro;
            }
            return Activator.CreateInstance(typeofTypeContext, BindingFlags.Instance | BindingFlags.NonPublic, null, new[]
            {
                rogas(tas),
                rogas(mas)
            }, null);
        }
        private object NewTypeContext() => NewTypeContext(
            method.DeclaringType?.GetGenericArguments(),
            GetGenericArguments()
        );
        private Type ResolveType(EntityHandle handle) => (Type)ecmaResolverResolveType.Invoke(null, new[] { handle, method.Module, NewTypeContext() });
        private Type ParseType(ref int index) =>
            //method.Module.ResolveType(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
            ResolveType(MetadataTokens.EntityHandle(ParseI4(ref index)));
        private static readonly MethodInfo methodSpecificationDecodeSignature = typeof(MethodSpecification).GetMethod(nameof(MethodSpecification.DecodeSignature)).MakeGenericMethod(typeofRoType, typeofTypeContext);
        private class GenericMethodSignatureTypeProvider : ISignatureTypeProvider<Type, object>
        {
            private readonly object module;

            public GenericMethodSignatureTypeProvider(object module) => this.module = module;
            private static readonly MethodBase getTypeFromDefinition = typeofEcmaModule.GetMethod(nameof(GetTypeFromDefinition));
            public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => (Type)getTypeFromDefinition.Invoke(module, new object[] { reader, handle, rawTypeKind });
            private static readonly MethodBase getTypeFromReference = typeofEcmaModule.GetMethod(nameof(GetTypeFromReference));
            public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => (Type)getTypeFromReference.Invoke(module, new object[] { reader, handle, rawTypeKind });
            private static readonly MethodBase getTypeFromSpecification = typeofEcmaModule.GetMethod(nameof(GetTypeFromSpecification));
            public Type GetTypeFromSpecification(MetadataReader reader, object context, TypeSpecificationHandle handle, byte rawTypeKind) => (Type)getTypeFromSpecification.Invoke(module, new[] { reader, context, handle, rawTypeKind });
            public Type GetSZArrayType(Type elementType) => elementType.MakeArrayType();
            public Type GetArrayType(Type elementType, ArrayShape shape) => elementType.MakeArrayType(shape.Rank);
            public Type GetByReferenceType(Type elementType) => elementType.MakeByRefType();
            public Type GetPointerType(Type elementType) => elementType.MakePointerType();
            public Type GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments) => genericType.MakeGenericType(typeArguments.ToArray());
            private static readonly MethodBase getGenericTypeParameter = typeofEcmaModule.GetMethod(nameof(GetGenericTypeParameter));
            public Type GetGenericTypeParameter(object context, int index) => (Type)getGenericTypeParameter.Invoke(module, new[] { context, index });
            public Type GetGenericMethodParameter(object context, int index) => Type.MakeGenericMethodParameter(index);
            public Type GetFunctionPointerType(MethodSignature<Type> signature) => throw new NotSupportedException();
            private static readonly MethodBase getModifiedType = typeofEcmaModule.GetMethod(nameof(GetModifiedType));
            public Type GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired) => (Type)getModifiedType.Invoke(module, new object[] { modifier, unmodifiedType, isRequired });
            private static readonly MethodBase getPinnedType = typeofEcmaModule.GetMethod(nameof(GetPinnedType));
            public Type GetPinnedType(Type elementType) => (Type)getPinnedType.Invoke(module, new[] { elementType });
            private static readonly MethodBase getPrimitiveType = typeofEcmaModule.GetMethod(nameof(GetPrimitiveType));
            public Type GetPrimitiveType(PrimitiveTypeCode typeCode) => (Type)getPrimitiveType.Invoke(module, new object[] { typeCode });
        }
        private MemberInfo ResolveMemberReference(EntityHandle handle)
        {
            var reader = GetMetadataReader();
            var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
            var type = ResolveType(reference.Parent);
            var name = reader.GetString(reference.Name);
            if (reference.GetKind() == MemberReferenceKind.Field) return type.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new Exception($"{type} {name}");
            if (name == ".cctor") return type.TypeInitializer ?? throw new Exception(".cctor");
            var signature = reference.DecodeMethodSignature(new GenericMethodSignatureTypeProvider(method.Module), NewTypeContext(type.GetGenericArguments(), null));
            var parameters = signature.ParameterTypes.ToArray();
            switch (name)
            {
                case ".ctor":
                    return type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null) ?? throw new Exception($"{type} .ctor({string.Join(", ", parameters.AsEnumerable())})");
                case "op_Implicit":
                case "op_Explicit":
                    return type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public).Single(x => x.Name == name && x.ReturnType == signature.ReturnType && x.GetParameters().Select(x => x.ParameterType).SequenceEqual(parameters));
                default:
                    {
                        var gpc = signature.GenericParameterCount;
                        return type.GetMethod(name, gpc, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null) ?? throw new Exception($"{type} {name}`{gpc}({string.Join(", ", parameters.AsEnumerable())})");
                    }
            }
        }
        private FieldInfo ResolveFieldDefinition(EntityHandle handle)
        {
            var reader = GetMetadataReader();
            var definition = reader.GetFieldDefinition((FieldDefinitionHandle)handle);
            var type = ResolveType(definition.GetDeclaringType());
            var name = reader.GetString(definition.Name);
            return type.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new Exception($"{type} {name}");
        }
        private FieldInfo ParseField(ref int index)
        {
            //return method.Module.ResolveField(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
            var handle = MetadataTokens.EntityHandle(ParseI4(ref index));
            return handle.Kind == HandleKind.FieldDefinition
                ? ResolveFieldDefinition(handle)
                : (FieldInfo)ResolveMemberReference(handle);
        }
        private MethodBase ResolveMethodDefinition(EntityHandle handle) => (MethodBase)ecmaResolverResolveMethod.Invoke(null, new[] { (MethodDefinitionHandle)handle, method.Module, NewTypeContext() });
        private MethodBase ParseMethod(ref int index)
        {
            //return method.Module.ResolveMethod(token, method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
            //MethodBase resolve(EntityHandle handle) => handle.Kind == HandleKind.MethodDefinition ? ResolveMethodDefinition(handle) : (MethodBase)ResolveMemberReference(handle);
            MethodBase _resolve(EntityHandle handle) => handle.Kind == HandleKind.MethodDefinition ? ResolveMethodDefinition(handle) : (MethodBase)ResolveMemberReference(handle);
            MethodBase resolve(EntityHandle handle)
            {
                var m = _resolve(handle);
                if (m.DeclaringType.FullName == "Interop+Sys")
                {
                    if (m.Name == "SetPosixSignalHandler") return TypeOf<Interop.Sys>().GetMethod(nameof(Interop.Sys.SetPosixSignalHandler));
                    if (m.Name == "SetTerminalInvalidationHandler") return TypeOf<Interop.Sys>().GetMethod(nameof(Interop.Sys.SetTerminalInvalidationHandler));
                }
                if (m.DeclaringType.FullName == "Interop+Globalization" && m.Name == "EnumCalendarInfo") return TypeOf<Interop.Globalization>().GetMethod(nameof(Interop.Globalization.EnumCalendarInfo));
                return m;
            }
            var handle = MetadataTokens.EntityHandle(ParseI4(ref index));
            if (handle.Kind != HandleKind.MethodSpecification) return resolve(handle);
            var specification = GetMetadataReader().GetMethodSpecification((MethodSpecificationHandle)handle);
            return ((MethodInfo)resolve(specification.Method)).MakeGenericMethod(((IEnumerable)methodSpecificationDecodeSignature.Invoke(specification, new[] { method.Module, NewTypeContext() })).Cast<Type>().ToArray());
        }
        private MemberInfo ParseMember(ref int index)
        {
            //return method.Module.ResolveMember(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
            var handle = MetadataTokens.EntityHandle(ParseI4(ref index));
            switch (handle.Kind)
            {
                case HandleKind.TypeReference:
                case HandleKind.TypeDefinition:
                case HandleKind.TypeSpecification:
                    return ResolveType(handle);
                case HandleKind.FieldDefinition:
                    return ResolveFieldDefinition(handle);
                case HandleKind.MethodDefinition:
                    return ResolveMethodDefinition(handle);
                case HandleKind.MemberReference:
                    return ResolveMemberReference(handle);
                default:
                    throw new Exception($"{handle.Kind}");
            }
        }
        private (CallingConventions, Type, Type[]) ParseSignature(ref int index)
        {
            //var bytes = method.Module.ResolveSignature(ParseI4(ref index));
            var handle = MetadataTokens.EntityHandle(ParseI4(ref index));
            if (handle.Kind != HandleKind.StandaloneSignature) throw new NotSupportedException($"{handle.Kind}");
            var reader = GetMetadataReader();
            var standalone = reader.GetStandaloneSignature((StandaloneSignatureHandle)handle);
            if (standalone.GetKind() != StandaloneSignatureKind.Method) throw new NotSupportedException($"{standalone.GetKind()}");
            var signature = standalone.DecodeMethodSignature(new GenericMethodSignatureTypeProvider(method.Module), NewTypeContext());
            return ((CallingConventions)signature.Header.RawValue, signature.ReturnType, signature.ParameterTypes.ToArray());
            /*var bytes = reader.GetBlobBytes(standalone.Signature);
            var cc = (CallingConventions)bytes[0];
            var i = 1;
            int next()
            {
                int c = bytes[i++];
                if ((c & 0x80) == 0) return c;
                if ((c & 0x40) == 0) return (c & 0x3f) << 8 | bytes[i++];
                c = (c & 0x3f) << 8 | bytes[i++];
                c = c << 8 | bytes[i++];
                return c << 8 | bytes[i++];
            }
            Type type()
            {
                var t = next();
                if (t == 0x41) t = next();
                Type other()
                {
                    var token = next();
                    return method.Module.ResolveType((token & 3) switch
                    {
                        0 => 0x02,
                        1 => 0x01,
                        2 => 0x1B,
                        _ => throw new Exception()
                    } << 24 | token >> 2, method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
                }
                return t switch
                {
                    0x01 => typeofVoid,
                    0x02 => typeofBoolean,
                    0x03 => typeofChar,
                    0x04 => typeofSByte,
                    0x05 => typeofByte,
                    0x06 => typeofInt16,
                    0x07 => typeofUInt16,
                    0x08 => typeofInt32,
                    0x09 => typeofUInt32,
                    0x0A => typeofInt64,
                    0x0B => typeofUInt64,
                    0x0C => typeofSingle,
                    0x0D => typeofDouble,
                    0x0E => typeofString,
                    0x0F => MakePointerType(type()),
                    0x10 => MakeByRefType(type()),
                    0x11 => other(),
                    0x12 => other(),
                    0x16 => typeofTypedReference,
                    0x18 => typeofIntPtr,
                    0x19 => typeofUIntPtr,
                    0x1C => typeofObject,
                    0x20 => ((Func<Type>)(() =>
                    {
                        other();
                        return type();
                    }))(),
                    _ => throw new Exception()
                };
            }
            var n = next();
            var @return = type();
            var parameters = new Type[n];
            for (var j = 0; j < n; ++j) parameters[j] = type();
            return (cc, @return, parameters);*/
        }
        private static readonly Type typeofEcmaField = Type.GetType("System.Reflection.TypeLoading.Ecma.EcmaField, System.Reflection.MetadataLoadContext", true);
        private static readonly FieldInfo ecmaFieldHandle = typeofEcmaField.GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo ecmaModulePEReader = typeofEcmaModule.GetProperty("PEReader", BindingFlags.Instance | BindingFlags.NonPublic);
        private int GetSizeConst(FieldInfo field)
        {
            var reader = GetMetadataReader(field.Module);
            var definition = reader.GetFieldDefinition((FieldDefinitionHandle)ecmaFieldHandle.GetValue(field));
            var br = reader.GetBlobReader(definition.GetMarshallingDescriptor());
            br.ReadByte();
            return br.ReadCompressedInteger();
        }
        private IEnumerable<byte> GetRVAData(FieldInfo field)
        {
            var handle = (FieldDefinitionHandle)ecmaFieldHandle.GetValue(field);
            var rva = GetMetadataReader(field.Module).GetFieldDefinition(handle).GetRelativeVirtualAddress();
            var size = field.FieldType.StructLayoutAttribute?.Size ?? 0;
            if (size <= 0) size = Marshal.SizeOf(Type.GetType(field.FieldType.ToString(), true));
            return ((PEReader)ecmaModulePEReader.GetValue(field.Module)).GetSectionData(rva).GetContent(0, size);
        }
        private Type GetVirtualThisType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeofSZArrayHelper ? type.GetGenericArguments()[0].MakeArrayType() : type;
        private Type GetThisType(MethodBase method)
        {
            var type = method.DeclaringType;
            return type.IsValueType ? MakePointerType(type) : GetVirtualThisType(type);
        }
        private Type GetArgumentType(int index)
        {
            var parameters = method.GetParameters();
            return method.IsStatic ? parameters[index].ParameterType : index > 0 ? parameters[index - 1].ParameterType : GetThisType(method);
        }
        private Type GetReturnType(MethodBase method) => method is MethodInfo x ? x.ReturnType : typeofVoid;
        private Stack EstimateCall(MethodBase method, Stack stack)
        {
            stack = stack.ElementAt(method.GetParameters().Length + (method.IsStatic ? 0 : 1));
            var @return = GetReturnType(method);
            return @return == typeofVoid ? stack : stack.Push(@return);
        }
        private void Estimate(int index, Stack stack)
        {
            log($"enter {index:x04}");
            while (index < bytes.Length)
            {
                if (indexToStack.TryGetValue(index, out var x))
                {
                    var xs = stack.Select(y => y.VariableType);
                    var ys = x.Select(y => y.VariableType);
                    if (!xs.SequenceEqual(ys)) throw new Exception($"{index:x04}: {string.Join("|", xs)} {string.Join("|", ys)}");
                    break;
                }
                indexToStack.Add(index, stack);
                log($"{index:x04}: ");
                var instruction = instructions1[bytes[index++]];
                if (instruction.OpCode == OpCodes.Prefix1) instruction = instructions2[bytes[index++]];
                log($"{instruction.OpCode}");
                (index, stack) = instruction.Estimate?.Invoke(index, stack) ?? throw new Exception($"{instruction.OpCode}");
                log(string.Join(string.Empty, stack.Reverse().Select(y => $"{y.Type}|")));
            }
            log("exit");
        }
        private static readonly HashSet<string> invalids = new()
        {
            "System.RuntimeType",
            "System.Diagnostics.StackFrameHelper",
            "System.Reflection.RuntimeAssembly",
            "System.Reflection.RuntimeMethodInfo",
            "System.Reflection.RuntimeModule",
            "System.Reflection.Emit.InternalAssemblyBuilder",
            "System.Reflection.Emit.InternalModuleBuilder",
            "System.Runtime.CompilerServices.QCallAssembly",
            "System.Runtime.CompilerServices.QCallTypeHandle"
        };
        private void ThrowIfInvalid(Type type)
        {
            if (invalids.Contains(type.FullName)) throw new Exception($"{type} in {method.DeclaringType} :: {method}");
        }
        private void Enqueue(Type type)
        {
            ThrowIfInvalid(type);
            queuedTypes.Enqueue(type);
        }
        public void Enqueue(MethodBase method)
        {
            ThrowIfInvalid(method.DeclaringType);
            if (method is MethodInfo mi) ThrowIfInvalid(mi.ReturnType);
            foreach (var x in method.GetParameters()) ThrowIfInvalid(x.ParameterType);
            queuedMethods.Enqueue(method);
        }
        private static bool IsComposite(Type x) => !(x.IsByRef || x.IsPointer || x.IsPrimitive || x.IsEnum);
        private Type Normalize(Type type)
        {
            if (!type.IsGenericParameter) return type;
            var name = type.ToString();
            if (genericParameters.TryGetValue(name, out var x)) return x;
            genericParameters.Add(name, type);
            return type;
        }
        private string Identifier(string name)
        {
            var x = name;
            for (var i = 0; !identifiers.Add(x); ++i) x = $"{name}__{i}";
            return x;
        }
        public string EscapeType(Type type)
        {
            type = Normalize(type);
            if (typeToIdentifier.TryGetValue(type, out var name)) return name;
            name = Identifier($"t_{Escape(type.ToString())}");
            typeToIdentifier.Add(type, name);
            return name;
        }
        public string Escape(Type type)
        {
            if (type.IsValueType)
            {
                Define(type);
            }
            else
            {
                var e = GetElementType(type);
                var name = e == null ? null : Escape(e);
                Enqueue(type);
                if (type.IsByRef) return $"{name}&";
                if (type.IsPointer) return $"{name}*";
            }
            return EscapeType(type);
        }
        private string Escape(Type type, string @object = "{0}*", string value = "{0}::t_value") =>
            type.IsByRef || type.IsPointer ? $"{EscapeForValue(GetElementType(type))}*" :
            type.IsInterface ? Escape(typeofObject, @object, value) :
            primitives.TryGetValue(type, out var x) ? x :
            type.IsEnum ? primitives[type.GetEnumUnderlyingType()] :
            string.Format(type.IsValueType ? value : @object, Escape(type));
        public string EscapeForValue(Type type) => Escape(type, "{0}*", "{0}::t_value");
        public string EscapeForMember(Type type) => Escape(type, "t_slot_of<{0}>", "{0}::t_value");
        public string EscapeForRoot(Type type) => Escape(type, "t_root<t_slot_of<{0}>>", "t_root<{0}::t_value>");
        public string EscapeForStacked(Type type) => Escape(type, "{0}*", "{0}::t_stacked");
        private bool ToBeSpilled(Type type)
        {
            if (!IsComposite(type)) return false;
            if (!type.IsValueType) return true;
            if (type.StructLayoutAttribute?.Value == LayoutKind.Explicit)
            {
                var map = ((TypeDefinition)Define(type)).ExplicitMap;
                return map[0] && map.Count == Define(typeofIntPtr).UnmanagedSize;
            }
            else
            {
                var fields = type.GetFields(declaredAndInstance);
                return fields.Length == 1 && ToBeSpilled(fields[0].FieldType);
            }
        }
        public string EscapeForArgument(Type type) => $"{EscapeForStacked(type)}{(ToBeSpilled(type) ? " RECYCLONE__SPILL" : string.Empty)}";
        public string Escape(FieldInfo field) => $"v_{Escape(field.Name)}{(!field.IsStatic && field.GetCustomAttributesData().Any(x => x.AttributeType == typeofFieldOffsetAttribute) ? ".v" : string.Empty)}";
        public string Escape(MethodBase method)
        {
            var key = ToKey(method);
            if (methodToIdentifier.TryGetValue(key, out var name)) return name;
            name = Identifier($"f_{EscapeType(method.DeclaringType)}__{Escape(method.Name)}");
            methodToIdentifier.Add(key, name);
            return name;
        }
        private string Escape(PropertyInfo property)
        {
            if (propertyToIdentifier.TryGetValue(property, out var name)) return name;
            name = Identifier($"v__property_{EscapeType(property.DeclaringType)}__{Escape(property.Name)}");
            propertyToIdentifier.Add(property, name);
            return name;
        }
        private string Escape(MemberInfo member) => member switch
        {
            FieldInfo field => Escape(field),
            MethodBase method => Escape(method),
            PropertyInfo property => Escape(property),
            _ => throw new Exception()
        };
        public string GenerateCheckNull(string variable) => CheckNull ? $"\tif (!{variable}) [[unlikely]] {GenerateThrow("NullReference")};\n" : string.Empty;
        private void GenerateCheckNull(Stack stack)
        {
            if (!stack.Type.IsByRef && !stack.Type.IsPointer && !stack.Type.IsValueType) writer.Write(GenerateCheckNull(stack.Variable));
        }
        public string GenerateCheckArgumentNull(string variable) => CheckNull ? $"\tif (!{variable}) [[unlikely]] {GenerateThrow("ArgumentNull")};\n" : string.Empty;
        public string GenerateCheckRange(string index, string length) => CheckRange ? $"\tif (static_cast<size_t>({index}) >= {length}) [[unlikely]] {GenerateThrow("IndexOutOfRange")};\n" : string.Empty;
        private void GenerateArrayAccess(Stack array, Stack index, Func<string, string> access)
        {
            GenerateCheckNull(array);
            writer.WriteLine($"\t{{auto p = static_cast<{Escape(array.Type)}*>({array.Variable});");
            writer.Write(GenerateCheckRange(index.AsUnsigned, "p->v__length"));
            writer.WriteLine($"\t{access($"p->f_data()[{index.AsUnsigned}]")};}}");
        }
        public string CastValue(Type type, string variable) =>
            type == typeofBoolean ? $"{variable} != 0" :
            type.IsPrimitive || type == typeofVoidPointer ? variable :
            type.IsByRef || type.IsPointer ? $"reinterpret_cast<{EscapeForValue(type)}>({variable})" :
            type.IsValueType ? $"const_cast<std::remove_volatile_t<decltype({variable})>&>({variable})" :
            $"static_cast<{EscapeForValue(type)}>({variable})";
        private string GenerateCall(MethodBase method, string function, IEnumerable<string> variables)
        {
            Enqueue(method);
            var arguments = new List<Type>();
            if (!method.IsStatic) arguments.Add(GetThisType(method));
            arguments.AddRange(method.GetParameters().Select(x => x.ParameterType));
            return $@"{function}({
    string.Join(",", arguments.Zip(variables, (a, v) => $"\n\t\t{CastValue(a, v)}"))
}{(arguments.Count > 0 ? "\n\t" : string.Empty)})";
        }
        private void GenerateCall(MethodBase method, string function, Stack stack, Stack after)
        {
            var call = GenerateCall(method, function, stack.Take(method.GetParameters().Length + (method.IsStatic ? 0 : 1)).Select(x => x.Variable).Reverse());
            writer.WriteLine($"\t{(GetReturnType(method) == typeofVoid ? string.Empty : $"{after.Variable} = ")}{call};");
        }
        private int EnqueueIndexOf(MethodBase method, IEnumerable<IReadOnlyList<MethodInfo>> concretes)
        {
            Escape(method);
            var i = Define(method.DeclaringType).GetIndex(method);
            foreach (var ms in concretes) Enqueue(ms[i]);
            return i;
        }
        private (int, int) EnqueueGenericIndexOf(MethodBase method, IEnumerable<IReadOnlyList<MethodInfo>> concretes)
        {
            var gm = ((MethodInfo)method).GetGenericMethodDefinition();
            var i = Define(method.DeclaringType).GetIndex(gm);
            var t2i = genericMethodToTypesToIndex[ToKey(gm)];
            var ga = method.GetGenericArguments();
            if (!t2i.TryGetValue(ga, out var j))
            {
                j = t2i.Count;
                t2i.Add(ga, j);
            }
            foreach (var ms in concretes) Enqueue(ms[i].MakeGenericMethod(ga));
            return (i, j);
        }
        private string GetVirtualFunctionPointer(MethodBase method) =>
            $"{EscapeForStacked(GetReturnType(method))}(*)({string.Join(", ", method.GetParameters().Select(x => EscapeForStacked(x.ParameterType)).Prepend($"{Escape(GetVirtualThisType(method.DeclaringType))}*"))})";
        public string GetVirtualFunction(MethodBase method, string target)
        {
            if (!method.IsVirtual) return Escape(method);
            string at(int i) => $"reinterpret_cast<void**>({target}->f_type() + 1)[{i}]";
            var concretes = runtimeDefinitions.Where(x => x is TypeDefinition && x.Type.IsSubclassOf(method.DeclaringType)).Select(x => x.Methods);
            if (method.IsGenericMethod)
            {
                var (i, j) = EnqueueGenericIndexOf(method, concretes);
                return $"reinterpret_cast<{GetVirtualFunctionPointer(method)}>(reinterpret_cast<void**>({at(i)})[{j}])";
            }
            else
            {
                return $"reinterpret_cast<{GetVirtualFunctionPointer(method)}>({at(EnqueueIndexOf(method, concretes))})";
            }
        }
        private string GetInterfaceFunction(MethodBase method, Func<string, string> normal, Func<string, string> generic)
        {
            var concretes = runtimeDefinitions.OfType<TypeDefinition>().Select(x => x.InterfaceToMethods.TryGetValue(method.DeclaringType, out var ms) ? ms : null).Where(x => x != null);
            if (method.IsGenericMethod)
            {
                var (i, j) = EnqueueGenericIndexOf(method, concretes);
                return generic($"{Escape(method.DeclaringType)}, {i}, {j}");
            }
            else
            {
                return normal($"{Escape(method.DeclaringType)}, {EnqueueIndexOf(method, concretes)}");
            }
        }
        public string GenerateVirtualCall(MethodBase method, string target, IEnumerable<string> variables, Func<string, string> construct)
        {
            if (method.DeclaringType.IsInterface)
            {
                Enqueue(method);
                var types = string.Join(", ", method.GetParameters().Select(x => x.ParameterType).Prepend(GetReturnType(method)).Select(EscapeForStacked));
                return $@"{'\t'}{{static auto site = &{GetInterfaceFunction(method,
                    x => $"f__invoke<{x}, {types}>",
                    x => $"f__generic_invoke<{x}, {types}>"
                )};
{construct($@"site(
{'\t'}{'\t'}{CastValue(typeofObject, target)},
{string.Join(string.Empty, method.GetParameters().Zip(variables, (a, v) => $"\t\t{CastValue(a.ParameterType, v)},\n"))}{'\t'}{'\t'}reinterpret_cast<void**>(&site)
{'\t'})")}{'\t'}}}
";
            }
            else
            {
                return construct(GenerateCall(method, GetVirtualFunction(method, target), variables.Prepend(target)));
            }
        }
        private string UnmanagedReturn(Type type) => type == typeofIntPtr || typeofSafeHandle.IsAssignableFrom(type) ? "void*" : EscapeForValue(type);
        private IEnumerable<string> UnmanagedSignature(IEnumerable<ParameterInfo> parameters, CharSet charset) => parameters.Select(x => x.ParameterType).Select(x =>
        {
            if (x == typeofString) return charset == CharSet.Unicode ? "const char16_t*" : "const char*";
            if (x == typeofStringBuilder) return charset == CharSet.Unicode ? "char16_t*" : "char*";
            if (x.IsByRef)
            {
                var e = GetElementType(x);
                if (e == typeofIntPtr || typeofSafeHandle.IsAssignableFrom(e)) return "void**";
                if (Define(e).HasUnmanaged) return $"{Escape(e)}__unmanaged*";
            }
            if (x == typeofIntPtr || typeofSafeHandle.IsAssignableFrom(x)) return "void*";
            if (IsComposite(x))
            {
                if (x.IsValueType) return EscapeForValue(x);
                if (x.IsArray) return $"{EscapeForValue(GetElementType(x))}*";
            }
            return EscapeForValue(x);
        });
        private void GenerateInvokeUnmanaged(
            Type @return, IEnumerable<(ParameterInfo Parameter, int Index)> parameters, string function, TextWriter writer,
            CallingConvention callingConvention = CallingConvention.Winapi, CharSet charSet = CharSet.Auto, bool setLastError = false
        )
        {
            if (Target == PlatformID.Win32NT)
            {
                if (callingConvention == CallingConvention.Winapi) callingConvention = CallingConvention.StdCall;
            }
            else
            {
                switch (callingConvention)
                {
                    case CallingConvention.Winapi:
                    case CallingConvention.StdCall:
                        callingConvention = CallingConvention.Cdecl;
                        break;
                }
            }
            foreach (var (x, i) in parameters)
                if (x.ParameterType == typeofStringBuilder)
                {
                    writer.WriteLine($"\tauto cs{i} = {(charSet == CharSet.Unicode ? "f__to_cs16" : "f__to_cs")}(a_{i});");
                }
                else if (x.ParameterType.IsByRef)
                {
                    var e = GetElementType(x.ParameterType);
                    var @out = x.GetCustomAttributesData().Any(x => x.AttributeType == typeofOutAttribute);
                    if (@out)
                    {
                        var t = EscapeForValue(e);
                        writer.WriteLine($"\tf__store(*a_{i}, {(t.EndsWith("*") ? $"static_cast<{t}>(nullptr)" : $"{t}{{}}")});");
                    }
                    if (typeofSafeHandle.IsAssignableFrom(e))
                    {
                        writer.WriteLine($"\tvoid* p{i};");
                        if (!@out) writer.WriteLine($"\tp{i} = *a_{i};");
                    }
                    else if (Define(e).HasUnmanaged)
                    {
                        writer.WriteLine($"\t{Escape(e)}__unmanaged p{i};");
                        if (!@out) writer.WriteLine($"\tp{i}.f_in(a_{i});");
                    }
                }
            writer.Write($@"{'\t'}{(
    @return == typeofVoid ? string.Empty : "auto result = "
)}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<{UnmanagedReturn(@return)}({(callingConvention == CallingConvention.StdCall ? "__stdcall " : string.Empty)}*)({
    string.Join(",", UnmanagedSignature(parameters.Select(x => x.Parameter), charSet).Select(x => $"\n\t\t\t{x}"))
}
{'\t'}{'\t'})>({function})(");
            writer.WriteLine(string.Join(",", parameters.Select(xi =>
            {
                var x = xi.Parameter.ParameterType;
                var i = xi.Index;
                if (x == typeofString)
                {
                    var s = $"&a_{i}->v__5ffirstChar";
                    return $"a_{i} ? {(charSet == CharSet.Unicode ? s : $"f__string({{{s}, static_cast<size_t>(a_{i}->v__5fstringLength)}}).c_str()")} : nullptr";
                }
                if (x == typeofStringBuilder) return $"cs{i}.data()";
                if (x.IsByRef)
                {
                    var e = GetElementType(x);
                    if (e == typeofIntPtr) return $"&a_{i}->v__5fvalue";
                    if (typeofSafeHandle.IsAssignableFrom(e) || Define(e).HasUnmanaged) return $"&p{i}";
                }
                if (typeofSafeHandle.IsAssignableFrom(x)) return $"a_{i}->v_handle";
                if (IsComposite(x))
                {
                    if (x.IsValueType) return $"a_{i}";
                    if (x.IsArray) return $"a_{i} ? a_{i}->f_data() : nullptr";
                }
                return $"a_{i}";
            }).Select(x => $"\n\t\t\t{x}")));
            writer.WriteLine("\t\t);\n\t});");
            if (setLastError) writer.WriteLine($"\tv_last_unmanaged_error = {(Target == PlatformID.Win32NT ? "GetLastError()" : "errno")};");
            foreach (var (x, i) in parameters)
                if (x.ParameterType == typeofStringBuilder)
                {
                    writer.WriteLine($"\tf__from(a_{i}, cs{i}.data());");
                }
                else if (x.ParameterType.IsByRef)
                {
                    var e = GetElementType(x.ParameterType);
                    if (typeofSafeHandle.IsAssignableFrom(e))
                        writer.WriteLine($"\t(*a_{i})->v_handle = p{i};");
                    else if (Define(e).HasUnmanaged)
                        writer.WriteLine($"\tp{i}.f_out(a_{i});");
                }
            if (typeofSafeHandle.IsAssignableFrom(@return))
            {
                ConstructorInfo getCI(Type type) => type.GetConstructor(declaredAndInstance, null, new[] { typeofIntPtr, typeofBoolean }, null);
                var ci = getCI(@return) ?? getCI(typeofSafeHandle);
                writer.WriteLine($@"{'\t'}auto p = f__new_zerod<{Escape(@return)}>();
{'\t'}{Escape(ci)}(p, result, true);
{'\t'}return p;");
                Enqueue(ci);
            }
            else if (@return != typeofVoid)
            {
                writer.WriteLine("\treturn result;");
            }
        }
        public string GenerateThrow(string name)
        {
            var m = typeofUtilities.GetMethod($"Throw{name}");
            Enqueue(m);
            return $"{Escape(m)}()";
        }
        static readonly IReadOnlyDictionary<char, string> escapes = new Dictionary<char, string>
        {
            ['\''] = "\\'",
            ['"'] = "\\\"",
            ['?'] = "\\?",
            ['\\'] = "\\\\",
            ['\a'] = "\\a",
            ['\b'] = "\\b",
            ['\f'] = "\\f",
            ['\n'] = "\\n",
            ['\r'] = "\\r",
            ['\t'] = "\\t",
            ['\v'] = "\\v"
        };
        private static void WriteNewString(TextWriter writer, string value)
        {
            writer.Write("f__new_string(u\"");
            foreach (var c in value)
                if (escapes.TryGetValue(c, out var e))
                    writer.Write(e);
                else if (c < 0x20 || c >= 0x7f)
                    writer.Write($"\\x{(ushort)c:X}");
                else
                    writer.Write(c);
            writer.WriteLine("\"sv)");
        }
        private void GenerateInvokeFunction(MethodBase method, IEnumerable<string> arguments, TextWriter writer)
        {
            var @return = GetReturnType(method);
            var @this = GetVirtualThisType(method.DeclaringType);
            string construct(string call) => @return == typeofVoid ? $"\t{call};\n\treturn nullptr;\n" : $"\treturn {(@return.IsValueType ? $"f__new_constructed<{Escape(@return)}>({call})" : call)};\n";
            var isConcrete = !method.DeclaringType.IsInterface && (!method.IsVirtual || method.IsFinal);
            if (isConcrete)
            {
                writer.Write(construct(GenerateCall(method, Escape(method), arguments)));
            }
            else if (@this.IsSealed)
            {
                var cm = GetConcrete(GetBaseDefinition((MethodInfo)method), @this);
                writer.Write(construct(GenerateCall(cm, Escape(cm), arguments)));
            }
            else
            {
                writer.Write(GenerateVirtualCall(GetBaseDefinition((MethodInfo)method), "a_this", arguments.Skip(1), construct));
            }
        }
        private string GenerateCheck(Type type, string x, string condition, string exception) => $"\tif ({condition} !{x}->f_type()->f_is(&t__type_of<{Escape(type)}>::v__instance)) [[unlikely]] {GenerateThrow(exception)};\n";
        private string GenerateInvokeFunction(MethodBase method)
        {
            using var writer = new StringWriter();
            writer.WriteLine("[](t__object* RECYCLONE__SPILL a_this, int32_t, t__object* RECYCLONE__SPILL, t__object* RECYCLONE__SPILL a_parameters, t__object* RECYCLONE__SPILL) -> t__object*\n{");
            var @return = GetReturnType(method);
            if (@return.IsByRef || @return.IsPointer || method.DeclaringType.IsByRefLike && !method.IsStatic)
            {
                writer.WriteLine($"\t{GenerateThrow("NotSupported")};\n}}");
                return writer.ToString();
            }
            var parameters = method.GetParameters();
            writer.WriteLine($@"{'\t'}auto parameters = static_cast<{EscapeForValue(TypeOf<object[]>())}>(a_parameters);
{'\t'}if ({(parameters.Length > 0 ? $"parameters->v__length != {parameters.Length}" : "parameters")}) [[unlikely]] {GenerateThrow("TargetParameterCount")};");
            var arguments = new List<string>();
            var @this = GetVirtualThisType(method.DeclaringType);
            if (!method.IsStatic)
            {
                writer.Write(GenerateCheck(@this, "a_this", "!a_this ||", "Target"));
                arguments.Add(@this.IsValueType ? $"&static_cast<{Escape(@this)}*>(a_this)->v__value" : "a_this");
            }
            arguments.AddRange(method.GetParameters().Select((x, i) =>
            {
                writer.WriteLine($"\tauto& p{i} = parameters->f_data()[{i}];");
                string f(Type type)
                {
                    if (type.IsValueType)
                    {
                        writer.Write(GenerateCheck(type, $"p{i}", $"!p{i} ||", "Argument"));
                        return $"static_cast<{Escape(type)}*>(p{i})->v__value";
                    }
                    else
                    {
                        writer.Write(GenerateCheck(type, $"p{i}", $"p{i} &&", "Argument"));
                        return $"p{i}";
                    }
                }
                var type = x.ParameterType;
                return type.IsByRef || type.IsPointer ? $"&{f(GetElementType(type))}" : f(type);
            }));
            GenerateInvokeFunction(method, arguments, writer);
            writer.Write('}');
            return writer.ToString();
        }
        private string GenerateWASMInvokeFunction(MethodBase method)
        {
            using var writer = new StringWriter();
            writer.WriteLine("[](t__object* RECYCLONE__SPILL a_this, void** a_parameters) -> t__object*\n{");
            var @return = GetReturnType(method);
            if (@return.IsByRef || @return.IsPointer || method.DeclaringType.IsByRefLike && !method.IsStatic)
            {
                writer.WriteLine($"\t{GenerateThrow("NotSupported")};\n}}");
                return writer.ToString();
            }
            var parameters = method.GetParameters();
            var arguments = new List<string>();
            var @this = GetVirtualThisType(method.DeclaringType);
            if (!method.IsStatic)
            {
                writer.Write(GenerateCheck(@this, "a_this", "!a_this ||", "Target"));
                arguments.Add(@this.IsValueType ? $"&static_cast<{Escape(@this)}*>(a_this)->v__value" : "a_this");
            }
            arguments.AddRange(method.GetParameters().Select((x, i) => x.ParameterType.IsValueType ? $"*static_cast<{EscapeForValue(x.ParameterType)}*>(a_parameters[{i}])" : $"a_parameters[{i}]"));
            GenerateInvokeFunction(method, arguments, writer);
            writer.Write('}');
            return writer.ToString();
        }
    }
}
