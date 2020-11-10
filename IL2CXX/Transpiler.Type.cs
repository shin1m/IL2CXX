﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace IL2CXX
{
    using static MethodKey;

    partial class Transpiler
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        class RuntimeDefinition : IEqualityComparer<Type[]>
        {
            bool IEqualityComparer<Type[]>.Equals(Type[] x, Type[] y) => x.SequenceEqual(y);
            int IEqualityComparer<Type[]>.GetHashCode(Type[] x) => x.Select(y => y.GetHashCode()).Aggregate((y, z) => y % z);

            public readonly Type Type;
            public bool IsManaged = false;
            public readonly List<MethodInfo> Methods = new List<MethodInfo>();
            public readonly Dictionary<MethodKey, int> MethodToIndex = new Dictionary<MethodKey, int>();

            public RuntimeDefinition(Type type) => Type = type;
            protected void Add(MethodInfo method, Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex)
            {
                var key = ToKey(method);
                MethodToIndex.Add(key, Methods.Count);
                Methods.Add(method);
                if (method.IsGenericMethod) genericMethodToTypesToIndex.Add(key, new Dictionary<Type[], int>(this));
            }
            protected virtual int GetIndex(MethodKey method) => throw new NotSupportedException();
            public int GetIndex(MethodBase method) => GetIndex(ToKey(method));
        }
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

        private readonly StringWriter typeDeclarations = new StringWriter();
        private readonly StringWriter typeDefinitions = new StringWriter();
        private readonly StringWriter staticDefinitions = new StringWriter();
        private readonly StringWriter staticMembers = new StringWriter();
        private readonly StringWriter threadStaticMembers = new StringWriter();
        private readonly StringWriter fieldDeclarations = new StringWriter();
        private readonly StringWriter fieldDefinitions = new StringWriter();
        private readonly List<RuntimeDefinition> runtimeDefinitions = new List<RuntimeDefinition>();
        private readonly Dictionary<Type, RuntimeDefinition> typeToRuntime = new Dictionary<Type, RuntimeDefinition>();
        private readonly Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex = new Dictionary<MethodKey, Dictionary<Type[], int>>();
        private bool processed;

        private RuntimeDefinition Define(Type type)
        {
            if (typeToRuntime.TryGetValue(type, out var definition)) return definition;
            if (processed) throw new InvalidOperationException($"{type}");
            if (type.IsByRef || type.IsPointer)
            {
                definition = new RuntimeDefinition(type);
                typeToRuntime.Add(type, definition);
                queuedTypes.Enqueue(GetElementType(type));
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
                                    ? $@"{variables("\t\t")}
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{string.Join(string.Empty, fields.Where(x => IsComposite(x.FieldType)).Select(x => $"\t\t\t{Escape(x)}.f__destruct();\n"))}{'\t'}{'\t'}}}
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
            writerForDefinitions.WriteLine($@"}}, &{assembly}, u""{type.Namespace}""sv, u""{type.Name}""sv, u""{type}""sv, {(
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
{'\t'}return q;");
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
{'\t'}return p;");
                }
                writerForDefinitions.WriteLine('}');
            }
        }
    }
}