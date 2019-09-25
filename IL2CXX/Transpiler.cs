using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace IL2CXX
{
    public interface IBuiltin
    {
        string GetFields(Transpiler transpiler, Type type);
        string GetInitialize(Transpiler transpiler, Type type);
        string GetBody(Transpiler transpiler, MethodBase method);
    }
    public class Transpiler
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        struct MethodKey : IEquatable<MethodKey>
        {
            private readonly RuntimeMethodHandle method;
            private readonly RuntimeTypeHandle type;

            public MethodKey(MethodBase method)
            {
                this.method = method.MethodHandle;
                type = method.DeclaringType.TypeHandle;
            }
            public static bool operator ==(MethodKey x, MethodKey y) => x.method == y.method && x.type.Equals(y.type);
            public static bool operator !=(MethodKey x, MethodKey y) => !(x == y);
            public bool Equals(MethodKey x) => this == x;
            public override bool Equals(object x) => x is MethodKey y && this == y;
            public override int GetHashCode() => method.GetHashCode() ^ type.GetHashCode();
        }
        private static MethodKey ToKey(MethodBase method) => new MethodKey(method);
        abstract class RuntimeDefinition : IEqualityComparer<Type[]>
        {
            bool IEqualityComparer<Type[]>.Equals(Type[] x, Type[] y) => x.SequenceEqual(y);
            int IEqualityComparer<Type[]>.GetHashCode(Type[] x) => x.Select(y => y.GetHashCode()).Aggregate((y, z) => y % z);

            public readonly Type Type;
            public readonly List<MethodInfo> Methods = new List<MethodInfo>();
            public readonly Dictionary<MethodKey, int> MethodToIndex = new Dictionary<MethodKey, int>();

            protected RuntimeDefinition(Type type) => Type = type;
            protected void Add(MethodInfo method, Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex)
            {
                var key = ToKey(method);
                MethodToIndex.Add(key, Methods.Count);
                Methods.Add(method);
                if (method.IsGenericMethod) genericMethodToTypesToIndex.Add(key, new Dictionary<Type[], int>(this));
            }
            protected abstract int GetIndex(MethodKey method);
            public int GetIndex(MethodBase method) => GetIndex(ToKey(method));
        }
        class InterfaceDefinition : RuntimeDefinition
        {
            public InterfaceDefinition(Type type, Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex) : base(type)
            {
                foreach (var x in Type.GetMethods()) Add(x, genericMethodToTypesToIndex);
            }
            protected override int GetIndex(MethodKey method) => MethodToIndex[method];
        }
        class TypeDefinition : RuntimeDefinition
        {
            private static readonly MethodKey finalizeKeyOfObject = new MethodKey(finalizeOfObject);

            public readonly TypeDefinition Base;
            public readonly Dictionary<Type, MethodInfo[]> InterfaceToMethods = new Dictionary<Type, MethodInfo[]>();

            public TypeDefinition(Type type, TypeDefinition @base, Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex, IEnumerable<(Type Type, InterfaceDefinition Definition)> interfaces) : base(type)
            {
                Base = @base;
                if (Base != null) Methods.AddRange(Base.Methods);
                foreach (var x in Type.GetMethods(declaredAndInstance).Where(x => x.IsVirtual))
                {
                    var i = GetIndex(x.GetBaseDefinition());
                    if (i < 0)
                        Add(x, genericMethodToTypesToIndex);
                    else
                        Methods[i] = x;
                }
                foreach (var (key, definition) in interfaces)
                {
                    var methods = new MethodInfo[definition.Methods.Count];
                    var map = (Type.IsArray && key.IsGenericType ? typeof(SZArrayHelper<>).MakeGenericType(GetElementType(Type)) : Type).GetInterfaceMap(key);
                    foreach (var (i, t) in map.InterfaceMethods.Zip(map.TargetMethods, (i, t) => (i, t))) methods[definition.GetIndex(i)] = t;
                    InterfaceToMethods.Add(key, methods);
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
                        prefix = "l";
                    }
                    else
                    {
                        VariableType = "int32_t";
                        prefix = "i";
                    }
                }
                else if (Type.IsValueType)
                {
                    VariableType = transpiler.EscapeForScoped(Type);
                    prefix = $"v{transpiler.Escape(Type)}__";
                }
                else
                {
                    VariableType = "t_scoped<t_slot>";
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
        private static readonly Type pointerType = typeof(void).MakePointerType();
        private static readonly Type typedReferenceByRefType = typeof(TypedReferenceTag).MakeByRefType();
        private static readonly Type typedReferencePointerType = typeof(TypedReferenceTag).MakePointerType();
        private static readonly IReadOnlyDictionary<(string, string), Type> typeOfAdd = new Dictionary<(string, string), Type> {
            [("int32_t", "int32_t")] = typeof(int),
            [("int32_t", "intptr_t")] = typeof(NativeInt),
            [("int32_t", "void*")] = pointerType,
            [("int64_t", "int64_t")] = typeof(long),
            [("intptr_t", "int32_t")] = typeof(NativeInt),
            [("intptr_t", "intptr_t")] = typeof(NativeInt),
            [("intptr_t", "void*")] = pointerType,
            [("double", "double")] = typeof(double),
            [("void*", "int32_t")] = pointerType,
            [("void*", "intptr_t")] = pointerType,
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
        private readonly Instruction[] instructions1 = new Instruction[256];
        private readonly Instruction[] instructions2 = new Instruction[256];
        private readonly StringWriter typeDeclarations = new StringWriter();
        private readonly StringWriter typeDefinitions = new StringWriter();
        private readonly StringWriter staticDeclarations = new StringWriter();
        private readonly StringWriter threadStaticDeclarations = new StringWriter();
        private readonly StringWriter memberDefinitions = new StringWriter();
        private readonly StringWriter functionDeclarations = new StringWriter();
        private readonly StringWriter functionDefinitions = new StringWriter();
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
        private StringWriter writer;
        private readonly Stack<ExceptionHandlingClause> tries = new Stack<ExceptionHandlingClause>();
        private readonly Stack<StringWriter> writers = new Stack<StringWriter>();
        private Type constrained;
        private bool @volatile;

        private static Type MakeByRefType(Type type) => type == typeof(TypedReference) ? typedReferenceByRefType : type.MakeByRefType();
        private static Type MakePointerType(Type type) => type == typeof(TypedReference) ? typedReferencePointerType : type.MakePointerType();
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
        private Type GetThisType(MethodBase method)
        {
            var type = method.DeclaringType;
            return type.IsValueType ? MakePointerType(type) : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SZArrayHelper<>) ? type.GetGenericArguments()[0].MakeArrayType() : type;
        }
        private Type GetArgumentType(int index)
        {
            var parameters = method.GetParameters();
            return method.IsStatic ? parameters[index].ParameterType : index > 0 ? parameters[index - 1].ParameterType : GetThisType(method);
        }
        private bool ReturnsValue(MethodBase method) => method is MethodInfo x && x.ReturnType != typeof(void);
        private Stack EstimateCall(MethodBase method, Stack stack)
        {
            stack = stack.ElementAt(method.GetParameters().Length + (method.IsStatic ? 0 : 1));
            return method is MethodInfo x && x.ReturnType != typeof(void) ? stack.Push(x.ReturnType) : stack;
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
        private bool HasSlots(Type x) => !(x.IsByRef || x.IsPointer || primitives.ContainsKey(x) || x.IsEnum);
        private RuntimeDefinition Define(Type type)
        {
            if (typeToRuntime.TryGetValue(type, out var definition)) return definition;
            if (type.IsInterface)
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
                typeToRuntime.Add(type, null);
                if (type.TypeInitializer != null) Enqueue(type.TypeInitializer);
                var td = new TypeDefinition(type, type.BaseType == null ? null : (TypeDefinition)Define(type.BaseType), genericMethodToTypesToIndex, type.GetInterfaces().Select(x => (x, (InterfaceDefinition)Define(x))));
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
                definition = td;
                typeToRuntime[type] = definition;
                var identifier = Escape(type);
                var declaration = $@"// {type.AssemblyQualifiedName}
struct {identifier}";
                var @base = type == typeof(object) ? " : t_object" : type.BaseType == null ? string.Empty : $" : {Escape(type.BaseType)}";
                var staticFields = new List<FieldInfo>();
                var threadStaticFields = new List<FieldInfo>();
                if (!type.IsEnum)
                    foreach (var x in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        (Attribute.IsDefined(x, typeof(ThreadStaticAttribute)) ? threadStaticFields : staticFields).Add(x);
                var staticMembers = new StringWriter();
                var staticDefinitions = new StringWriter();
                if (staticFields.Count > 0)
                {
                    staticMembers.WriteLine("\tstruct\n\t{");
                    foreach (var x in staticFields)
                    {
                        staticMembers.WriteLine($"\t\t{EscapeForScoped(x.FieldType)} {Escape(x)}{{}};");
                        var content = string.Empty;
                        if (x.FieldType.Name.StartsWith("__StaticArrayInitTypeSize="))
                        {
                            var bytes = new byte[Marshal.SizeOf(x.FieldType)];
                            RuntimeHelpers.InitializeArray(bytes, x.FieldHandle);
                            content = $"\tuint8_t v__content[{bytes.Length}] = {{{string.Join(", ", bytes.Select(y => $"0x{y:x02}"))}}};\n";
                        }
                        var field = $"t__field_{Escape(x.DeclaringType)}__{Escape(x.Name)}";
                        staticDefinitions.WriteLine($@"
struct {field}
{{
{content}{'\t'}static {field} v__instance;
}};
{field} {field}::v__instance;");
                    }
                    staticMembers.WriteLine($"\t}} v_{identifier};");
                }
                var threadStaticMembers = new StringWriter();
                if (threadStaticFields.Count > 0)
                {
                    threadStaticMembers.WriteLine("\tstruct\n\t{");
                    foreach (var x in threadStaticFields) threadStaticMembers.WriteLine($"\t\t{EscapeForScoped(x.FieldType)} {Escape(x)}{{}};");
                    threadStaticMembers.WriteLine($"\t}} v_{identifier};");
                }
                string members;
                if (primitives.TryGetValue(type, out var name) || type.IsEnum)
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
                    members = builtin.GetFields(this, type);
                    if (members == null)
                    {
                        string scan(Type x, string y) => x.IsValueType ? $"{y}.f__scan(a_scan)" : $"a_scan({y})";
                        if (type.IsArray)
                        {
                            var element = GetElementType(type);
                            var elementIdentifier = EscapeForVariable(element);
                            members = $@"{'\t'}t__bound v__bounds[{type.GetArrayRank()}];
{'\t'}{elementIdentifier}* f__data()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<{elementIdentifier}*>(this + 1);
{'\t'}}}
";
                            if (HasSlots(element)) members += $@"{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{'\t'}{'\t'}{Escape(type.BaseType)}::f__scan(a_scan);
{'\t'}{'\t'}auto p = f__data();
{'\t'}{'\t'}for (size_t i = 0; i < v__length; ++i) {scan(element, "p[i]")};
{'\t'}}}
";
                            members += $@"{'\t'}t_scoped<t_slot> f__clone() const
{'\t'}{{
{'\t'}{'\t'}auto p = t_object::f_allocate<{identifier}>(sizeof({identifier}) * v__length);
{'\t'}{'\t'}p->v__length = v__length;
{'\t'}{'\t'}std::copy_n(v__bounds, {type.GetArrayRank()}, p->v__bounds);
{'\t'}{'\t'}auto p0 = reinterpret_cast<const {elementIdentifier}*>(this + 1);
{'\t'}{'\t'}auto p1 = p->f__data();
{'\t'}{'\t'}for (size_t i = 0; i < v__length; ++i) new(p1 + i) {elementIdentifier}(p0[i]);
{'\t'}{'\t'}return p;
{'\t'}}}
";
                        }
                        else
                        {
                            var fields = type.GetFields(declaredAndInstance);
                            string variables(string indent) => string.Join(string.Empty, fields.Select(x => $"{indent}{EscapeForVariable(x.FieldType)} {Escape(x)};\n"));
                            string scanSlots(string indent) => string.Join(string.Empty, fields.Where(x => HasSlots(x.FieldType)).Select(x => $"{indent}{scan(x.FieldType, Escape(x))};\n"));
                            members = type.IsValueType
                                ? $@"{variables("\t\t")}
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{string.Join(string.Empty, fields.Where(x => HasSlots(x.FieldType)).Select(x => $"\t\t\t{Escape(x)}.f__destruct();\n"))}{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{scanSlots("\t\t\t")}{'\t'}{'\t'}}}
"
                                : $@"{variables("\t")}
{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{(type.BaseType == null ? string.Empty : $"\t\t{Escape(type.BaseType)}::f__scan(a_scan);\n")}{scanSlots("\t\t")}{'\t'}}}
{'\t'}void f__construct({identifier}* a_p) const
{'\t'}{{
{(type.BaseType == null ? string.Empty : $"\t\t{Escape(type.BaseType)}::f__construct(a_p);\n")}{string.Join(string.Empty, fields.Select(x => $"{'\t'}{'\t'}new(&a_p->{Escape(x)}) decltype({Escape(x)})({Escape(x)});\n"))}{'\t'}}}
{'\t'}t_scoped<t_slot> f__clone() const
{'\t'}{{
{'\t'}{'\t'}auto p = t_object::f_allocate<{identifier}>();
{'\t'}{'\t'}f__construct(p);
{'\t'}{'\t'}return p;
{'\t'}}}
";
                        }
                    }
                    if (type.IsValueType) members = $@"{'\t'}struct t_value
{'\t'}{{
{members}{'\t'}}} v__value;
{'\t'}void f__construct(t_value&& a_value)
{'\t'}{{
{'\t'}{'\t'}new(&v__value) t_value(std::move(a_value));
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
                staticDeclarations.Write(staticMembers);
                memberDefinitions.Write(staticDefinitions);
                threadStaticDeclarations.Write(threadStaticMembers);
            }
            runtimeDefinitions.Add(definition);
            return definition;
        }
        private string EscapeType(Type type)
        {
            if (typeToIdentifier.TryGetValue(type, out var name)) return name;
            var escaped = name = $"t_{Escape(type.ToString())}";
            for (var i = 0; !typeIdentifiers.Add(name); ++i) name = $"{escaped}__{i}";
            typeToIdentifier.Add(type, name);
            return name;
        }
        public string Escape(Type type)
        {
            if (type.IsByRef || type.IsPointer) return $"{Escape(GetElementType(type))}*";
            if (type.IsValueType)
            {
                Define(type);
            }
            else
            {
                if (type.IsArray) Escape(GetElementType(type));
                queuedTypes.Enqueue(type);
            }
            return EscapeType(type);
        }
        public string EscapeForVariable(Type type) =>
            type.IsByRef || type.IsPointer ? $"{EscapeForVariable(GetElementType(type))}*" :
            type.IsInterface ? EscapeForVariable(typeof(object)) :
            primitives.TryGetValue(type, out var x) ? x :
            type.IsEnum ? primitives[type.GetEnumUnderlyingType()] :
            type.IsValueType ? $"{Escape(type)}::t_value" :
            $"t_slot_of<{Escape(type)}>";
        public string EscapeForScoped(Type type) =>
            type.IsByRef || type.IsPointer ? $"{EscapeForVariable(GetElementType(type))}*" :
            type.IsInterface ? EscapeForScoped(typeof(object)) :
            primitives.TryGetValue(type, out var x) ? x :
            type.IsEnum ? primitives[type.GetEnumUnderlyingType()] :
            type.IsValueType ? $"t_scoped<{Escape(type)}::t_value>" :
            $"t_scoped<t_slot_of<{Escape(type)}>>";
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
        public string FormatMove(Type type, string variable) =>
            type == typeof(bool) ? $"{variable} != 0" :
            type == typeof(IntPtr) || type == typeof(UIntPtr) ? $"{EscapeForVariable(type)}{{{variable}}}" :
            type.IsByRef || type.IsPointer ? $"reinterpret_cast<{EscapeForVariable(type)}>({variable})" :
            type.IsPrimitive || type.IsEnum ? variable :
            $"std::move({variable})";
        private void GenerateCall(MethodBase method, string function, IEnumerable<string> variables, Stack after)
        {
            var arguments = new List<Type>();
            if (!method.IsStatic) arguments.Add(GetThisType(method));
            arguments.AddRange(method.GetParameters().Select(x => x.ParameterType));
            var call = $@"{function}({
    string.Join(",", arguments.Zip(variables.Reverse(), (a, v) => $"\n\t\t{FormatMove(a, v)}"))
}{(arguments.Count > 0 ? "\n\t" : string.Empty)})";
            writer.WriteLine(method is MethodInfo m && m.ReturnType != typeof(void) ? $"\t{after.Variable} = {call};" : $"\t{call};");
        }
        private void GenerateCall(MethodBase method, string function, Stack stack, Stack after) => GenerateCall(method, function, stack.Take(method.GetParameters().Length + (method.IsStatic ? 0 : 1)).Select(x => x.Variable), after);
        private string FunctionPointer(MethodBase method)
        {
            var parameters = method.GetParameters().Select(x => x.ParameterType);
            if (!method.IsStatic) parameters = parameters.Prepend(method.DeclaringType.IsValueType ? typeof(object) : GetThisType(method));
            return $"{(method is MethodInfo m && m.ReturnType != typeof(void) ? EscapeForScoped(m.ReturnType) : "void")}(*)({string.Join(", ", parameters.Select(EscapeForScoped))})";
        }
        private static MethodBase GetGenericTypeMethod(MethodBase method) => MethodBase.GetMethodFromHandle(method.MethodHandle, method.DeclaringType.GetGenericTypeDefinition().TypeHandle);
        private void Do()
        {
            method = queuedMethods.Dequeue();
            var key = ToKey(method);
            if (!visitedMethods.Add(key) || method.IsAbstract) return;
            var builtin = this.builtin.GetBody(this, method);
            var returns = method is MethodInfo m ? m.ReturnType == typeof(void) ? "void" : EscapeForScoped(m.ReturnType) : method.IsStatic || builtin == null ? "void" : EscapeForScoped(method.DeclaringType);
            var identifier = Escape(method);
            var parameters = method.GetParameters().Select(x => x.ParameterType);
            if (!method.IsStatic && !(method.IsConstructor && builtin != null)) parameters = parameters.Prepend(GetThisType(method));
            string argument(Type t, int i) => $"\n\t{EscapeForScoped(t)} a_{i}";
            var arguments = parameters.Select(argument).ToList();
            var prototype = $@"
// {method.DeclaringType.AssemblyQualifiedName}
// {method}
// {(method.IsPublic ? "public " : string.Empty)}{(method.IsPrivate ? "private " : string.Empty)}{(method.IsStatic ? "static " : string.Empty)}{(method.IsFinal ? "final " : string.Empty)}{(method.IsVirtual ? "virtual " : string.Empty)}{method.MethodImplementationFlags}
{returns}
{identifier}({string.Join(",", arguments)}{(arguments.Any() ? "\n" : string.Empty)})";
            functionDeclarations.Write(prototype);
            functionDeclarations.WriteLine(';');
            if ((method.DeclaringType.IsValueType) && !method.IsStatic && !method.IsConstructor) functionDeclarations.WriteLine($@"
{returns}
{identifier}__v({string.Join(",", arguments.Skip(1).Prepend(argument(typeof(object), 0)))}
)
{{
{'\t'}{(returns == "void" ? string.Empty : "return ")}{identifier}({
    string.Join(", ", arguments.Skip(1).Select((x, i) => $"std::move(a_{i + 1})").Prepend($"&static_cast<{Escape(method.DeclaringType)}*>(a_0)->v__value"))
});
}}");
            if (builtin != null)
            {
                writer.WriteLine($"{prototype}\n{{\n{builtin}}}");
                return;
            }
            var body = method.GetMethodBody();
            bytes = body?.GetILAsByteArray();
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
            foreach (var x in body.LocalVariables)
                writer.WriteLine($"\t{EscapeForScoped(x.LocalType)} l{x.LocalIndex}{(body.InitLocals ? "{}" : string.Empty)};");
            foreach (var x in definedIndices)
                for (var i = 0; i < x.Value.Index; ++i)
                    writer.WriteLine($"\t{x.Key} {x.Value.Prefix}{i};");
            writer.WriteLine("\tf_epoch_point();");
            var tryBegins = new Queue<ExceptionHandlingClause>(body.ExceptionHandlingClauses.OrderBy(x => x.TryOffset).ThenByDescending(x => x.HandlerOffset + x.HandlerLength));
            var index = 0;
            while (index < bytes.Length)
            {
                var stack = indexToStack[index];
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
                        switch (clause.Flags)
                        {
                            case ExceptionHandlingClauseOptions.Clause:
                                writer.WriteLine($@"// catch {clause.CatchType}
}} catch ({EscapeForScoped(clause.CatchType)} e) {{");
                                break;
                            case ExceptionHandlingClauseOptions.Filter:
                                writer.WriteLine($@"// filter
}} catch ({EscapeForScoped(typeof(Exception))} e) {{");
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
                writer.Write($"L_{index:x04}: // ");
                var instruction = instructions1[bytes[index++]];
                if (instruction.OpCode == OpCodes.Prefix1) instruction = instructions2[bytes[index++]];
                writer.Write(instruction.OpCode.Name);
                index = instruction.Generate(index, stack);
            }
            writer.WriteLine('}');
        }

        public Transpiler(IBuiltin builtin, Action<string> log)
        {
            this.builtin = builtin;
            this.log = log;
            for (int i = 0; i < 256; ++i)
            {
                instructions1[i] = new Instruction { OpCode = opcodes1[i] };
                instructions2[i] = new Instruction { OpCode = opcodes2[i] };
            }
            instructions1[OpCodes.Nop.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) => {
                    writer.WriteLine();
                    return index;
                };
            });
            new[] {
                OpCodes.Ldarg_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldarg_3
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(GetArgumentType(i)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = a_{i};");
                    return index;
                };
            }));
            new[] {
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldloc_2,
                OpCodes.Ldloc_3
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(method.GetMethodBody().LocalVariables[i].LocalType));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = l{i};");
                    return index;
                };
            }));
            new[] {
                OpCodes.Stloc_0,
                OpCodes.Stloc_1,
                OpCodes.Stloc_2,
                OpCodes.Stloc_3
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\tl{i} = {FormatMove(method.GetMethodBody().LocalVariables[i].LocalType, stack.Variable)};");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldarg_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    return (index, stack.Push(GetArgumentType(i)));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = a_{i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldarga_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    return (index, stack.Push(MakePointerType(GetArgumentType(i))));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = &a_{i};");
                    return index;
                };
            });
            instructions1[OpCodes.Starg_S.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 1, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\ta_{i} = {FormatMove(GetArgumentType(i), stack.Variable)};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldloc_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    return (index, stack.Push(method.GetMethodBody().LocalVariables[i].LocalType));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = l{i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldloca_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    var type = method.GetMethodBody().LocalVariables[i].LocalType;
                    return (index, stack.Push(MakePointerType(type)));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = &l{i};");
                    return index;
                };
            });
            instructions1[OpCodes.Stloc_S.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 1, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\tl{i} = {FormatMove(method.GetMethodBody().LocalVariables[i].LocalType, stack.Variable)};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldnull.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(typeof(object)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = nullptr;");
                    return index;
                };
            });
            new[] {
                OpCodes.Ldc_I4_M1,
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_3,
                OpCodes.Ldc_I4_4,
                OpCodes.Ldc_I4_5,
                OpCodes.Ldc_I4_6,
                OpCodes.Ldc_I4_7,
                OpCodes.Ldc_I4_8
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {i - 1};");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldc_I4_S.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 1, stack.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_I4.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI4(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_I8.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 8, stack.Push(typeof(long)));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI8(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {(i > long.MinValue ? $"{i}" : $"{i + 1} - 1")};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_R4.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(float)));
                x.Generate = (index, stack) =>
                {
                    var r = ParseR4(ref index);
                    writer.WriteLine($" {r:G9}\n\t{indexToStack[index].Variable} = ");
                    if (r == float.PositiveInfinity)
                        writer.WriteLine("std::numeric_limits<float>::infinity();");
                    else if (r == float.NegativeInfinity)
                        writer.WriteLine("-std::numeric_limits<float>::infinity();");
                    else if (r == float.NaN)
                        writer.WriteLine("std::numeric_limits<float>::quiet_NaN();");
                    else
                        writer.WriteLine($"{r:G9};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_R8.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 8, stack.Push(typeof(double)));
                x.Generate = (index, stack) =>
                {
                    var r = ParseR8(ref index);
                    writer.WriteLine($" {r:G17}\n\t{indexToStack[index].Variable} = ");
                    if (r == double.PositiveInfinity)
                        writer.WriteLine("std::numeric_limits<double>::infinity();");
                    else if (r == double.NegativeInfinity)
                        writer.WriteLine("-std::numeric_limits<double>::infinity();");
                    else if (r == double.NaN)
                        writer.WriteLine("std::numeric_limits<double>::quiet_NaN();");
                    else
                        writer.WriteLine($"{r:G17};");
                    return index;
                };
            });
            instructions1[OpCodes.Dup.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(stack.Type));
                x.Generate = (index, stack) =>
                {
                    stack = indexToStack[index];
                    writer.WriteLine($"\n\t{stack.Variable} = {stack.Pop.Variable};");
                    return index;
                };
            });
            instructions1[OpCodes.Pop.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    if (HasSlots(stack.Type)) writer.WriteLine($"\t{stack.Variable}.f__destruct();");
                    return index;
                };
            });
            instructions1[OpCodes.Call.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    return (index, EstimateCall(m, stack));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    writer.WriteLine($" {m.DeclaringType}::[{m}]");
                    GenerateCall(m, Escape(m), stack, indexToStack[index]);
                    Enqueue(m);
                    return index;
                };
            });
            instructions1[OpCodes.Ret.Value].For(x =>
            {
                x.Estimate = (index, stack) => (int.MaxValue, ReturnsValue(method) ? stack.Pop : stack);
                x.Generate = (index, stack) =>
                {
                    writer.Write("\n\treturn");
                    if (method is MethodInfo m && m.ReturnType != typeof(void))
                    {
                        writer.Write($" {FormatMove(m.ReturnType, stack.Variable)}");
                        stack = stack.Pop;
                    }
                    writer.WriteLine(";");
                    if (stack.Pop != null) throw new Exception();
                    return index;
                };
            });
            string unsigned(Stack stack) => $"static_cast<u{stack.VariableType}>({stack.Variable})";
            string condition_Un(Stack stack, string integer, string @float)
            {
                if (stack.VariableType == "double") return string.Format(@float, stack.Pop.Variable, stack.Variable);
                if (stack.VariableType == "t_scoped<t_slot>") return $"{stack.Pop.Variable} {integer} {stack.Variable}";
                return $"{unsigned(stack.Pop)} {integer} {unsigned(stack)}";
            }
            string @goto(int index, int target) => target < index ? $@"{{
{'\t'}{'\t'}f_epoch_point();
{'\t'}{'\t'}goto L_{target:x04};
{'\t'}}}" : $"goto L_{target:x04};";
            new[] {
                (OpCode: OpCodes.Br_S, Target: (ParseBranchTarget)ParseBranchTargetI1),
                (OpCode: OpCodes.Br, Target: (ParseBranchTarget)ParseBranchTargetI4)
            }.ForEach(baseSet =>
            {
                instructions1[baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack);
                        return (int.MaxValue, stack);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        writer.WriteLine($" {target:x04}");
                        if (target < index) writer.WriteLine("\tf_epoch_point();");
                        writer.WriteLine($"\tgoto L_{target:x04};");
                        return index;
                    };
                });
                new[] {
                    (OpCode: OpCodes.Brfalse_S, Operator: "!"),
                    (OpCode: OpCodes.Brtrue_S, Operator: string.Empty)
                }.ForEach(set => instructions1[set.OpCode.Value - OpCodes.Br_S.Value + baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack.Pop);
                        return (index, stack.Pop);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        writer.WriteLine($" {target:x04}\n\tif ({set.Operator}{stack.Variable}) {@goto(index, target)}");
                        return index;
                    };
                }));
                new[] {
                    (OpCode: OpCodes.Beq_S, Operator: "=="),
                    (OpCode: OpCodes.Bge_S, Operator: ">="),
                    (OpCode: OpCodes.Bgt_S, Operator: ">"),
                    (OpCode: OpCodes.Ble_S, Operator: "<="),
                    (OpCode: OpCodes.Blt_S, Operator: "<")
                }.ForEach(set => instructions1[set.OpCode.Value - OpCodes.Br_S.Value + baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack.Pop.Pop);
                        return (index, stack.Pop.Pop);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        bool isPointer(Stack s) => s.Type.IsByRef || s.Type.IsPointer;
                        var format = isPointer(stack.Pop) || isPointer(stack) ? "reinterpret_cast<char*>({0})" : "{0}";
                        writer.WriteLine($" {target:x04}\n\tif ({string.Format(format, stack.Pop.Variable)} {set.Operator} {string.Format(format, stack.Variable)}) {@goto(index, target)}");
                        return index;
                    };
                }));
                new[] {
                    (OpCode: OpCodes.Bne_Un_S, Integer: "!=", Float: "std::isunordered({0}, {1}) || {0} != {1}"),
                    (OpCode: OpCodes.Bge_Un_S, Integer: ">=", Float: "std::isgreaterequal({0}, {1})"),
                    (OpCode: OpCodes.Bgt_Un_S, Integer: ">", Float: "std::isgreater({0}, {1})"),
                    (OpCode: OpCodes.Ble_Un_S, Integer: "<=", Float: "std::islessequal({0}, {1})"),
                    (OpCode: OpCodes.Blt_Un_S, Integer: "<", Float: "std::isless({0}, {1})")
                }.ForEach(set => instructions1[set.OpCode.Value - OpCodes.Br_S.Value + baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack.Pop.Pop);
                        return (index, stack.Pop.Pop);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        writer.WriteLine($" {target:x04}\n\tif ({condition_Un(stack, set.Integer, set.Float)}) {@goto(index, target)}");
                        return index;
                    };
                }));
            });
            instructions1[OpCodes.Switch.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var n = ParseI4(ref index);
                    var next = index + n * 4;
                    for (; n > 0; --n) Estimate(next + ParseI4(ref index), stack.Pop);
                    return (index, stack.Pop);
                };
                x.Generate = (index, stack) =>
                {
                    var n = ParseI4(ref index);
                    var next = index + n * 4;
                    writer.WriteLine($" {n}\n\tswitch({stack.Variable}) {{");
                    for (var i = 0; i < n; ++i) writer.WriteLine($@"{'\t'}case {i}:
{'\t'}{'\t'}goto L_{next + ParseI4(ref index):x04};");
                    writer.WriteLine("\t}");
                    return index;
                };
            });
            void withVolatile(Action action)
            {
                if (@volatile) writer.WriteLine("\tstd::atomic_thread_fence(std::memory_order_consume);");
                action();
                if (@volatile) writer.WriteLine("\tstd::atomic_thread_fence(std::memory_order_consume);");
                @volatile = false;
            }
            new[] {
                (OpCode: OpCodes.Ldind_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Ldind_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Ldind_I2, Type: typeof(short)),
                (OpCode: OpCodes.Ldind_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Ldind_I4, Type: typeof(int)),
                (OpCode: OpCodes.Ldind_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Ldind_I8, Type: typeof(long)),
                (OpCode: OpCodes.Ldind_I, Type: typeof(NativeInt)),
                (OpCode: OpCodes.Ldind_R4, Type: typeof(float)),
                (OpCode: OpCodes.Ldind_R8, Type: typeof(double))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t{indexToStack[index].Variable} = *static_cast<{primitives[set.Type]}*>({stack.Variable});"));
                    return index;
                };
            }));
            instructions1[OpCodes.Ldind_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(GetElementType(stack.Type)));
                x.Generate = (index, stack) =>
                {
                    var after = indexToStack[index];
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t{after.Variable} = *static_cast<{EscapeForVariable(after.Type)}*>({stack.Variable});"));
                    return index;
                };
            });
            instructions1[OpCodes.Stind_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t*reinterpret_cast<{EscapeForVariable(typeof(object))}*>({stack.Pop.Variable}) = std::move({stack.Variable});"));
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Stind_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Stind_I2, Type: typeof(short)),
                (OpCode: OpCodes.Stind_I4, Type: typeof(int)),
                (OpCode: OpCodes.Stind_I8, Type: typeof(long)),
                (OpCode: OpCodes.Stind_R4, Type: typeof(float)),
                (OpCode: OpCodes.Stind_R8, Type: typeof(double)),
                (OpCode: OpCodes.Stind_I, Type: typeof(NativeInt))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t*reinterpret_cast<{primitives[set.Type]}*>({stack.Pop.Variable}) = {stack.Variable};"));
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Add, Operator: "+", Type: typeOfAdd),
                (OpCode: OpCodes.Sub, Operator: "-", Type: typeOfAdd),
                (OpCode: OpCodes.Mul, Operator: "*", Type: typeOfAdd),
                (OpCode: OpCodes.Div, Operator: "/", Type: typeOfAdd),
                (OpCode: OpCodes.Rem, Operator: "%", Type: typeOfAdd),
                (OpCode: OpCodes.And, Operator: "&", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Or, Operator: "|", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Xor, Operator: "^", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Shl, Operator: "<<", Type: typeOfShl),
                (OpCode: OpCodes.Shr, Operator: ">>", Type: typeOfShl)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(set.Type[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    string operand(Stack s) => s.Type.IsByRef || s.Type.IsPointer ? $"static_cast<char*>({s.Variable})" : s.Variable;
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {operand(stack.Pop)} {set.Operator} {operand(stack)};");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Div_Un, Operator: "/", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Rem_Un, Operator: "%", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Shr_Un, Operator: ">>", Type: typeOfShl)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(set.Type[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {unsigned(stack.Pop)} {set.Operator} {unsigned(stack)};");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Neg, Operator: "-"),
                (OpCode: OpCodes.Not, Operator: "~")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{stack.Variable} = {set.Operator}{stack.Variable};");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Conv_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Conv_I2, Type: typeof(short)),
                (OpCode: OpCodes.Conv_I4, Type: typeof(int)),
                (OpCode: OpCodes.Conv_I8, Type: typeof(long)),
                (OpCode: OpCodes.Conv_R4, Type: typeof(float)),
                (OpCode: OpCodes.Conv_R8, Type: typeof(double)),
                (OpCode: OpCodes.Conv_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Conv_U8, Type: typeof(ulong)),
                (OpCode: OpCodes.Conv_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Conv_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Conv_I, Type: typeof(NativeInt)),
                (OpCode: OpCodes.Conv_U, Type: typeof(NativeInt)),
                (OpCode: OpCodes.Conv_R_Un, Type: typeof(double))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    var after = indexToStack[index];
                    writer.Write($"\n\t{after.Variable} = ");
                    var type = stack.Type;
                    if (type.IsByRef || type.IsPointer)
                        writer.WriteLine($"reinterpret_cast<{after.VariableType}>({stack.Variable});");
                    else if (type.IsValueType)
                        writer.WriteLine($"static_cast<{after.VariableType}>({stack.Variable});");
                    else
                        writer.WriteLine($"reinterpret_cast<{after.VariableType}>(static_cast<t_object*>({stack.Variable}));");
                    return index;
                };
            }));
            instructions1[OpCodes.Callvirt.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    Define(m.DeclaringType);
                    return (index, EstimateCall(m, stack));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    var after = indexToStack[index];
                    var definition = typeToRuntime[m.DeclaringType];
                    void generate(string target)
                    {
                        void forVirtual(Func<int, string> method, Func<int, int, string> genericMethod, IEnumerable<IReadOnlyList<MethodInfo>> concretes)
                        {
                            var variables = stack.Take(m.GetParameters().Length).Select(y => y.Variable).Append(target);
                            if (m.IsGenericMethod)
                            {
                                var gm = ((MethodInfo)m).GetGenericMethodDefinition();
                                var i = definition.GetIndex(gm);
                                var t2i = genericMethodToTypesToIndex[ToKey(gm)];
                                var ga = m.GetGenericArguments();
                                if (!t2i.TryGetValue(ga, out var j))
                                {
                                    j = t2i.Count;
                                    t2i.Add(ga, j);
                                }
                                GenerateCall(m, $"reinterpret_cast<{FunctionPointer(m)}>({genericMethod(i, j)})", variables, after);
                                foreach (var ms in concretes) Enqueue(ms[i].MakeGenericMethod(ga));
                            }
                            else
                            {
                                var i = definition.GetIndex(m);
                                GenerateCall(m, $"reinterpret_cast<{FunctionPointer(m)}>({method(i)})", variables, after);
                                Escape(m);
                                foreach (var ms in concretes) Enqueue(ms[i]);
                            }
                        }
                        Enqueue(m);
                        if (m.DeclaringType.IsInterface)
                        {
                            var resolve = $"reinterpret_cast<void*(*)(void*&, t__type*)>(site)(site, {target}->f_type())";
                            forVirtual(i =>
                            {
                                writer.WriteLine($"\t{{static auto site = reinterpret_cast<void*>(f__resolve<{Escape(m.DeclaringType)}, {i}>);");
                                return resolve;
                            }, (i, j) =>
                            {
                                writer.WriteLine($"\t{{static auto site = reinterpret_cast<void*>(f__generic_resolve<{Escape(m.DeclaringType)}, {i}, {j}>);");
                                return resolve;
                            }, runtimeDefinitions.OfType<TypeDefinition>().Select(y => y.InterfaceToMethods.TryGetValue(m.DeclaringType, out var ms) ? ms : null).Where(y => y != null));
                            writer.WriteLine($"\t}}");
                        }
                        else if (m.IsVirtual)
                        {
                            string method(int i) => $"reinterpret_cast<void**>({target}->f_type() + 1)[{i}]";
                            forVirtual(method, (i, j) => $"reinterpret_cast<void**>({method(i)})[{j}]", runtimeDefinitions.Where(y => !y.Type.IsInterface && y.Type.IsSubclassOf(m.DeclaringType)).Select(y => y.Methods));
                        }
                        else
                        {
                            GenerateCall(m, Escape(m), stack, after);
                        }
                    }
                    writer.WriteLine($" {m.DeclaringType}::[{m}]");
                    if (constrained == null)
                    {
                        generate(stack.ElementAt(m.GetParameters().Length).Variable);
                    }
                    else
                    {
                        if (constrained.IsValueType)
                        {
                            if (m.IsVirtual)
                            {
                                var ct = (TypeDefinition)typeToRuntime[constrained];
                                var cm = (m.DeclaringType.IsInterface ? ct.InterfaceToMethods[m.DeclaringType] : (IReadOnlyList<MethodInfo>)ct.Methods)[definition.GetIndex(m)];
                                if (cm.DeclaringType == constrained)
                                {
                                    Enqueue(cm);
                                    GenerateCall(cm, Escape(cm), stack, after);
                                }
                                else
                                {
                                    var target = stack.ElementAt(m.GetParameters().Length);
                                    writer.WriteLine($"\t{{auto p = f__new_constructed<{Escape(constrained)}>(std::move(*{FormatMove(MakePointerType(constrained), target.Variable)}));");
                                    generate("p");
                                    writer.WriteLine($"\t}}");
                                }
                            }
                            else
                            {
                                Enqueue(m);
                                GenerateCall(m, Escape(m), stack, after);
                            }
                        }
                        else
                        {
                            generate($"(*static_cast<{Escape(constrained)}**>({stack.ElementAt(m.GetParameters().Length).Variable}))");
                        }
                        constrained = null;
                    }
                    return index;
                };
            });
            instructions1[OpCodes.Ldobj.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    withVolatile(() => writer.WriteLine($"\t{indexToStack[index].Variable} = *static_cast<{EscapeForVariable(t)}*>({stack.Variable});"));
                    return index;
                };
            });
            instructions1[OpCodes.Ldstr.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(string)));
                x.Generate = (index, stack) =>
                {
                    var s = method.Module.ResolveString(ParseI4(ref index));
                    writer.Write($" {s}\n\t{indexToStack[index].Variable} = f__string(u");
                    using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                        provider.GenerateCodeFromExpression(new CodePrimitiveExpression(s), writer, null);
                    writer.WriteLine("sv);");
                    return index;
                };
            });
            instructions1[OpCodes.Newobj.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    return (index, stack.ElementAt(m.GetParameters().Length).Push(m.DeclaringType));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    var after = indexToStack[index];
                    writer.WriteLine($@" {m.DeclaringType}::[{m}]");
                    var parameters = m.GetParameters();
                    var arguments = parameters.Zip(stack.Take(parameters.Length).Reverse(), (p, s) => $"\n\t\t{FormatMove(p.ParameterType, s.Variable)}");
                    if (builtin.GetBody(this, m) != null)
                    {
                        writer.WriteLine($@"{'\t'}{after.Variable} = {Escape(m)}({string.Join(",", arguments)}
{'\t'});");
                    }
                    else
                    {
                        if (m.DeclaringType.IsValueType)
                        {
                            writer.WriteLine($"\t{after.Variable} = {{}};");
                            arguments = arguments.Prepend($"&{after.Variable}");
                        }
                        else
                        {
                            writer.WriteLine($"\t{{auto p = f__new_zerod<{Escape(m.DeclaringType)}>();");
                            arguments = arguments.Prepend("p");
                        }
                        writer.WriteLine($@"{'\t'}{Escape(m)}(
{'\t'}{'\t'}{string.Join(",", arguments)}
{'\t'});");
                        if (!m.DeclaringType.IsValueType) writer.WriteLine($"\t{after.Variable} = std::move(p);}}");
                    }
                    Enqueue(m);
                    return index;
                };
            });
            instructions1[OpCodes.Castclass.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{stack.Variable}->f_type()->{(t.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(t)}>::v__instance)) throw std::runtime_error(\"InvalidCastException\");");
                    return index;
                };
            });
            instructions1[OpCodes.Isinst.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{stack.Variable}->f_type()->{(t.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(t)}>::v__instance)) {indexToStack[index].Variable} = nullptr;");
                    return index;
                };
            });
            instructions1[OpCodes.Unbox.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(MakeByRefType(t)));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    if (!t.IsValueType) throw new Exception(t.ToString());
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = static_cast<{Escape(t)}*>({stack.Variable});");
                    return index;
                };
            });
            instructions1[OpCodes.Throw.Value].For(x =>
            {
                x.Estimate = (index, stack) => (int.MaxValue, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\tthrow {stack.Variable};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldfld.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Pop.Push(f.FieldType));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() =>
                    {
                        writer.Write($"\t{indexToStack[index].Variable} = ");
                        if (stack.Type.IsValueType)
                            writer.Write($"{stack.Variable}.");
                        else
                            writer.Write($"static_cast<{Escape(f.DeclaringType)}{(f.DeclaringType.IsValueType ? "::t_value" : string.Empty)}*>({stack.Variable})->");
                        writer.WriteLine($"{Escape(f)};");
                    });
                    return index;
                };
            });
            instructions1[OpCodes.Ldflda.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Pop.Push(MakePointerType(f.FieldType)));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.Write($" {f.DeclaringType}::[{f}]\n\t{indexToStack[index].Variable} = &");
                    if (stack.Type.IsValueType)
                        writer.Write($"{stack.Variable}.");
                    else
                        writer.Write($"static_cast<{Escape(f.DeclaringType)}{(f.DeclaringType.IsValueType ? "::t_value" : string.Empty)}*>({stack.Variable})->");
                    writer.WriteLine($"{Escape(f)};");
                    return index;
                };
            });
            instructions1[OpCodes.Stfld.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() => writer.WriteLine($"\tstatic_cast<{(f.DeclaringType.IsValueType ? EscapeForVariable(f.DeclaringType) : Escape(f.DeclaringType))}*>({stack.Pop.Variable})->{Escape(f)} = {FormatMove(f.FieldType, stack.Variable)};"));
                    return index;
                };
            });
            string @static(FieldInfo x) => Attribute.IsDefined(x, typeof(ThreadStaticAttribute)) ? "t_thread_static" : "t_static";
            instructions1[OpCodes.Ldsfld.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Push(f.FieldType));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() => writer.WriteLine($"\t{indexToStack[index].Variable} = {@static(f)}::v_instance->v_{Escape(f.DeclaringType)}.{Escape(f)};"));
                    return index;
                };
            });
            instructions1[OpCodes.Ldsflda.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Push(MakePointerType(f.FieldType)));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]\n\t{indexToStack[index].Variable} = &{@static(f)}::v_instance->v_{Escape(f.DeclaringType)}.{Escape(f)};");
                    return index;
                };
            });
            instructions1[OpCodes.Stsfld.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() => writer.WriteLine($"\t{@static(f)}::v_instance->v_{Escape(f.DeclaringType)}.{Escape(f)} = {FormatMove(f.FieldType, stack.Variable)};"));
                    return index;
                };
            });
            instructions1[OpCodes.Stobj.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    withVolatile(() => writer.WriteLine($"\t*static_cast<{EscapeForVariable(t)}*>({stack.Pop.Variable}) = {stack.Variable};"));
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Conv_Ovf_I1_Un, Type: typeof(sbyte)),
                (OpCode: OpCodes.Conv_Ovf_I2_Un, Type: typeof(short)),
                (OpCode: OpCodes.Conv_Ovf_I4_Un, Type: typeof(int)),
                (OpCode: OpCodes.Conv_Ovf_I8_Un, Type: typeof(long)),
                (OpCode: OpCodes.Conv_Ovf_U1_Un, Type: typeof(byte)),
                (OpCode: OpCodes.Conv_Ovf_U2_Un, Type: typeof(ushort)),
                (OpCode: OpCodes.Conv_Ovf_U4_Un, Type: typeof(uint)),
                (OpCode: OpCodes.Conv_Ovf_U8_Un, Type: typeof(ulong)),
                (OpCode: OpCodes.Conv_Ovf_I_Un, Type: typeof(NativeInt)),
                (OpCode: OpCodes.Conv_Ovf_U_Un, Type: typeof(NativeInt))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{stack.VariableType}>({stack.Variable});");
                    return index;
                };
            }));
            instructions1[OpCodes.Box.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Push(typeof(object)));
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = {string.Format(t.IsValueType ? $"f__new_constructed<{Escape(t)}>({{0}})" : "{0}", $"std::move({stack.Variable})")};");
                    return index;
                };
            });
            instructions1[OpCodes.Newarr.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t.MakeArrayType()));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = f__new_array<{Escape(t.MakeArrayType())}, {EscapeForVariable(t)}>({stack.Variable});");
                    return index;
                };
            });
            instructions1[OpCodes.Ldlen.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(NativeInt)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{Escape(stack.Type)}*>({stack.Variable})->v__length;");
                    return index;
                };
            });
            instructions1[OpCodes.Ldelema.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Pop.Push(MakePointerType(t)));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var array = stack.Pop;
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = &static_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Variable}];");
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Ldelem_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Ldelem_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Ldelem_I2, Type: typeof(short)),
                (OpCode: OpCodes.Ldelem_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Ldelem_I4, Type: typeof(int)),
                (OpCode: OpCodes.Ldelem_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Ldelem_I8, Type: typeof(long)),
                (OpCode: OpCodes.Ldelem_I, Type: typeof(NativeInt)),
                (OpCode: OpCodes.Ldelem_R4, Type: typeof(float)),
                (OpCode: OpCodes.Ldelem_R8, Type: typeof(double)),
                (OpCode: OpCodes.Ldelem_Ref, Type: typeof(object))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    var array = stack.Pop;
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Variable}];");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Stelem_I, Type: "int"),
                (OpCode: OpCodes.Stelem_I1, Type: "int8_t"),
                (OpCode: OpCodes.Stelem_I2, Type: "int16_t"),
                (OpCode: OpCodes.Stelem_I4, Type: "int32_t"),
                (OpCode: OpCodes.Stelem_I8, Type: "int64_t"),
                (OpCode: OpCodes.Stelem_R4, Type: "float"),
                (OpCode: OpCodes.Stelem_R8, Type: "double")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var array = stack.Pop.Pop;
                    writer.WriteLine($"\n\tstatic_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Pop.Variable}] = static_cast<{set.Type}>({stack.Variable});");
                    return index;
                };
            }));
            instructions1[OpCodes.Stelem_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var array = stack.Pop.Pop;
                    writer.WriteLine($"\n\tstatic_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Pop.Variable}] = {FormatMove(GetElementType(array.Type), stack.Variable)};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldelem.Value].For(x =>
            {
                x.Estimate = (index, stack) => {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var array = stack.Pop;
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = static_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Variable}];");
                    return index;
                };
            });
            instructions1[OpCodes.Stelem.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var array = stack.Pop.Pop;
                    writer.WriteLine($" {t}\n\tstatic_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Pop.Variable}] = {FormatMove(t, stack.Variable)};");
                    return index;
                };
            });
            instructions1[OpCodes.Unbox_Any.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = static_cast<{Escape(t)}*>({stack.Variable}){(t.IsValueType ? "->v__value" : string.Empty)};");
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Conv_Ovf_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Conv_Ovf_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Conv_Ovf_I2, Type: typeof(short)),
                (OpCode: OpCodes.Conv_Ovf_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Conv_Ovf_I4, Type: typeof(int)),
                (OpCode: OpCodes.Conv_Ovf_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Conv_Ovf_I8, Type: typeof(long)),
                (OpCode: OpCodes.Conv_Ovf_U8, Type: typeof(ulong)),
                (OpCode: OpCodes.Conv_Ovf_I, Type: typeof(NativeInt)),
                (OpCode: OpCodes.Conv_Ovf_U, Type: typeof(NativeInt))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{stack.VariableType}>({stack.Variable});");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldtoken.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    switch (method.Module.ResolveMember(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments()))
                    {
                        case FieldInfo f:
                            return (index, stack.Push(typeof(RuntimeFieldHandle)));
                        case MethodInfo m:
                            return (index, stack.Push(typeof(RuntimeMethodHandle)));
                        case Type t:
                            return (index, stack.Push(typeof(RuntimeTypeHandle)));
                        default:
                            throw new Exception();
                    }
                };
                x.Generate = (index, stack) =>
                {
                    var member = method.Module.ResolveMember(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
                    writer.Write($" {member}\n\t{indexToStack[index].Variable} = ");
                    switch (member)
                    {
                        case FieldInfo f:
                            writer.WriteLine($"{Escape(typeof(RuntimeFieldHandle))}::t_value{{&t__field_{Escape(f.DeclaringType)}__{Escape(f.Name)}::v__instance}};");
                            break;
                        case MethodInfo m:
                            writer.WriteLine($"{Escape(m)}::v__handle;");
                            break;
                        case Type t:
                            writer.WriteLine($"{Escape(typeof(RuntimeTypeHandle))}::t_value{{&t__type_of<{Escape(t)}>::v__instance}};");
                            break;
                    }
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Add_Ovf, Operator: "+"),
                (OpCode: OpCodes.Add_Ovf_Un, Operator: "+"),
                (OpCode: OpCodes.Mul_Ovf, Operator: "*"),
                (OpCode: OpCodes.Mul_Ovf_Un, Operator: "*"),
                (OpCode: OpCodes.Sub_Ovf, Operator: "-"),
                (OpCode: OpCodes.Sub_Ovf_Un, Operator: "-")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeOfAdd[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Pop.Variable} {set.Operator} {stack.Variable};");
                    return index;
                };
            }));
            instructions1[OpCodes.Endfinally.Value].For(x =>
            {
                x.Estimate = (index, stack) => (int.MaxValue, stack);
                x.Generate = (index, stack) => 
                {
                    if (tries.Peek().Flags == ExceptionHandlingClauseOptions.Finally)
                        writer.WriteLine("\n\treturn;");
                    else
                        writer.WriteLine("\n\tthrow;");
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Leave, Target: (ParseBranchTarget)ParseBranchTargetI4),
                (OpCode: OpCodes.Leave_S, Target: (ParseBranchTarget)ParseBranchTargetI1)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    Estimate(set.Target(ref index), stack);
                    return (int.MaxValue, stack);
                };
                x.Generate = (index, stack) =>
                {
                    var target = set.Target(ref index);
                    writer.WriteLine($" {target:x04}\n\tgoto L_{target:x04};");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Ceq, Operator: "=="),
                (OpCode: OpCodes.Cgt, Operator: ">"),
                (OpCode: OpCodes.Clt, Operator: "<")
            }.ForEach(set => instructions2[set.OpCode.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Pop.Variable} {set.Operator} {stack.Variable} ? 1 : 0;");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Cgt_Un, Integer: ">", Float: "std::isgreater({0}, {1})"),
                (OpCode: OpCodes.Clt_Un, Integer: "<", Float: "std::isless({0}, {1})")
            }.ForEach(set => instructions2[set.OpCode.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {condition_Un(stack, set.Integer, set.Float)} ? 1 : 0;");
                    return index;
                };
            }));
            instructions2[OpCodes.Ldftn.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    return (index, stack.Push(MakePointerType(m.GetType())));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    writer.WriteLine($" {m.DeclaringType}::[{m}]\n\t{indexToStack[index].Variable} = reinterpret_cast<void*>(&{Escape(m)});");
                    Enqueue(m);
                    return index;
                };
            });
            instructions2[OpCodes.Ldvirtftn.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    return (index, stack.Pop.Push(MakePointerType(m.GetType())));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    writer.WriteLine($" {m.DeclaringType}::[{m}]\n\t{indexToStack[index].Variable} = &{stack.Variable}->{Escape(m)};");
                    return index;
                };
            });
            instructions2[OpCodes.Stloc.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var i = ParseI4(ref index);
                    writer.WriteLine($" {i}\n\tl{i} = {FormatMove(method.GetMethodBody().LocalVariables[i].LocalType, stack.Variable)};");
                    return index;
                };
            });
            instructions2[OpCodes.Localloc.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(MakePointerType(typeof(byte))));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = alloca({stack.Variable});");
                    return index;
                };
            });
            instructions2[OpCodes.Endfilter.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(Exception)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($@"
{'\t'}if ({stack.Variable}) throw;
{'\t'}{indexToStack[index].Variable} = std::move(e);");
                    return index;
                };
            });
            instructions2[OpCodes.Volatile.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    @volatile = true;
                    writer.WriteLine();
                    return index;
                };
            });
            instructions2[OpCodes.Initobj.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t*reinterpret_cast<{EscapeForVariable(t)}*>({stack.Variable}) = {{}};");
                    return index;
                };
            });
            instructions2[OpCodes.Constrained.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    Define(ParseType(ref index));
                    return (index, stack);
                };
                x.Generate = (index, stack) =>
                {
                    constrained = ParseType(ref index);
                    writer.WriteLine($" {constrained}");
                    return index;
                };
            });
            instructions2[OpCodes.Rethrow.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine("\n\tthrow;");
                    return index;
                };
            });
            instructions2[OpCodes.Sizeof.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(uint)));
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = sizeof({EscapeForVariable(t)});");
                    return index;
                };
            });
            instructions2[OpCodes.Refanytype.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Variable}.f_type();");
                    return index;
                };
            });
            instructions2[OpCodes.Readonly.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine("\n\treadonly ");
                    return index;
                };
            });
            writer = functionDefinitions;
        }
        private void WriteRuntimeDefinition(RuntimeDefinition definition, TextWriter writer)
        {
            var type = definition.Type;
            var @base = definition is TypeDefinition && FinalizeOf(type).MethodHandle != finalizeOfObject.MethodHandle ? "t__type_finalizee" : "t__type";
            var identifier = Escape(type);
            writer.WriteLine($@"
template<>
struct t__type_of<{identifier}> : {@base}
{{");
            memberDefinitions.Write($@"
t__type_of<{identifier}>::t__type_of() : {@base}({(type.BaseType == null ? "nullptr" : $"&t__type_of<{Escape(type.BaseType)}>::v__instance")}, {{");
            if (definition is TypeDefinition td)
            {
                void writeMethods(IEnumerable<MethodInfo> methods, Func<int, MethodInfo, string, string> pointer, Func<int, int, MethodInfo, string, string> genericPointer, Func<MethodInfo, MethodInfo> origin, string indent)
                {
                    foreach (var (m, i) in methods.Select((x, i) => (x, i))) writer.WriteLine($@"{indent}// {m}
{indent}void* v_method{i} = {(
    m.IsAbstract ? "nullptr" :
    m.IsGenericMethod ? $"&v_generic__{Escape(m)}" :
    methodToIdentifier.ContainsKey(ToKey(m)) ? $"reinterpret_cast<void*>({pointer(i, m, $"{Escape(m)}{(m.DeclaringType.IsValueType ? "__v" : string.Empty)}")})" :
    "nullptr"
)};");
                    foreach (var (m, i) in methods.Where(x => !x.IsAbstract && x.IsGenericMethod).Select((x, i) => (x, i))) writer.WriteLine($@"{indent}struct
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
                    writer.WriteLine($@"{'\t'}struct
{'\t'}{{");
                    var ii = Escape(p.Key);
                    var ms = typeToRuntime[p.Key].Methods;
                    writeMethods(p.Value,
                        (i, m, name) => $"f__method<{ii}, {i}, {identifier}, {FunctionPointer(m)}, {name}>",
                        (i, j, m, name) => $"f__generic_method<{ii}, {i}, {j}, {identifier}, {FunctionPointer(m)}, {name}>",
                        x => ms[Array.IndexOf(p.Value, x)],
                        "\t\t"
                    );
                    writer.WriteLine($"\t}} v_interface__{ii};");
                }
                memberDefinitions.WriteLine(string.Join(",", td.InterfaceToMethods.Select(p => $"\n\t{{&t__type_of<{Escape(p.Key)}>::v__instance, reinterpret_cast<void**>(&v_interface__{Escape(p.Key)})}}")));
                writer.WriteLine($@"{'\t'}virtual void f_scan(t_object* a_this, t_scan a_scan);
{'\t'}virtual t_scoped<t_slot> f_clone(const t_object* a_this);");
                if (type.IsValueType) writer.WriteLine("\tvirtual void f_copy(const char* a_from, size_t a_n, char* a_to);");
            }
            writer.WriteLine($@"{'\t'}t__type_of();
{'\t'}static t__type_of v__instance;
}};");
            memberDefinitions.WriteLine($@"}}, sizeof({EscapeForVariable(type)}){(type.IsArray ? $", &t__type_of<{Escape(GetElementType(type))}>::v__instance, {type.GetArrayRank()}" : string.Empty)})
{{
}}
t__type_of<{identifier}> t__type_of<{identifier}>::v__instance;");
            if (definition is TypeDefinition)
            {
                memberDefinitions.WriteLine($@"void t__type_of<{identifier}>::f_scan(t_object* a_this, t_scan a_scan)
{{
{'\t'}static_cast<{identifier}*>(a_this)->f__scan(a_scan);
}}
t_scoped<t_slot> t__type_of<{identifier}>::f_clone(const t_object* a_this)
{{");
                memberDefinitions.WriteLine(type.IsValueType
                    ? $@"{'\t'}auto p = t_object::f_allocate<{identifier}>();
{'\t'}new(&p->v__value) decltype({identifier}::v__value)(static_cast<const {identifier}*>(a_this)->v__value);
{'\t'}return p;
}}
void t__type_of<{identifier}>::f_copy(const char* a_from, size_t a_n, char* a_to)
{{
{'\t'}std::copy_n(reinterpret_cast<const decltype({identifier}::v__value)*>(a_from), a_n, reinterpret_cast<decltype({identifier}::v__value)*>(a_to));
}}"
                    : $@"{'\t'}return static_cast<const {identifier}*>(a_this)->f__clone();
}}");
            }
        }
        public void Do(MethodInfo method, TextWriter writer)
        {
            Define(typeof(Type));
            typeDefinitions.WriteLine("\n#include <il2cxx/type.h>");
            Escape(finalizeOfObject);
            Define(typeof(IntPtr));
            Define(typeof(UIntPtr));
            Define(typeof(Thread));
            Enqueue(typeof(ThreadStart).GetMethod("Invoke"));
            Enqueue(typeof(ParameterizedThreadStart).GetMethod("Invoke"));
            Define(typeof(string));
            Enqueue(method);
            do
            {
                Do();
                while (queuedTypes.Count > 0) Define(queuedTypes.Dequeue());
            }
            while (queuedMethods.Count > 0);
            writer.WriteLine(@"#include <il2cxx/base.h>

namespace il2cxx
{
");
            writer.Write(typeDeclarations);
            writer.Write(typeDefinitions);
            writer.Write(functionDeclarations);
            foreach (var x in runtimeDefinitions) WriteRuntimeDefinition(x, writer);
            writer.Write(memberDefinitions);
            writer.WriteLine(@"
}

#include <il2cxx/engine.h>

namespace il2cxx
{

struct t_static
{");
            writer.Write(staticDeclarations);
            writer.WriteLine($@"
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

t_static* t_static::v_instance;

struct t_thread_static
{{");
            writer.Write(threadStaticDeclarations);
            writer.WriteLine($@"
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

IL2CXX__PORTABLE__THREAD t_thread_static* t_thread_static::v_instance;");
            writer.WriteLine(functionDefinitions);
            writer.WriteLine($@"}}

#include ""slot.cc""
#include ""object.cc""
#include ""type.cc""
#include ""thread.cc""
#include ""engine.cc""

int main(int argc, char* argv[])
{{
{'\t'}using namespace il2cxx;
{'\t'}t_engine::t_options options;
{'\t'}options.v_verbose = true;
{'\t'}t_engine engine(options, argc, argv);
{'\t'}t_static s;
{'\t'}t_thread_static ts;
{string.Join(string.Empty, runtimeDefinitions.Select(x => builtin.GetInitialize(this, x.Type)).Where(x => x != null))}
{string.Join(string.Empty, runtimeDefinitions.Select(x => x.Type.TypeInitializer).Where(x => x != null).Select(x => $"\t{Escape(x)}();\n"))}
{'\t'}auto n = {Escape(method)}();
{'\t'}engine.f_shutdown();
{'\t'}return n;
}}");
        }
    }
}
