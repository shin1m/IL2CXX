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
            var ims = @interface.GetMethods();
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
                type.IsPointer ? 0xf :
                type.IsArray ? 0x14 :
                type == typeofIntPtr ? 0x18 :
                type == typeofUIntPtr ? 0x19 :
                type.IsSZArray ? 0x1d : 0x1c
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
            public readonly TypeDefinition Base;
            public readonly Dictionary<Type, MethodInfo[]> InterfaceToMethods = new();
            public readonly string DefaultConstructor;
            public readonly string Delegate;

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
                foreach (var x in Type.GetInterfaces())
                {
                    var definition = (InterfaceDefinition)transpiler.Define(x);
                    var methods = new MethodInfo[definition.Methods.Count];
                    var map = transpiler.GetInterfaceMap(Type.IsArray && x.IsGenericType ? transpiler.typeofSZArrayHelper.MakeGenericType(transpiler.GetElementType(Type)) : Type, x);
                    foreach (var (i, t) in map.InterfaceMethods.Zip(map.TargetMethods, (i, t) => (i, t))) methods[definition.GetIndex(i)] = t;
                    InterfaceToMethods.Add(x, methods);
                }
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    var identifier = transpiler.Escape(Type);
                    DefaultConstructor = $@"
t__runtime_constructor_info v__default_constructor_{identifier}{{&t__type_of<t__runtime_constructor_info>::v__instance, []() -> t__object*
{{
{'\t'}auto p = f__new_zerod<{identifier}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}return p;
}}}};
";
                    transpiler.Enqueue(constructor);
                }
                if (Type.IsSubclassOf(transpiler.typeofDelegate) && Type != transpiler.typeofMulticastDelegate)
                {
                    var invoke = (MethodInfo)Type.GetMethod("Invoke");
                    transpiler.Enqueue(invoke);
                    var @return = invoke.ReturnType;
                    var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                    string generate(Type t, string body) => $@"reinterpret_cast<void*>(+[]({
    string.Join(",", parameters.Prepend(t).Select((x, i) => $"\n\t\t{transpiler.EscapeForStacked(x)} a_{i}"))
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
                    if ((@return == transpiler.typeofVoid ? parameters : parameters.Prepend(@return)).All(x => !IsComposite(x) || x == transpiler.typeofString || x == transpiler.typeofStringBuilder || transpiler.typeofSafeHandle.IsAssignableFrom(x) || x.IsArray || transpiler.Define(x).IsMarshallable))
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
        private readonly StringWriter fieldDefinitions = new();
        private readonly List<RuntimeDefinition> runtimeDefinitions = new();
        private readonly Dictionary<Type, RuntimeDefinition> typeToRuntime = new();
        private readonly Dictionary<MethodKey, Dictionary<Type[], int>> genericMethodToTypesToIndex = new();
        private bool processed;

        public RuntimeDefinition Define(Type type)
        {
            if (typeToRuntime.TryGetValue(type, out var definition)) return definition;
            ThrowIfInvalid(type);
            if (processed) throw new InvalidOperationException($"{type}");
            if (type.IsGenericType)
            {
                Enqueue(type.GetGenericTypeDefinition());
                foreach (var x in type.GetGenericArguments()) Enqueue(x);
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
                foreach (var m in td.Methods.Where(x => !x.IsAbstract)) enqueue(GetBaseDefinition(m), m);
                foreach (var p in td.InterfaceToMethods)
                {
                    var id = typeToRuntime[p.Key];
                    foreach (var m in id.Methods) enqueue(m, p.Value[id.GetIndex(m)]);
                }
                typeToRuntime[type] = definition = td;
                var identifier = Escape(type);
                var builtinStaticMembers = builtin.GetStaticMembers(this, type);
                var staticFields = new List<FieldInfo>();
                var threadStaticFields = new List<FieldInfo>();
                if (builtinStaticMembers == null && !type.IsEnum)
                    foreach (var x in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        (x.GetCustomAttributesData().Any(x => x.AttributeType == typeofThreadStaticAttribute) ? threadStaticFields : staticFields).Add(x);
                var staticDefinitions = new StringWriter();
                var staticMembers = new StringWriter();
                var initialize = builtin.GetInitialize(this, type);
                if (type.Name == "<PrivateImplementationDetails>")
                {
                    foreach (var x in staticFields)
                    {
                        fieldDeclarations.WriteLine($@"extern uint8_t v__field_{identifier}__{Escape(x.Name)}[];
inline void* f__field_{identifier}__{Escape(x.Name)}()
{{
{'\t'}return v__field_{identifier}__{Escape(x.Name)};
}}");
                        fieldDefinitions.WriteLine($@"uint8_t v__field_{identifier}__{Escape(x.Name)}[] = {{{string.Join(", ", GetRVAData(x).Select(y => $"0x{y:x02}"))}}};");
                    }
                }
                else if (builtinStaticMembers != null || staticFields.Count > 0 || initialize != null || type.TypeInitializer != null)
                {
                    staticDefinitions.WriteLine($@"
struct t__static_{identifier}
{{");
                    if (builtinStaticMembers != null) staticDefinitions.WriteLine(builtinStaticMembers);
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
                                var fields = type.GetFields(declaredAndInstance);
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
                }
                this.staticDefinitions.Write(staticDefinitions);
                this.staticMembers.Write(staticMembers);
                this.threadStaticMembers.Write(threadStaticMembers);
            }
            runtimeDefinitions.Add(definition);
            // TODO
            //if (!type.IsArray && !type.IsByRef && !type.IsPointer && type != typeofVoid)
            if (type.IsEnum)
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
                writerForDefinitions.WriteLine($@"
t__type* v__generic_arguments_{Escape(type)}[] = {{
{string.Join(string.Empty, type.GetGenericArguments().Select(x => $"\t&t__type_of<{Escape(x)}>::v__instance,\n"))}{'\t'}nullptr
}};");
                if (type.IsGenericTypeDefinition) writerForDefinitions.WriteLine($@"
t__type* v__constructed_generic_types_{Escape(type)}[] = {{
{string.Join(string.Empty, (genericTypeDefinitionToConstructeds.TryGetValue(type, out var xs) ? xs : Enumerable.Empty<Type>()).Select(x => $"\t&t__type_of<{Escape(x)}>::v__instance,\n"))}{'\t'}nullptr
}};");
            }
            if (type.IsEnum && !type.ContainsGenericParameters) writerForDefinitions.Write($@"
std::pair<uint64_t, std::u16string_view> v__enum_pairs_{identifier}[] = {{{
    string.Join(",", type.GetFields(BindingFlags.Static | BindingFlags.Public).Select(x => $"\n\t{{{(ulong)Convert.ToInt64(x.GetRawConstantValue())}ul, u\"{x.Name}\"sv}}"))
}
}};");
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
                if (type != typeofVoid && type.IsValueType) writerForDeclarations.WriteLine($@"{'\t'}static void f_do_initialize(const void* a_from, size_t a_n, void* a_to);
{'\t'}static void f_do_clear(void* a_p, size_t a_n);
{'\t'}static void f_do_copy(const void* a_from, size_t a_n, void* a_to);");
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
            writerForDefinitions.WriteLine($@"}}, &{assembly}, u""{type.Namespace}""sv, u""{type.Name}""sv, u""{type.FullName}""sv, u""{type}""sv, {(
    definition.IsManaged ? "true" : "false"
)}, {(
    type.IsValueType ? "true" : "false"
)}, {(
    type.IsEnum ? "true" : "false"
)}, {(
    type == typeofVoid || type.ContainsGenericParameters ? "0" : $"sizeof({EscapeForValue(type)})"
)}, {szarray})
{{");
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
            if (type.IsGenericType)
            {
                writerForDefinitions.WriteLine($@"{'\t'}v__generic_type_definition = &t__type_of<{Escape(type.GetGenericTypeDefinition())}>::v__instance;
{'\t'}v__generic_arguments = v__generic_arguments_{Escape(type)};");
                if (type.IsGenericTypeDefinition) writerForDefinitions.WriteLine($"\tv__constructed_generic_types = v__constructed_generic_types_{Escape(type)};");
            }
            if (type.IsArray) writerForDefinitions.WriteLine($@"{'\t'}v__element = &t__type_of<{Escape(GetElementType(type))}>::v__instance;
{'\t'}v__rank = {type.GetArrayRank()};
{'\t'}v__cor_element_type = {GetCorElementType(GetElementType(type))};");
            if (td?.DefaultConstructor != null) writerForDefinitions.WriteLine($"\tv__default_constructor = &v__default_constructor_{identifier};");
            writerForDefinitions.Write(td?.Delegate);
            var nv = Nullable.GetUnderlyingType(type);
            if (nv != null) writerForDefinitions.WriteLine($"\tv__nullable_value = &t__type_of<{Escape(nv)}>::v__instance;");
            if (definition is TypeDefinition)
            {
                writerForDefinitions.WriteLine($@"{'\t'}t__type::f_scan = f_do_scan;
{'\t'}f_clone = f_do_clone;");
                if (type != typeofVoid && type.IsValueType) writerForDefinitions.WriteLine($@"{'\t'}f_initialize = f_do_initialize;
{'\t'}f_clear = f_do_clear;
{'\t'}f_copy = f_do_copy;");
                if (type.IsEnum) writerForDefinitions.WriteLine($@"{'\t'}v__enum_pairs = v__enum_pairs_{identifier};
{'\t'}v__enum_count = std::size(v__enum_pairs_{identifier});
{'\t'}v__cor_element_type = {GetCorElementType(type.GetEnumUnderlyingType())};");
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
void t__type_of<{identifier}>::f_do_initialize(const void* a_from, size_t a_n, void* a_to)
{{
{'\t'}std::uninitialized_copy_n(static_cast<const decltype({identifier}::v__value)*>(a_from), a_n, static_cast<decltype({identifier}::v__value)*>(a_to));
}}
void t__type_of<{identifier}>::f_do_clear(void* a_p, size_t a_n)
{{
{'\t'}std::fill_n(static_cast<decltype({identifier}::v__value)*>(a_p), a_n, decltype({identifier}::v__value){{}});
}}
void t__type_of<{identifier}>::f_do_copy(const void* a_from, size_t a_n, void* a_to)
{{
{'\t'}f__copy(static_cast<const decltype({identifier}::v__value)*>(a_from), a_n, static_cast<decltype({identifier}::v__value)*>(a_to));" :
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
