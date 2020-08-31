using System;
using System.IO;
using System.Reflection;

namespace IL2CXX
{
    public class RuntimeAssembly : Assembly
    {
        public static readonly RuntimeAssembly Instance = new RuntimeAssembly();

        public override Stream GetManifestResourceStream(string name) => null;
    }
    public abstract class RuntimeType : Type
    {
        public override Assembly Assembly => RuntimeAssembly.Instance;
        public override Type BaseType => throw new NotImplementedException();
        public override bool IsAssignableFrom(Type c) => throw new NotImplementedException();
        public override RuntimeTypeHandle TypeHandle => throw new NotImplementedException();
        public override Type UnderlyingSystemType => this;
    }
}
