using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace IL2CXX;

class RuntimeAssembly : Assembly
{
    public override MethodInfo EntryPoint => throw new NotImplementedException();
    public override string FullName => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<Attribute>();
    public override Type[] GetExportedTypes() => throw new NotImplementedException();
    public override string[] GetManifestResourceNames() => throw new NotImplementedException();
    public override Stream GetManifestResourceStream(string name) => throw new NotImplementedException();
    public override AssemblyName GetName(bool copiedName) => new(FullName);
    public override Type? GetType(string name, bool throwOnError, bool ignoreCase) => Type.GetType($"{name}, {FullName}", throwOnError, ignoreCase);
}
abstract class RuntimeFieldInfo : FieldInfo
{
    public override FieldAttributes Attributes => throw new NotImplementedException();
    public override Type DeclaringType => throw new NotImplementedException();
    public override Type FieldType => throw new NotImplementedException();
    public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
    public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
    public override object GetValue(object? @this) => throw new NotImplementedException();
    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    public override string Name => throw new NotImplementedException();
    public override Type ReflectedType => throw new NotImplementedException();
    public new void SetValue(object? @this, object? value) => throw new NotImplementedException();
    public override void SetValue(object? @this, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture) => SetValue(@this, binder == null || binder == Type.DefaultBinder ? value : RuntimeType.ChangeType(binder, value, FieldType, culture));
}
abstract class RuntimeConstructorInfo : ConstructorInfo
{
    public override MethodAttributes Attributes => throw new NotImplementedException();
    public override Type DeclaringType => throw new NotImplementedException();
    public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
    public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
    public override ParameterInfo[] GetParameters() => throw new NotImplementedException();
    public new object Invoke(object?[]? parameters) => throw new NotImplementedException();
    public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        if (binder != null && parameters != null) RuntimeType.ChangeType(GetParameters(), binder, parameters, culture);
        return Invoke(parameters?.Length > 0 ? parameters : null);
    }
    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    public override string Name => throw new NotImplementedException();
    public override Type ReflectedType => throw new NotImplementedException();
}
abstract class RuntimeMethodInfo : MethodInfo
{
    public override MethodAttributes Attributes => throw new NotImplementedException();
    public override Delegate CreateDelegate(Type type) => throw new NotImplementedException();
    public override Delegate CreateDelegate(Type type, object? target) => throw new NotImplementedException();
    public override Type DeclaringType => throw new NotImplementedException();
    public override MethodInfo GetBaseDefinition()
    {
        for (var @this = this;;)
        {
            var parent = @this.GetParentDefinition();
            if (parent == null) return @this;
            @this = parent;
        }
    }
    public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
    public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
    public override ParameterInfo[] GetParameters() => throw new NotImplementedException();
    public RuntimeMethodInfo GetParentDefinition() => throw new NotImplementedException();
    public new object Invoke(object? @this, object?[]? parameters) => throw new NotImplementedException();
    public override object Invoke(object? @this, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        if (binder != null && parameters != null) RuntimeType.ChangeType(GetParameters(), binder, parameters, culture);
        return Invoke(@this, parameters);
    }
    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    public override MethodInfo MakeGenericMethod(params Type[] types) => throw new NotImplementedException();
    public override string Name => throw new NotImplementedException();
    public override Type ReflectedType => throw new NotImplementedException();
    public override Type ReturnType => throw new NotImplementedException();
}
abstract class RuntimePropertyInfo : PropertyInfo
{
    public override PropertyAttributes Attributes => throw new NotImplementedException();
    public override Type DeclaringType => throw new NotImplementedException();
    public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
    public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
    public override ParameterInfo[] GetIndexParameters() => throw new NotImplementedException();
    public override MethodInfo GetMethod => throw new NotImplementedException();
    public PropertyInfo? GetParentDefinition(Type[] parameters) => ((RuntimeMethodInfo)(GetMethod ?? SetMethod)).GetParentDefinition()?.DeclaringType.GetProperty(Name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, PropertyType, parameters, null);
    public new object GetValue(object? @this, object?[]? index) => throw new NotImplementedException();
    public override object GetValue(object? @this, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        if (binder != null && index != null) RuntimeType.ChangeType(GetIndexParameters(), binder, index, culture);
        return GetValue(@this, index);
    }
    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    public override string Name => throw new NotImplementedException();
    public override Type PropertyType => throw new NotImplementedException();
    public override Type ReflectedType => throw new NotImplementedException();
    public override MethodInfo SetMethod => throw new NotImplementedException();
    public new void SetValue(object? @this, object? value, object?[]? index) => throw new NotImplementedException();
    public override void SetValue(object? @this, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        if (binder != null)
        {
            value = RuntimeType.ChangeType(binder, value, PropertyType, culture);
            if (index != null) RuntimeType.ChangeType(GetIndexParameters(), binder, index, culture);
        }
        SetValue(@this, value, index);
    }
}
abstract class RuntimeType : Type
{
    public override Assembly Assembly => throw new NotImplementedException();
    public override string AssemblyQualifiedName => $"{FullName}, {Assembly.FullName}";
    public override Type BaseType => throw new NotImplementedException();
    public override Type DeclaringType => throw new NotImplementedException();
    public override string FullName => throw new NotImplementedException();
    public override int GetArrayRank() => throw new NotImplementedException();
    protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
    protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[] types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingFlags) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
    public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
    public override Type GetElementType() => throw new NotImplementedException();
    public override string GetEnumName(object value) => throw new NotImplementedException();
    public override string[] GetEnumNames() => throw new NotImplementedException();
    public override Array GetEnumValues() => throw new NotImplementedException();
    public override FieldInfo GetField(string name, BindingFlags bindingFlags) => throw new NotImplementedException();
    public override FieldInfo[] GetFields(BindingFlags bindingFlags) => throw new NotImplementedException();
    public override Type[] GetGenericArguments() => throw new NotImplementedException();
    public override Type GetGenericTypeDefinition() => throw new NotImplementedException();
    public override Type[] GetInterfaces() => throw new NotImplementedException();
    protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    protected override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    public override MethodInfo[] GetMethods(BindingFlags bindingFlags) => throw new NotImplementedException();
    public override PropertyInfo[] GetProperties(BindingFlags bindingFlags) => throw new NotImplementedException();
    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingFlags, Binder? binder, Type? @return, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    protected override TypeCode GetTypeCodeImpl() => throw new NotImplementedException();
    protected override bool HasElementTypeImpl() => throw new NotImplementedException();
    protected override bool IsArrayImpl() => throw new NotImplementedException();
    public override bool IsAssignableFrom(Type? c) => throw new NotImplementedException();
    protected override bool IsByRefImpl() => throw new NotImplementedException();
    public override bool IsByRefLike => throw new NotImplementedException();
    public override bool IsConstructedGenericType => throw new NotImplementedException();
    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    public override bool IsGenericType => throw new NotImplementedException();
    public override bool IsGenericTypeDefinition => throw new NotImplementedException();
    protected override bool IsPrimitiveImpl() => throw new NotImplementedException();
    protected override bool IsPointerImpl() => throw new NotImplementedException();
    public override Type MakeGenericType(params Type[] arguments) => throw new NotImplementedException();
    public override string Namespace => throw new NotImplementedException();
    public override string Name => throw new NotImplementedException();
    public override Type ReflectedType => throw new NotImplementedException();
    public override string ToString() => throw new NotImplementedException();
    public override RuntimeTypeHandle TypeHandle => throw new NotImplementedException();
    public override Type UnderlyingSystemType => this;

    public static bool ValueEquals(RuntimeType type, nint x, object y) => throw new NotImplementedException();
    public static int ValueGetHashCode(RuntimeType type, nint x) => throw new NotImplementedException();
    public static string ValueToString(RuntimeType type, nint x) => throw new NotImplementedException();
    public static object? ChangeType(Binder binder, object? value, Type type, CultureInfo? culture) => value == null ? null : binder.ChangeType(value, type, culture);
    public static void ChangeType(ParameterInfo[] pis, Binder binder, object?[] parameters, CultureInfo? culture)
    {
        if (parameters.Length != pis.Length) throw new TargetParameterCountException();
        for (var i = 0; i < pis.Length; ++i) parameters[i] = ChangeType(binder, parameters[i], pis[i].ParameterType, culture);
    }
    public static short ConvertInvokeParameterToInt16(object x) => x switch
    {
        sbyte y => y,
        byte y => y,
        _ => throw new ArgumentException()
    };
    public static ushort ConvertInvokeParameterToUInt16(object x) => x switch
    {
        byte y => y,
        _ => throw new ArgumentException()
    };
    public static int ConvertInvokeParameterToInt32(object x) => x switch
    {
        sbyte y => y,
        byte y => y,
        short y => y,
        ushort y => y,
        _ => throw new ArgumentException()
    };
    public static uint ConvertInvokeParameterToUInt32(object x) => x switch
    {
        byte y => y,
        ushort y => y,
        _ => throw new ArgumentException()
    };
    public static long ConvertInvokeParameterToInt64(object x) => x switch
    {
        sbyte y => y,
        byte y => y,
        short y => y,
        ushort y => y,
        int y => y,
        uint y => y,
        nint y => y,
        _ => throw new ArgumentException()
    };
    public static ulong ConvertInvokeParameterToUInt64(object x) => x switch
    {
        byte y => y,
        ushort y => y,
        uint y => y,
        nuint y => y,
        _ => throw new ArgumentException()
    };
    public static float ConvertInvokeParameterToSingle(object x) => x switch
    {
        sbyte y => y,
        byte y => y,
        short y => y,
        ushort y => y,
        int y => y,
        uint y => y,
        long y => y,
        ulong y => y,
        nint y => y,
        nuint y => y,
        _ => throw new ArgumentException()
    };
    public static double ConvertInvokeParameterToDouble(object x) => x switch
    {
        sbyte y => y,
        byte y => y,
        short y => y,
        ushort y => y,
        int y => y,
        uint y => y,
        long y => y,
        ulong y => y,
        float y => y,
        nint y => y,
        nuint y => y,
        _ => throw new ArgumentException()
    };
    public static decimal ConvertInvokeParameterToDecimal(object x) => x switch
    {
        sbyte y => y,
        byte y => y,
        short y => y,
        ushort y => y,
        int y => y,
        uint y => y,
        long y => y,
        ulong y => y,
        nint y => y,
        nuint y => y,
        _ => throw new ArgumentException()
    };
    public static nint ConvertInvokeParameterToIntPtr(object x) => x switch
    {
        sbyte y => y,
        byte y => y,
        short y => y,
        ushort y => y,
        int y => y,
        _ => throw new ArgumentException()
    };
    public static nuint ConvertInvokeParameterToUIntPtr(object x) => x switch
    {
        byte y => y,
        ushort y => y,
        uint y => y,
        _ => throw new ArgumentException()
    };
}
abstract class RuntimeGenericParameter : Type
{
    public override Assembly Assembly => throw new NotSupportedException();
    public override string? AssemblyQualifiedName => null;
    public override Type BaseType => throw new NotImplementedException();
    public override string FullName => throw new NotSupportedException();
    protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
    protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[] types, ParameterModifier[]? modifiers) => null;
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingFlags) => Array.Empty<ConstructorInfo>();
    public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
    public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
    public override Type? GetElementType() => null;
    public override FieldInfo GetField(string name, BindingFlags bindingFlags) => throw new NotImplementedException();
    public override FieldInfo[] GetFields(BindingFlags bindingFlags) => throw new NotImplementedException();
    public override Type[] GetInterfaces() => throw new NotImplementedException();
    protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    protected override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    public override MethodInfo[] GetMethods(BindingFlags bindingFlags) => throw new NotImplementedException();
    public override PropertyInfo[] GetProperties(BindingFlags bindingFlags) => throw new NotImplementedException();
    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingFlags, Binder? binder, Type? @return, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    protected override bool HasElementTypeImpl() => false;
    protected override bool IsArrayImpl() => false;
    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    public override bool IsGenericParameter => true;
    protected override bool IsPointerImpl() => false;
    public override string? Namespace => null;
    public override string Name => throw new NotImplementedException();
    public override string ToString() => Name;
    public override Type UnderlyingSystemType => this;
}
abstract class RuntimeGenericTypeParameter : RuntimeGenericParameter
{
    public override bool IsGenericTypeParameter => true;
}
abstract class RuntimeGenericMethodParameter : RuntimeGenericParameter
{
    public override bool IsGenericMethodParameter => true;
}
abstract class RuntimeGenericParameterPointer : Type
{
    public override Assembly Assembly => throw new NotSupportedException();
    public override string? AssemblyQualifiedName => null;
    public override Type BaseType => throw new NotImplementedException();
    public override string FullName => throw new NotSupportedException();
    protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
    protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[] types, ParameterModifier[]? modifiers) => null;
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingFlags) => Array.Empty<ConstructorInfo>();
    public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
    public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
    public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
    public override Type? GetElementType() => throw new NotImplementedException();
    public override FieldInfo GetField(string name, BindingFlags bindingFlags) => throw new NotImplementedException();
    public override FieldInfo[] GetFields(BindingFlags bindingFlags) => throw new NotImplementedException();
    public override Type[] GetInterfaces() => throw new NotImplementedException();
    protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    protected override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingFlags, Binder? binder, CallingConventions callingConventions, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    public override MethodInfo[] GetMethods(BindingFlags bindingFlags) => throw new NotImplementedException();
    public override PropertyInfo[] GetProperties(BindingFlags bindingFlags) => throw new NotImplementedException();
    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingFlags, Binder? binder, Type? @return, Type[]? types, ParameterModifier[]? modifiers) => throw new NotImplementedException();
    protected override bool HasElementTypeImpl() => true;
    protected override bool IsArrayImpl() => throw new NotImplementedException();
    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    protected override bool IsPointerImpl() => throw new NotImplementedException();
    public override string? Namespace => null;
    public override string Name => throw new NotImplementedException();
    public override string ToString() => Name;
    public override Type UnderlyingSystemType => this;
}
class RuntimeCustomAttributeData : CustomAttributeData
{
    public static IList<CustomAttributeData> Get(MemberInfo member) => throw new NotImplementedException();
    public static object[] GetAttributes(MemberInfo member, Type attributeType, bool inherit)
    {
        IEnumerable<CustomAttributeData> get(MemberInfo m) => m.GetCustomAttributesData().Where(x => attributeType.IsAssignableFrom(x.AttributeType));
        var data = get(member);
        if (inherit)
            switch (member)
            {
                case Type t:
                    while (true)
                    {
                        var u = t.BaseType;
                        if (u == null) break;
                        t = u;
                        data = data.Concat(get(t));
                    }
                    break;
                case RuntimeMethodInfo m:
                    while (true)
                    {
                        m = m.GetParentDefinition();
                        if (m == null) break;
                        data = data.Concat(get(m));
                    }
                    break;
            }
        var cads = data.ToList();
        object? value(CustomAttributeTypedArgument x)
        {
            if (x.Value == null) return null;
            var type = x.ArgumentType;
            if (type.IsEnum) return Enum.ToObject(type, x.Value);
            if (!type.IsArray) return x.Value;
            var xs = (ReadOnlyCollection<CustomAttributeTypedArgument>)x.Value;
            var ys = Array.CreateInstance(type.GetElementType() ?? throw new Exception(), xs.Count);
            for (var i = 0; i < ys.Length; ++i) ys.SetValue(value(xs[i]), i);
            return ys;
        }
        var attributes = Array.CreateInstance(attributeType, cads.Count);
        for (var i = 0; i < cads.Count; ++i)
        {
            var cad = cads[i];
            var cas = new object?[cad.ConstructorArguments.Count];
            for (var j = 0; j < cas.Length; ++j) cas[j] = value(cad.ConstructorArguments[j]);
            var a = cad.Constructor.Invoke(cas);
            foreach (var x in cad.NamedArguments)
                if (x.MemberInfo is FieldInfo f)
                    f.SetValue(a, value(x.TypedValue));
                else
                    ((PropertyInfo)x.MemberInfo).SetValue(a, value(x.TypedValue));
            attributes.SetValue(a, i);
        }
        return (object[])attributes;
    }
    public static bool InternalIsDefined(MemberInfo member, Type attributeType) => throw new NotImplementedException();
    public static bool IsDefined(MemberInfo member, Type attributeType, bool inherit)
    {
        if (InternalIsDefined(member, attributeType)) return true;
        if (!inherit) return false;
        switch (member)
        {
            case Type t:
                while (true)
                {
                    var u = t.BaseType;
                    if (u == null) break;
                    t = u;
                    if (InternalIsDefined(t, attributeType)) return true;
                }
                break;
            case RuntimeMethodInfo m:
                while (true)
                {
                    m = m.GetParentDefinition();
                    if (m == null) break;
                    if (InternalIsDefined(m, attributeType)) return true;
                }
                break;
        }
        return false;
    }

