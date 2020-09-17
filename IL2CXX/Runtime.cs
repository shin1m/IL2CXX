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
        public override AssemblyName GetName(bool copiedName) => new AssemblyName { Name = FullName };
    }
    public abstract class RuntimeConstructorInfo : ConstructorInfo
    {
        public override object Invoke(BindingFlags bindingFlags, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();
    }
    public abstract class RuntimeMethodInfo : MethodInfo
    {
        public override Type DeclaringType => throw new NotImplementedException();
    }
    public abstract class RuntimeType : Type
    {
        public override Assembly Assembly => throw new NotImplementedException();
        public override Type BaseType => throw new NotImplementedException();
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingFlags, Binder binder, CallingConventions callingConventions, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
        protected override bool IsArrayImpl() => throw new NotImplementedException();
        public override bool IsAssignableFrom(Type c) => throw new NotImplementedException();
        public override string Namespace => throw new NotImplementedException();
        public override RuntimeTypeHandle TypeHandle => throw new NotImplementedException();
        public override Type UnderlyingSystemType => this;
    }
}
