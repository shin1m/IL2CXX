using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace IL2CXX
{
    public class RuntimeAssembly : Assembly
    {
        public override MethodInfo EntryPoint => throw new NotImplementedException();
        public override string FullName => throw new NotImplementedException();
        public string Name => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<Attribute>();
        public override Stream GetManifestResourceStream(string name)
        {
            try
            {
                return File.OpenRead(Path.Combine(Path.GetDirectoryName(Location), "resources", Name, name));
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
        public override AssemblyName GetName(bool copiedName) => new() { Name = FullName };
    }
    public abstract class RuntimeFieldInfo : FieldInfo
    {
        public override FieldAttributes Attributes => throw new NotImplementedException();
        public override Type DeclaringType => throw new NotImplementedException();
        public override Type FieldType => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override object GetValue(object @this) => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
        public override void SetValue(object @this, object value, BindingFlags bindingFlags, Binder binder, CultureInfo culture) => throw new NotImplementedException();
    }
    public abstract class RuntimeConstructorInfo : ConstructorInfo
    {
        public override Type DeclaringType => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override ParameterInfo[] GetParameters() => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
        public override object Invoke(BindingFlags bindingFlags, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();
    }
    public abstract class RuntimeMethodInfo : MethodInfo
    {
        public override Type DeclaringType => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override ParameterInfo[] GetParameters() => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
        public override object Invoke(object @this, BindingFlags bindingFlags, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();
    }
    public abstract class RuntimePropertyInfo : PropertyInfo
    {
        public override PropertyAttributes Attributes => throw new NotImplementedException();
        public override Type DeclaringType => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override object GetValue(object @this, BindingFlags bindingFlags, Binder binder, object[] index, CultureInfo culture) => throw new NotImplementedException();
        public override void SetValue(object @this, object value, BindingFlags bindingFlags, Binder binder, object[] index, CultureInfo culture) => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
        public override Type PropertyType => throw new NotImplementedException();
    }
    public abstract class RuntimeType : Type
    {
        public override Assembly Assembly => throw new NotImplementedException();
        public override Type BaseType => throw new NotImplementedException();
        public override Type DeclaringType => throw new NotImplementedException();
        public override string FullName => throw new NotImplementedException();
        protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingFlags) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type type, bool inherit) => throw new NotImplementedException();
        public override string[] GetEnumNames() => throw new NotImplementedException();
        public override Array GetEnumValues() => throw new NotImplementedException();
        public override FieldInfo GetField(string name, BindingFlags bindingFlags) => throw new NotImplementedException();
        public override FieldInfo[] GetFields(BindingFlags bindingFlags) => throw new NotImplementedException();
        public override Type[] GetGenericArguments() => throw new NotImplementedException();
        public override Type GetGenericTypeDefinition() => throw new NotImplementedException();
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        public override MethodInfo[] GetMethods(BindingFlags bindingFlags) => throw new NotImplementedException();
        public override PropertyInfo[] GetProperties(BindingFlags bindingFlags) => throw new NotImplementedException();
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingFlags, Binder binder, Type @return, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        public override Type[] GetInterfaces() => throw new NotImplementedException();
        protected override bool HasElementTypeImpl() => throw new NotImplementedException();
        protected override bool IsArrayImpl() => throw new NotImplementedException();
        public override bool IsAssignableFrom(Type c) => throw new NotImplementedException();
        public override bool IsByRefLike => throw new NotImplementedException();
        public override bool IsConstructedGenericType => throw new NotImplementedException();
        public override bool IsGenericTypeDefinition => throw new NotImplementedException();
        protected override bool IsPointerImpl() => throw new NotImplementedException();
        public override Type MakeGenericType(params Type[] arguments) => throw new NotImplementedException();
        public override string Namespace => throw new NotImplementedException();
        public override string Name => throw new NotImplementedException();
        public override string ToString() => throw new NotImplementedException();
        public override RuntimeTypeHandle TypeHandle => throw new NotImplementedException();
        public override Type UnderlyingSystemType => this;
    }
}
