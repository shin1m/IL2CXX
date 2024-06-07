using System.Reflection;

namespace IL2CXX;
using static MethodKey;

public class Builtin : IBuiltin
{
    private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public class Code
    {
        public string Base;
        public Func<Transpiler, string> StaticMembers;
        public Func<Transpiler, (string, bool, string)> Members;
        public Func<Transpiler, Type[], (string, bool, string)> GenericMembers;
        public Func<Transpiler, string> Initialize;
        public Dictionary<MethodKey, Func<Transpiler, (string body, int inline)>> MethodToBody = new();
        public Dictionary<MethodKey, Func<Transpiler, Type[], (string body, int inline)>> GenericMethodToBody = new();
        public Dictionary<MethodKey, Func<Transpiler, Type, (string body, int inline)>> MethodTreeToBody = new();
        public Func<Transpiler, MethodBase, (string body, int inline)> AnyToBody;

        public void For(MethodBase method, Func<Transpiler, (string body, int inline)> body)
        {
            if (method.ReflectedType != method.DeclaringType) throw new InvalidOperationException($"{method.ReflectedType} != {method.DeclaringType}");
            MethodToBody[ToKey(method)] = body;
        }
        public void ForGeneric(MethodBase method, Func<Transpiler, Type[], (string body, int inline)> body) => GenericMethodToBody.Add(ToKey(method), body);
        public void ForTree(MethodInfo method, Func<Transpiler, Type, (string body, int inline)> body) => MethodTreeToBody.Add(ToKey(method), body);
    }

    public Dictionary<Type, Code> TypeToCode = new();
    public Dictionary<string, Dictionary<string, Func<Transpiler, MethodBase, (string body, int inline)>>> TypeNameToMethodNameToBody = new();
    public Dictionary<string, Func<Transpiler, MethodBase, (string body, int inline)>> MethodNameToBody = new();

    public Builtin For(Type type, Action<Type, Code> action)
    {
        if (!TypeToCode.TryGetValue(type, out var code)) TypeToCode.Add(type, code = new Code());
        action(type, code);
        return this;
    }

