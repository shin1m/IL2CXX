using System.Reflection;

namespace IL2CXX;

public struct MethodKey : IEquatable<MethodKey>
{
    public static MethodKey ToKey(MethodBase method) => new(method);

    public readonly MethodBase Method;

    public MethodKey(MethodBase method)
    {
        var t = method.DeclaringType;
        Method = t == null || method.ReflectedType == t ? method : t.GetMethod(
            method.Name,
            method.GetGenericArguments().Length,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            method.GetParameters().Select(x => x.ParameterType).ToArray(),
            null
        ) ?? throw new Exception();
    }
    public static bool operator ==(MethodKey x, MethodKey y) => x.Method == y.Method;
    public static bool operator !=(MethodKey x, MethodKey y) => !(x == y);
    public bool Equals(MethodKey x) => this == x;
    public override bool Equals(object? x) => x is MethodKey y && this == y;
    public override int GetHashCode() => Method.GetHashCode();
}
