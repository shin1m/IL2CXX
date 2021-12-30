using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace IL2CXX
{
    using static MethodKey;

    partial class Transpiler
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags exactInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding;

        private static Type ReplaceGenericMethodParameter(Type type)
        {
            if (type.IsGenericMethodParameter) return Type.MakeGenericMethodParameter(type.GenericParameterPosition);
            if (type.IsSZArray) return ReplaceGenericMethodParameter(type.GetElementType()).MakeArrayType();
            if (type.IsArray) return ReplaceGenericMethodParameter(type.GetElementType()).MakeArrayType(type.GetArrayRank());
            if (type.IsByRef) return ReplaceGenericMethodParameter(type.GetElementType()).MakeByRefType();
            if (type.IsPointer) return ReplaceGenericMethodParameter(type.GetElementType()).MakePointerType();
            if (type.IsGenericTypeParameter) return type;
            if (type.ContainsGenericParameters) return type.GetGenericTypeDefinition().MakeGenericType(type.GetGenericArguments().Select(ReplaceGenericMethodParameter).ToArray());
            return type;
        }
        public static MethodInfo GetBaseDefinition(MethodInfo method)
        {
            if (method.ReflectedType != method.DeclaringType) throw new InvalidOperationException();
            if (!method.IsVirtual || method.DeclaringType.IsInterface) return method;
            var name = method.Name;
            var g = method.GetGenericArguments().Length;
            var ps = method.GetParameters().Select(x => x.ParameterType).Select(ReplaceGenericMethodParameter).ToArray();
            MethodInfo get(Type t) => t.GetMethod(name, g, exactInstance, null, ps, null);
            while (!method.Attributes.HasFlag(MethodAttributes.NewSlot)) method = get(get(method.DeclaringType.BaseType).DeclaringType);
            return method;
        }
        private MethodInfo GetConcrete(MethodInfo method, Type type)
        {
            var ct = (TypeDefinition)Define(type);
            var methods = method.DeclaringType.IsInterface ? ct.InterfaceToMethods[method.DeclaringType] : (IReadOnlyList<MethodInfo>)ct.Methods;
            var dt = Define(method.DeclaringType);
            return method.IsGenericMethod
                ? methods[dt.GetIndex(method.GetGenericMethodDefinition())].MakeGenericMethod(method.GetGenericArguments())
                : methods[dt.GetIndex(method)];
        }
        private static string ExplicitName(Type type)
        {
            if (type.IsGenericTypeParameter) return type.Name;
            var prefix = type.IsNested ? $"{ExplicitName(type.DeclaringType)}." : type.Namespace == null ? string.Empty : $"{type.Namespace}.";
            if (!type.IsGenericType) return prefix + type.Name;
            var name = type.GetGenericTypeDefinition().Name;
            var i = name.IndexOf('`');
            return $"{prefix}{(i < 0 ? name : name.Substring(0, i))}<{string.Join(",", type.GetGenericArguments().Select(ExplicitName))}>";
        }
        private InterfaceMapping GetInterfaceMap(Type type, Type @interface)
        {
            var ims = @interface.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var tms = new MethodInfo[ims.Length];
            for (var i = 0; i < ims.Length; ++i)
            {
                var m = ims[i];
                var g = m.GetGenericArguments().Length;
                var ps = m.GetParameters().Select(x => x.ParameterType).Select(ReplaceGenericMethodParameter).ToArray();
                MethodInfo get(Type t, string name)
                {
                    var m = t.GetMethod(name, g, exactInstance, null, ps, null);
                    return m == null || m.DeclaringType == t ? m : get(m.DeclaringType, name);
                }
                var t = type;
                do
                {
                    var prefix = ExplicitName(t.IsGenericType
                        ? t.GetGenericTypeDefinition().GetInterfaces()[Array.IndexOf(t.GetInterfaces(), @interface)]
                        : @interface
                    );
                    if ((tms[i] = get(t, $"{prefix}.{m.Name}") ?? get(t, m.Name)) != null) break;
                }
                while ((t = t.BaseType) != null);
                if (tms[i] == null) throw new Exception($"{type} -> {@interface} {m}");
            }
            return new InterfaceMapping
            {
                InterfaceType = @interface,
                InterfaceMethods = ims,
                TargetType = type,
                TargetMethods = tms
            };
        }
        private byte GetCorElementType(Type type) => (byte)(Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => 0x2,
            TypeCode.Char => 0x3,
            TypeCode.SByte => 0x4,
            TypeCode.Byte => 0x5,
            TypeCode.Int16 => 0x6,
            TypeCode.UInt16 => 0x7,
            TypeCode.Int32 => 0x8,
            TypeCode.UInt32 => 0x9,
            TypeCode.Int64 => 0xa,
            TypeCode.UInt64 => 0xb,
            TypeCode.Single => 0xc,
            TypeCode.Double => 0xd,
            //TypeCode.String => 0xe,
            _ =>
                type == typeofVoid ? 0x1 :
                type.IsPointer ? 0xf :
                type == typeofIntPtr ? 0x18 :
                type == typeofUIntPtr ? 0x19 :
                type.IsValueType ? 0x11 :
                type.IsSZArray ? 0x1d :
                type.IsArray ? 0x14 : 0x1c
        });

        public class RuntimeDefinition : IEqualityComparer<Type[]>
        {
            bool IEqualityComparer<Type[]>.Equals(Type[] x, Type[] y) => x.SequenceEqual(y);
            int IEqualityComparer<Type[]>.GetHashCode(Type[] x) => x.Select(y => y.GetHashCode()).Aggregate((y, z) => y % z);

            public readonly Type Type;
            public bool IsManaged;
            public bool IsBlittable;
            public int Alignment;
            public int UnmanagedSize;
            public bool HasUnmanaged;
            public bool IsMarshallable => IsBlittable || HasUnmanaged || Type.GetTypeCode(Type) == TypeCode.Boolean;
            public readonly List<MethodInfo> Methods = new();
            public readonly Dictionary<MethodKey, int> MethodToIndex = new();
            public readonly StringWriter Definitions = new();
            public bool HasMethods;
            public bool HasProperties;
            public string Attributes;

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
                foreach (var x in Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) Add(x, genericMethodToTypesToIndex);
            }
            protected override int GetIndex(MethodKey method) => MethodToIndex[method];
        }
        class TypeDefinition : RuntimeDefinition
        {
            public readonly TypeDefinition Base;
            public readonly Dictionary<Type, MethodInfo[]> InterfaceToMethods = new();
            public readonly string Delegate;
            public bool HasFields;
            public bool HasConstructors;
            public List<bool> ExplicitMap;

            public TypeDefinition(Type type, Transpiler transpiler) : base(type)
            {
                IsManaged = !Type.IsValueType;
                if (Type.IsPrimitive)
                {
                    if (Type == transpiler.typeofBoolean)
                    {
                        UnmanagedSize = transpiler.Define(transpiler.typeofInt32).UnmanagedSize;
                    }
                    else if (Type != transpiler.typeofChar)
                    {
                        IsBlittable = true;
                        UnmanagedSize = Type == transpiler.typeofIntPtr || Type == transpiler.typeofUIntPtr
                            ? transpiler.Is64Bit ? 8 : 4
                            : Marshal.SizeOf(Type.GetType(Type.ToString(), true));
                    }
                }
                else if (Type.IsEnum)
                {
                    IsBlittable = true;
                    UnmanagedSize = transpiler.Define(Type.GetEnumUnderlyingType()).UnmanagedSize;
                }
                Alignment = UnmanagedSize;
                if (Type.BaseType != null)
                {
                    Base = (TypeDefinition)transpiler.Define(Type.BaseType);
                    Methods.AddRange(Base.Methods);
                }
                foreach (var x in Type.GetMethods(declaredAndInstance).Where(x => x.IsVirtual))
                {
                    var i = GetIndex(GetBaseDefinition(x));
                    if (i < 0)
                        Add(x, transpiler.genericMethodToTypesToIndex);
                    else
                        Methods[i] = x;
                }
                // TODO: Work around for szarray bug.
                var interfaces = Type.GetInterfaces();
                if (Type.IsSZArray)
                {
                    var roc = transpiler.getType(typeof(IReadOnlyCollection<>)).MakeGenericType(new[] { transpiler.GetElementType(Type) });
                    if (!interfaces.Contains(roc)) interfaces = interfaces.Append(roc).ToArray();
                }
                foreach (var x in interfaces)
                {
                    var definition = (InterfaceDefinition)transpiler.Define(x);
                    var methods = new MethodInfo[definition.Methods.Count];
                    var map = transpiler.GetInterfaceMap(Type.IsArray && x.IsGenericType ? transpiler.typeofSZArrayHelper.MakeGenericType(transpiler.GetElementType(Type)) : Type, x);
                    foreach (var (i, t) in map.InterfaceMethods.Zip(map.TargetMethods, (i, t) => (i, t))) methods[definition.GetIndex(i)] = t;
                    InterfaceToMethods.Add(x, methods);
                }
                if (Type.IsSubclassOf(transpiler.typeofDelegate) && Type != transpiler.typeofMulticastDelegate)
                {
                    var invoke = (MethodInfo)Type.GetMethod("Invoke");
                    transpiler.Enqueue(invoke);
                    var @return = invoke.ReturnType;
                    var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                    string generate(Type t, string body) => $@"reinterpret_cast<void*>(+[]({
    string.Join(",", parameters.Prepend(t).Select((x, i) => $"\n\t\t{transpiler.EscapeForArgument(x)} a_{i}"))
}
{'\t'}) -> {transpiler.EscapeForStacked(@return)}
{'\t'}{{
{body}{'\t'}}})";
                    string call(string @this) => $"{transpiler.Escape(invoke)}({string.Join(", ", parameters.Select((_, i) => $"a_{i + 1}").Prepend(transpiler.CastValue(Type, @this)))});";
                    Delegate = $@"{'\t'}v__multicast_invoke = {generate(transpiler.typeofMulticastDelegate, $@"{'\t'}{'\t'}auto xs = static_cast<{transpiler.Escape(transpiler.typeofObject.MakeArrayType())}*>(a_0->v__5finvocationList)->f_data();
{'\t'}{'\t'}auto n = static_cast<intptr_t>(a_0->v__5finvocationCount) - 1;
{'\t'}{'\t'}for (intptr_t i = 0; i < n; ++i) {call("xs[i]")};
{'\t'}{'\t'}return {call("xs[n]")};
")};
";
                    bool arrayElementIsBlittable(Type x) => !IsComposite(x) || x.IsValueType && transpiler.Define(x).IsBlittable;
                    if ((@return == transpiler.typeofVoid ? parameters : parameters.Prepend(@return)).All(x => !IsComposite(x) || x == transpiler.typeofString || x == transpiler.typeofStringBuilder || transpiler.typeofSafeHandle.IsAssignableFrom(x) || x.IsArray && arrayElementIsBlittable(transpiler.GetElementType(x)) || transpiler.Define(x).IsMarshallable))
                    {
                        using var writer = new StringWriter();
                        transpiler.GenerateInvokeUnmanaged(@return, invoke.GetParameters().Select((x, i) => (x, i + 1)), "a_0->v__5fmethodPtrAux.v__5fvalue", writer);
                        Delegate += $"{'\t'}v__invoke_unmanaged = {generate(Type, writer.ToString())};\n";
                    }
                }
            }
            protected override int GetIndex(MethodKey method) => MethodToIndex.TryGetValue(method, out var i) ? i : Base?.GetIndex(method) ?? -1;
        }

        private readonly StringWriter typeDeclarations = new();
        private readonly StringWriter typeDefinitions = new();
        private readonly StringWriter staticDefinitions = new();
        private readonly StringWriter staticMembers = new();
        private readonly StringWriter threadStaticMembers = new();
        private readonly StringWriter fieldDeclarations = new();
        private readonly List<RuntimeDefinition> runtimeDefinitions = new();
        private readonly Dictionary<Type, RuntimeDefinition> typeToRuntime = new();
        private readonly Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex = new();
        private bool processed;

        private IEnumerable<MethodInfo> GetMethods(Type type) => type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | (type.IsInterface ? BindingFlags.Default : BindingFlags.Static) | BindingFlags.Public | BindingFlags.NonPublic).Where(x => !invalids.Contains(x.ReturnType.FullName));
        private IEnumerable<PropertyInfo> GetProperties(Type type) => type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | (type.IsInterface ? BindingFlags.Default : BindingFlags.Static) | BindingFlags.Public | BindingFlags.NonPublic).Where(x => !invalids.Contains(x.PropertyType.FullName));
        public RuntimeDefinition Define(Type type)
        {
            type = Normalize(type);
            if (typeToRuntime.TryGetValue(type, out var definition)) return definition;
            ThrowIfInvalid(type);
            if (processed) throw new InvalidOperationException($"{type}");
            if (type.IsGenericType)
            {
                var gtd = type.GetGenericTypeDefinition();
                if (ShouldGenerateReflection(gtd))
                {
                    Enqueue(gtd);
                    foreach (var x in type.GetGenericArguments()) Enqueue(x);
                }
            }
            byte[] getBytes(object value) => value switch
            {
                byte b => new[] { b },
                sbyte b => new[] { (byte)b },
                _ => (byte[])typeof(BitConverter).GetMethod(nameof(BitConverter.GetBytes), new[] { value.GetType() }).Invoke(null, new[] { value })
            };
            string writeAttributes(MemberInfo member, string name)
            {
                var attributes = member.GetCustomAttributesData();
                if (attributes.Count <= 0) return "t__custom_attribute::v_empty_attributes";
                string attributeName(int i) => $"v__attributes_{name}__a{i}";
                string attributeData(CustomAttributeTypedArgument a, string name)
                {
                    var type = a.ArgumentType;
                    if (type == typeofString) return $"const_cast<char16_t*>({ToLiteral((string)a.Value)})";
                    if (type == typeofType) return $"&t__type_of<{Escape((Type)a.Value)}>::v__instance";
                    if (type.IsSZArray)
                    {
                        var elements = (IReadOnlyCollection<CustomAttributeTypedArgument>)a.Value;
                        var e = type.GetElementType();
                        if (e == typeofString)
                            definition.Definitions.WriteLine($@"static const char16_t* {name}__elements[] = {{{string.Join(", ", elements.Select(x => ToLiteral((string)x.Value)))}}};
static std::pair<size_t, const char16_t**> {name}__data{{{elements.Count}, {name}__elements}};");
                        else if (e == typeofType)
                            definition.Definitions.WriteLine($@"static t__type* {name}__elements[] = {{{string.Join(", ", elements.Select(x => $"&t__type_of<{Escape((Type)x.Value)}>::v__instance"))}}};
static std::pair<size_t, t__type**> {name}__data{{{elements.Count}, {name}__elements}};");
                        else
                            definition.Definitions.WriteLine($@"static uint8_t {name}__elements[] = {{{string.Join(", ", elements.SelectMany(x => getBytes(x.Value)).Select(x => $"0x{x:x02}"))}}};
static std::pair<size_t, uint8_t*> {name}__data{{{elements.Count}, {name}__elements}};");
                        return $"&{name}__data";
                    }
                    definition.Definitions.WriteLine($"static uint8_t {name}__data[] = {{{string.Join(", ", getBytes(a.Value).Select(x => $"0x{x:x02}"))}}};");
                    return $"{name}__data";
                }
                foreach (var (x, i) in attributes.Select((x, i) => (x, i)))
                {
                    Enqueue(x.AttributeType);
                    var aname = attributeName(i);
                    var cas = "t__custom_attribute::v_empty_cas";
                    if (x.ConstructorArguments.Count > 0)
                    {
                        cas = $"{aname}__cas";
                        foreach (var (y, j) in x.ConstructorArguments.Select((x, i) => (x, i))) definition.Definitions.WriteLine($"static t__custom_attribute::t_typed {cas}{j}{{&t__type_of<{Escape(y.ArgumentType)}>::v__instance, {attributeData(y, $"{cas}{j}")}}};");
                        definition.Definitions.WriteLine($@"static t__custom_attribute::t_typed* {cas}[] = {{
{string.Join(string.Empty, x.ConstructorArguments.Select((_, i) => $"\t&{cas}{i},\n"))}{'\t'}nullptr
}};");
                    }
                    var nas = "t__custom_attribute::v_empty_nas";
                    if (x.NamedArguments.Count > 0)
                    {
                        nas = $"{aname}__nas";
                        foreach (var (y, j) in x.NamedArguments.Select((x, i) => (x, i))) definition.Definitions.WriteLine($"static t__custom_attribute::t_named {nas}{j}{{&t__type_of<{Escape(y.TypedValue.ArgumentType)}>::v__instance, {attributeData(y.TypedValue, $"{nas}{j}")}, &v__{(y.IsField ? "field" : "property")}_{Escape(y.MemberInfo.DeclaringType)}__{Escape(y.MemberInfo.Name)}}};");
                        definition.Definitions.WriteLine($@"static t__custom_attribute::t_named* {nas}[] = {{
{string.Join(string.Empty, x.NamedArguments.Select((_, i) => $"\t&{nas}{i},\n"))}{'\t'}nullptr
}};");
                    }
                    definition.Definitions.WriteLine($"static t__custom_attribute {aname}{{&v__method_{Escape(x.Constructor)}, {cas}, {nas}}};");
                }
                definition.Definitions.WriteLine($@"static t__custom_attribute* v__attributes_{name}[] = {{
{string.Join(string.Empty, attributes.Select((x, i) => $"\t&{attributeName(i)},\n"))}{'\t'}nullptr
}};");
                return $"v__attributes_{name}";
            }
            string writeParameters(ParameterInfo[] parameters, string name)
            {
                if (parameters.Length <= 0) return "t__runtime_parameter_info::v__empty_parameters";
                definition.Definitions.WriteLine();
                string parameterName(ParameterInfo x) => $"v__parameter_{name}__{(x.Name == null ? $"p{x.Position}" : Escape(x.Name))}";
                foreach (var x in parameters)
                {
                    var pname = parameterName(x);
                    var @default = "nullptr";
                    if (x.HasDefaultValue && x.RawDefaultValue != null)
                    {
                        @default = $"{pname}__default";
                        definition.Definitions.WriteLine($"static uint8_t {@default}[] = {{{string.Join(", ", GetDefaultValue(x).Select(y => $"0x{y:x02}"))}}};");
                    }
                    definition.Definitions.WriteLine($"static t__runtime_parameter_info {pname}{{{(int)x.Attributes}, &t__type_of<{Escape(x.ParameterType)}>::v__instance, {@default}}};");
                }
                definition.Definitions.WriteLine($@"static t__runtime_parameter_info* v__parameters_{name}[] = {{
{string.Join(string.Empty, parameters.Select(x => $"\t&{parameterName(x)},\n"))}{'\t'}nullptr
}};");
                return $"v__parameters_{name}";
            }
            if (type.ContainsGenericParameters)
            {
                if (type.BaseType != null) Define(type.BaseType);
                definition = new RuntimeDefinition(type);
                typeToRuntime.Add(type, definition);
                typeDeclarations.WriteLine($@"// {type.AssemblyQualifiedName}
struct {Escape(type)}
{{
}};");
            }
            else if (type.IsByRef || type.IsPointer)
            {
                definition = new RuntimeDefinition(type);
                if (type.IsPointer)
                {
                    definition.IsBlittable = true;
                    definition.Alignment = definition.UnmanagedSize = Define(typeofIntPtr).UnmanagedSize;
                }
                typeToRuntime.Add(type, definition);
                Enqueue(GetElementType(type));
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
                foreach (var m in td.Methods.Where(x => !x.IsAbstract)) enqueue(GetBaseDefinition(m), m);
                foreach (var p in td.InterfaceToMethods)
                {
                    var id = typeToRuntime[p.Key];
                    foreach (var m in id.Methods) enqueue(m, p.Value[id.GetIndex(m)]);
                }
                typeToRuntime[type] = definition = td;
                var identifier = Escape(type);
                var builtinStaticMembers = builtin.GetStaticMembers(this, type);
                var fields = Enumerable.Empty<FieldInfo>();
                var staticFields = new List<FieldInfo>();
                var threadStaticFields = new List<FieldInfo>();
                if (builtinStaticMembers == null)
                    foreach (var x in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        (x.GetCustomAttributesData().Any(x => x.AttributeType == typeofThreadStaticAttribute) ? threadStaticFields : staticFields).Add(x);
                var staticDefinitions = new StringWriter();
                var staticMembers = new StringWriter();
                var initialize = builtin.GetInitialize(this, type);
                void writeFields(IEnumerable<FieldInfo> fields, Func<FieldInfo, string> address)
                {
                    foreach (var x in fields)
                    {
                        var name = $"field_{identifier}__{Escape(x.Name)}";
                        fieldDeclarations.WriteLine($"extern t__runtime_field_info v__{name};");
                        td.Definitions.WriteLine($@"t__runtime_field_info v__{name}{{&t__type_of<t__runtime_field_info>::v__instance, &t__type_of<{identifier}>::v__instance, u""{x.Name}""sv, {(int)x.Attributes}, {writeAttributes(x, name)}, &t__type_of<{Escape(x.FieldType)}>::v__instance, +[](void* a_this) -> void*
{{
{address(x)}}}}};");
                    }
                }
                if (builtinStaticMembers != null || staticFields.Count > 0 || initialize != null || type.TypeInitializer != null)
                {
                    staticDefinitions.WriteLine($@"
struct t__static_{identifier}
{{");
                    if (builtinStaticMembers != null) staticDefinitions.WriteLine(builtinStaticMembers);
                    foreach (var x in staticFields)
                        if (x.Attributes.HasFlag(FieldAttributes.Literal) && x.GetRawConstantValue() is object value && value.GetType().IsPrimitive)
                        {
                            fieldDeclarations.WriteLine($"extern uint8_t v__field_{identifier}__{Escape(x.Name)}__literal[];");
                            td.Definitions.WriteLine($"uint8_t v__field_{identifier}__{Escape(x.Name)}__literal[] = {{{string.Join(", ", getBytes(value).Select(y => $"0x{y:x02}"))}}};");
                        }
                        else if (x.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
                        {
                            fieldDeclarations.WriteLine($"extern uint8_t v__field_{identifier}__{Escape(x.Name)}__data[];");
                            td.Definitions.WriteLine($"uint8_t v__field_{identifier}__{Escape(x.Name)}__data[] = {{{string.Join(", ", GetRVAData(x).Select(y => $"0x{y:x02}"))}}};");
                        }
                        else
                        {
                            staticDefinitions.WriteLine($"\t{EscapeForRoot(x.FieldType)} {Escape(x)}{{}};");
                        }
                    staticDefinitions.WriteLine($@"{'\t'}void f_initialize()
{'\t'}{{");
                    if (initialize != null) staticDefinitions.WriteLine(initialize);
                    foreach (var x in staticFields.Where(x => x.Attributes.HasFlag(FieldAttributes.Literal)))
                        if (x.GetRawConstantValue() is string value)
                        {
                            staticDefinitions.Write($"\t\t{Escape(x)} = ");
                            WriteNewString(staticDefinitions, value);
                            staticDefinitions.WriteLine(';');
                        }
                    if (type.TypeInitializer != null)
                    {
                        staticDefinitions.WriteLine($"\t\t{Escape(type.TypeInitializer)}();");
                        Enqueue(type.TypeInitializer);
                    }
                    staticDefinitions.WriteLine($@"{'\t'}}}
}};");
                    staticMembers.WriteLine($"\tt__lazy<t__static_{identifier}> v_{identifier};");
                    if (ShouldGenerateReflection(type))
                        writeFields(staticFields, x =>
                            x.Attributes.HasFlag(FieldAttributes.Literal) && (x.GetRawConstantValue()?.GetType().IsPrimitive ?? false) ? $"\treturn v__field_{identifier}__{Escape(x.Name)}__literal;\n" :
                            x.Attributes.HasFlag(FieldAttributes.HasFieldRVA) ? $"\treturn v__field_{identifier}__{Escape(x.Name)}__data;\n" :
                            $"\treturn &t_static::v_instance->v_{identifier}->{Escape(x)};\n"
                        );
                    else
                        writeFields(staticFields.Where(x => x.Attributes.HasFlag(FieldAttributes.HasFieldRVA)), x => $"\treturn v__field_{identifier}__{Escape(x.Name)}__data;\n");
                }
                var threadStaticMembers = new StringWriter();
                if (threadStaticFields.Count > 0)
                {
                    threadStaticMembers.WriteLine("\tstruct\n\t{");
                    foreach (var x in threadStaticFields) threadStaticMembers.WriteLine($"\t\t{EscapeForRoot(x.FieldType)} {Escape(x)}{{}};");
                    threadStaticMembers.WriteLine($"\t}} v_{identifier};");
                    if (ShouldGenerateReflection(type)) writeFields(threadStaticFields, x => $"\treturn &t_thread_static::v_instance->v_{identifier}.{Escape(x)};\n");
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
                    if (type == typeofVoid)
                    {
                        members = string.Empty;
                    }
                    else if (primitives.TryGetValue(type, out var name) || type.IsEnum)
                    {
                        if (name == null) name = primitives[type.GetEnumUnderlyingType()];
                        members = $@"{'\t'}{name} v__value;
{'\t'}void f_construct({name} a_value)
{'\t'}{{
{'\t'}{'\t'}v__value = a_value;
{'\t'}}}
";
                    }
                    else
                    {
                        var mm = builtin.GetMembers(this, type);
                        members = mm.members;
                        var unmanaged = mm.unmanaged;
                        if (members == null)
                        {
                            string scan(Type x, string y) => x.IsValueType ? $"{y}.f__scan(a_scan)" : $"a_scan({y})";
                            if (type.IsArray)
                            {
                                var element = GetElementType(type);
                                var elementIdentifier = EscapeForMember(element);
                                members = $@"{'\t'}t__bound v__bounds[{type.GetArrayRank()}];
{'\t'}{elementIdentifier}* f_data()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<{elementIdentifier}*>(this + 1);
{'\t'}}}
";
                                if (IsComposite(element)) members += $@"{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{{
{'\t'}{'\t'}{Escape(type.BaseType)}::f__scan(a_scan);
{'\t'}{'\t'}auto p = f_data();
{'\t'}{'\t'}for (size_t i = 0; i < v__length; ++i) {scan(element, "p[i]")};
{'\t'}}}
";
                            }
                            else
                            {
                                fields = type.GetFields(declaredAndInstance);
                                if (type.IsValueType) td.IsManaged = fields.Select(x => x.FieldType).Any(x => IsComposite(x) && (!x.IsValueType || Define(x).IsManaged));
                                var layout = type.StructLayoutAttribute;
                                var kind = layout?.Value ?? LayoutKind.Auto;
                                CustomAttributeData getMarshalAs(FieldInfo x) => x.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType == typeofMarshalAsAttribute);
                                UnmanagedType? getMarshalAsValue(CustomAttributeData x) => (UnmanagedType?)(int?)x?.ConstructorArguments[0].Value;
                                if (kind != LayoutKind.Auto)
                                {
                                    td.IsBlittable = fields.All(x => Define(x.FieldType).IsBlittable);
                                    var pack = layout.Pack == 0 ? Define(typeofIntPtr).Alignment : layout.Pack;
                                    var sizeofTChar = layout.CharSet == CharSet.Unicode ? 2 : 1;
                                    td.Alignment = Math.Min(fields.Select(x => x.FieldType == typeofString
                                        ? getMarshalAsValue(getMarshalAs(x)) == UnmanagedType.ByValTStr
                                            ? sizeofTChar
                                            : Define(typeofIntPtr).Alignment
                                        : Define(x.FieldType).Alignment
                                    ).DefaultIfEmpty(pack).Max(), pack);
                                }
                                if (kind == LayoutKind.Sequential && !td.IsBlittable && fields.Select(x => x.FieldType).All(x => x == typeofString || Define(x).IsMarshallable))
                                {
                                    var sb = new StringBuilder($@"
#pragma pack(push, 1)
struct {Escape(type)}__unmanaged
{{
");
                                    var i = 0;
                                    int align(int n) => (i + n - 1) / n * n;
                                    void pad(int j)
                                    {
                                        if (j > i) sb.AppendLine($"\tchar v__padding{i}[{j - i}];");
                                        i = j;
                                    }
                                    foreach (var x in fields)
                                    {
                                        var f = Escape(x);
                                        if (x.FieldType == typeofString)
                                        {
                                            var marshalAs = getMarshalAs(x);
                                            var value = getMarshalAsValue(marshalAs);
                                            var unicode = layout.CharSet == CharSet.Unicode;
                                            if (value == UnmanagedType.ByValTStr)
                                            {
                                                if (unicode) pad(align(Math.Min(2, td.Alignment)));
                                                // TODO: Work around for ByValTStr bug.
                                                //var size = (int)marshalAs.NamedArguments.First(x => x.MemberName == nameof(MarshalAsAttribute.SizeConst)).TypedValue.Value;
                                                var size = GetSizeConst(x);
                                                sb.AppendLine($"\t{(unicode ? "char16_t" : "char")} {f}[{size}];");
                                                i += size * (unicode ? 2 : 1);
                                            }
                                            else
                                            {
                                                var ftd = Define(typeofIntPtr);
                                                pad(align(Math.Min(ftd.Alignment, td.Alignment)));
                                                switch (value)
                                                {
                                                    case null:
                                                    case UnmanagedType.LPStr:
                                                        unicode = false;
                                                        break;
                                                    case UnmanagedType.LPWStr:
                                                        unicode = true;
                                                        break;
                                                }
                                                sb.AppendLine($"\t{(unicode ? "char16_t" : "char")}* {f};");
                                                i += ftd.UnmanagedSize;
                                            }
                                        }
                                        else
                                        {
                                            var ftd = Define(x.FieldType);
                                            pad(align(Math.Min(ftd.Alignment, td.Alignment)));
                                            sb.AppendLine(
                                                x.FieldType == typeofBoolean ? $"\t{EscapeForValue(typeofInt32)} {f};" :
                                                ftd.HasUnmanaged ? $"\t{Escape(x.FieldType)}__unmanaged {f};" :
                                                $"\t{EscapeForValue(x.FieldType)} {f};"
                                            );
                                            i += ftd.UnmanagedSize;
                                        }
                                    }
                                    td.UnmanagedSize = Math.Max(align(td.Alignment), layout.Size);
                                    pad(td.UnmanagedSize);
                                    var at = $"{Escape(type)}{(type.IsValueType ? "::t_value" : string.Empty)}*";
                                    var fs = fields.Select(Escape);
                                    unmanaged = sb.AppendLine($@"{'\t'}void f_in(const {at} a_p)
{'\t'}{{
{string.Join(string.Empty, fs.Select(x => $"\t\tf__marshal_in({x}, a_p->{x});\n"))}{'\t'}}}
{'\t'}void f_out({at} a_p) const
{'\t'}{{
{string.Join(string.Empty, fs.Select(x => $"\t\tf__marshal_out({x}, a_p->{x});\n"))}{'\t'}}}
{'\t'}void f_destroy()
{'\t'}{{
{string.Join(string.Empty, fs.Select(x => $"\t\tf__marshal_destroy({x});\n"))}{'\t'}}}
}};
#pragma pack(pop)").ToString();
                                }
                                var slots = fields.Where(x => IsComposite(x.FieldType)).Select(x => (Type: x.FieldType, Name: Escape(x)));
                                var constructs = fields.Select(Escape);
                                List<string> mergedFields = null;
                                int fieldOffset(FieldInfo x) => (int)x.GetCustomAttributesData().First(x => x.AttributeType == typeofFieldOffsetAttribute).ConstructorArguments[0].Value;
                                if (kind == LayoutKind.Explicit)
                                {
                                    td.UnmanagedSize = Math.Max(td.Alignment, layout.Size);
                                    var map = Enumerable.Repeat(false, td.UnmanagedSize).ToList();
                                    var rn = Define(typeofIntPtr).UnmanagedSize;
                                    foreach (var x in fields)
                                    {
                                        var reference = IsComposite(x.FieldType) && !x.FieldType.IsValueType;
                                        var i = fieldOffset(x);
                                        var j = i + (reference ? rn : Define(x.FieldType).UnmanagedSize);
                                        while (map.Count < j) map.Add(false);
                                        for (; i < j; ++i) map[i] = reference;
                                    }
                                    td.UnmanagedSize = map.Count;
                                    td.ExplicitMap = map;
                                    mergedFields = new();
                                    var ss = new List<(Type, string)>();
                                    for (var i = 0; i < map.Count;)
                                        if (map[i])
                                        {
                                            mergedFields.Add(($"{EscapeForMember(typeofObject)} r{i}"));
                                            ss.Add((typeofObject, $"v__merged.r{i}"));
                                            i += rn;
                                        }
                                        else
                                        {
                                            var j = i;
                                            do ++i; while (i < map.Count && !map[i]);
                                            mergedFields.Add(($"char p{j}[{i - j}]"));
                                        }
                                    slots = ss;
                                    constructs = new[] { "v__merged" };
                                }
                                string variables(string indent)
                                {
                                    var sb = new StringBuilder();
                                    string variable(FieldInfo x) => $"{EscapeForMember(x.FieldType)} {Escape(x)};";
                                    if (kind == LayoutKind.Explicit)
                                    {
                                        sb.AppendLine($@"#pragma pack(push, 1)
{indent}union
{indent}{{
{indent}{'\t'}struct
{indent}{'\t'}{{
{string.Join(string.Empty, mergedFields.Select(x => $"{indent}\t\t{x};\n"))}{indent}{'\t'}}} v__merged;");
                                        foreach (var x in fields) sb.AppendLine($@"{indent}{'\t'}struct
{indent}{'\t'}{{
{indent}{'\t'}{'\t'}char o[{fieldOffset(x)}];
{indent}{'\t'}{'\t'}{EscapeForMember(x.FieldType)} v;
{indent}{'\t'}}} v_{Escape(x.Name)};");
                                        sb.AppendLine($@"{indent}}};
#pragma pack(pop)

{indent}t_value() = default;
{indent}t_value(const t_value& a_x) : v__merged(a_x.v__merged)
{indent}{{
{indent}}}
{indent}t_value& operator=(const t_value& a_x)
{indent}{{
{indent}{'\t'}v__merged = a_x.v__merged;
{indent}{'\t'}return *this;
{indent}}}");
                                    }
                                    else if (td.IsBlittable)
                                    {
                                        sb.AppendLine("#pragma pack(push, 1)");
                                        var i = 0;
                                        int align(int n) => (i + n - 1) / n * n;
                                        void pad(int j)
                                        {
                                            if (j > i) sb.AppendLine($"{indent}char v__padding{i}[{j - i}];");
                                            i = j;
                                        }
                                        foreach (var x in fields)
                                        {
                                            var ftd = Define(x.FieldType);
                                            pad(align(Math.Min(ftd.Alignment, td.Alignment)));
                                            sb.AppendLine($"{indent}{variable(x)}");
                                            i += ftd.UnmanagedSize;
                                        }
                                        pad(Math.Max(align(td.Alignment), layout?.Size ?? 0));
                                        td.UnmanagedSize = i;
                                        sb.AppendLine("#pragma pack(pop)");
                                    }
                                    else
                                    {
                                        foreach (var x in fields) sb.AppendLine($"{indent}{variable(x)}");
                                    }
                                    return sb.ToString();
                                }
                                string scanSlots(string indent) => string.Join(string.Empty, slots.Select(x => $"{indent}{scan(x.Type, x.Name)};\n"));
                                members = type.IsValueType
                                    ? $@"{variables("\t\t")}
{'\t'}{'\t'}void f_destruct()
{'\t'}{'\t'}{{
{string.Join(string.Empty, slots.Select(x => $"\t\t\t{x.Name}.f_destruct();\n"))}{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{'\t'}{{
{scanSlots("\t\t\t")}{'\t'}{'\t'}}}
"
                                    : $@"{variables("\t")}
{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{{
{(type.BaseType == null ? string.Empty : $"\t\t{Escape(type.BaseType)}::f__scan(a_scan);\n")}{scanSlots("\t\t")}{'\t'}}}
{'\t'}void f_construct({identifier}* a_p) const
{'\t'}{{
{(type.BaseType == null ? string.Empty : $"\t\t{Escape(type.BaseType)}::f_construct(a_p);\n")}{string.Join(string.Empty, constructs.Select(x => $"{'\t'}{'\t'}new(&a_p->{x}) decltype({x})({x});\n"))}{'\t'}}}
";
                            }
                        }
                        else
                        {
                            if (type.IsValueType) td.IsManaged = mm.managed;
                        }
                        if (type.IsValueType) members = $@"{'\t'}struct t_value
{'\t'}{{
{members}{'\t'}}};
{'\t'}using t_stacked = {(td.IsManaged ? "il2cxx::t_stacked<t_value>" : "t_value")};
{'\t'}t_value v__value;
{'\t'}template<typename T>
{'\t'}void f_construct(T&& a_value)
{'\t'}{{
{'\t'}{'\t'}new(&v__value) decltype(v__value)(std::forward<T>(a_value));
{'\t'}}}
{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{{
{'\t'}{'\t'}v__value.f__scan(a_scan);
{'\t'}}}
";
                        staticDefinitions.Write(unmanaged);
                        td.HasUnmanaged = unmanaged?.Length > 0;
                    }
                    typeDeclarations.WriteLine($"{declaration};");
                    typeDefinitions.WriteLine($@"
{declaration}{@base}
{{
{members}}};");
                    if (ShouldGenerateReflection(type))
                    {
                        writeFields(fields, x => GenerateCheckNull("a_this") + $@"{'\t'}auto p = {(type.IsValueType ? string.Empty : "*")}static_cast<{EscapeForValue(type)}*>(a_this);
{'\t'}return &p->{Escape(x)};
");
                        fields = fields.Concat(staticFields).Concat(threadStaticFields);
                        if (td.HasFields = fields.Any()) td.Definitions.WriteLine($@"
static t__runtime_field_info* v__fields_{identifier}[] = {{
{string.Join(string.Empty, fields.Select(x => $"\t&v__field_{identifier}__{Escape(x.Name)},\n"))}{'\t'}nullptr
}};");
                        foreach (var x in type.GetConstructors(declaredAndInstance))
                        {
                            td.HasConstructors = true;
                            var name = Escape(x);
                            fieldDeclarations.WriteLine($"extern t__runtime_constructor_info v__method_{name};");
                            definition.Definitions.WriteLine($"t__runtime_constructor_info v__method_{name}{{&t__type_of<t__runtime_constructor_info>::v__instance, &t__type_of<{identifier}>::v__instance, u\"{x.Name}\"sv, {(int)x.Attributes}, {writeAttributes(x, name)}, {writeParameters(x.GetParameters(), name)}, {GenerateInvokeFunction(x)}}};");
                        }
                    }
                }
                this.staticDefinitions.Write(staticDefinitions);
                this.staticMembers.Write(staticMembers);
                this.threadStaticMembers.Write(threadStaticMembers);
            }
            if ((definition is InterfaceDefinition || definition is TypeDefinition) && ShouldGenerateReflection(type))
            {
                var identifier = Escape(type);
                foreach (var x in GetMethods(type))
                {
                    definition.HasMethods = true;
                    var name = Escape(x);
                    fieldDeclarations.WriteLine($"extern t__runtime_method_info v__method_{name};");
                    definition.Definitions.WriteLine($@"t__runtime_method_info v__method_{name}{{&t__type_of<t__runtime_method_info>::v__instance, &t__type_of<{identifier}>::v__instance, u""{x.Name}""sv, {(int)x.Attributes}, {writeAttributes(x, name)}, {writeParameters(x.GetParameters(), name)}, {GenerateInvokeFunction(x)}
#ifdef __EMSCRIPTEN__
{'\t'}, {GenerateWASMInvokeFunction(x)}
#endif
}};");
                }
                foreach (var x in GetProperties(type))
                {
                    definition.HasProperties = true;
                    var variable = Escape(x);
                    fieldDeclarations.WriteLine($"extern t__runtime_property_info {variable};");
                    var get = x.GetMethod;
                    var set = x.SetMethod;
                    definition.Definitions.WriteLine($"t__runtime_property_info {variable}{{&t__type_of<t__runtime_property_info>::v__instance, &t__type_of<{identifier}>::v__instance, u\"{x.Name}\"sv, {(int)x.Attributes}, {writeAttributes(x, variable)}, &t__type_of<{Escape(x.PropertyType)}>::v__instance, {writeParameters(x.GetIndexParameters(), variable)}, {(get == null ? "nullptr" : $"&v__method_{Escape(get)}")}, {(set == null ? "nullptr" : $"&v__method_{Escape(set)}")}}};");
                }
                definition.Attributes = writeAttributes(type, identifier);
            }
            runtimeDefinitions.Add(definition);
            if (type.IsEnum && ShouldGenerateReflection(type))
                try
                {
                    Enqueue(type.MakeArrayType());
                } catch { }
            return definition;
        }
        private void WriteRuntimeDefinition(RuntimeDefinition definition, string assembly, IReadOnlyDictionary<Type, IEnumerable<Type>> genericTypeDefinitionToConstructeds, TextWriter writerForDeclarations, TextWriter writerForDefinitions)
        {
            var type = definition.Type;
            var @base = definition is TypeDefinition && FinalizeOf(type) != null ? "t__type_finalizee" : "t__type";
            var identifier = Escape(type);
            if (type.IsGenericType)
            {
                var gtd = type.GetGenericTypeDefinition();
                if (ShouldGenerateReflection(gtd))
                {
                    writerForDefinitions.WriteLine($@"
t__type* v__generic_arguments_{Escape(type)}[] = {{
{string.Join(string.Empty, type.GetGenericArguments().Select(x => $"\t&t__type_of<{Escape(x)}>::v__instance,\n"))}{'\t'}nullptr
}};");
                    if (type.IsGenericTypeDefinition) writerForDefinitions.WriteLine($@"
t__type* v__constructed_generic_types_{Escape(type)}[] = {{
{string.Join(string.Empty, (genericTypeDefinitionToConstructeds.TryGetValue(type, out var xs) ? xs : Enumerable.Empty<Type>()).Select(x => $"\t&t__type_of<{Escape(x)}>::v__instance,\n"))}{'\t'}nullptr
}};");
                }
            }
            writerForDefinitions.Write(definition.Definitions);
            if ((definition as TypeDefinition)?.HasConstructors ?? false) writerForDefinitions.Write($@"
static t__runtime_constructor_info* v__constructors_{identifier}[] = {{
{string.Join(string.Empty, type.GetConstructors(declaredAndInstance).Select(x => $"\t&v__method_{Escape(x)},\n"))}{'\t'}nullptr
}};
");
            if (definition.HasMethods) writerForDefinitions.Write($@"
static t__runtime_method_info* v__methods_{identifier}[] = {{
{string.Join(string.Empty, GetMethods(type).Select(x => $"\t&v__method_{Escape(x)},\n"))}{'\t'}nullptr
}};
");
            if (definition.HasProperties) writerForDefinitions.Write($@"
static t__runtime_property_info* v__properties_{identifier}[] = {{
{string.Join(string.Empty, GetProperties(type).Select(x => $"\t&{Escape(x)},\n"))}{'\t'}nullptr
}};
");
            writerForDeclarations.WriteLine($@"
template<>
struct t__type_of<{identifier}> : {@base}
{{");
            writerForDefinitions.Write($@"
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
                writeMethods(td.Methods, (i, m, name) => name, (i, j, m, name) => name, GetBaseDefinition, "\t");
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
                writerForDeclarations.WriteLine($@"{'\t'}static void f_do_scan(t_object<t__type>* a_this, t_scan<t__type> a_scan);
{'\t'}static t__object* f_do_clone(const t__object* a_this);");
                if (type != typeofVoid && type.IsValueType) writerForDeclarations.WriteLine($@"{'\t'}static void f_do_clear(void* a_p, size_t a_n);
{'\t'}static void f_do_copy(const void* a_from, size_t a_n, void* a_to);
{'\t'}static t__object* f_do_box(void* a_p);");
                if (definition.HasUnmanaged)
                    writerForDeclarations.WriteLine($@"{'\t'}static void f_do_to_unmanaged(const t__object* a_this, void* a_p);
{'\t'}static void f_do_from_unmanaged(t__object* a_this, const void* a_p);
{'\t'}static void f_do_destroy_unmanaged(void* a_p);");
            }
            else
            {
                td = null;
            }
            var szarray = "nullptr";
            if (!type.IsArray)
                try
                {
                    var sza = type.MakeArrayType();
                    if (typeToRuntime.ContainsKey(sza)) szarray = $"&t__type_of<{Escape(sza)}>::v__instance";
                } catch { }
            writerForDeclarations.WriteLine($@"{'\t'}t__type_of();
{'\t'}static t__type_of v__instance;
}};");
            writerForDefinitions.WriteLine($@"}}, &{assembly}, u""{(type.IsGenericParameter ? string.Empty : type.Namespace)}""sv, u""{type.Name}""sv, u""{type.FullName}""sv, u""{type}""sv, {
    (int)type.Attributes
}, {
    definition.Attributes ?? "nullptr"
}, {(
    definition.IsManaged ? "true" : "false"
)}, {(
    type.IsValueType ? "true" : "false"
)}, {(
    type.IsArray ? "true" : "false"
)}, {(
    type.IsEnum ? "true" : "false"
)}, {(
    type.IsByRef ? "true" : "false"
)}, {(
    type.IsPointer ? "true" : "false"
)}, {(
    type.IsByRefLike ? "true" : "false"
)}, {(
    type.IsGenericType ? "true" : "false"
)}, {(
    type == typeofVoid || type.ContainsGenericParameters ? "0" : $"sizeof({EscapeForValue(type)})"
)}, {szarray})
{{");
            writerForDefinitions.WriteLine($@"{'\t'}v__cor_element_type = {GetCorElementType(type)};
{'\t'}v__type_code = {(int)Type.GetTypeCode(type)};");
            if (definition is TypeDefinition) writerForDefinitions.WriteLine($"\tv__managed_size = sizeof({Escape(type)});");
            if (definition.HasUnmanaged)
                writerForDefinitions.WriteLine($@"{'\t'}v__unmanaged_size = sizeof({Escape(type)}__unmanaged);
{'\t'}f_to_unmanaged = f_do_to_unmanaged;
{'\t'}f_from_unmanaged = f_do_from_unmanaged;
{'\t'}f_destroy_unmanaged = f_do_destroy_unmanaged;");
            else if (!type.IsArray && !type.IsEnum && definition.IsBlittable)
                writerForDefinitions.WriteLine($@"{'\t'}v__unmanaged_size = sizeof({EscapeForValue(type)});
{'\t'}f_to_unmanaged = f_do_to_unmanaged_blittable;
{'\t'}f_from_unmanaged = f_do_from_unmanaged_blittable;
{'\t'}f_destroy_unmanaged = f_do_destroy_unmanaged_blittable;");
            if (type.HasElementType) writerForDefinitions.WriteLine($"\tv__element = &t__type_of<{Escape(GetElementType(type))}>::v__instance;");
            if (type.IsArray) writerForDefinitions.WriteLine($"\tv__rank = {type.GetArrayRank()};");
            if (type.IsEnum)
            {
                writerForDefinitions.WriteLine($"\tv__underlying = &t__type_of<{Escape(Enum.GetUnderlyingType(type))}>::v__instance;");
            }
            else
            {
                var nv = Nullable.GetUnderlyingType(type);
                if (nv != null) writerForDefinitions.WriteLine($"\tv__underlying = &t__type_of<{Escape(nv)}>::v__instance;");
            }
            if (type.IsGenericType)
            {
                var gtd = type.GetGenericTypeDefinition();
                if (ShouldGenerateReflection(gtd))
                {
                    writerForDefinitions.WriteLine($@"{'\t'}v__generic_type_definition = &t__type_of<{Escape(gtd)}>::v__instance;
{'\t'}v__generic_arguments = v__generic_arguments_{Escape(type)};");
                    if (type.IsGenericTypeDefinition) writerForDefinitions.WriteLine($"\tv__constructed_generic_types = v__constructed_generic_types_{Escape(type)};");
                }
            }
            if (ShouldGenerateReflection(type))
            {
                writerForDefinitions.WriteLine($"\tv__fields = {(td?.HasFields ?? false ? $"v__fields_{identifier}" : "v__empty_fields")};");
                writerForDefinitions.WriteLine($"\tv__constructors = {(td?.HasConstructors ?? false ? $"v__constructors_{identifier}" : "v__empty_constructors")};");
                writerForDefinitions.WriteLine($"\tv__methods = {(definition.HasMethods ? $"v__methods_{identifier}" : "v__empty_methods")};");
                writerForDefinitions.WriteLine($"\tv__properties = {(definition.HasProperties ? $"v__properties_{identifier}" : "v__empty_properties")};");
            }
            writerForDefinitions.Write(td?.Delegate);
            if (definition is TypeDefinition)
            {
                writerForDefinitions.WriteLine($@"{'\t'}t__type::f_scan = f_do_scan;
{'\t'}f_clone = f_do_clone;");
                if (type != typeofVoid && type.IsValueType) writerForDefinitions.WriteLine($@"{'\t'}f_clear = f_do_clear;
{'\t'}f_copy = f_do_copy;
{'\t'}f_box = f_do_box;
{'\t'}f_unbox = f_do_unbox_value;");
            }
            writerForDefinitions.WriteLine($@"}}
t__type_of<{identifier}> t__type_of<{identifier}>::v__instance;");
            if (definition is TypeDefinition)
            {
                writerForDefinitions.WriteLine($@"void t__type_of<{identifier}>::f_do_scan(t_object<t__type>* a_this, t_scan<t__type> a_scan)
{{
{'\t'}static_cast<{identifier}*>(a_this)->f__scan(a_scan);
}}
t__object* t__type_of<{identifier}>::f_do_clone(const t__object* a_this)
{{");
                if (type.IsArray)
                {
                    var element = EscapeForMember(GetElementType(type));
                    writerForDefinitions.WriteLine($@"{'\t'}auto p = static_cast<const {identifier}*>(a_this);
{'\t'}t__new<{identifier}> q(sizeof({element}) * p->v__length);
{'\t'}q->v__length = p->v__length;
{'\t'}std::memcpy(q->v__bounds, p->v__bounds, sizeof(p->v__bounds));
{'\t'}auto p0 = reinterpret_cast<const {element}*>(p + 1);
{'\t'}auto p1 = q->f_data();
{'\t'}for (size_t i = 0; i < p->v__length; ++i) new(p1 + i) {element}(p0[i]);
{'\t'}return q;");
                }
                else
                {
                    writerForDefinitions.WriteLine(
                        type == typeofVoid ? $"\treturn t__new<{identifier}>(0);" :
                        type.IsValueType ? $@"{'\t'}t__new<{identifier}> p(0);
{'\t'}new(&p->v__value) decltype({identifier}::v__value)(static_cast<const {identifier}*>(a_this)->v__value);
{'\t'}return p;
}}
void t__type_of<{identifier}>::f_do_clear(void* a_p, size_t a_n)
{{
{'\t'}std::fill_n(static_cast<decltype({identifier}::v__value)*>(a_p), a_n, decltype({identifier}::v__value){{}});
}}
void t__type_of<{identifier}>::f_do_copy(const void* a_from, size_t a_n, void* a_to)
{{
{'\t'}f__copy(static_cast<const decltype({identifier}::v__value)*>(a_from), a_n, static_cast<decltype({identifier}::v__value)*>(a_to));
}}
t__object* t__type_of<{identifier}>::f_do_box(void* a_p)
{{
{'\t'}return f__new_constructed<{identifier}>(*static_cast<decltype({identifier}::v__value)*>(a_p));" :
                        $@"{'\t'}t__new<{identifier}> p(0);
{'\t'}static_cast<const {identifier}*>(a_this)->f_construct(p);
{'\t'}return p;");
                }
                writerForDefinitions.WriteLine('}');
                if (definition.HasUnmanaged)
                {
                    string @this(string qualifier) => type.IsValueType
                        ? $"reinterpret_cast<{qualifier} {identifier}::t_value*>(a_this + 1)"
                        : $"static_cast<{qualifier} {identifier}*>(a_this)";
                    var at = $"{identifier}{(type.IsValueType ? "::t_value" : string.Empty)}*";
                    writerForDefinitions.WriteLine($@"void t__type_of<{identifier}>::f_do_to_unmanaged(const t__object* a_this, void* a_p)
{{
{'\t'}static_cast<{identifier}__unmanaged*>(a_p)->f_in({@this("const ")});
}}
void t__type_of<{identifier}>::f_do_from_unmanaged(t__object* a_this, const void* a_p)
{{
{'\t'}static_cast<const {identifier}__unmanaged*>(a_p)->f_out({@this(string.Empty)});
}}
void t__type_of<{identifier}>::f_do_destroy_unmanaged(void* a_p)
{{
{'\t'}static_cast<{identifier}__unmanaged*>(a_p)->f_destroy();
}}");
                }
            }
        }
    }
}
