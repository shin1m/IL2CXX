using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace IL2CXX
{
    class RuntimeAssembly : Assembly
    {
        public static void Initialize(AssemblyName x)
        {
            var xs = x.Name.Split(",");
            if (xs.Length < 2) return;
            x.Name = xs[0].Trim();
            for (var i = 1; i < xs.Length; ++i)
            {
                var ys = xs[i].Split("=", 2);
                if (ys.Length < 2) continue;
                var key = ys[0].Trim();
                if (key == "Version")
                    x.Version = new Version(ys[1]);
                else if (key == "Culture")
                    try
                    {
                        x.CultureInfo = new CultureInfo(ys[1]);
                    }
                    catch { }
                else if (key == "PublicKeyToken")
                    try
                    {
                        x.SetPublicKeyToken(Convert.FromHexString(ys[1]));
                    }
                    catch { }
            }
        }

        public override MethodInfo EntryPoint => throw new NotImplementedException();
        public override string FullName => throw new NotImplementedException();
        public string Name => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<Attribute>();
        public override Type[] GetExportedTypes() => throw new NotImplementedException();
        public override Stream GetManifestResourceStream(string name) => throw new NotImplementedException();
        public override AssemblyName GetName(bool copiedName) => new(FullName);
        public override Type GetType(string name, bool throwOnError, bool ignoreCase) => Type.GetType($"{name}, {FullName}", throwOnError, ignoreCase);
    }
    abstract class RuntimeFieldInfo : FieldInfo
    {
        public override FieldAttributes Attributes => throw new NotImplementedException();
        public override Type DeclaringType => throw new NotImplementedException();
        public override Type FieldType => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
        public override object GetValue(object @this) => throw new NotImplementedException();
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
        public override void SetValue(object @this, object value, BindingFlags bindingFlags, Binder binder, CultureInfo culture) => throw new NotImplementedException();
    }
    abstract class RuntimeConstructorInfo : ConstructorInfo
    {
        public override MethodAttributes Attributes => throw new NotImplementedException();
        public override Type DeclaringType => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
        public override ParameterInfo[] GetParameters() => throw new NotImplementedException();
        public override object Invoke(BindingFlags bindingFlags, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
    }
    abstract class RuntimeMethodInfo : MethodInfo
    {
        public override MethodAttributes Attributes => throw new NotImplementedException();
        public override Delegate CreateDelegate(Type type) => throw new NotImplementedException();
        public override Delegate CreateDelegate(Type type, object target) => throw new NotImplementedException();
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
        public override object Invoke(object @this, BindingFlags bindingFlags, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override MethodInfo MakeGenericMethod(params Type[] types) => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
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
        public PropertyInfo GetParentDefinition(Type[] parameters) => ((RuntimeMethodInfo)(GetMethod ?? SetMethod)).GetParentDefinition()?.DeclaringType.GetProperty(Name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, PropertyType, parameters, null);
        public override object GetValue(object @this, BindingFlags bindingFlags, Binder binder, object[] index, CultureInfo culture) => throw new NotImplementedException();
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
        public override Type PropertyType => throw new NotImplementedException();
        public override MethodInfo SetMethod => throw new NotImplementedException();
        public override void SetValue(object @this, object value, BindingFlags bindingFlags, Binder binder, object[] index, CultureInfo culture) => throw new NotImplementedException();
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
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
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
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        protected override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        public override MethodInfo[] GetMethods(BindingFlags bindingFlags) => throw new NotImplementedException();
        public override PropertyInfo[] GetProperties(BindingFlags bindingFlags) => throw new NotImplementedException();
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingFlags, Binder binder, Type @return, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        protected override TypeCode GetTypeCodeImpl() => throw new NotImplementedException();
        protected override bool HasElementTypeImpl() => throw new NotImplementedException();
        protected override bool IsArrayImpl() => throw new NotImplementedException();
        public override bool IsAssignableFrom(Type c) => throw new NotImplementedException();
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
        public override string ToString() => throw new NotImplementedException();
        public override RuntimeTypeHandle TypeHandle => throw new NotImplementedException();
        public override Type UnderlyingSystemType => this;

        public static bool ValueEquals(RuntimeType type, IntPtr x, object y) => throw new NotImplementedException();
        public static int ValueGetHashCode(RuntimeType type, IntPtr x) => throw new NotImplementedException();
        public static string ValueToString(RuntimeType type, IntPtr x) => throw new NotImplementedException();
    }
    abstract class RuntimeGenericParameter : Type
    {
        public override Assembly Assembly => throw new NotSupportedException();
        public override string AssemblyQualifiedName => null;
        public override Type BaseType => throw new NotImplementedException();
        public override string FullName => throw new NotSupportedException();
        protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => null;
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingFlags) => Array.Empty<ConstructorInfo>();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();
        public override Type GetElementType() => null;
        public override FieldInfo GetField(string name, BindingFlags bindingFlags) => throw new NotImplementedException();
        public override FieldInfo[] GetFields(BindingFlags bindingFlags) => throw new NotImplementedException();
        public override Type[] GetInterfaces() => throw new NotImplementedException();
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        protected override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        public override MethodInfo[] GetMethods(BindingFlags bindingFlags) => throw new NotImplementedException();
        public override PropertyInfo[] GetProperties(BindingFlags bindingFlags) => throw new NotImplementedException();
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingFlags, Binder binder, Type @return, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        protected override bool HasElementTypeImpl() => false;
        protected override bool IsArrayImpl() => false;
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override bool IsGenericParameter => true;
        protected override bool IsPointerImpl() => false;
        public override string Namespace => null;
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
                            t = t.BaseType;
                            if (t == null) break;
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
            object value(CustomAttributeTypedArgument x)
            {
                var type = x.ArgumentType;
                if (type.IsEnum) return Enum.ToObject(type, x.Value);
                if (!type.IsArray) return x.Value;
                type = type.GetElementType();
                var xs = (ReadOnlyCollection<CustomAttributeTypedArgument>)x.Value;
                var ys = Array.CreateInstance(type, xs.Count);
                if (type.IsEnum)
                    for (var i = 0; i < ys.Length; ++i) ys.SetValue(Enum.ToObject(type, xs[i].Value), i);
                else
                    for (var i = 0; i < ys.Length; ++i) ys.SetValue(xs[i].Value, i);
                return ys;
            }
            var attributes = Array.CreateInstance(attributeType, cads.Count);
            for (var i = 0; i < cads.Count; ++i)
            {
                var cad = cads[i];
                var cas = new object[cad.ConstructorArguments.Count];
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
                        t = t.BaseType;
                        if (t == null) break;
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
            AttributeType = constructor.DeclaringType;
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
        private static readonly Dictionary<int, DateTime> id2at = new();
        private static Thread thread;

        public static void Call(int id) => throw new NotImplementedException();
        public static IntPtr Create(uint duration, int id)
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
                return (IntPtr)id;
            }
        }
        public static bool Change(IntPtr handle, uint duration)
        {
            lock (id2at)
            {
                id2at[(int)handle] = DateTime.Now + TimeSpan.FromMilliseconds(duration);
                Monitor.Pulse(id2at);
            }
            return true;
        }
        public static bool Delete(IntPtr handle)
        {
            lock (id2at) id2at.Remove((int)handle);
            return true;
        }
    }
}