    public RuntimeCustomAttributeData(ConstructorInfo constructor, IList<CustomAttributeTypedArgument> constructorArguments, IList<CustomAttributeNamedArgument> namedArguments)
    {
        AttributeType = constructor.DeclaringType ?? throw new Exception();
        Constructor = constructor;
        ConstructorArguments = constructorArguments;
        NamedArguments = namedArguments;
    }
    public override Type AttributeType { get; }
    public override ConstructorInfo Constructor { get; }
    public override IList<CustomAttributeTypedArgument> ConstructorArguments { get; }
    public override IList<CustomAttributeNamedArgument> NamedArguments { get; }
}
static class RuntimeTimer
{
    private static readonly Dictionary<int, DateTime> id2at = [];
    private static Thread? thread;

    public static void Call(int id) => throw new NotImplementedException();
    public static nint Create(uint duration, int id)
    {
        lock (id2at)
        {
            if (thread == null)
            {
                thread = new Thread(() =>
                {
                    while (true)
                    {
                        int? id = null;
                        lock (id2at) while (true)
                        {
                            var now = DateTime.Now;
                            var next = now + TimeSpan.FromMilliseconds(int.MaxValue);
                            foreach (var (key, value) in id2at)
                                if (value > now)
                                {
                                    if (value < next) next = value;
                                }
                                else
                                {
                                    id = key;
                                    id2at.Remove(key);
                                    break;
                                }
                            if (id != null) break;
                            Monitor.Wait(id2at, next - now);
                        }
                        Call(id.Value);
                    }
                })
                {
                    IsBackground = true
                };
                thread.Start();
            }
            id2at.Add(id, DateTime.Now + TimeSpan.FromMilliseconds(duration));
            Monitor.Pulse(id2at);
            return (nint)id;
        }
    }
    public static bool Change(nint handle, uint duration)
    {
        lock (id2at)
        {
            id2at[(int)handle] = DateTime.Now + TimeSpan.FromMilliseconds(duration);
            Monitor.Pulse(id2at);
        }
        return true;
    }
    public static bool Delete(nint handle)
    {
        lock (id2at) id2at.Remove((int)handle);
        return true;
    }
}
