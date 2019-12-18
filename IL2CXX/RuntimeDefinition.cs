using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IL2CXX
{
    using static MethodKey;

    public class RuntimeDefinition : IEqualityComparer<Type[]>
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
}
