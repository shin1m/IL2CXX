using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace IL2CXX
{
    static class Utilities
    {
        public static void For<T>(this T x, Action<T> action) => action(x);
        public static void ForEach<T>(this IEnumerable<T> xs, Action<T> action)
        {
            foreach (var x in xs) action(x);
        }
        public static void ForEach<T>(this IEnumerable<T> xs, Action<T, int> action)
        {
            var i = 0;
            foreach (var x in xs) action(x, i++);
        }
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
                this.type = method.DeclaringType.TypeHandle;
            }
            public static bool operator ==(MethodKey x, MethodKey y) => x.method == y.method && x.type.Equals(y.type);
            public static bool operator !=(MethodKey x, MethodKey y) => !(x == y);
            public bool Equals(MethodKey x) => this == x;
            public override bool Equals(object x) => x is MethodKey y && this == y;
            public override int GetHashCode() => method.GetHashCode() ^ type.GetHashCode();
        }
        private static MethodKey ToKey(MethodBase method) => new MethodKey(method);
        private static IEnumerable<MethodInfo> UpToOrigin(MethodInfo method)
        {
            var origin = ToKey(method.GetBaseDefinition());
            for (var type = method.DeclaringType;;)
            {
                yield return method;
                if (ToKey(method) == origin) break;
                do
                {
                    type = type.BaseType;
                    method = type.GetMethods(declaredAndInstance).FirstOrDefault(x => ToKey(x.GetBaseDefinition()) == origin);
                } while (method == null);
            }
        }
        abstract class MethodTable : IEqualityComparer<Type[]>
        {
            bool IEqualityComparer<Type[]>.Equals(Type[] x, Type[] y) => x.SequenceEqual(y);
            int IEqualityComparer<Type[]>.GetHashCode(Type[] x) => x.Select(y => y.GetHashCode()).Aggregate((y, z) => y % z);

            public readonly List<MethodInfo> Methods = new List<MethodInfo>();
            public readonly Dictionary<MethodKey, int> MethodToIndex = new Dictionary<MethodKey, int>();

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
        class InterfaceDefinition : MethodTable
        {
            public InterfaceDefinition(Type type, Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex)
            {
                foreach (var x in type.GetMethods()) Add(x, genericMethodToTypesToIndex);
            }
            protected override int GetIndex(MethodKey method) => MethodToIndex[method];
        }
        class SZArrayHelper<T> : IList<T>, ICollection<T>, IEnumerable<T>, IReadOnlyList<T>, IReadOnlyCollection<T>
        {
            public class Enumerator : IEnumerator<T>
            {
                private T[] array;
                private int index = -1;

                public Enumerator(T[] array) => this.array = array;
                public void Dispose() { }
                public bool MoveNext() => ++index < array.Length;
                public void Reset() => index = -1;
                public T Current => array[index];
                object IEnumerator.Current => Current;
            }

            public int Count => throw new NotImplementedException();
            public bool IsReadOnly => true;
            public T this[int index] {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => IndexOf(item) >= 0;
            public void CopyTo(T[] array, int index) => throw new NotImplementedException();
            public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int IndexOf(T item) => throw new NotImplementedException();
            public void Insert(int index, T item) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
        }
        class TypeDefinition : MethodTable
        {
            public readonly TypeDefinition Base;
            public readonly Dictionary<Type, MethodInfo[]> InterfaceToMethods = new Dictionary<Type, MethodInfo[]>();

            public TypeDefinition(Type type, TypeDefinition @base, Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex, IEnumerable<(Type Type, InterfaceDefinition Definition)> interfaces)
            {
                Base = @base;
                if (Base != null) Methods.AddRange(Base.Methods);
                foreach (var x in type.GetMethods(declaredAndInstance).Where(x => x.IsVirtual))
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
                    var map = (type.IsArray && key.IsGenericType ? typeof(SZArrayHelper<>).MakeGenericType(GetElementType(type)) : type).GetInterfaceMap(key);
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
                if (Type.IsByRef)
                    VariableType = "t_managed_pointer";
                else if (Type.IsPointer)
                    VariableType = "void*";
                else if (primitives.ContainsKey(Type))
                    if (Type == typeof(long) || Type == typeof(ulong))
                        VariableType = "int64_t";
                    else if (Type == typeof(float) || Type == typeof(double))
                        VariableType = "double";
                    else if (Type == typeof(NativeInt))
                        VariableType = "intptr_t";
                    else
                        VariableType = "int32_t";
                else if (Type.IsEnum)
                {
                    var underlying = Type.GetEnumUnderlyingType();
                    VariableType = underlying == typeof(long) || underlying == typeof(ulong) ? "int64_t" : "int32_t";
                }
                else if (Type.IsValueType)
                    VariableType = transpiler.EscapeForVariable(Type);
                else
                    VariableType = $"{transpiler.Escape(typeof(object))}*";
                Indices.TryGetValue(VariableType, out var index);
                Variable = $"s{Escape(VariableType)}__{index}";
                Indices[VariableType] = ++index;
                transpiler.definedIndices.TryGetValue(VariableType, out var defined);
                if (index > defined) transpiler.definedIndices[VariableType] = index;
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
        private static readonly Type managedPointerType = typeof(void).MakeByRefType();
        private static readonly Type transientPointerType = typeof(void).MakePointerType();
        private static readonly Type typedReferenceByRefType = typeof(TypedReferenceTag).MakeByRefType();
        private static readonly Type typedReferencePointerType = typeof(TypedReferenceTag).MakePointerType();
        private static readonly IReadOnlyDictionary<(string, string), Type> typeOfAdd = new Dictionary<(string, string), Type> {
            [("int32_t", "int32_t")] = typeof(int),
            [("int32_t", "intptr_t")] = typeof(NativeInt),
            [("int32_t", "t_managed_pointer")] = managedPointerType,
            [("int32_t", "void*")] = transientPointerType,
            [("int64_t", "int64_t")] = typeof(long),
            [("intptr_t", "int32_t")] = typeof(NativeInt),
            [("intptr_t", "intptr_t")] = typeof(NativeInt),
            [("intptr_t", "t_managed_pointer")] = managedPointerType,
            [("intptr_t", "void*")] = transientPointerType,
            [("double", "double")] = typeof(double),
            [("t_managed_pointer", "int32_t")] = managedPointerType,
            [("t_managed_pointer", "intptr_t")] = managedPointerType,
            [("void*", "int32_t")] = transientPointerType,
            [("void*", "intptr_t")] = transientPointerType,
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

        static Transpiler()
        {
            foreach (var x in typeof(OpCodes).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public))
            {
                var opcode = (OpCode)x.GetValue(null);
                (opcode.Size == 1 ? opcodes1 : opcodes2)[opcode.Value & 0xff] = opcode;
            }
        }

        private readonly Instruction[] instructions1 = new Instruction[256];
        private readonly Instruction[] instructions2 = new Instruction[256];
        private readonly Action<string> log;
        private readonly StringWriter typeDeclarations = new StringWriter();
        private readonly StringWriter typeDefinitions = new StringWriter();
        private readonly StringWriter memberDefinitions = new StringWriter();
        private readonly StringWriter functionDeclarations = new StringWriter();
        private readonly StringWriter functionDefinitions = new StringWriter();
        private readonly Dictionary<Type, Func<string>> typeToBuiltinFields = new Dictionary<Type, Func<string>>();
        private readonly Dictionary<Type, Func<string>> typeToBuiltinInitialize = new Dictionary<Type, Func<string>>();
        private readonly Dictionary<Type, MethodTable> typeToDefinition = new Dictionary<Type, MethodTable>();
        private readonly HashSet<string> typeIdentifiers = new HashSet<string>();
        private readonly Dictionary<Type, string> typeToIdentifier = new Dictionary<Type, string>();
        private readonly Dictionary<MethodKey, Func<string>> methodToBuiltinBody = new Dictionary<MethodKey, Func<string>>();
        private readonly Dictionary<MethodKey, Func<Type[], string>> genericMethodToBuiltinBody = new Dictionary<MethodKey, Func<Type[], string>>();
        private readonly Dictionary<MethodKey, Func<Type, string>> methodTreeToBuiltinBody = new Dictionary<MethodKey, Func<Type, string>>();
        private readonly Dictionary<string, Func<MethodBase, string>> methodNameToBuiltinBody = new Dictionary<string, Func<MethodBase, string>>();
        private readonly HashSet<(Type, string)> methodIdentifiers = new HashSet<(Type, string)>();
        private readonly Dictionary<MethodKey, string> methodToIdentifier = new Dictionary<MethodKey, string>();
        private readonly Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex = new Dictionary<MethodKey, Dictionary<Type[], int>>();
        private readonly Queue<Type> queuedTypes = new Queue<Type>();
        private readonly HashSet<MethodKey> visitedMethods = new HashSet<MethodKey>();
        private readonly Queue<MethodBase> queuedMethods = new Queue<MethodBase>();
        private MethodBase method;
        private byte[] bytes;
        private Dictionary<string, int> definedIndices;
        private Dictionary<int, Stack> indexToStack;
        private StringWriter writer;
        private readonly Stack<ExceptionHandlingClause> tries = new Stack<ExceptionHandlingClause>();
        private readonly Stack<StringWriter> writers = new Stack<StringWriter>();
        private Type constrained;

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
                    var xs = stack.Select(y => y.VariableType).Select(y => y == "t_managed_pointer" ? "void*" : y);
                    var ys = x.Select(y => y.VariableType).Select(y => y == "t_managed_pointer" ? "void*" : y);
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
        private MethodTable Define(Type type)
        {
            if (typeToDefinition.TryGetValue(type, out var definition)) return definition;
            if (type.IsInterface)
            {
                definition = new InterfaceDefinition(type, genericMethodToTypesToIndex);
                typeToDefinition.Add(type, definition);
                typeDeclarations.WriteLine($@"// {type.AssemblyQualifiedName}
struct {Escape(type)}
{{
}};");
            }
            else
            {
                typeToDefinition.Add(type, null);
                if (type.TypeInitializer != null) queuedMethods.Enqueue(type.TypeInitializer);
                var td = new TypeDefinition(type, type.BaseType == null ? null : (TypeDefinition)Define(type.BaseType), genericMethodToTypesToIndex, type.GetInterfaces().Select(x => (x, (InterfaceDefinition)Define(x))));
                void enqueue(MethodInfo m, MethodInfo concrete)
                {
                    if (m.IsGenericMethod)
                        foreach (var k in genericMethodToTypesToIndex[ToKey(m)].Keys)
                            queuedMethods.Enqueue(concrete.MakeGenericMethod(k));
                    else if (methodToIdentifier.ContainsKey(ToKey(m)))
                        queuedMethods.Enqueue(concrete);
                }
                foreach (var m in td.Methods.Where(x => !x.IsAbstract)) enqueue(m.GetBaseDefinition(), m);
                foreach (var p in td.InterfaceToMethods)
                {
                    var id = typeToDefinition[p.Key];
                    foreach (var m in id.Methods) enqueue(m, p.Value[id.GetIndex(m)]);
                }
                definition = td;
                typeToDefinition[type] = definition;
                var identifier = Escape(type);
                var declaration = $@"// {type.AssemblyQualifiedName}
struct {identifier}";
                var @base = type.BaseType == null ? string.Empty : $" : {Escape(type.BaseType)}";
                var staticFields = type.IsEnum ? Enumerable.Empty<FieldInfo>() : type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var staticDeclarations = string.Join(string.Empty, staticFields.Select(x => $"\tstatic {EscapeForVariable(x.FieldType)} v_{Escape(x.Name)};\n"));
                var staticDefinitions = string.Join(string.Empty, staticFields.Select(x => $"{EscapeForVariable(x.FieldType)} {identifier}::v_{Escape(x.Name)};\n"));
                var fields = typeToBuiltinFields.TryGetValue(type, out var builtin) ? builtin() : string.Join(string.Empty, type.GetFields(declaredAndInstance).Select(x => $"\t{EscapeForVariable(x.FieldType)} v_{Escape(x.Name)};\n"));
                if (primitives.TryGetValue(type, out var name) || type.IsEnum)
                {
                    if (name == null) name = primitives[type.GetEnumUnderlyingType()];
                    fields = $@"{'\t'}{name} v__value;
{'\t'}{identifier}({name} a_value) : v__value(a_value)
{'\t'}{{
{'\t'}}}
";
                }
                else if (type.IsValueType)
                {
                    fields = $@"{'\t'}struct t_value
{'\t'}{{
{fields}{'\t'}}} v__value;
{'\t'}{identifier}(t_value&& a_value) : v__value(std::move(a_value))
{'\t'}{{
{'\t'}}}
";
                }
                var arrayMembers = type.IsArray ? $@"{'\t'}t__bound v__bounds[{type.GetArrayRank()}];
{'\t'}{EscapeForVariable(GetElementType(type))}* f__data()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<{EscapeForVariable(GetElementType(type))}*>(this + 1);
{'\t'}}}
" : string.Empty;
                typeDeclarations.WriteLine($"{declaration};");
                typeDefinitions.WriteLine($@"
{declaration}{@base}
{{
{fields}{staticDeclarations}{arrayMembers}}};");
                if (staticDefinitions != string.Empty)
                {
                    memberDefinitions.WriteLine();
                    memberDefinitions.Write(staticDefinitions);
                }
            }
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
        private string Escape(Type type)
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
        private string EscapeForVariable(Type type) =>
            type.IsByRef || type.IsPointer ? $"{EscapeForVariable(GetElementType(type))}*" :
            type.IsInterface ? $"{Escape(typeof(object))}*" :
            primitives.TryGetValue(type, out var x) ? x :
            type.IsEnum ? primitives[type.GetEnumUnderlyingType()] :
            $"{Escape(type)}{(type.IsValueType ? "::t_value" : "*")}";
        private string EscapeForParameter(Type type) =>
            type.IsByRef || type.IsPointer ? $"{EscapeForVariable(GetElementType(type))}*" :
            type.IsInterface ? $"{Escape(typeof(object))}*" :
            primitives.TryGetValue(type, out var x) ? x :
            type.IsEnum ? primitives[type.GetEnumUnderlyingType()] :
            type.IsValueType ? $"{Escape(type)}::t_value" :
            $"{Escape(type)}*";
        private string Escape(FieldInfo field) => $"{(field.IsStatic ? $"{Escape(field.DeclaringType)}::" : string.Empty)}v_{Escape(field.Name)}";
        private string Escape(MethodBase method)
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
        private string MoveFormat(Type type) =>
            type == typeof(bool) ? "{0} != 0" :
            type == typeof(IntPtr) || type == typeof(UIntPtr) ? "{{{0}}}" :
            type.IsByRef || type.IsPointer ? $"reinterpret_cast<{EscapeForVariable(type)}>({{0}})" :
            type.IsPrimitive || type.IsEnum ? "{0}" :
            type.IsValueType ? "std::move({0})" :
            $"std::move(static_cast<{EscapeForVariable(type)}>({{0}}))";
        private void GenerateCall(MethodBase method, string function, IEnumerable<string> variables, Stack after)
        {
            var arguments = new List<Type>();
            if (!method.IsStatic) arguments.Add(GetThisType(method));
            arguments.AddRange(method.GetParameters().Select(x => x.ParameterType));
            var call = $@"{function}({
    string.Join(",", arguments.Zip(variables.Reverse(), (a, v) => $"\n\t\t{string.Format(MoveFormat(a), v)}"))
}{(arguments.Count > 0 ? "\n\t" : string.Empty)})";
            writer.WriteLine(method is MethodInfo m && m.ReturnType != typeof(void) ? $"\t{after.Variable} = {(m.ReturnType.IsValueType ? call : $"static_cast<{EscapeForVariable(m.ReturnType)}>({call})")};" : $"\t{call};");
        }
        private void GenerateCall(MethodBase method, string function, Stack stack, Stack after) => GenerateCall(method, function, stack.Take(method.GetParameters().Length + (method.IsStatic ? 0 : 1)).Select(x => x.Variable), after);
        private string FunctionPointer(MethodBase method)
        {
            var parameters = method.GetParameters().Select(x => x.ParameterType);
            if (!method.IsStatic) parameters = parameters.Prepend(GetThisType(method));
            return $"{(method is MethodInfo m && m.ReturnType != typeof(void) ? EscapeForVariable(m.ReturnType) : "void")}(*)({string.Join(", ", parameters.Select(EscapeForParameter))})";
        }
        private static MethodBase GetGenericTypeMethod(MethodBase method) => MethodBase.GetMethodFromHandle(method.MethodHandle, method.DeclaringType.GetGenericTypeDefinition().TypeHandle);
        private void Do()
        {
            method = queuedMethods.Dequeue();
            var key = ToKey(method);
            if (!visitedMethods.Add(key) || method.IsAbstract) return;
            var gotBuiltinBody = methodToBuiltinBody.TryGetValue(key, out var builtin);
            if (!gotBuiltinBody && method.IsGenericMethod)
            {
                gotBuiltinBody = genericMethodToBuiltinBody.TryGetValue(ToKey(((MethodInfo)method).GetGenericMethodDefinition()), out var gb);
                if (gotBuiltinBody) builtin = () => gb(method.GetGenericArguments());
            }
            if (!gotBuiltinBody && method.DeclaringType.IsGenericType)
            {
                gotBuiltinBody = genericMethodToBuiltinBody.TryGetValue(ToKey(GetGenericTypeMethod(method)), out var gb);
                if (gotBuiltinBody) builtin = () => gb(method.DeclaringType.GetGenericArguments());
            }
            var returns = method is MethodInfo m ? m.ReturnType == typeof(void) ? "void" : EscapeForVariable(m.ReturnType) : method.IsStatic || builtin == null ? "void" : EscapeForVariable(method.DeclaringType);
            var identifier = Escape(method);
            var parameters = method.GetParameters().Select(x => x.ParameterType);
            if (!method.IsStatic && !(method.IsConstructor && builtin != null)) parameters = parameters.Prepend(GetThisType(method));
            string argument(Type t, int i) => $"\n\t{EscapeForParameter(t)} a_{i}";
            var arguments = parameters.Select(argument).ToList();
            var prototype = $@"
// {method.DeclaringType}
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
            if (gotBuiltinBody)
            {
                if (builtin != null)
                {
                    writer.WriteLine($"{prototype}\n{{\n{builtin()}}}");
                    return;
                }
            }
            else
            {
                if (method is MethodInfo mi)
                    foreach (var x in UpToOrigin(mi.IsGenericMethod ? mi.GetGenericMethodDefinition() : mi))
                        if (methodTreeToBuiltinBody.TryGetValue(ToKey(x), out var builtin0))
                        {
                            if (builtin0 == null) break;
                            writer.WriteLine($"{prototype}\n{{\n{builtin0(method.DeclaringType)}}}");
                            return;
                        }
                {
                    if (methodNameToBuiltinBody.TryGetValue(method.ToString(), out var builtin0))
                    {
                        if (builtin0 != null)
                        {
                            writer.WriteLine($"{prototype}\n{{\n{builtin0(method)}}}");
                            return;
                        }
                    }
                }
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
            definedIndices = new Dictionary<string, int>();
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
                writer.WriteLine($"\t{EscapeForVariable(x.LocalType)} l{x.LocalIndex}{(body.InitLocals ? "{}" : string.Empty)};");
            foreach (var x in definedIndices)
            {
                var name = Escape(x.Key);
                for (var i = 0; i < x.Value; ++i) writer.WriteLine($"\t{x.Key} s{name}__{i};");
            }
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
}} catch ({EscapeForVariable(clause.CatchType)} e) {{");
                                break;
                            case ExceptionHandlingClauseOptions.Filter:
                                writer.WriteLine($@"// filter
}} catch ({EscapeForVariable(typeof(Exception))} e) {{");
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

        public Transpiler(Action<string> log)
        {
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
                    writer.WriteLine($"\n\tl{i} = {string.Format(MoveFormat(method.GetMethodBody().LocalVariables[i].LocalType), stack.Variable)};");
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
                    writer.WriteLine($" {i}\n\ta_{i} = {string.Format(MoveFormat(GetArgumentType(i)), stack.Variable)};");
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
                    writer.WriteLine($" {i}\n\tl{i} = {string.Format(MoveFormat(method.GetMethodBody().LocalVariables[i].LocalType), stack.Variable)};");
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
                    queuedMethods.Enqueue(m);
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
                        writer.Write($" {string.Format(MoveFormat(m.ReturnType), stack.Variable)}");
                        stack = stack.Pop;
                    }
                    writer.WriteLine(";");
                    if (stack.Pop != null) throw new Exception();
                    return index;
                };
            });
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
                        writer.WriteLine($" {target:x04}\n\tgoto L_{target:x04};");
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
                        writer.WriteLine($" {target:x04}\n\tif ({set.Operator}{stack.Variable}) goto L_{target:x04};");
                        return index;
                    };
                }));
                new[] {
                    (OpCode: OpCodes.Beq_S, Operator: "=="),
                    (OpCode: OpCodes.Bge_S, Operator: ">="),
                    (OpCode: OpCodes.Bgt_S, Operator: ">"),
                    (OpCode: OpCodes.Ble_S, Operator: "<="),
                    (OpCode: OpCodes.Blt_S, Operator: "<"),
                    (OpCode: OpCodes.Bne_Un_S, Operator: "!="),
                    (OpCode: OpCodes.Bge_Un_S, Operator: ">="),
                    (OpCode: OpCodes.Bgt_Un_S, Operator: ">"),
                    (OpCode: OpCodes.Ble_Un_S, Operator: "<="),
                    (OpCode: OpCodes.Blt_Un_S, Operator: "<")
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
                        writer.WriteLine($" {target:x04}\n\tif ({string.Format(format, stack.Pop.Variable)} {set.Operator} {string.Format(format, stack.Variable)}) goto L_{target:x04};");
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
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = *static_cast<{primitives[set.Type]}*>({stack.Variable});");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldind_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(GetElementType(stack.Type)));
                x.Generate = (index, stack) =>
                {
                    var after = indexToStack[index];
                    writer.WriteLine($"\n\t{after.Variable} = *static_cast<{EscapeForVariable(after.Type)}*>({stack.Variable});");
                    return index;
                };
            });
            instructions1[OpCodes.Stind_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t*reinterpret_cast<{EscapeForVariable(typeof(object))}*>({stack.Pop.Variable}) = std::move({stack.Variable});");
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
                    writer.WriteLine($"\n\t*reinterpret_cast<{primitives[set.Type]}*>({stack.Pop.Variable}) = {stack.Variable};");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Add, Operator: "+", Type: typeOfAdd),
                (OpCode: OpCodes.Sub, Operator: "-", Type: typeOfAdd),
                (OpCode: OpCodes.Mul, Operator: "*", Type: typeOfAdd),
                (OpCode: OpCodes.Div, Operator: "/", Type: typeOfAdd),
                (OpCode: OpCodes.Div_Un, Operator: "/", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Rem, Operator: "%", Type: typeOfAdd),
                (OpCode: OpCodes.Rem_Un, Operator: "%", Type: typeOfDiv_Un),
                (OpCode: OpCodes.And, Operator: "&", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Or, Operator: "|", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Xor, Operator: "^", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Shl, Operator: "<<", Type: typeOfShl),
                (OpCode: OpCodes.Shr, Operator: ">>", Type: typeOfShl),
                (OpCode: OpCodes.Shr_Un, Operator: ">>", Type: typeOfShl)
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
                    writer.WriteLine($"\n\t{after.Variable} = {(stack.Type.IsByRef || stack.Type.IsPointer || !stack.Type.IsValueType ? "reinterpret_cast" : "static_cast")}<{after.VariableType}>({stack.Variable});");
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
                    var definition = typeToDefinition[m.DeclaringType];
                    void generate(string target)
                    {
                        void forVirtual(string table, IEnumerable<IReadOnlyList<MethodInfo>> concretes)
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
                                GenerateCall(m, $"reinterpret_cast<{FunctionPointer(m)}>(reinterpret_cast<void**>({table}[{i}])[{j}])", variables, after);
                                foreach (var ms in concretes) queuedMethods.Enqueue(ms[i].MakeGenericMethod(ga));
                            }
                            else
                            {
                                var i = definition.GetIndex(m);
                                GenerateCall(m, $"reinterpret_cast<{FunctionPointer(m)}>({table}[{i}])", variables, after);
                                Escape(m);
                                foreach (var ms in concretes) queuedMethods.Enqueue(ms[i]);
                            }
                        }
                        queuedMethods.Enqueue(m);
                        if (m.DeclaringType.IsInterface)
                            forVirtual($"{target}->v__type->v__interface_to_methods[&t__type_of<{Escape(m.DeclaringType)}>::v__instance]", typeToDefinition.Values.OfType<TypeDefinition>().Select(y => y.InterfaceToMethods.TryGetValue(m.DeclaringType, out var ms) ? ms : null).Where(y => y != null));
                        else if (m.IsVirtual)
                            forVirtual($"reinterpret_cast<void**>({target}->v__type + 1)", typeToDefinition.Where(y => !y.Key.IsInterface && y.Key.IsSubclassOf(m.DeclaringType)).Select(y => y.Value.Methods));
                        else
                            GenerateCall(m, Escape(m), stack, after);
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
                            var cm = typeToDefinition[constrained].Methods[definition.GetIndex(m)];
                            if (cm.DeclaringType == constrained)
                            {
                                GenerateCall(cm, Escape(cm), stack, after);
                                queuedMethods.Enqueue(cm);
                            }
                            else
                            {
                                var target = stack.ElementAt(m.GetParameters().Length);
                                writer.WriteLine($@"{'\t'}{{auto p = new {Escape(constrained)}(std::move(*{string.Format(MoveFormat(MakePointerType(constrained)), target.Variable)}));
{'\t'}p->v__type = &t__type_of<{Escape(constrained)}>::v__instance;");
                                generate("p");
                                writer.WriteLine($"\t}}");
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
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = *static_cast<{EscapeForVariable(t)}*>({stack.Variable});");
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
                    var arguments = parameters.Zip(stack.Take(parameters.Length).Reverse(), (p, s) => $"\n\t\t{string.Format(MoveFormat(p.ParameterType), s.Variable)}");
                    if (methodToBuiltinBody.ContainsKey(ToKey(m)) || m.DeclaringType.IsGenericType && genericMethodToBuiltinBody.ContainsKey(ToKey(GetGenericTypeMethod(m))))
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
                            writer.WriteLine($@"{'\t'}{{auto p = f__new<{Escape(m.DeclaringType)}>();
{'\t'}std::fill_n(reinterpret_cast<char*>(p) + sizeof(t__type*), sizeof({Escape(m.DeclaringType)}) - sizeof(t__type*), '\0');");
                            arguments = arguments.Prepend("p");
                        }
                        writer.WriteLine($@"{'\t'}{Escape(m)}(
{'\t'}{'\t'}{string.Join(",", arguments)}
{'\t'});");
                        if (!m.DeclaringType.IsValueType) writer.WriteLine($"\t{after.Variable} = std::move(p); }}");
                    }
                    queuedMethods.Enqueue(m);
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
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{stack.Variable}->v__type->{(t.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(t)}>::v__instance)) throw std::runtime_error(\"InvalidCastException\");");
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
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{stack.Variable}->v__type->{(t.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(t)}>::v__instance)) {indexToStack[index].Variable} = nullptr;");
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
                    writer.Write($" {f.DeclaringType}::[{f}]\n\t{indexToStack[index].Variable} = ");
                    if (stack.Type.IsValueType)
                        writer.Write($"{stack.Variable}.");
                    else
                        writer.Write($"static_cast<{Escape(f.DeclaringType)}{(f.DeclaringType.IsValueType ? "::t_value" : string.Empty)}*>({stack.Variable})->");
                    writer.WriteLine($"{Escape(f)};");
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
                    writer.WriteLine($@" {f.DeclaringType}::[{f}]
{'\t'}static_cast<{(f.DeclaringType.IsValueType ? EscapeForVariable(f.DeclaringType) : Escape(f.DeclaringType))}*>({stack.Pop.Variable})->{Escape(f)} = {string.Format(MoveFormat(f.FieldType), stack.Variable)};");
                    return index;
                };
            });
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
                    writer.WriteLine($" {f.DeclaringType}::[{f}]\n\t{indexToStack[index].Variable} = {Escape(f)};");
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
                    writer.WriteLine($" {f.DeclaringType}::[{f}]\n\t{indexToStack[index].Variable} = &{Escape(f)};");
                    return index;
                };
            });
            instructions1[OpCodes.Stsfld.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]\n\t{Escape(f)} = {string.Format(MoveFormat(f.FieldType), stack.Variable)};");
                    return index;
                };
            });
            instructions1[OpCodes.Stobj.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t*static_cast<{EscapeForVariable(t)}*>({stack.Pop.Variable}) = {stack.Variable};");
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
                    var after = indexToStack[index];
                    writer.WriteLine($" {t}\n\t{after.Variable} = f__new<{Escape(t)}>(std::move({stack.Variable}));");
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
                    writer.WriteLine($@" {t}
{'\t'}{{auto p = f__new_sized<{Escape(t.MakeArrayType())}>(sizeof({EscapeForVariable(t)}) * {stack.Variable});
{'\t'}p->v__length = {stack.Variable};
{'\t'}p->v__bounds[0] = {{{stack.Variable}, 0}};
{'\t'}std::fill_n(p->f__data(), {stack.Variable}, ({EscapeForVariable(t)}){{}});
{'\t'}{indexToStack[index].Variable} = p;
{'\t'}}}");
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
                    writer.WriteLine($"\n\tstatic_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Pop.Variable}] = {string.Format(MoveFormat(GetElementType(array.Type)), stack.Variable)};");
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
                    writer.WriteLine($" {t}\n\tstatic_cast<{Escape(array.Type)}*>({array.Variable})->f__data()[{stack.Pop.Variable}] = {string.Format(MoveFormat(t), stack.Variable)};");
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
                            writer.WriteLine($"{Escape(f)}::v__handle;");
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
                (OpCode: OpCodes.Cgt_Un, Operator: ">"),
                (OpCode: OpCodes.Clt, Operator: "<"),
                (OpCode: OpCodes.Clt_Un, Operator: "<")
            }.ForEach(set => instructions2[set.OpCode.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Pop.Variable} {set.Operator} {stack.Variable} ? 1 : 0;");
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
                    queuedMethods.Enqueue(m);
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
                    writer.WriteLine($" {i}\n\tl{i} = {string.Format(MoveFormat(method.GetMethodBody().LocalVariables[i].LocalType), stack.Variable)};");
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
                    writer.WriteLine("\n\tvolatile ");
                    return index;
                };
            });
            instructions2[OpCodes.Initobj.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\tstd::fill_n(reinterpret_cast<char*>({stack.Variable}), sizeof({EscapeForVariable(t)}), '\\0');");
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
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Variable}.v__type;");
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
            typeToBuiltinFields.Add(typeof(object), () => "\tt__type* v__type;\n");
            typeToBuiltinFields.Add(typeof(Type), () => $"\t{Escape(typeof(Type))}();\n");
            typeToBuiltinFields.Add(typeof(RuntimeTypeHandle), () => $"\t{EscapeForVariable(typeof(Type))} v__type;\n");
            typeToBuiltinFields.Add(typeof(Array), () => $@"{'\t'}struct t__bound
{'\t'}{{
{'\t'}{'\t'}int v_length;
{'\t'}{'\t'}int v_lower;
{'\t'}}};
{'\t'}size_t v__length;
{'\t'}t__bound* f__bounds()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<t__bound*>(this + 1);
{'\t'}}}
");
            typeToBuiltinFields.Add(typeof(Exception), () => $@"{'\t'}t_System_2eString* v__5fclassName;
{'\t'}t_System_2eString* v__5fmessage;
{'\t'}t_System_2eObject* v__5fdata;
{'\t'}t_System_2eException* v__5finnerException;
{'\t'}t_System_2eString* v__5fhelpURL;
{'\t'}t_System_2eObject* v__5fstackTrace;
{'\t'}t_System_2eObject* v__5fwatsonBuckets;
{'\t'}t_System_2eString* v__5fstackTraceString;
{'\t'}t_System_2eString* v__5fremoteStackTraceString;
{'\t'}int32_t v__5fremoteStackIndex;
{'\t'}t_System_2eObject* v__5fdynamicMethods;
{'\t'}int32_t v__5fHResult;
{'\t'}t_System_2eString* v__5fsource;
{'\t'}t_System_2eIntPtr::t_value v__5fxptrs;
{'\t'}int32_t v__5fxcode;
{'\t'}t_System_2eUIntPtr::t_value v__5fipForWatsonBuckets;
");
            typeToBuiltinFields.Add(typeof(string), () => "\tsize_t v__length;\n");
            typeToBuiltinInitialize.Add(typeof(string), () => $"\t{Escape(typeof(string).GetField(nameof(string.Empty)))} = f__string(u\"\"sv);\n");
            methodToBuiltinBody.Add(ToKey(typeof(object).GetMethod(nameof(object.GetType))), () => "\treturn a_0->v__type;\n");
            methodToBuiltinBody.Add(ToKey(typeof(object).GetMethod(nameof(object.ToString))), () => "\treturn f__string(u\"object\"sv);\n");
            methodToBuiltinBody.Add(ToKey(typeof(ValueType).GetMethod(nameof(object.Equals))), () => "\treturn a_0 == a_1;\n");
            methodToBuiltinBody.Add(ToKey(typeof(ValueType).GetMethod(nameof(object.ToString))), () => "\treturn f__string(u\"struct\"sv);\n");
            methodToBuiltinBody.Add(ToKey(typeof(Type).TypeInitializer), () => string.Empty);
            methodToBuiltinBody.Add(ToKey(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))), () => "\treturn a_0.v__type;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Type).GetMethod("op_Equality")), () => "\treturn a_0 == a_1;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Type).GetMethod("op_Inequality")), () => "\treturn a_0 != a_1;\n");
            methodToBuiltinBody.Add(ToKey(typeof(char).TypeInitializer), () => string.Empty);
            methodToBuiltinBody.Add(ToKey(typeof(Enum).GetMethod(nameof(object.Equals))), () => "\treturn a_0 == a_1;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Enum).GetMethod(nameof(Enum.ToString), new[] { typeof(string) })), () => "\treturn f__string(u\"enum\"sv);\n");
            methodToBuiltinBody.Add(ToKey(typeof(TypedReference).GetMethod("InternalToObject", BindingFlags.Static | BindingFlags.NonPublic)), () => $@"{'\t'}auto p = static_cast<{EscapeForVariable(typeof(TypedReference))}*>(a_0);
{'\t'}auto type = static_cast<t__type*>(p->v_Type.v__5fvalue);
{'\t'}auto value = p->v_Value.v__5fvalue;
{'\t'}if (type->f__is(&t__type_of<{Escape(typeof(ValueType))}>::v__instance)) {{
{'\t'}{'\t'}auto p = new(new char[sizeof({Escape(typeof(object))}) + type->v__size]) {Escape(typeof(object))};
{'\t'}{'\t'}p->v__type = type;
{'\t'}{'\t'}std::memcpy(p + 1, value, type->v__size);
{'\t'}{'\t'}return p;
{'\t'}}} else {{
{'\t'}{'\t'}return *static_cast<{EscapeForVariable(typeof(object))}*>(value);
{'\t'}}}
");
            methodToBuiltinBody.Add(ToKey(typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })), () => $@"{'\t'}if (a_0->v__type == a_2->v__type) {{
{'\t'}{'\t'}auto n = a_0->v__type->v__element->v__size;
{'\t'}{'\t'}std::copy_n(reinterpret_cast<char*>(a_0 + 1) + a_1 * n, a_4 * n, reinterpret_cast<char*>(a_2 + 1) + a_3 * n);
{'\t'}}} else {{
{'\t'}}}
");
            methodToBuiltinBody.Add(ToKey(typeof(Array).GetMethod(nameof(Array.GetLowerBound))), () => $@"
{'\t'}if (a_1 < 0 || a_1 >= a_0->v__type->v__rank) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}return a_0->f__bounds()[a_1].v_lower;
");
            methodToBuiltinBody.Add(ToKey(typeof(Array).GetMethod(nameof(Array.GetUpperBound))), () => $@"
{'\t'}if (a_1 < 0 || a_1 >= a_0->v__type->v__rank) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}auto& bound = a_0->f__bounds()[a_1];
{'\t'}return bound.v_lower + bound.v_length - 1;
");
            methodToBuiltinBody.Add(ToKey(typeof(Array).GetProperty(nameof(Array.Length)).GetMethod), () => "\treturn a_0->v__length;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Array).GetProperty(nameof(Array.Rank)).GetMethod), () => "\treturn a_0->v__type->v__rank;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Array).GetMethod("TrySZIndexOf", BindingFlags.Static | BindingFlags.NonPublic)), () => "\treturn false;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Array).GetMethod("InternalGetReference", declaredAndInstance)), () => $@"
{'\t'}auto bounds = a_0->f__bounds();
{'\t'}size_t n = 0;
{'\t'}for (size_t i = 0; i < a_2; ++i) n = n * bounds[i].v_length + (a_3[i] - bounds[i].v_lower);
{'\t'}auto type = a_0->v__type;
{'\t'}auto p = reinterpret_cast<{EscapeForVariable(typeof(TypedReference))}*>(a_1);
{'\t'}p->v_Type = {{type->v__element}};
{'\t'}p->v_Value = {{reinterpret_cast<char*>(bounds + type->v__rank) + n * type->v__element->v__size}};
");
            genericMethodToBuiltinBody.Add(ToKey(typeof(SZArrayHelper<>).GetProperty(nameof(SZArrayHelper<object>.Count)).GetMethod), types => $"\treturn a_0->v__length;\n");
            genericMethodToBuiltinBody.Add(ToKey(typeof(SZArrayHelper<>).GetProperty("Item").GetMethod), types => $@"{'\t'}if (a_1 < 0 || a_1 >= a_0->v__length) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}return a_0->f__data()[a_1];
");
            genericMethodToBuiltinBody.Add(ToKey(typeof(SZArrayHelper<>).GetProperty("Item").SetMethod), types => $@"{'\t'}if (a_1 < 0 || a_1 >= a_0->v__length) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}a_0->f__data()[a_1] = std::move(a_2);
");
            genericMethodToBuiltinBody.Add(ToKey(typeof(SZArrayHelper<>).GetMethod(nameof(SZArrayHelper<object>.CopyTo))), types => $@"{'\t'}if (!a_1) throw std::runtime_error(""ArgumentNullException"");
{'\t'}if (a_2 < 0) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}if (a_2 + a_0->v__length > a_1->v__length) throw std::out_of_range(""ArgumentException"");
{'\t'}std::copy_n(a_0->f__data(), a_0->v__length, a_1->f__data() + a_2);
");
            genericMethodToBuiltinBody.Add(ToKey(typeof(SZArrayHelper<>).GetMethod(nameof(SZArrayHelper<object>.GetEnumerator))), types => $@"{'\t'}auto p = f__new<{Escape(typeof(SZArrayHelper<>).GetNestedType(nameof(SZArrayHelper<object>.Enumerator)).MakeGenericType(types))}>();
{'\t'}p->v_array = a_0;
{'\t'}p->v_index = -1;
{'\t'}return p;
");
            genericMethodToBuiltinBody.Add(ToKey(typeof(SZArrayHelper<>).GetMethod(nameof(SZArrayHelper<object>.IndexOf))), types =>
            {
                var method = typeof(Array).GetMethod(nameof(Array.IndexOf), new[] { types[0].MakeArrayType(), types[0] });
                queuedMethods.Enqueue(method);
                return $"\treturn {Escape(method)}(a_0, a_1);\n";
            });
            genericMethodToBuiltinBody.Add(ToKey(typeof(Func<,>).GetConstructor(new[] { typeof(object), typeof(IntPtr) })), types =>
            {
                var type = Escape(typeof(Func<,>).MakeGenericType(types));
                return $@"{'\t'}auto p = f__new<{type}>();
{'\t'}std::fill_n(reinterpret_cast<char*>(p) + sizeof(t__type*), sizeof({type}) - sizeof(t__type*), '\0');
{'\t'}p->v__5ftarget = a_0;
{'\t'}p->v__5fmethodPtr = a_1;
{'\t'}return p;
";
            });
            genericMethodToBuiltinBody.Add(ToKey(typeof(Func<,>).GetMethod("Invoke")), types => $"\treturn reinterpret_cast<{EscapeForVariable(types[1])}(*)({EscapeForVariable(typeof(object))}, {EscapeForVariable(types[0])})>(a_0->v__5fmethodPtr.v__5fvalue)(a_0->v__5ftarget, a_1);\n");
            methodToBuiltinBody.Add(ToKey(typeof(Exception).GetMethod("GetMessageFromNativeResources", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Exception).GetNestedType("ExceptionMessageKind", BindingFlags.NonPublic) }, null)), () => "\treturn f__string(u\"message from native resources\"sv);\n");
            methodToBuiltinBody.Add(ToKey(Type.GetType("System.Runtime.CompilerServices.JitHelpers").GetMethod("GetRawSzArrayData", BindingFlags.Static | BindingFlags.NonPublic)), () => "\treturn reinterpret_cast<uint8_t*>(a_0 + 1);\n");
            methodToBuiltinBody.Add(ToKey(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("get_OffsetToStringData")), () => $"\treturn sizeof({Escape(typeof(string))});\n");
            {
                var method = Type.GetType("System.ByReference`1[System.Char]").GetConstructor(new[] { typeof(char).MakeByRefType() });
                methodToBuiltinBody.Add(ToKey(method), () => $"\treturn {{{Escape(typeof(IntPtr))}::t_value{{a_0}}}};\n");
            }
            {
                var methods = Type.GetType("Internal.Runtime.CompilerServices.Unsafe").GetMethods().Where(x => x.IsGenericMethodDefinition);
                {
                    var method = methods.First(x => x.Name == "Add" && x.GetGenericArguments().Length == 1);
                    methodToBuiltinBody.Add(ToKey(method.MakeGenericMethod(typeof(char))), () => "\treturn a_0 + a_1;\n");
                }
                {
                    var method = methods.First(x => x.Name == "As" && x.GetGenericArguments().Length == 2);
                    methodToBuiltinBody.Add(ToKey(method.MakeGenericMethod(typeof(byte), typeof(char))), () => "\treturn reinterpret_cast<char16_t*>(a_0);\n");
                }
            }
            // TODO: tentative
            methodTreeToBuiltinBody.Add(ToKey(typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object) })), type => "\treturn a_0 == a_1;\n");
            methodTreeToBuiltinBody.Add(ToKey(typeof(object).GetMethod(nameof(object.ToString))), type => $"\treturn f__string(u\"{type}\"sv);\n");
            methodTreeToBuiltinBody.Add(ToKey(typeof(ValueType).GetMethod(nameof(object.Equals))), type =>
            {
                var identifier = Escape(type);
                return $"\treturn a_1 && a_1->v__type->f__is(&t__type_of<{identifier}>::v__instance) && std::memcmp(a_0, &static_cast<{identifier}*>(a_1)->v__value, sizeof({identifier})) == 0;\n";
            });
            methodToBuiltinBody.Add(ToKey(typeof(int).GetMethod(nameof(object.ToString), Type.EmptyTypes)), () => $"\treturn f__string(f__u16string(std::to_string(*a_0)));\n");
            methodToBuiltinBody.Add(ToKey(typeof(int).GetMethod(nameof(object.ToString), new[] { typeof(string), typeof(IFormatProvider) })), () => $"\treturn f__string(f__u16string(std::to_string(*a_0)));\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod(nameof(object.ToString), Type.EmptyTypes)), null);
            methodToBuiltinBody.Add(ToKey(typeof(System.Text.StringBuilder).GetMethod(nameof(object.ToString), Type.EmptyTypes)), null);
            methodTreeToBuiltinBody.Add(ToKey(typeof(Exception).GetMethod(nameof(object.ToString))), type => $"\treturn f__string(u\"{type}\"sv);\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod("FastAllocateString", BindingFlags.Static | BindingFlags.NonPublic)), () => $@"{'\t'}auto p = f__new_sized<{Escape(typeof(string))}>(sizeof(char16_t) * a_0);
{'\t'}p->v__length = a_0;
{'\t'}return p;
");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod("FillStringChecked", BindingFlags.Static | BindingFlags.NonPublic)), () => $@"{'\t'}if (a_1 < 0 || a_1 + a_2->v__length > a_0->v__length) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}std::copy_n(reinterpret_cast<char16_t*>(a_2 + 1), a_2->v__length, reinterpret_cast<char16_t*>(a_0 + 1) + a_1);
");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod("wstrcpy", BindingFlags.Static | BindingFlags.NonPublic)), () => $"\tstd::copy_n(a_1, a_2, a_0);\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod("GetRawStringData", BindingFlags.Instance | BindingFlags.NonPublic)), () => $"\treturn reinterpret_cast<char16_t*>(a_0 + 1);\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod("EqualsHelper", BindingFlags.Static | BindingFlags.NonPublic)), () => $@"{'\t'}auto p = reinterpret_cast<char16_t*>(a_0 + 1);
{'\t'}return std::equal(p, p + a_0->v__length, reinterpret_cast<char16_t*>(a_1 + 1));
");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod("InternalSubString", BindingFlags.Instance | BindingFlags.NonPublic)), () => $@"{'\t'}auto p = f__new_sized<{Escape(typeof(string))}>(sizeof(char16_t) * a_2);
{'\t'}p->v__length = a_2;
{'\t'}std::copy_n(reinterpret_cast<char16_t*>(a_0 + 1) + a_1, a_2, reinterpret_cast<char16_t*>(p + 1));
{'\t'}return p;
");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetConstructor(new[] { typeof(ReadOnlySpan<char>) })), () => $"\treturn f__string(std::u16string_view(static_cast<char16_t*>(a_0.v__5fpointer.v__5fvalue.v__5fvalue), a_0.v__5flength));\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetProperty(nameof(string.Length)).GetMethod), () => "\treturn a_0->v__length;\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetProperty("Chars").GetMethod), () => "\treturn reinterpret_cast<const char16_t*>(a_0 + 1)[a_1];\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod(nameof(object.Equals), new[] { typeof(object) })), () =>
            {
                var method = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) });
                queuedMethods.Enqueue(method);
                return $"\treturn a_1 && a_1->v__type->f__is(&t__type_of<{Escape(typeof(string))}>::v__instance) && {Escape(method)}(a_0, static_cast<{EscapeForVariable(typeof(string))}>(a_1));\n";
            });
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string), typeof(StringComparison) })), () =>
            {
                var method = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) });
                queuedMethods.Enqueue(method);
                return $"\treturn {Escape(method)}(a_0, a_1);\n";
            });
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod(nameof(string.Join), new[] { typeof(string), typeof(object[]) })), () => $"\treturn f__string(u\"join\"sv);\n");
            methodToBuiltinBody.Add(ToKey(typeof(string).GetMethod(nameof(string.ToLowerInvariant), Type.EmptyTypes)), () => $@"{'\t'}auto n = a_0->v__length;
{'\t'}auto p = f__new_sized<{Escape(typeof(string))}>(sizeof(char16_t) * n);
{'\t'}p->v__length = n;
{'\t'}auto q = reinterpret_cast<char16_t*>(a_0 + 1);
{'\t'}std::transform(q, q + n, reinterpret_cast<char16_t*>(p + 1), [](auto x)
{'\t'}{{
{'\t'}{'\t'}return x >= u'A' && x <= u'Z' ? x + (u'a' - u'A') : x;
{'\t'}}});
{'\t'}return p;
");
            methodToBuiltinBody.Add(ToKey(Type.GetType("System.SR, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e").GetMethod("InternalGetResourceString", BindingFlags.Static | BindingFlags.NonPublic)), () => "\treturn a_0;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Environment).GetProperty(nameof(Environment.CurrentManagedThreadId)).GetMethod), () => "\treturn 0;\n");
            methodToBuiltinBody.Add(ToKey(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })), () => $@"{'\t'}auto p = static_cast<{Escape(typeof(string))}*>(a_0);
{'\t'}std::mbstate_t state{{}};
{'\t'}char cs[MB_LEN_MAX];
{'\t'}for (auto c : std::u16string_view(reinterpret_cast<const char16_t*>(p + 1), p->v__length)) {{
{'\t'}{'\t'}auto n = std::c16rtomb(cs, c, &state);
{'\t'}{'\t'}if (n != size_t(-1)) std::cout << std::string_view(cs, n);
{'\t'}}}
{'\t'}auto n = std::c16rtomb(cs, u'\0', &state);
{'\t'}if (n != size_t(-1) && n > 1) std::cout << std::string_view(cs, n - 1);
{'\t'}std::cout << std::endl;
");
            // TODO: tentative
            methodNameToBuiltinBody.Add("System.String ToString(System.String, System.IFormatProvider)", method => $"\treturn f__string(u\"{method.ReflectedType}\"sv);\n");
            var tryFormat = $@"*a_2 = 0;
{'\t'}return false;
";
            methodNameToBuiltinBody.Add("Boolean TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)", method => tryFormat);
            methodNameToBuiltinBody.Add("Boolean System.ISpanFormattable.TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)", method => tryFormat);
            writer = functionDefinitions;
        }
        private void WriteTypeDefinition(Type type, MethodTable definition, TextWriter writer)
        {
            var identifier = Escape(type);
            writer.Write($@"
template<>
struct t__type_of<{identifier}> : ");
            if (definition is TypeDefinition td)
            {
                void writeMethods(IEnumerable<MethodInfo> methods, Func<MethodInfo, MethodInfo> origin, string indent)
                {
                    foreach (var (m, i) in methods.Select((x, i) => (x, i))) writer.WriteLine($"{indent}// {m}\n{indent}void* v_method{i} = {(m.IsAbstract ? "nullptr" : m.IsGenericMethod ? $"&v_generic__{Escape(m)}" : methodToIdentifier.TryGetValue(ToKey(m), out var name) ? $"reinterpret_cast<void*>(&{name}{(m.DeclaringType.IsValueType ? "__v" : string.Empty)})" : "nullptr")};");
                    foreach (var m in methods.Where(x => !x.IsAbstract && x.IsGenericMethod)) writer.WriteLine($@"{indent}struct
{indent}{{
{
    string.Join(string.Empty, genericMethodToTypesToIndex[ToKey(origin(m))].OrderBy(x => x.Value).Select(p =>
    {
        var x = m.MakeGenericMethod(p.Key);
        return $"{indent}\t// {x}\n{indent}\tvoid* v_method{p.Value} = reinterpret_cast<void*>(&{Escape(x)}{(x.DeclaringType.IsValueType ? "__v" : string.Empty)});\n";
    }))
}{indent}}} v_generic__{Escape(m)};");
                }
                writer.WriteLine(@"t__type
{");
                writeMethods(td.Methods, x => x.GetBaseDefinition(), "\t");
                foreach (var p in td.InterfaceToMethods)
                {
                    writer.WriteLine($@"{'\t'}struct
{'\t'}{{");
                    var ms = typeToDefinition[p.Key].Methods;
                    writeMethods(p.Value, x => ms[Array.IndexOf(p.Value, x)], "\t\t");
                    writer.WriteLine($"\t}} v_interface__{Escape(p.Key)};");
                }
                writer.WriteLine("\tt__type_of();");
                memberDefinitions.Write($@"
t__type_of<{identifier}>::t__type_of() : t__type({(type.BaseType == null ? "nullptr" : $"&t__type_of<{Escape(type.BaseType)}>::v__instance")}, {{{
    string.Join(",", td.InterfaceToMethods.Select(p => $"\n\t{{&t__type_of<{Escape(p.Key)}>::v__instance, reinterpret_cast<void**>(&v_interface__{Escape(p.Key)})}}"))
}
}}, sizeof({EscapeForVariable(type)}){(type.IsArray ? $", &t__type_of<{Escape(GetElementType(type))}>::v__instance, {type.GetArrayRank()}" : string.Empty)})
{{
}}");
            }
            else
            {
                writer.WriteLine($@"{Escape(typeof(Type))}
{{");
            }
            writer.WriteLine($@"{'\t'}static t__type_of v__instance;
}};");
            memberDefinitions.WriteLine($"\nt__type_of<{identifier}> t__type_of<{identifier}>::v__instance;");
        }
        public void Do(MethodInfo method, TextWriter writer)
        {
            Define(typeof(Type));
            memberDefinitions.WriteLine($@"
{Escape(typeof(Type))}::{Escape(typeof(Type))}()
{{
{'\t'}v__type = &t__type_of<{Escape(typeof(Type))}>::v__instance;
}}
");
            typeDefinitions.WriteLine($@"
struct t__type : {Escape(typeof(Type))}
{{
{'\t'}t__type* v__base;
{'\t'}std::map<{EscapeForVariable(typeof(Type))}, void**> v__interface_to_methods;
{'\t'}size_t v__size;
{'\t'}t__type* v__element;
{'\t'}size_t v__rank;

{'\t'}t__type(t__type* a_base, std::map<{EscapeForVariable(typeof(Type))}, void**>&& a_interface_to_methods, size_t a_size, t__type* a_element = nullptr, size_t a_rank = 0) : v__base(a_base), v__interface_to_methods(std::move(a_interface_to_methods)), v__size(a_size), v__element(a_element), v__rank(a_rank)
{'\t'}{{
{'\t'}}}
{'\t'}bool f__is(t__type* a_type) const
{'\t'}{{
{'\t'}{'\t'}auto p = this;
{'\t'}{'\t'}do {{
{'\t'}{'\t'}{'\t'}if (p == a_type) return true;
{'\t'}{'\t'}{'\t'}p = p->v__base;
{'\t'}{'\t'}}} while (p);
{'\t'}{'\t'}return false;
{'\t'}}}
{'\t'}void** f__implementation({EscapeForVariable(typeof(Type))} a_interface) const
{'\t'}{{
{'\t'}{'\t'}auto i = v__interface_to_methods.find(a_interface);
{'\t'}{'\t'}return i == v__interface_to_methods.end() ? nullptr : i->second;
{'\t'}}}
}};

template<typename T>
struct t__type_of;

template<typename T, typename... T_an>
T* f__new(T_an&&... a_n)
{{
{'\t'}auto p = new T(std::forward<T_an>(a_n)...);
{'\t'}p->v__type = &t__type_of<T>::v__instance;
{'\t'}return p;
}}

template<typename T, typename... T_an>
T* f__new_sized(size_t a_extra, T_an&&... a_n)
{{
{'\t'}auto p = new(new char[sizeof(T) + a_extra]) T(std::forward<T_an>(a_n)...);
{'\t'}p->v__type = &t__type_of<T>::v__instance;
{'\t'}return p;
}}");
            Define(typeof(IntPtr));
            Define(typeof(UIntPtr));
            var fas = typeof(string).GetMethod("FastAllocateString", BindingFlags.Static | BindingFlags.NonPublic);
            queuedMethods.Enqueue(fas);
            functionDefinitions.WriteLine($@"
{EscapeForVariable(typeof(string))} f__string(std::u16string_view a_value)
{{
{'\t'}auto p = {Escape(fas)}(a_value.size());
{'\t'}std::copy(a_value.begin(), a_value.end(), reinterpret_cast<char16_t*>(p + 1));
{'\t'}return p;
}}");
            queuedMethods.Enqueue(method);
            do
            {
                Do();
                while (queuedTypes.Count > 0) Define(queuedTypes.Dequeue());
            }
            while (queuedMethods.Count > 0);
            writer.WriteLine($@"#include <algorithm>
#include <exception>
#include <iostream>
#include <limits>
#include <map>
#include <stdexcept>
#include <utility>
#include <climits>
#include <cstdint>
#include <cstring>
#include <cuchar>

namespace il2cxx
{{

using namespace std::literals;

template<typename T>
struct t__finally
{{
{'\t'}T v_f;

{'\t'}~t__finally()
{'\t'}{{
{'\t'}{'\t'}v_f();
{'\t'}}}
}};

template<typename T>
t__finally<T> f__finally(T&& a_f)
{{
{'\t'}return {{{{std::move(a_f)}}}};
}}

using t_managed_pointer = void*;

struct t__type;

std::u16string f__u16string(const std::string& a_x)
{{
{'\t'}std::vector<char16_t> cs;
{'\t'}std::mbstate_t state{{}};
{'\t'}auto p = a_x.c_str();
{'\t'}auto q = p + a_x.size() + 1;
{'\t'}while (p < q) {{
{'\t'}{'\t'}char16_t c;
{'\t'}{'\t'}auto n = std::mbrtoc16(&c, p, q - p, &state);
{'\t'}{'\t'}switch (n) {{
{'\t'}{'\t'}case size_t(-3):
{'\t'}{'\t'}{'\t'}cs.push_back(c);
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}case size_t(-2):
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}case size_t(-1):
{'\t'}{'\t'}{'\t'}++p;
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}case 0:
{'\t'}{'\t'}{'\t'}cs.push_back('\0');
{'\t'}{'\t'}{'\t'}++p;
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}default:
{'\t'}{'\t'}{'\t'}cs.push_back(c);
{'\t'}{'\t'}{'\t'}p += n;
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}}}
{'\t'}}}
{'\t'}return {{cs.data(), cs.size() - 1}};
}}
");
            writer.Write(typeDeclarations);
            writer.Write(typeDefinitions);
            writer.Write(functionDeclarations);
            foreach (var p in typeToDefinition) WriteTypeDefinition(p.Key, p.Value, writer);
            writer.Write(memberDefinitions);
            writer.WriteLine(functionDefinitions);
            writer.WriteLine($@"}}

int main(int argc, char* argv[])
{{
{'\t'}using namespace il2cxx;
{string.Join(string.Empty, typeToDefinition.Keys.Select(x => typeToBuiltinInitialize.TryGetValue(x, out var y) ? y : null).Where(x => x != null).Select(x => x()))}
{string.Join(string.Empty, typeToDefinition.Keys.Select(x => x.TypeInitializer).Where(x => x != null).Select(x => $"\t{Escape(x)}();\n"))}
{'\t'}return {Escape(method)}();
}}");
        }
    }
}
