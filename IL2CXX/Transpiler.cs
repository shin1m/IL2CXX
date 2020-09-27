﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace IL2CXX
{
    using static MethodKey;

    public partial class Transpiler
    {
        struct NativeInt { }
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
                if (Type.IsByRef || Type.IsPointer)
                {
                    VariableType = "void*";
                    prefix = "p";
                }
                else if (primitives.ContainsKey(Type))
                {
                    if (Type == typeof(long) || Type == typeof(ulong))
                    {
                        VariableType = "int64_t";
                        prefix = "j";
                    }
                    else if (Type == typeof(float) || Type == typeof(double))
                    {
                        VariableType = "double";
                        prefix = "f";
                    }
                    else if (Type == typeof(NativeInt))
                    {
                        VariableType = "intptr_t";
                        prefix = "q";
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
                    if (underlying == typeof(long) || underlying == typeof(ulong))
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
                else if (Type.IsValueType)
                {
                    var t = transpiler.Escape(Type);
                    VariableType = $"t_stacked<{t}::t_value>";
                    prefix = $"v{t}__";
                }
                else
                {
                    VariableType = "t_object*";
                    prefix = "o";
                }
                Indices.TryGetValue(VariableType, out var index);
                Variable = prefix + index;
                Indices[VariableType] = ++index;
                transpiler.definedIndices.TryGetValue(VariableType, out var defined);
                if (index > defined.Index) transpiler.definedIndices[VariableType] = (prefix, index);
            }
            public Stack Push(Type type) => new Stack(this, type);
            public IEnumerator<Stack> GetEnumerator()
            {
                for (var x = this; x != null; x = x.Pop) yield return x;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool IsPointer => VariableType == "void*";
        }
        class Instruction
        {
            public OpCode OpCode;
            public Func<int, Stack, (int, Stack)> Estimate;
            public Func<int, Stack, int> Generate;
        }

        private static readonly OpCode[] opcodes1 = new OpCode[256];
        private static readonly OpCode[] opcodes2 = new OpCode[256];
        private static readonly Regex unsafeCharacters = new Regex(@"(\W|_)", RegexOptions.Compiled);
        private static string Escape(string name) => unsafeCharacters.Replace(name, m => string.Join(string.Empty, m.Value.Select(x => $"_{(int)x:x}")));
        private static readonly IReadOnlyDictionary<Type, string> builtinTypes = new Dictionary<Type, string> {
            [typeof(object)] = "t_object",
            [typeof(Assembly)] = "t__assembly",
            [typeof(RuntimeAssembly)] = "t__runtime_assembly",
            [typeof(MemberInfo)] = "t__member_info",
            [typeof(MethodBase)] = "t__method_base",
            [typeof(ConstructorInfo)] = "t__constructor_info",
            [typeof(RuntimeConstructorInfo)] = "t__runtime_constructor_info",
            [typeof(MethodInfo)] = "t__method_info",
            [typeof(RuntimeMethodInfo)] = "t__runtime_method_info",
            [typeof(Type)] = "t__abstract_type",
            [typeof(RuntimeType)] = "t__type",
            [typeof(CriticalFinalizerObject)] = "t__critical_finalizer_object"
        };
        private static readonly IReadOnlyDictionary<Type, string> primitives = new Dictionary<Type, string> {
            [typeof(bool)] = "bool",
            [typeof(byte)] = "uint8_t",
            [typeof(sbyte)] = "int8_t",
            [typeof(short)] = "int16_t",
            [typeof(ushort)] = "uint16_t",
            [typeof(int)] = "int32_t",
            [typeof(uint)] = "uint32_t",
            [typeof(long)] = "int64_t",
            [typeof(ulong)] = "uint64_t",
            [typeof(NativeInt)] = "intptr_t",
            [typeof(char)] = "char16_t",
            [typeof(double)] = "double",
            [typeof(float)] = "float",
            [typeof(void)] = "void"
        };
        private static readonly Type typedReferenceByRefType = typeof(TypedReferenceTag).MakeByRefType();
        private static readonly IReadOnlyDictionary<(string, string), Type> typeOfAdd = new Dictionary<(string, string), Type> {
            [("int32_t", "int32_t")] = typeof(int),
            [("int32_t", "intptr_t")] = typeof(NativeInt),
            [("int32_t", "void*")] = typeof(void*),
            [("int64_t", "int64_t")] = typeof(long),
            [("intptr_t", "int32_t")] = typeof(NativeInt),
            [("intptr_t", "intptr_t")] = typeof(NativeInt),
            [("intptr_t", "void*")] = typeof(void*),
            [("double", "double")] = typeof(double),
            [("void*", "int32_t")] = typeof(void*),
            [("void*", "intptr_t")] = typeof(void*),
            [("void*", "void*")] = typeof(NativeInt)
        };
        private static readonly IReadOnlyDictionary<(string, string), Type> typeOfDiv_Un = new Dictionary<(string, string), Type> {
            [("int32_t", "int32_t")] = typeof(int),
            [("int32_t", "intptr_t")] = typeof(NativeInt),
            [("int64_t", "int64_t")] = typeof(long),
            [("intptr_t", "int32_t")] = typeof(NativeInt),
            [("intptr_t", "intptr_t")] = typeof(NativeInt)
        };
        private static readonly IReadOnlyDictionary<(string, string), Type> typeOfShl = new Dictionary<(string, string), Type> {
            [("int32_t", "int32_t")] = typeof(int),
            [("int32_t", "intptr_t")] = typeof(NativeInt),
            [("int64_t", "int32_t")] = typeof(long),
            [("int64_t", "intptr_t")] = typeof(long),
            [("intptr_t", "int32_t")] = typeof(NativeInt),
            [("intptr_t", "intptr_t")] = typeof(NativeInt)
        };
        private static MethodInfo FinalizeOf(Type x) => x.GetMethod(nameof(Finalize), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo finalizeOfObject = FinalizeOf(typeof(object));

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
        public readonly bool CheckNull;
        public readonly bool CheckRange;
        private readonly Instruction[] instructions1 = new Instruction[256];
        private readonly Instruction[] instructions2 = new Instruction[256];
        private readonly HashSet<string> typeIdentifiers = new HashSet<string>();
        private readonly Dictionary<Type, string> typeToIdentifier = new Dictionary<Type, string>();
        private readonly HashSet<(Type, string)> methodIdentifiers = new HashSet<(Type, string)>();
        private readonly Dictionary<MethodKey, string> methodToIdentifier = new Dictionary<MethodKey, string>();

        private static Type MakeByRefType(Type type) => type == typeof(TypedReference) ? typedReferenceByRefType : type.MakeByRefType();
        private static Type MakePointerType(Type type) => type == typeof(TypedReference) ? typeof(TypedReferenceTag*) : type.MakePointerType();
        private static Type GetElementType(Type type)
        {
            var t = type.GetElementType();
            return t == typeof(TypedReferenceTag) ? typeof(TypedReference) : t;
        }
        private sbyte ParseI1(ref int index) => (sbyte)bytes[index++];
        private short ParseI2(ref int index)
        {
            var x = BitConverter.ToInt16(bytes, index);
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
        private Type[] GetGenericArguments()
        {
            try
            {
                return method.GetGenericArguments();
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }
        private Type ParseType(ref int index) =>
            method.Module.ResolveType(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
        private FieldInfo ParseField(ref int index) =>
            method.Module.ResolveField(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
        private MethodBase ParseMethod(ref int index) =>
            method.Module.ResolveMethod(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
        private static Type GetVirtualThisType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SZArrayHelper<>) ? type.GetGenericArguments()[0].MakeArrayType() : type;
        private static Type GetThisType(MethodBase method)
        {
            var type = method.DeclaringType;
            return type.IsValueType ? MakePointerType(type) : GetVirtualThisType(type);
        }
        private Type GetArgumentType(int index)
        {
            var parameters = method.GetParameters();
            return method.IsStatic ? parameters[index].ParameterType : index > 0 ? parameters[index - 1].ParameterType : GetThisType(method);
        }
        private static Type GetReturnType(MethodBase method) => method is MethodInfo x ? x.ReturnType : typeof(void);
        private Stack EstimateCall(MethodBase method, Stack stack)
        {
            stack = stack.ElementAt(method.GetParameters().Length + (method.IsStatic ? 0 : 1));
            var @return = GetReturnType(method);
            return @return == typeof(void) ? stack : stack.Push(@return);
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
        public void Enqueue(MethodBase method) => queuedMethods.Enqueue(method);
        private bool IsComposite(Type x) => !(x.IsByRef || x.IsPointer || x.IsPrimitive || x.IsEnum || x == typeof(NativeInt));
        public string EscapeType(Type type)
        {
            if (typeToIdentifier.TryGetValue(type, out var name)) return name;
            var escaped = name = $"t_{Escape(type.ToString())}";
            for (var i = 0; !typeIdentifiers.Add(name); ++i) name = $"{escaped}__{i}";
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
                queuedTypes.Enqueue(type);
                if (type.IsByRef) return $"{name}&";
                if (type.IsPointer) return $"{name}*";
            }
            return EscapeType(type);
        }
        public string EscapeForValue(Type type, string tag = "{0}*") =>
            type.IsByRef || type.IsPointer ? $"{EscapeForValue(GetElementType(type))}*" :
            type.IsInterface ? EscapeForValue(typeof(object), tag) :
            primitives.TryGetValue(type, out var x) ? x :
            type.IsEnum ? primitives[type.GetEnumUnderlyingType()] :
            type.IsValueType ? $"{Escape(type)}::t_value" :
            string.Format(tag, Escape(type));
        public string EscapeForMember(Type type) => EscapeForValue(type, "t_slot_of<{0}>");
        public string EscapeForRoot(Type type) => type.IsValueType && !primitives.ContainsKey(type) && !type.IsEnum ? $"t_root<{EscapeForValue(type)}>" : EscapeForValue(type, "t_root<t_slot_of<{0}>>");
        public string EscapeForStacked(Type type) => type.IsValueType && !primitives.ContainsKey(type) && !type.IsEnum ? $"t_stacked<{EscapeForValue(type)}>" : EscapeForValue(type);
        public string Escape(FieldInfo field) => $"v_{Escape(field.Name)}";
        public string Escape(MethodBase method)
        {
            var key = ToKey(method);
            if (methodToIdentifier.TryGetValue(key, out var name)) return name;
            var escaped = name = $"f_{EscapeType(method.DeclaringType)}__{Escape(method.Name)}";
            for (var i = 0; !methodIdentifiers.Add((method.DeclaringType, name)); ++i) name = $"{escaped}__{i}";
            methodToIdentifier.Add(key, name);
            return name;
        }
        private string Escape(MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo field:
                    return Escape(field);
                case MethodBase method:
                    return Escape(method);
                default:
                    throw new Exception();
            }
        }
        public string GenerateCheckNull(string variable) => CheckNull ? $"\tif (!{variable}) [[unlikely]] f__throw_null_reference();\n" : string.Empty;
        private void GenerateCheckNull(Stack stack)
        {
            if (!stack.Type.IsValueType) writer.Write(GenerateCheckNull(stack.Variable));
        }
        public string GenerateCheckArgumentNull(string variable) => CheckNull ? $"\tif (!{variable}) [[unlikely]] f__throw_argument_null();\n" : string.Empty;
        public string GenerateCheckRange(string index, string length) => CheckRange ? $"\tif (static_cast<size_t>({index}) >= {length}) [[unlikely]] f__throw_index_out_of_range();\n" : string.Empty;
        private void GenerateArrayAccess(Stack array, Stack index, Func<string, string> access)
        {
            GenerateCheckNull(array);
            writer.WriteLine($"\t{{auto p = static_cast<{Escape(array.Type)}*>({array.Variable});");
            writer.Write(GenerateCheckRange(index.Variable, "p->v__length"));
            writer.WriteLine($"\t{access($"p->f__data()[{index.Variable}]")};}}");
        }
        public string CastValue(Type type, string variable) =>
            type == typeof(bool) ? $"{variable} != 0" :
            type.IsByRef || type.IsPointer ? $"reinterpret_cast<{EscapeForValue(type)}>({variable})" :
            type.IsPrimitive || type.IsValueType ? variable :
            $"static_cast<{EscapeForValue(type)}>({variable})";
        private string GenerateCall(MethodBase method, string function, IEnumerable<string> variables)
        {
            var arguments = new List<Type>();
            if (!method.IsStatic) arguments.Add(GetThisType(method));
            arguments.AddRange(method.GetParameters().Select(x => x.ParameterType));
            return $@"{function}({
    string.Join(",", arguments.Zip(variables.Reverse(), (a, v) => $"\n\t\t{CastValue(a, v)}"))
}{(arguments.Count > 0 ? "\n\t" : string.Empty)})";
        }
        private void GenerateCall(MethodBase method, string function, Stack stack, Stack after)
        {
            var call = GenerateCall(method, function, stack.Take(method.GetParameters().Length + (method.IsStatic ? 0 : 1)).Select(x => x.Variable));
            writer.Write($"\t{(GetReturnType(method) == typeof(void) ? string.Empty : $"{after.Variable} = ")}{call};\n");
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
            Enqueue(method);
            if (method.IsVirtual)
            {
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
            else
            {
                return Escape(method);
            }
        }
        private string GetInterfaceFunction(MethodBase method, Func<string, string> normal, Func<string, string> generic)
        {
            Enqueue(method);
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
                var types = string.Join(", ", method.GetParameters().Select(x => x.ParameterType).Prepend(GetReturnType(method)).Select(EscapeForStacked));
                return $@"{'\t'}{{static auto site = &{GetInterfaceFunction(method,
                    x => $"f__invoke<{x}, {types}>",
                    x => $"f__generic_invoke<{x}, {types}>"
                )};
{construct($@"site(
{'\t'}{'\t'}{CastValue(typeof(object), target)},
{string.Join(string.Empty, method.GetParameters().Zip(variables.Reverse(), (a, v) => $"\t\t{CastValue(a.ParameterType, v)},\n"))}{'\t'}{'\t'}reinterpret_cast<void**>(&site)
{'\t'})")}{'\t'}}}
";
            }
            else
            {
                return construct(GenerateCall(method, GetVirtualFunction(method, target), variables.Append(target)));
            }
        }
        private void GenerateInvokeUnmanaged(Type @return, IEnumerable<(ParameterInfo Parameter, int Index)> parameters, string function, TextWriter writer, CharSet charset = CharSet.Auto)
        {
            foreach (var (x, i) in parameters)
                if (x.ParameterType == typeof(StringBuilder))
                {
                    writer.WriteLine($"\tauto cs{i} = {(charset == CharSet.Unicode ? "f__to_cs16" : "f__to_cs")}(a_{i});");
                }
                else if (Attribute.IsDefined(x, typeof(OutAttribute)) && x.ParameterType.IsByRef)
                {
                    var e = GetElementType(x.ParameterType);
                    writer.WriteLine($"\t*a_{i} = ({EscapeForValue(e)}){{}};");
                    if (typeof(SafeHandle).IsAssignableFrom(e)) writer.WriteLine($"\tvoid* p{i};");
                }
            writer.Write($"\t{(@return == typeof(void) ? string.Empty : "auto result = ")}reinterpret_cast<{(typeof(SafeHandle).IsAssignableFrom(@return) ? "void*" : EscapeForValue(@return))}(*)(");
            writer.WriteLine(string.Join(",", parameters.Select(x => x.Parameter.ParameterType).Select(x =>
            {
                if (x == typeof(string)) return charset == CharSet.Unicode ? "const char16_t*" : "const char*";
                if (x == typeof(StringBuilder)) return charset == CharSet.Unicode ? "char16_t*" : "char*";
                if (x.IsByRef)
                {
                    var e = GetElementType(x);
                    if (e == typeof(IntPtr) || typeof(SafeHandle).IsAssignableFrom(e)) return "void**";
                }
                if (x == typeof(IntPtr) || typeof(SafeHandle).IsAssignableFrom(x)) return "void*";
                if (IsComposite(x))
                {
                    if (x.IsValueType) return EscapeForValue(x);
                    if (x.IsArray) return $"{EscapeForValue(GetElementType(x))}*";
                }
                return EscapeForValue(x);
            }).Select(x => $"\n\t\t{x}")));
            writer.Write($"\t)>({function})(");
            writer.WriteLine(string.Join(",", parameters.Select(xi =>
            {
                var x = xi.Parameter.ParameterType;
                var i = xi.Index;
                if (x == typeof(string))
                {
                    var s = $"&a_{i}->v__5ffirstChar";
                    return charset == CharSet.Unicode ? s : $"f__string({{{s}, static_cast<size_t>(a_{i}->v__5fstringLength)}}).c_str()";
                }
                if (x == typeof(StringBuilder)) return $"cs{i}.data()";
                if (x.IsByRef)
                {
                    var e = GetElementType(x);
                    if (e == typeof(IntPtr)) return $"&a_{i}->v__5fvalue";
                    if (typeof(SafeHandle).IsAssignableFrom(e)) return $"&p{i}";
                }
                if (typeof(SafeHandle).IsAssignableFrom(x)) return $"a_{i}->v_handle";
                if (IsComposite(x))
                {
                    if (x.IsValueType) return $"a_{i}";
                    if (x.IsArray) return $"a_{i} ? a_{i}->f__data() : nullptr";
                }
                return $"a_{i}";
            }).Select(x => $"\n\t\t{x}")));
            writer.WriteLine("\t);");
            foreach (var (x, i) in parameters)
                if (x.ParameterType == typeof(StringBuilder))
                {
                    writer.WriteLine($"\tf__from(a_{i}, cs{i}.data());");
                }
                else if (Attribute.IsDefined(x, typeof(OutAttribute)) && x.ParameterType.IsByRef)
                {
                    if (typeof(SafeHandle).IsAssignableFrom(GetElementType(x.ParameterType))) writer.WriteLine($"\t(*a_{i})->v_handle = p{i};");
                }
            if (typeof(SafeHandle).IsAssignableFrom(@return))
            {
                ConstructorInfo getCI(Type type) => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(IntPtr), typeof(bool) }, null);
                var ci = getCI(@return) ?? getCI(typeof(SafeHandle));
                writer.WriteLine($@"{'\t'}auto p = f__new_zerod<{Escape(@return)}>();
{'\t'}{Escape(ci)}(p, result, true);
{'\t'}return p;");
                Enqueue(ci);
            }
            else if (@return != typeof(void))
            {
                writer.WriteLine("\treturn result;");
            }
        }
    }
}
