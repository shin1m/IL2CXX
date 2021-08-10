using System;

namespace IL2CXX
{
    public interface IBuiltin
    {
        string GetBase(Type type);
        (string members, bool managed, string unmanaged) GetMembers(Transpiler transpiler, Type type);
        string GetInitialize(Transpiler transpiler, Type type);
        (string body, int inline) GetBody(Transpiler transpiler, MethodKey key);
    }
}
