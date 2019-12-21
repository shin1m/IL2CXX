using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IL2CXX
{
    public class Builtin : IBuiltin
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public class Code
        {
            public Func<Transpiler, (string, bool)> Members;
            public Func<Transpiler, string> Initialize;
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, string>> MethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, string>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], string>> GenericMethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], string>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, string>> MethodTreeToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, string>>();

            public void For(MethodBase method, Func<Transpiler, string> body) => MethodToBody.Add(method.MethodHandle, body);
            public void ForGeneric(MethodBase method, Func<Transpiler, Type[], string> body) => GenericMethodToBody.Add(method.MethodHandle, body);
            public void ForTree(MethodInfo method, Func<Transpiler, Type, string> body) => MethodTreeToBody.Add(method.MethodHandle, body);
        }

        public Dictionary<Type, Code> TypeToCode = new Dictionary<Type, Code>();
        public Dictionary<string, Dictionary<string, Func<Transpiler, MethodBase, string>>> TypeNameToMethodNameToBody = new Dictionary<string, Dictionary<string, Func<Transpiler, MethodBase, string>>>();
        public Dictionary<string, Func<Transpiler, MethodBase, string>> MethodNameToBody = new Dictionary<string, Func<Transpiler, MethodBase, string>>();

        public Builtin For(Type type, Action<Type, Code> action)
        {
            var code = new Code();
            TypeToCode.Add(type, code);
            action(type, code);
            return this;
        }

        public (string members, bool managed) GetMembers(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Members?.Invoke(transpiler) ?? (null, false) : (null, false);
        public string GetInitialize(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Initialize?.Invoke(transpiler) : null;
        public string GetBody(Transpiler transpiler, MethodBase method)
        {
            var type = method.DeclaringType;
            var handle = method.MethodHandle;
            if (type.IsSubclassOf(typeof(Delegate)) && type != typeof(MulticastDelegate))
            {
                var invoke = type.GetMethod("Invoke");
                if (handle == type.GetConstructor(new[] { typeof(object), typeof(IntPtr) }).MethodHandle)
                {
                    var @return = invoke.ReturnType;
                    var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                    return $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5ftarget = std::move(a_0);
{'\t'}if (p->v__5ftarget) {{
{'\t'}{'\t'}p->v__5fmethodPtr = a_1;
{'\t'}}} else {{
{'\t'}{'\t'}p->v__5ftarget = p;
{'\t'}{'\t'}p->v__5fmethodPtr = reinterpret_cast<void*>(static_cast<{
    transpiler.EscapeForScoped(@return)
}(*)({
    string.Join(",", parameters.Prepend(type).Select(x => $"\n\t\t\t{transpiler.EscapeForScoped(x)}"))
}
{'\t'}{'\t'})>([]({
    string.Join(",", parameters.Prepend(type).Select((_, i) => $"\n\t\t\tauto a_{i}"))
}
{'\t'}{'\t'})
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}{(@return == typeof(void) ? string.Empty : "return ")}reinterpret_cast<{(@return == typeof(void) ? "void" : transpiler.EscapeForScoped(@return))}(*)({string.Join(", ", parameters.Select(transpiler.EscapeForScoped))})>(a_0->v__5fmethodPtrAux.v__5fvalue)({string.Join(", ", parameters.Select((x, i) => transpiler.FormatMove(x, $"a_{i + 1}")))});
{'\t'}{'\t'}}}));
{'\t'}{'\t'}p->v__5fmethodPtrAux = a_1;
{'\t'}}}
{'\t'}return p;
";
                }
                else if (handle == invoke.MethodHandle)
                {
                    var @return = invoke.ReturnType;
                    var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                    return $"\t{(@return == typeof(void) ? string.Empty : "return ")}reinterpret_cast<{(@return == typeof(void) ? "void" : transpiler.EscapeForScoped(@return))}(*)({string.Join(", ", parameters.Prepend(typeof(object)).Select(transpiler.EscapeForScoped))})>(a_0->v__5fmethodPtr.v__5fvalue)({string.Join(", ", parameters.Select((x, i) => transpiler.FormatMove(x, $"a_{i + 1}")).Prepend("a_0->v__5ftarget"))});\n";
                }
            }
            if (TypeToCode.TryGetValue(type, out var code))
            {
                if (code.MethodToBody.TryGetValue(handle, out var body0)) return body0(transpiler);
                if (method.IsGenericMethod && code.GenericMethodToBody.TryGetValue(((MethodInfo)method).GetGenericMethodDefinition().MethodHandle, out var body1)) return body1(transpiler, method.GetGenericArguments());
            }
            if (type.IsGenericType)
            {
                var gt = type.GetGenericTypeDefinition();
                if (TypeToCode.TryGetValue(gt, out var gc) && gc.GenericMethodToBody.TryGetValue(MethodBase.GetMethodFromHandle(handle, gt.TypeHandle).MethodHandle, out var body)) return body(transpiler, type.GetGenericArguments());
            }
            if (method is MethodInfo mi)
            {
                if (mi.IsGenericMethod) mi = mi.GetGenericMethodDefinition();
                var origin = mi.GetBaseDefinition().MethodHandle;
                for (var t = mi.DeclaringType;;)
                {
                    if (TypeToCode.TryGetValue(t, out var c) && c.MethodTreeToBody.TryGetValue(mi.MethodHandle, out var body)) return body(transpiler, type);
                    if (mi.MethodHandle == origin) break;
                    do
                    {
                        t = t.BaseType;
                        mi = t.GetMethods(declaredAndInstance).FirstOrDefault(x => x.GetBaseDefinition().MethodHandle == origin);
                    } while (mi == null);
                }
            }
            if (method.DeclaringType.FullName != null && TypeNameToMethodNameToBody.TryGetValue(method.DeclaringType.FullName, out var name2body) && name2body.TryGetValue(method.ToString(), out var body2)) return body2(transpiler, method);
            return MethodNameToBody.TryGetValue(method.ToString(), out var body3) ? body3(transpiler, method) : null;
        }
    }
}
