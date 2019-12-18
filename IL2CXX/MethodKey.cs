using System;
using System.Reflection;

namespace IL2CXX
{
    public struct MethodKey : IEquatable<MethodKey>
    {
        public static MethodKey ToKey(MethodBase method) => new MethodKey(method);

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
}
