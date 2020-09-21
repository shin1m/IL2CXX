using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace IL2CXX
{
    using static MethodKey;

    public partial class Transpiler
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        class InterfaceDefinition : RuntimeDefinition
        {
            public InterfaceDefinition(Type type, Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex) : base(type)
            {
                IsManaged = true;
                foreach (var x in Type.GetMethods()) Add(x, genericMethodToTypesToIndex);
            }
            protected override int GetIndex(MethodKey method) => MethodToIndex[method];
        }
        class TypeDefinition : RuntimeDefinition
        {
            private static readonly MethodKey finalizeKeyOfObject = new MethodKey(finalizeOfObject);

            public readonly TypeDefinition Base;
            public readonly Dictionary<Type, MethodInfo[]> InterfaceToMethods = new Dictionary<Type, MethodInfo[]>();
            public readonly string DefaultConstructor;
            public readonly string Delegate;

            public TypeDefinition(Type type, Transpiler transpiler) : base(type)
            {
                if (Type.BaseType != null)
                {
                    Base = (TypeDefinition)transpiler.Define(Type.BaseType);
                    Methods.AddRange(Base.Methods);
                }
                IsManaged = Type == typeof(object) || Type != typeof(ValueType) && Base.IsManaged;
                foreach (var x in Type.GetMethods(declaredAndInstance).Where(x => x.IsVirtual))
                {
                    var i = GetIndex(x.GetBaseDefinition());
                    if (i < 0)
                        Add(x, transpiler.genericMethodToTypesToIndex);
                    else
                        Methods[i] = x;
                }
                foreach (var x in Type.GetInterfaces())
                {
                    var definition = (InterfaceDefinition)transpiler.Define(x);
                    var methods = new MethodInfo[definition.Methods.Count];
                    var map = (Type.IsArray && x.IsGenericType ? typeof(SZArrayHelper<>).MakeGenericType(GetElementType(Type)) : Type).GetInterfaceMap(x);
                    foreach (var (i, t) in map.InterfaceMethods.Zip(map.TargetMethods, (i, t) => (i, t))) methods[definition.GetIndex(i)] = t;
                    InterfaceToMethods.Add(x, methods);
                }
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    var identifier = transpiler.Escape(Type);
                    DefaultConstructor = $@"
t__runtime_constructor_info v__default_constructor_{identifier}{{&t__type_of<t__runtime_constructor_info>::v__instance, [](t_object*) -> t_object*
{{
{'\t'}auto p = f__new_zerod<{identifier}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}return p;
}}}};
";
                    transpiler.Enqueue(constructor);
                }
                if (Type.IsSubclassOf(typeof(Delegate)) && Type != typeof(MulticastDelegate))
                {
                    var invoke = (MethodInfo)Type.GetMethod("Invoke");
                    transpiler.Enqueue(invoke);
                    var @return = invoke.ReturnType;
                    var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                    string generate(Type t, string body) => $@"reinterpret_cast<void*>(+[]({
    string.Join(",", parameters.Prepend(t).Select((x, i) => $"\n\t{transpiler.EscapeForStacked(x)} a_{i}"))
}
) -> {transpiler.EscapeForStacked(@return)}
{{
{body}}});";
                    using (var writer = new StringWriter())
                    {
                        transpiler.GenerateInvokeUnmanaged(@return, invoke.GetParameters().Select((x, i) => (x, i + 1)), "a_0->v__5fmethodPtrAux.v__5fvalue", writer);
                        Delegate = $@"v__multicast_invoke = {generate(typeof(MulticastDelegate), $@"{'\t'}auto xs = static_cast<{transpiler.Escape(typeof(object[]))}*>(a_0->v__5finvocationList)->f__data();
{'\t'}auto n = static_cast<intptr_t>(a_0->v__5finvocationCount) - 1;
{'\t'}for (intptr_t i = 0; i < n; ++i) {transpiler.Escape(invoke)}({string.Join(", ", parameters.Select((_, i) => $"a_{i + 1}").Prepend(transpiler.CastValue(Type, "xs[i]")))});
{'\t'}{(@return == typeof(void) ? string.Empty : "return ")}{transpiler.Escape(invoke)}({string.Join(", ", parameters.Select((x, i) => $"a_{i + 1}").Prepend(transpiler.CastValue(Type, "xs[n]")))});
")}
v__invoke_unmanaged = {generate(Type, writer.ToString())}
";
                    }
                }
            }
            protected override int GetIndex(MethodKey method) => MethodToIndex.TryGetValue(method, out var i) ? i : Base?.GetIndex(method) ?? -1;
        }
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
        private readonly StringWriter typeDeclarations = new StringWriter();
        private readonly StringWriter typeDefinitions = new StringWriter();
        private readonly StringWriter staticDefinitions = new StringWriter();
        private readonly StringWriter staticMembers = new StringWriter();
        private readonly StringWriter threadStaticMembers = new StringWriter();
        private readonly StringWriter fieldDeclarations = new StringWriter();
        private readonly StringWriter fieldDefinitions = new StringWriter();
        private readonly StringWriter functionDeclarations = new StringWriter();
        private readonly List<RuntimeDefinition> runtimeDefinitions = new List<RuntimeDefinition>();
        private readonly Dictionary<Type, RuntimeDefinition> typeToRuntime = new Dictionary<Type, RuntimeDefinition>();
        private readonly HashSet<string> typeIdentifiers = new HashSet<string>();
        private readonly Dictionary<Type, string> typeToIdentifier = new Dictionary<Type, string>();
        private readonly HashSet<(Type, string)> methodIdentifiers = new HashSet<(Type, string)>();
        private readonly Dictionary<MethodKey, string> methodToIdentifier = new Dictionary<MethodKey, string>();
        private readonly Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex = new Dictionary<MethodKey, Dictionary<Type[], int>>();
        private readonly Queue<Type> queuedTypes = new Queue<Type>();
        private readonly HashSet<MethodKey> visitedMethods = new HashSet<MethodKey>();
        private readonly Queue<MethodBase> queuedMethods = new Queue<MethodBase>();
        private MethodBase method;
        private byte[] bytes;
        private SortedDictionary<string, (string Prefix, int Index)> definedIndices;
        private Dictionary<int, Stack> indexToStack;
        private TextWriter writer;
        private readonly Stack<ExceptionHandlingClause> tries = new Stack<ExceptionHandlingClause>();
        private Type constrained;
        private bool @volatile;
        private bool processed;

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
        public RuntimeDefinition Define(Type type)
        {
            if (typeToRuntime.TryGetValue(type, out var definition)) return definition;
            if (processed) throw new InvalidOperationException($"{type}");
            if (type.IsByRef || type.IsPointer)
            {
                definition = new RuntimeDefinition(type);
                typeToRuntime.Add(type, definition);
            }
            else if (type.IsInterface)
            {
                definition = new InterfaceDefinition(type, genericMethodToTypesToIndex);
                typeToRuntime.Add(type, definition);
                typeDeclarations.WriteLine($@"// {type.AssemblyQualifiedName}
struct {Escape(type)}
{{
}};");
            }
            else
            {
                if (type.DeclaringType?.Name == "<PrivateImplementationDetails>") return null;
                typeToRuntime.Add(type, null);
                var td = new TypeDefinition(type, this);
                void enqueue(MethodInfo m, MethodInfo concrete)
                {
                    if (m.IsGenericMethod)
                        foreach (var k in genericMethodToTypesToIndex[ToKey(m)].Keys)
                            Enqueue(concrete.MakeGenericMethod(k));
                    else if (methodToIdentifier.ContainsKey(ToKey(m)))
                        Enqueue(concrete);
                }
                foreach (var m in td.Methods.Where(x => !x.IsAbstract)) enqueue(m.GetBaseDefinition(), m);
                foreach (var p in td.InterfaceToMethods)
                {
                    var id = typeToRuntime[p.Key];
                    foreach (var m in id.Methods) enqueue(m, p.Value[id.GetIndex(m)]);
                }
                typeToRuntime[type] = definition = td;
                var identifier = Escape(type);
                var staticFields = new List<FieldInfo>();
                var threadStaticFields = new List<FieldInfo>();
                if (!type.IsEnum)
                    foreach (var x in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        (Attribute.IsDefined(x, typeof(ThreadStaticAttribute)) ? threadStaticFields : staticFields).Add(x);
                var staticDefinitions = new StringWriter();
                var staticMembers = new StringWriter();
                var initialize = builtin.GetInitialize(this, type);
                if (type.Name == "<PrivateImplementationDetails>")
                {
                    foreach (var x in staticFields)
                    {
                        var bytes = new byte[Marshal.SizeOf(x.FieldType)];
                        RuntimeHelpers.InitializeArray(bytes, x.FieldHandle);
                        fieldDeclarations.WriteLine($@"extern uint8_t v__field_{identifier}__{Escape(x.Name)}[];
inline void* f__field_{identifier}__{Escape(x.Name)}()
{{
{'\t'}return v__field_{identifier}__{Escape(x.Name)};
}}");
                        fieldDefinitions.WriteLine($@"uint8_t v__field_{identifier}__{Escape(x.Name)}[] = {{{string.Join(", ", bytes.Select(y => $"0x{y:x02}"))}}};");
                    }
                }
                else if (staticFields.Count > 0 || initialize != null || type.TypeInitializer != null)
                {
                    staticDefinitions.WriteLine($@"
struct t__static_{identifier}
{{");
                    foreach (var x in staticFields) staticDefinitions.WriteLine($"\t{EscapeForRoot(x.FieldType)} {Escape(x)}{{}};");
                    staticDefinitions.WriteLine($@"{'\t'}void f_initialize()
{'\t'}{{");
                    if (initialize != null) staticDefinitions.WriteLine(initialize);
                    if (type.TypeInitializer != null)
                    {
                        staticDefinitions.WriteLine($"\t\t{Escape(type.TypeInitializer)}();");
                        Enqueue(type.TypeInitializer);
                    }
                    staticDefinitions.WriteLine($@"{'\t'}}}
}};");
                    foreach (var x in staticFields) fieldDeclarations.WriteLine($@"inline void* f__field_{identifier}__{Escape(x.Name)}()
{{
{'\t'}return &t_static::v_instance->v_{identifier}->{Escape(x)};
}}");
                    staticMembers.WriteLine($"\tt__lazy<t__static_{identifier}> v_{identifier};");
                }
                var threadStaticMembers = new StringWriter();
                if (threadStaticFields.Count > 0)
                {
                    threadStaticMembers.WriteLine("\tstruct\n\t{");
                    foreach (var x in threadStaticFields) threadStaticMembers.WriteLine($"\t\t{EscapeForRoot(x.FieldType)} {Escape(x)}{{}};");
                    threadStaticMembers.WriteLine($"\t}} v_{identifier};");
                }
                var declaration = $"// {type.AssemblyQualifiedName}";
                if (builtinTypes.TryGetValue(type, out var builtinName))
                {
                    typeDeclarations.WriteLine($"{declaration}\nusing {identifier} = {builtinName};");
                }
                else
                {
                    declaration += $"\nstruct {identifier}";
                    var @base = type.BaseType == null ? string.Empty : $" : {builtin.GetBase(type) ?? Escape(type.BaseType)}";
                    string members;
                    if (type == typeof(void))
                    {
                        members = string.Empty;
                    }
                    else if (primitives.TryGetValue(type, out var name) || type.IsEnum)
                    {
                        if (name == null) name = primitives[type.GetEnumUnderlyingType()];
                        members = $@"{'\t'}{name} v__value;
{'\t'}void f__construct({name} a_value)
{'\t'}{{
{'\t'}{'\t'}v__value = a_value;
{'\t'}}}
";
                    }
                    else
                    {
                        var mm = builtin.GetMembers(this, type);
                        members = mm.members;
                        if (members == null)
                        {
                            string scan(Type x, string y) => x.IsValueType ? $"{y}.f__scan(a_scan)" : $"a_scan({y})";
                            if (type.IsArray)
                            {
                                var element = GetElementType(type);
                                var elementIdentifier = EscapeForMember(element);
                                members = $@"{'\t'}t__bound v__bounds[{type.GetArrayRank()}];
{'\t'}{elementIdentifier}* f__data()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<{elementIdentifier}*>(this + 1);
{'\t'}}}
";
                                if (IsComposite(element)) members += $@"{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{'\t'}{'\t'}{Escape(type.BaseType)}::f__scan(a_scan);
{'\t'}{'\t'}auto p = f__data();
{'\t'}{'\t'}for (size_t i = 0; i < v__length; ++i) {scan(element, "p[i]")};
{'\t'}}}
";
                            }
                            else
                            {
                                var fields = type.GetFields(declaredAndInstance);
                                td.IsManaged |= fields.Select(x => x.FieldType).Any(x => IsComposite(x) && (!x.IsValueType || Define(x).IsManaged));
                                string variables(string indent)
                                {
                                    var sb = new StringBuilder();
                                    string variable(FieldInfo x) => $"{EscapeForMember(x.FieldType)} {Escape(x)};";
                                    var layout = type.StructLayoutAttribute;
                                    if (layout?.Value == LayoutKind.Explicit)
                                    {
                                        sb.AppendLine($"{indent}union\n{indent}{{");
                                        if (layout.Size > 0) sb.AppendLine($"{indent}\tchar v__size[{layout.Size}];");
                                        var i = 0;
                                        foreach (var x in fields)
                                        {
                                            var offset = x.GetCustomAttribute<FieldOffsetAttribute>().Value;
                                            sb.AppendLine(offset > 0 ? $@"{indent}{'\t'}struct
{indent}{'\t'}{{
{indent}{'\t'}{'\t'}char v__offset{i++}[{offset}];
{indent}{'\t'}{'\t'}{variable(x)}
{indent}{'\t'}}};" : $"{indent}\t{variable(x)}");
                                        }
                                        sb.AppendLine($"{indent}}};");
                                    }
                                    else
                                    {
                                        var i = 0;
                                        foreach (var x in fields)
                                        {
                                            sb.AppendLine($"{indent}{variable(x)}");
                                            try
                                            {
                                                i += Marshal.SizeOf(x.FieldType);
                                            } catch { }
                                        }
                                        if (layout?.Size > i) sb.AppendLine($"{indent}char v__padding[{layout.Size - i}];");
                                    }
                                    return sb.ToString();
                                }
                                string scanSlots(string indent) => string.Join(string.Empty, fields.Where(x => IsComposite(x.FieldType)).Select(x => $"{indent}{scan(x.FieldType, Escape(x))};\n"));
                                members = type.IsValueType
                                    ? td.IsManaged
                                        ? $@"{variables("\t\t")}
{'\t'}{'\t'}t_value() = default;
{'\t'}{'\t'}t_value(const t_value& a_value){(fields.Length > 0 ? $@" :
{string.Join(",\n", fields.Select(x => $"\t\t\t{Escape(x)}(a_value.{Escape(x)})"))}
" : string.Empty)}{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}IL2CXX__PORTABLE__ALWAYS_INLINE t_value& operator=(const t_value& a_value)
{'\t'}{'\t'}{{
{string.Join(string.Empty, fields.Select(x => $"\t\t\t{Escape(x)} = a_value.{Escape(x)};\n"))}{'\t'}{'\t'}{'\t'}return *this;
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{string.Join(string.Empty, fields.Where(x => IsComposite(x.FieldType)).Select(x => $"\t\t\t{Escape(x)}.f__destruct();\n"))}{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{scanSlots("\t\t\t")}{'\t'}{'\t'}}}
"
                                        : $@"{variables("\t\t")}
{'\t'}{'\t'}t_value() = default;
{'\t'}{'\t'}t_value(const t_value& a_value)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}std::memcpy(this, &a_value, sizeof(t_value));
{'\t'}{'\t'}}}
{'\t'}{'\t'}t_value& operator=(const t_value& a_value)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}std::memcpy(this, &a_value, sizeof(t_value));
{'\t'}{'\t'}{'\t'}return *this;
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
"
                                    : $@"{variables("\t")}
{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{(type.BaseType == null ? string.Empty : $"\t\t{Escape(type.BaseType)}::f__scan(a_scan);\n")}{scanSlots("\t\t")}{'\t'}}}
{'\t'}void f__construct({identifier}* a_p) const
{'\t'}{{
{(type.BaseType == null ? string.Empty : $"\t\t{Escape(type.BaseType)}::f__construct(a_p);\n")}{string.Join(string.Empty, fields.Select(x => $"{'\t'}{'\t'}new(&a_p->{Escape(x)}) decltype({Escape(x)})({Escape(x)});\n"))}{'\t'}}}
";
                            }
                        }
                        else
                        {
                            td.IsManaged |= mm.managed;
                        }
                        if (type.IsValueType) members = $@"{'\t'}struct t_value
{'\t'}{{
{members}{'\t'}}};
{'\t'}t_value v__value;
{'\t'}template<typename T>
{'\t'}void f__construct(T&& a_value)
{'\t'}{{
{'\t'}{'\t'}new(&v__value) decltype(v__value)(std::forward<T>(a_value));
{'\t'}}}
{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{'\t'}{'\t'}v__value.f__scan(a_scan);
{'\t'}}}
";
                    }
                    typeDeclarations.WriteLine($"{declaration};");
                    typeDefinitions.WriteLine($@"
{declaration}{@base}
{{
{members}}};");
                }
                this.staticDefinitions.Write(staticDefinitions);
                this.staticMembers.Write(staticMembers);
                this.threadStaticMembers.Write(threadStaticMembers);
            }
            runtimeDefinitions.Add(definition);
            return definition;
        }
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
        private void ProcessNextMethod(Func<Type, bool, TextWriter> writerForType)
        {
            method = queuedMethods.Dequeue();
            var key = ToKey(method);
            if (!visitedMethods.Add(key) || method.IsAbstract) return;
            var builtin = this.builtin.GetBody(this, method);
            var description = new StringWriter();
            description.Write($@"
// {method.DeclaringType.AssemblyQualifiedName}
// {method}
// {(method.IsPublic ? "public " : string.Empty)}{(method.IsPrivate ? "private " : string.Empty)}{(method.IsStatic ? "static " : string.Empty)}{(method.IsFinal ? "final " : string.Empty)}{(method.IsVirtual ? "virtual " : string.Empty)}{method.MethodImplementationFlags}");
            string attributes(string prefix, ICustomAttributeProvider cap) => string.Join(string.Empty, cap.GetCustomAttributes(false).Select(x => $"\n{prefix}// [{x}]"));
            var parameters = method.GetParameters().Select(x => (
                Prefix: $"{attributes("\t", x)}\n\t// {x}", Type: x.ParameterType
            ));
            if (!method.IsStatic && !(method.IsConstructor && builtin.body != null)) parameters = parameters.Prepend((string.Empty, GetThisType(method)));
            string argument(Type t, int i) => $"\n\t{EscapeForStacked(t)} a_{i}";
            var arguments = parameters.Select((x, i) => $"{x.Prefix}{argument(x.Type, i)}").ToList();
            if (method is MethodInfo) description.Write(attributes(string.Empty, ((MethodInfo)method).ReturnParameter));
            var returns = method is MethodInfo m ? EscapeForStacked(m.ReturnType) : method.IsStatic || builtin.body == null ? "void" : EscapeForStacked(method.DeclaringType);
            var identifier = Escape(method);
            var prototype = $@"{returns}
{identifier}({string.Join(",", arguments)}
)";
            functionDeclarations.WriteLine(description);
            functionDeclarations.Write(prototype);
            functionDeclarations.WriteLine(';');
            if (method.DeclaringType.IsValueType && !method.IsStatic && !method.IsConstructor) functionDeclarations.WriteLine($@"
inline {returns}
{identifier}__v({string.Join(",", arguments.Skip(1).Prepend($"\n\t{Escape(method.DeclaringType)}* a_0"))}
)
{{
{'\t'}{(returns == "void" ? string.Empty : "return ")}{identifier}({
    string.Join(", ", arguments.Skip(1).Select((x, i) => $"a_{i + 1}").Prepend($"&a_0->v__value"))
});
}}");
            if (builtin.body != null)
            {
                writer = writerForType(method.DeclaringType, builtin.inline > 0);
                writer.WriteLine(description);
                if (builtin.inline < 0)
                {
                    writer.Write("IL2CXX__PORTABLE__NOINLINE ");
                }
                else
                {
                    if (builtin.inline > 1) writer.Write("IL2CXX__PORTABLE__ALWAYS_INLINE ");
                    if (builtin.inline > 0) writer.Write("inline ");
                }
                writer.WriteLine($"{prototype}\n{{\n{builtin.body}}}");
                return;
            }
            var body = method.GetMethodBody();
            bytes = body?.GetILAsByteArray();
            if (method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining))
            {
                writer = writerForType(method.DeclaringType, false);
                writer.WriteLine(description);
                writer.Write("IL2CXX__PORTABLE__NOINLINE ");
            }
            else
            {
                var aggressive = method.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveInlining);
                var inline = aggressive || bytes?.Length <= 64;
                writer = writerForType(method.DeclaringType, inline);
                writer.WriteLine(description);
                if (aggressive) writer.Write("IL2CXX__PORTABLE__ALWAYS_INLINE ");
                if (inline) writer.Write("inline ");
            }
            var dllimport = method.GetCustomAttribute<DllImportAttribute>();
            if (dllimport != null)
            {
                functionDeclarations.WriteLine("// DLL import:");
                functionDeclarations.WriteLine($"//\tValue: {dllimport.Value}");
                functionDeclarations.WriteLine($"//\tEntryPoint: {dllimport.EntryPoint}");
                functionDeclarations.WriteLine($"//\tSetLastError: {dllimport.SetLastError}");
                writer.WriteLine(prototype);
                writer.WriteLine('{');
                writer.WriteLine($"\tstatic t_library library(\"{dllimport.Value}\"s, \"{dllimport.EntryPoint ?? method.Name}\");");
                GenerateInvokeUnmanaged(GetReturnType(method), method.GetParameters().Select((x, i) => (x, i)), "library.f_symbol()", writer, dllimport.CharSet);
                writer.WriteLine('}');
                return;
            }
            if (bytes == null)
            {
                functionDeclarations.WriteLine("// TO BE PROVIDED");
                return;
            }
            writer.Write(prototype);
            writer.WriteLine($@"
{{{string.Join(string.Empty, body.ExceptionHandlingClauses.Select(x => $"\n\t// {x}"))}");
            definedIndices = new SortedDictionary<string, (string, int)>();
            indexToStack = new Dictionary<int, Stack>();
            log($"{method.DeclaringType}::[{method}]");
            foreach (var x in body.ExceptionHandlingClauses)
            {
                log($"{x.Flags}");
                log($"\ttry: {x.TryOffset:x04} to {x.TryOffset + x.TryLength:x04}");
                log($"\thandler: {x.HandlerOffset:x04} to {x.HandlerOffset + x.HandlerLength:x04}");
                switch (x.Flags)
                {
                    case ExceptionHandlingClauseOptions.Clause:
                        log($"\tcatch: {x.CatchType}");
                        break;
                    case ExceptionHandlingClauseOptions.Filter:
                        log($"\tfilter: {x.FilterOffset:x04}");
                        break;
                }
            }
            Estimate(0, new Stack(this));
            foreach (var x in body.ExceptionHandlingClauses)
                switch (x.Flags)
                {
                    case ExceptionHandlingClauseOptions.Clause:
                        Estimate(x.HandlerOffset, new Stack(this).Push(x.CatchType));
                        break;
                    case ExceptionHandlingClauseOptions.Filter:
                        Estimate(x.FilterOffset, new Stack(this).Push(typeof(Exception)));
                        break;
                    default:
                        Estimate(x.HandlerOffset, new Stack(this));
                        break;
                }
            log("\n");
            writer.WriteLine($"\t// init locals: {body.InitLocals}");
            foreach (var x in body.LocalVariables)
                writer.WriteLine($"\t{EscapeForStacked(x.LocalType)} l{x.LocalIndex}{(body.InitLocals ? "{}" : string.Empty)};");
            foreach (var x in definedIndices)
                for (var i = 0; i < x.Value.Index; ++i)
                    writer.WriteLine($"\t{x.Key} {x.Value.Prefix}{i};");
            var writers = new Stack<TextWriter>();
            var tryBegins = new Queue<ExceptionHandlingClause>(body.ExceptionHandlingClauses.OrderBy(x => x.TryOffset).ThenByDescending(x => x.HandlerOffset + x.HandlerLength));
            var index = 0;
            while (index < bytes.Length)
            {
                while (tryBegins.Count > 0)
                {
                    var clause = tryBegins.Peek();
                    if (index < clause.TryOffset) break;
                    tryBegins.Dequeue();
                    tries.Push(clause);
                    if (clause.Flags == ExceptionHandlingClauseOptions.Finally)
                    {
                        writer.WriteLine("{auto finally = f__finally([&]\n{");
                        writers.Push(writer);
                        writer = new StringWriter();
                        writer.WriteLine("});");
                    }
                    else
                    {
                        writer.WriteLine("try {");
                    }
                }
                if (tries.Count > 0)
                {
                    var clause = tries.Peek();
                    if (index == (clause.Flags == ExceptionHandlingClauseOptions.Filter ? clause.FilterOffset : clause.HandlerOffset))
                    {
                        var s = indexToStack[index];
                        switch (clause.Flags)
                        {
                            case ExceptionHandlingClauseOptions.Clause:
                                writer.WriteLine($@"// catch {clause.CatchType}
}} catch (t_object* e) {{
{'\t'}if (!(e && e->f_type()->{(clause.CatchType.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(clause.CatchType)}>::v__instance))) throw;
{'\t'}{s.Variable} = e;");
                                break;
                            case ExceptionHandlingClauseOptions.Filter:
                                writer.WriteLine($@"// filter
}} catch (t_object* e) {{
{'\t'}{s.Variable} = e;");
                                break;
                            case ExceptionHandlingClauseOptions.Finally:
                                writers.Push(writer);
                                writer = new StringWriter();
                                break;
                            case ExceptionHandlingClauseOptions.Fault:
                                writer.WriteLine(@"// fault
} catch (...) {");
                                break;
                        }
                    }
                }
                writer.Write($"L_{index:x04}: // ");
                var stack = indexToStack[index];
                var instruction = instructions1[bytes[index++]];
                if (instruction.OpCode == OpCodes.Prefix1) instruction = instructions2[bytes[index++]];
                writer.Write(instruction.OpCode.Name);
                index = instruction.Generate(index, stack);
                if (tries.Count > 0)
                {
                    var clause = tries.Peek();
                    if (index >= clause.HandlerOffset + clause.HandlerLength)
                    {
                        tries.Pop();
                        if (clause.Flags == ExceptionHandlingClauseOptions.Finally)
                        {
                            var f = writer;
                            var t = writers.Pop();
                            writer = writers.Pop();
                            writer.Write(f);
                            writer.Write(t);
                        }
                        writer.WriteLine('}');
                    }
                }
            }
            writer.WriteLine('}');
        }

        private void WriteRuntimeDefinition(RuntimeDefinition definition, string assembly, TextWriter writerForDeclarations, TextWriter writerForDefinitions)
        {
            var type = definition.Type;
            var @base = definition is TypeDefinition && FinalizeOf(type).MethodHandle != finalizeOfObject.MethodHandle ? "t__type_finalizee" : "t__type";
            var identifier = Escape(type);
            writerForDeclarations.WriteLine($@"
template<>
struct t__type_of<{identifier}> : {@base}
{{");
            writerForDefinitions.Write($@"{(definition as TypeDefinition)?.DefaultConstructor}
t__type_of<{identifier}>::t__type_of() : {@base}(&t__type_of<t__type>::v__instance, {(type.BaseType == null ? "nullptr" : $"&t__type_of<{Escape(type.BaseType)}>::v__instance")}, {{");
            if (definition is TypeDefinition td)
            {
                void writeMethods(IEnumerable<MethodInfo> methods, Func<int, MethodInfo, string, string> pointer, Func<int, int, MethodInfo, string, string> genericPointer, Func<MethodInfo, MethodInfo> origin, string indent)
                {
                    foreach (var (m, i) in methods.Select((x, i) => (x, i))) writerForDeclarations.WriteLine($@"{indent}// {m}
{indent}void* v_method{i} = {(
    m.IsAbstract ? "nullptr" :
    m.IsGenericMethod ? $"&v_generic__{Escape(m)}" :
    methodToIdentifier.ContainsKey(ToKey(m)) ? $"reinterpret_cast<void*>({pointer(i, m, $"{Escape(m)}{(m.DeclaringType.IsValueType ? "__v" : string.Empty)}")})" :
    "nullptr"
)};");
                    foreach (var (m, i) in methods.Where(x => !x.IsAbstract && x.IsGenericMethod).Select((x, i) => (x, i))) writerForDeclarations.WriteLine($@"{indent}struct
{indent}{{
{
    string.Join(string.Empty, genericMethodToTypesToIndex[ToKey(origin(m))].OrderBy(x => x.Value).Select(p =>
    {
        var x = m.MakeGenericMethod(p.Key);
        return $@"{indent}{'\t'}// {x}
{indent}{'\t'}void* v_method{p.Value} = reinterpret_cast<void*>({genericPointer(i, p.Value, x, $"{Escape(x)}{(x.DeclaringType.IsValueType ? "__v" : string.Empty)}")});
";
    }))
}{indent}}} v_generic__{Escape(m)};");
                }
                writeMethods(td.Methods, (i, m, name) => name, (i, j, m, name) => name, x => x.GetBaseDefinition(), "\t");
                foreach (var p in td.InterfaceToMethods)
                {
                    string types(MethodInfo m) => string.Join(", ", m.GetParameters().Select(x => x.ParameterType).Prepend(GetReturnType(m)).Select(EscapeForStacked));
                    var ii = Escape(p.Key);
                    var ms = typeToRuntime[p.Key].Methods;
                    writerForDeclarations.WriteLine($@"{'\t'}struct
{'\t'}{{");
                    writeMethods(p.Value, (i, m, name) => name, (i, j, m, name) => name, x => ms[Array.IndexOf(p.Value, x)], "\t\t");
                    writerForDeclarations.WriteLine($@"{'\t'}}} v_interface__{ii}__methods;
{'\t'}struct
{'\t'}{{");
                    writeMethods(p.Value,
                        (i, m, name) => $"f__method<{ii}, {i}, {identifier}, {GetVirtualFunctionPointer(m)}, {name}, {types(m)}>",
                        (i, j, m, name) => $"f__generic_method<{ii}, {i}, {j}, {identifier}, {GetVirtualFunctionPointer(m)}, {name}, {types(m)}>",
                        x => ms[Array.IndexOf(p.Value, x)],
                        "\t\t"
                    );
                    writerForDeclarations.WriteLine($"\t}} v_interface__{ii}__thunks;");
                }
                writerForDefinitions.WriteLine(string.Join(",", td.InterfaceToMethods.Select(p => $"\n\t{{&t__type_of<{Escape(p.Key)}>::v__instance, {{reinterpret_cast<void**>(&v_interface__{Escape(p.Key)}__thunks), reinterpret_cast<void**>(&v_interface__{Escape(p.Key)}__methods)}}}}")));
                writerForDeclarations.WriteLine($@"{'\t'}static void f_do_scan(t_object* a_this, t_scan a_scan);
{'\t'}static t_object* f_do_clone(const t_object* a_this);");
                if (type != typeof(void) && type.IsValueType) writerForDeclarations.WriteLine("\tstatic void f_do_copy(const char* a_from, size_t a_n, char* a_to);");
            }
            else
            {
                td = null;
            }
            writerForDeclarations.WriteLine($@"{'\t'}t__type_of();
{'\t'}static t__type_of v__instance;
}};");
            writerForDefinitions.WriteLine($@"}}, &{assembly}, u""{type.Namespace}""sv, {(
    definition.IsManaged ? "true" : "false"
)}, {(
    type == typeof(void) ? "0" : $"sizeof({EscapeForValue(type)})"
)})
{{");
            if (type.IsArray) writerForDefinitions.WriteLine($@"{'\t'}v__element = &t__type_of<{Escape(GetElementType(type))}>::v__instance;
{'\t'}v__rank = {type.GetArrayRank()};");
            if (td?.DefaultConstructor != null) writerForDefinitions.WriteLine($"\tv__default_constructor = &v__default_constructor_{identifier};");
            writerForDefinitions.Write(td?.Delegate);
            var nv = Nullable.GetUnderlyingType(type);
            if (nv != null) writerForDefinitions.WriteLine($"\tv__nullable_value = &t__type_of<{Escape(nv)}>::v__instance;");
            if (definition is TypeDefinition)
            {
                writerForDefinitions.WriteLine($@"{'\t'}f_scan = f_do_scan;
{'\t'}f_clone = f_do_clone;");
                if (type != typeof(void) && type.IsValueType) writerForDefinitions.WriteLine("\tf_copy = f_do_copy;");
            }
            writerForDefinitions.WriteLine($@"}}
t__type_of<{identifier}> t__type_of<{identifier}>::v__instance;");
            if (definition is TypeDefinition)
            {
                writerForDefinitions.WriteLine($@"void t__type_of<{identifier}>::f_do_scan(t_object* a_this, t_scan a_scan)
{{
{'\t'}static_cast<{identifier}*>(a_this)->f__scan(a_scan);
}}
t_object* t__type_of<{identifier}>::f_do_clone(const t_object* a_this)
{{");
                if (type.IsArray)
                {
                    var element = EscapeForMember(GetElementType(type));
                    writerForDefinitions.WriteLine($@"
{'\t'}auto p = static_cast<const {identifier}*>(a_this);
{'\t'}t__new<{identifier}> q(sizeof({element}) * p->v__length);
{'\t'}q->v__length = p->v__length;
{'\t'}std::memcpy(q->v__bounds, p->v__bounds, sizeof(p->v__bounds));
{'\t'}auto p0 = reinterpret_cast<const {element}*>(p + 1);
{'\t'}auto p1 = q->f__data();
{'\t'}for (size_t i = 0; i < p->v__length; ++i) new(p1 + i) {element}(p0[i]);
{'\t'}return q;
");
                }
                else
                {
                    writerForDefinitions.WriteLine(
                        type == typeof(void) ? $"\treturn t__new<{identifier}>(0);" :
                        type.IsValueType ? $@"{'\t'}t__new<{identifier}> p(0);
{'\t'}new(&p->v__value) decltype({identifier}::v__value)(static_cast<const {identifier}*>(a_this)->v__value);
{'\t'}return p;
}}
void t__type_of<{identifier}>::f_do_copy(const char* a_from, size_t a_n, char* a_to)
{{
{'\t'}f__copy(reinterpret_cast<const decltype({identifier}::v__value)*>(a_from), a_n, reinterpret_cast<decltype({identifier}::v__value)*>(a_to));" :
                    $@"{'\t'}t__new<{identifier}> p(0);
{'\t'}static_cast<const {identifier}*>(a_this)->f__construct(p);
{'\t'}return p;
");
                }
                writerForDefinitions.WriteLine('}');
            }
        }
        public void Do(MethodInfo method, TextWriter writerForDeclarations, TextWriter writerForDefinitions, Func<Type, bool, TextWriter> writerForType, string resources)
        {
            Define(typeof(RuntimeAssembly));
            Define(typeof(RuntimeConstructorInfo));
            Define(typeof(RuntimeMethodInfo));
            Define(typeof(RuntimeType));
            Escape(finalizeOfObject);
            Define(typeof(Thread));
            Enqueue(typeof(ThreadStart).GetMethod("Invoke"));
            Enqueue(typeof(ParameterizedThreadStart).GetMethod("Invoke"));
            Define(typeof(void));
            Define(typeof(string));
            Define(typeof(StringBuilder));
            Define(method.DeclaringType);
            Enqueue(method);
            do
            {
                ProcessNextMethod(writerForType);
                while (queuedTypes.Count > 0) Define(queuedTypes.Dequeue());
            }
            while (queuedMethods.Count > 0);
            processed = true;
            writerForDeclarations.WriteLine(@"#include <il2cxx/base.h>

namespace il2cxx
{
");
            writerForDeclarations.Write(typeDeclarations);
            writerForDeclarations.Write(typeDefinitions);
            writerForDeclarations.WriteLine(@"
extern t__runtime_assembly* const v__entry_assembly;

extern std::map<std::string_view, t__type*> v__name_to_type;");
            writerForDeclarations.Write(functionDeclarations);
            var assemblyIdentifiers = new HashSet<string>();
            var assemblyToIdentifier = new Dictionary<Assembly, string>();
            foreach (var definition in runtimeDefinitions)
            {
                var writer = writerForType(definition.Type, false);
                var assembly = definition.Type.Assembly;
                if (!assemblyToIdentifier.TryGetValue(assembly, out var name))
                {
                    var escaped = name = Escape(assembly.GetName().Name);
                    for (var i = 0; !assemblyIdentifiers.Add(name); ++i) name = $"{escaped}__{i}";
                    assemblyToIdentifier.Add(assembly, name);
                    var entry = assembly.EntryPoint;
                    if (entry == method)
                    {
                        writerForDeclarations.WriteLine($"\nextern t__runtime_method_info v__assembly_{name}__entry_point;");
                        writer.WriteLine($"\nt__runtime_method_info v__assembly_{name}__entry_point{{&t__type_of<t__runtime_method_info>::v__instance, &t__type_of<{Escape(entry.DeclaringType)}>::v__instance}};");
                    }
                    writerForDeclarations.WriteLine($"\nextern t__runtime_assembly v__assembly_{name};");
                    writer.WriteLine($"\nt__runtime_assembly v__assembly_{name}{{&t__type_of<t__runtime_assembly>::v__instance, u\"{assembly.FullName}\"sv, u\"{name}\"sv, {(entry == method ? $"&v__assembly_{name}__entry_point" : "nullptr")}}};");
                    var names = assembly.GetManifestResourceNames();
                    if (names.Length > 0)
                    {
                        var path = Path.Combine(resources, name);
                        Directory.CreateDirectory(path);
                        foreach (var x in names)
                            using (var source = assembly.GetManifestResourceStream(x))
                            using (var destination = File.Create(Path.Combine(path, x)))
                                source.CopyTo(destination);
                    }
                }
                WriteRuntimeDefinition(definition, $"v__assembly_{name}", writerForDeclarations, writer);
            }
            writerForDeclarations.WriteLine("\n#include \"utilities.h\"");
            writerForDeclarations.Write(staticDefinitions);
            writerForDeclarations.WriteLine(@"
struct t_static
{");
            writerForDeclarations.Write(staticMembers);
            writerForDeclarations.WriteLine($@"
{'\t'}static t_static* v_instance;
{'\t'}t_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = this;
{'\t'}}}
{'\t'}~t_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = nullptr;
{'\t'}}}
}};

struct t_thread_static
{{");
            writerForDeclarations.Write(threadStaticMembers);
            writerForDeclarations.WriteLine($@"
{'\t'}static IL2CXX__PORTABLE__THREAD t_thread_static* v_instance;
{'\t'}t_thread_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = this;
{'\t'}}}
{'\t'}~t_thread_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = nullptr;
{'\t'}}}
}};
");
            writerForDeclarations.WriteLine(fieldDeclarations);
            writerForDeclarations.WriteLine('}');
            writerForDefinitions.WriteLine($@"namespace il2cxx
{{

#include ""utilities.cc""

t__runtime_assembly* const v__entry_assembly = &v__assembly_{assemblyToIdentifier[method.DeclaringType.Assembly]};

std::map<std::string_view, t__type*> v__name_to_type{{
{string.Join(",\n", runtimeDefinitions.Select(x => $"\t{{\"{x.Type.AssemblyQualifiedName}\"sv, &t__type_of<{Escape(x.Type)}>::v__instance}}"))}
}};
");
            writerForDefinitions.Write(fieldDefinitions);
            writerForDefinitions.WriteLine($@"
t_static* t_static::v_instance;

IL2CXX__PORTABLE__THREAD t_thread_static* t_thread_static::v_instance;

}}

int main(int argc, char* argv[])
{{
{'\t'}using namespace il2cxx;
{'\t'}std::setlocale(LC_ALL, """");
{'\t'}t_engine::t_options options;
{'\t'}options.v_verbose = std::getenv(""IL2CXX_VERBOSE"");
{'\t'}options.v_verify = std::getenv(""IL2CXX_VERIFY_LEAKS"");
{'\t'}options.v_name_to_type = std::getenv(""IL2CXX_DUMP_LEAKS"") ? &v__name_to_type : nullptr;
{'\t'}t_engine engine(options, argc, argv);
{'\t'}return engine.f_run<{Escape(typeof(Thread))}, t_static, t_thread_static>([](auto a_p)
{'\t'}{{
{'\t'}{'\t'}reinterpret_cast<void(*)(t_object*)>(reinterpret_cast<void**>(a_p->f_type() + 1)[{typeToRuntime[typeof(object)].GetIndex(finalizeOfObject)}])(a_p);
{'\t'}}}, []
{'\t'}{{
{'\t'}{'\t'}{(method.ReturnType == typeof(void) ? $"{Escape(method)}();\n\t\treturn 0" : $"return {Escape(method)}()")};
{'\t'}}});
}}");
        }
    }
}