    public string GetBase(Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Base : null;
    public string GetStaticMembers(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.StaticMembers?.Invoke(transpiler) : null;
    public (string members, bool managed, string unmanaged) GetMembers(Transpiler transpiler, Type type)
    {
        if (TypeToCode.TryGetValue(type, out var code) && code.Members != null) return code.Members(transpiler);
        if (type.IsGenericType && TypeToCode.TryGetValue(type.GetGenericTypeDefinition(), out code) && code.GenericMembers != null) return code.GenericMembers(transpiler, type.GetGenericArguments());
        return default;
    }
    public string GetInitialize(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Initialize?.Invoke(transpiler) : null;
    public (string body, int inline) GetBody(Transpiler transpiler, MethodKey key)
    {
        var method = key.Method;
        var type = method.DeclaringType;
        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var element = type.GetElementType();
            if (method == type.GetConstructor(Enumerable.Repeat(transpiler.typeofInt32, rank).ToArray())) return ((transpiler.CheckRange ? string.Join(string.Empty, Enumerable.Range(0, rank).Select(i => $"\tif (a_{i} < 0) [[unlikely]] {transpiler.GenerateThrow("IndexOutOfRange")};\n")) : string.Empty) + $@"{'\t'}auto n = {string.Join(" * ", Enumerable.Range(0, rank).Select(i => $"a_{i}"))};
{'\t'}auto extra = sizeof({transpiler.EscapeForMember(element)}) * n;
{'\t'}t__new<{transpiler.Escape(type)}> p(extra);
{'\t'}p->v__length = n;
{string.Join(string.Empty, Enumerable.Range(0, rank).Select(i => $"\tp->v__bounds[{i}] = {{static_cast<size_t>(a_{i}), 0}};\n"))
}{'\t'}std::memset(p->f_data(), 0, extra);
{'\t'}return p;
", 0);
            if (rank == 1)
            {
                var indices = 0;
                for (var t = element; t.IsArray; t = t.GetElementType())
                {
                    ++indices;
                    if (method == type.GetConstructor(Enumerable.Repeat(transpiler.typeofInt32, indices + 1).ToArray()))
                    {
                        var c = element.GetConstructor(Enumerable.Repeat(transpiler.typeofInt32, indices).ToArray());
                        transpiler.Enqueue(c);
                        return ((transpiler.CheckRange ? $"\tif (a_0 < 0) [[unlikely]] {transpiler.GenerateThrow("IndexOutOfRange")};\n" : string.Empty) + $@"{'\t'}auto extra = sizeof({transpiler.EscapeForMember(element)}) * a_0;
{'\t'}t__new<{transpiler.Escape(type)}> p(extra);
{'\t'}p->v__length = a_0;
{'\t'}p->v__bounds[0] = {{static_cast<size_t>(a_0), 0}};
{'\t'}std::memset(p->f_data(), 0, extra);
{'\t'}for (size_t i = 0; i < a_0; ++i) p->f_data()[i] = {transpiler.Escape(c)}({string.Join(", ", Enumerable.Range(1, indices).Select(i => $"a_{i}"))});
{'\t'}return p;
", 0);
                    }
                }
            }
            string prepare() => transpiler.GenerateCheckNull("a_0") + $@"{'\t'}size_t i = 0;
{'\t'}auto bounds = a_0->f_bounds();
{string.Join(string.Empty, Enumerable.Range(0, rank).Select(i => $@"{'\t'}{{
{'\t'}{'\t'}int j = a_{i + 1} - bounds[{i}].v_lower;
{'\t'}{transpiler.GenerateCheckRange($"j", $"bounds[{i}].v_length")
}{'\t'}{'\t'}i = i * bounds[{i}].v_length + j;
{'\t'}}}
"))}";
            if (key == ToKey(type.GetMethod("Get"))) return (prepare() + "\treturn a_0->f_data()[i];\n", 1);
            if (key == ToKey(type.GetMethod("Set"))) return (prepare() + $"\ta_0->f_data()[i] = a_{rank + 1};\n", 1);
            var address = type.GetMethod("Address");
            if (key == ToKey(address)) return (prepare() + $"\treturn reinterpret_cast<{transpiler.EscapeForStacked(address.ReturnType)}>(a_0->f_data() + i);\n", 1);
        }
        if (type.IsSubclassOf(transpiler.typeofDelegate) && type != transpiler.typeofMulticastDelegate)
        {
            if (method == type.GetConstructor([transpiler.typeofObject, transpiler.typeofIntPtr])) return ($@"{'\t'}auto RECYCLONE__SPILL p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}if (a_0) {{
{'\t'}{'\t'}p->v__5ftarget = a_0;
{'\t'}{'\t'}p->v__5fmethodPtr = a_1;
{'\t'}}} else {{
{'\t'}{'\t'}p->v__5ftarget = p;
{'\t'}{'\t'}p->v__5fmethodPtr = t__type_of<{transpiler.Escape(type)}>::v__instance.v__invoke_static;
{'\t'}{'\t'}p->v__5fmethodPtrAux = a_1;
{'\t'}}}
{'\t'}return p;
", 1);
            var invoke = type.GetMethod("Invoke");
            if (key == ToKey(invoke))
            {
                var @return = invoke.ReturnType;
                var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                return ($"\treturn reinterpret_cast<{transpiler.EscapeForStacked(@return)}(*)({string.Join(", ", parameters.Prepend(transpiler.typeofObject).Select(x => transpiler.EscapeForStacked(x)))})>(a_0->v__5fmethodPtr.v__5fvalue)({string.Join(", ", parameters.Select((x, i) => transpiler.CastValue(x, $"a_{i + 1}")).Prepend("a_0->v__5ftarget"))});\n", 1);
            }
            if (key == ToKey(type.GetMethod("BeginInvoke"))) return ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0);
            if (key == ToKey(type.GetMethod("EndInvoke"))) return ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0);
        }
        if (TypeToCode.TryGetValue(type, out var code))
        {
            if (code.MethodToBody.TryGetValue(key, out var body0)) return body0(transpiler);
            if (method.IsGenericMethod && code.GenericMethodToBody.TryGetValue(ToKey(((MethodInfo)method).GetGenericMethodDefinition()), out var body1)) return body1(transpiler, method.GetGenericArguments());
            var body4 = code.AnyToBody?.Invoke(transpiler, method) ?? default;
            if (body4 != default) return body4;
        }
        if (type.IsGenericType)
        {
            var gt = type.GetGenericTypeDefinition();
            if (TypeToCode.TryGetValue(gt, out var gc))
            {
                MethodBase gm;
                if (method == type.TypeInitializer)
                {
                    gm = gt.TypeInitializer;
                }
                else
                {
                    var all = declaredAndInstance | BindingFlags.Static;
                    gm = method.IsConstructor
                        ? gt.GetConstructors(all)[Array.IndexOf(type.GetConstructors(all), method)]
                        : gt.GetMethods(all)[Array.IndexOf(type.GetMethods(all), method)];
                }
                if (gc.GenericMethodToBody.TryGetValue(ToKey(gm), out var body)) return body(transpiler, type.GetGenericArguments());
            }
        }
        if (method is MethodInfo mi)
        {
            if (mi.IsGenericMethod) mi = mi.GetGenericMethodDefinition();
            var origin = Transpiler.GetBaseDefinition(mi);
            for (var t = mi.DeclaringType;;)
            {
                if (TypeToCode.TryGetValue(t, out var c) && c.MethodTreeToBody.TryGetValue(ToKey(mi), out var body)) return body(transpiler, type);
                if (mi == origin) break;
                do
                {
                    t = t.BaseType;
                    mi = t.GetMethods(declaredAndInstance).FirstOrDefault(x => Transpiler.GetBaseDefinition(x) == origin);
                } while (mi == null);
            }
        }
        if (method.DeclaringType.FullName != null && TypeNameToMethodNameToBody.TryGetValue(method.DeclaringType.FullName, out var name2body) && name2body.TryGetValue(method.ToString(), out var body2)) return body2(transpiler, method);
        return MethodNameToBody.TryGetValue(method.ToString(), out var body3) ? body3(transpiler, method) : default;
    }
}
