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
            public string Base;
            public Func<Transpiler, (string, bool)> Members;
            public Func<Transpiler, string> Initialize;
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, (string body, int inline)>> MethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, (string, int)>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], (string body, int inline)>> GenericMethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], (string, int)>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, (string body, int inline)>> MethodTreeToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, (string, int)>>();

            public void For(MethodBase method, Func<Transpiler, (string body, int inline)> body) => MethodToBody.Add(method.MethodHandle, body);
            public void ForGeneric(MethodBase method, Func<Transpiler, Type[], (string body, int inline)> body) => GenericMethodToBody.Add(method.MethodHandle, body);
            public void ForTree(MethodInfo method, Func<Transpiler, Type, (string body, int inline)> body) => MethodTreeToBody.Add(method.MethodHandle, body);
        }

        public Dictionary<Type, Code> TypeToCode = new Dictionary<Type, Code>();
        public Dictionary<string, Dictionary<string, Func<Transpiler, MethodBase, (string body, int inline)>>> TypeNameToMethodNameToBody = new Dictionary<string, Dictionary<string, Func<Transpiler, MethodBase, (string, int)>>>();
        public Dictionary<string, Func<Transpiler, MethodBase, (string body, int inline)>> MethodNameToBody = new Dictionary<string, Func<Transpiler, MethodBase, (string, int)>>();

        public Builtin For(Type type, Action<Type, Code> action)
        {
            var code = new Code();
            TypeToCode.Add(type, code);
            action(type, code);
            return this;
        }

        public string GetBase(Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Base : null;
        public (string members, bool managed) GetMembers(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Members?.Invoke(transpiler) ?? default : default;
        public string GetInitialize(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Initialize?.Invoke(transpiler) : null;
        public (string body, int inline) GetBody(Transpiler transpiler, MethodBase method)
        {
            var type = method.DeclaringType;
            var handle = method.MethodHandle;
            if (type.IsArray)
            {
                var rank = type.GetArrayRank();
                if (handle == type.GetConstructor(Enumerable.Repeat(typeof(int), rank).ToArray()).MethodHandle) return ((transpiler.CheckRange ? string.Join(string.Empty, Enumerable.Range(0, rank).Select(i => $"\tif (a_{i} < 0) [[unlikely]] f__throw_index_out_of_range();\n")) : string.Empty) + $@"{'\t'}auto n = {string.Join(" * ", Enumerable.Range(0, rank).Select(i => $"a_{i}"))};
{'\t'}auto extra = sizeof({transpiler.Escape(type.GetElementType())}) * n;
{'\t'}t__new<{transpiler.Escape(type)}> p(extra);
{'\t'}p->v__length = n;
{string.Join(string.Empty, Enumerable.Range(0, rank).Select(i => $"\tp->v__bounds[{i}] = {{static_cast<size_t>(a_{i}), 0}};\n"))
}{'\t'}std::memset(p->f__data(), 0, extra);
{'\t'}return p;
", 1);
                string prepare() => transpiler.GenerateCheckNull("a_0") + $@"{'\t'}size_t i = 0;
{'\t'}auto bounds = a_0->f__bounds();
{string.Join(string.Empty, Enumerable.Range(0, rank).Select(i => $@"{'\t'}{{
{'\t'}{'\t'}int j = a_{i + 1} - bounds[{i}].v_lower;
{'\t'}{transpiler.GenerateCheckRange($"j", $"bounds[{i}].v_length")
}{'\t'}{'\t'}i = i * bounds[{i}].v_length + j;
{'\t'}}}
"))}";
                if (handle == type.GetMethod("Get").MethodHandle) return (prepare() + "\treturn a_0->f__data()[i];\n", 1);
                if (handle == type.GetMethod("Set").MethodHandle) return (prepare() + $"\ta_0->f__data()[i] = a_{rank + 1};\n", 1);
                if (handle == type.GetMethod("Address").MethodHandle) return (prepare() + "\treturn a_0->f__data() + i;\n", 1);
            }
            if (type.IsSubclassOf(typeof(Delegate)) && type != typeof(MulticastDelegate))
            {
                var invoke = type.GetMethod("Invoke");
                if (handle == type.GetConstructor(new[] { typeof(object), typeof(IntPtr) }).MethodHandle)
                {
                    var @return = invoke.ReturnType;
                    var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                    return ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}if (a_0) {{
{'\t'}{'\t'}p->v__5ftarget = a_0;
{'\t'}{'\t'}p->v__5fmethodPtr = a_1;
{'\t'}}} else {{
{'\t'}{'\t'}p->v__5ftarget = p;
{'\t'}{'\t'}p->v__5fmethodPtr = reinterpret_cast<void*>(+[]({
    string.Join(",", parameters.Prepend(type).Select((x, i) => $"\n\t\t\t{transpiler.EscapeForStacked(x)} a_{i}"))
}
{'\t'}{'\t'}) -> {transpiler.EscapeForStacked(@return)}
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}{(@return == typeof(void) ? string.Empty : "return ")}reinterpret_cast<{(@return == typeof(void) ? "void" : transpiler.EscapeForStacked(@return))}(*)({string.Join(", ", parameters.Select(x => transpiler.EscapeForStacked(x)))})>(a_0->v__5fmethodPtrAux.v__5fvalue)({string.Join(", ", parameters.Select((x, i) => transpiler.CastValue(x, $"a_{i + 1}")))});
{'\t'}{'\t'}}});
{'\t'}{'\t'}p->v__5fmethodPtrAux = a_1;
{'\t'}}}
{'\t'}return p;
", 1);
                }
                else if (handle == invoke.MethodHandle)
                {
                    var @return = invoke.ReturnType;
                    var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                    return ($"\t{(@return == typeof(void) ? string.Empty : "return ")}reinterpret_cast<{(@return == typeof(void) ? "void" : transpiler.EscapeForStacked(@return))}(*)({string.Join(", ", parameters.Prepend(typeof(object)).Select(x => transpiler.EscapeForStacked(x)))})>(a_0->v__5fmethodPtr.v__5fvalue)({string.Join(", ", parameters.Select((x, i) => transpiler.CastValue(x, $"a_{i + 1}")).Prepend("a_0->v__5ftarget"))});\n", 1);
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
            return MethodNameToBody.TryGetValue(method.ToString(), out var body3) ? body3(transpiler, method) : default;
        }
    }
}
