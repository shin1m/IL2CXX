using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace IL2CXX;

partial class DefaultBuiltin
{
    private static Builtin SetupSystemReflection(this Builtin @this, Func<Type, Type> get, PlatformID target) => @this
    .For(get(typeof(Assembly)), (type, code) =>
    {
        code.For(
            type.GetMethod(nameof(Assembly.GetEntryAssembly)),
            transpiler => ("\treturn v__entry_assembly;\n", 1)
        );
        code.For(
            type.GetMethod(nameof(Assembly.GetExecutingAssembly)),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
        if (target == PlatformID.Win32NT)
            code.For(
                type.GetProperty(nameof(Assembly.Location))!.GetMethod,
                transpiler => ($@"{'\t'}char cs[MAX_PATH];
{'\t'}auto n = GetModuleFileNameA(NULL, cs, sizeof(cs));
{'\t'}if (n == 0) throw std::system_error(GetLastError(), std::system_category());
{'\t'}return f__new_string(std::string_view(cs, n));
", 0)
            );
        else
            code.For(
                type.GetProperty(nameof(Assembly.Location))!.GetMethod,
                transpiler => ($@"{'\t'}char cs[PATH_MAX];
{'\t'}auto r = readlink(""/proc/self/exe"", cs, sizeof(cs));
{'\t'}if (r == -1) throw std::system_error(errno, std::generic_category());
{'\t'}return f__new_string(std::string_view(cs, static_cast<size_t>(r)));
", 0)
            );
    })
    .For(get(typeof(CustomAttributeData)), (type, code) =>
    {
        code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(get(typeof(ConstructorInfo)))} v_m_5fctor;
{'\t'}{transpiler.EscapeForMember(get(typeof(Module)))} v_m_5fscope;
{'\t'}//{transpiler.EscapeForMember(get(typeof(MemberInfo[])))} v_m_5fmembers;
{'\t'}//{transpiler.EscapeForMember(get(Type.GetType("System.Reflection.CustomAttributeCtorParameter[]", true)!))} v_m_5fctorParams;
{'\t'}//{transpiler.EscapeForMember(get(Type.GetType("System.Reflection.CustomAttributeNamedParameter[]", true)!))} v_m_5fnamedParams;
{'\t'}{transpiler.EscapeForMember(get(typeof(object)))} v_m_5ftypedCtorArgs;
{'\t'}{transpiler.EscapeForMember(get(typeof(object)))} v_m_5fnamedArgs;
", true, null);
        // TODO
        code.For(
            type.GetProperty(nameof(CustomAttributeData.ConstructorArguments))!.GetMethod,
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
        // TODO
        code.For(
            type.GetProperty(nameof(CustomAttributeData.NamedArguments))!.GetMethod,
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    })
    .For(get(typeof(LocalVariableInfo)), (type, code) =>
    {
        code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(get(typeof(Type)))} v_m_5ftype;
{'\t'}int32_t v_m_5fisPinned;
{'\t'}int32_t v_m_5flocalIndex;
", true, null);
    })
    .For(get(typeof(MethodBase)), (type, code) =>
    {
        code.For(
            type.GetMethod(nameof(MethodBase.GetMethodFromHandle), [get(typeof(RuntimeMethodHandle))]),
            transpiler => ("\treturn a_0.v__method;\n", 1)
        );
        code.For(
            type.GetMethod(nameof(MethodBase.GetMethodFromHandle), [get(typeof(RuntimeMethodHandle)), get(typeof(RuntimeTypeHandle))]),
            transpiler => ("\treturn a_0.v__method;\n", 1)
        );
    })
    .For(get(typeof(ParameterInfo)), (type, code) =>
    {
        code.For(
            type.GetProperty(nameof(ParameterInfo.DefaultValue))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v_DefaultValueImpl;\n", 0)
        );
        code.For(
            type.GetProperty(nameof(ParameterInfo.HasDefaultValue))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + $"\treturn a_0->v_AttrsImpl & {(int)ParameterAttributes.HasDefault};\n", 0)
        );
    })
    .For(get(typeof(AssemblyBuilder)), (type, code) =>
    {
        code.Members = transpiler => (string.Empty, false, null);
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    })
    .For(get(typeof(ConstructorBuilder)), (type, code) =>
    {
        code.Members = transpiler => (string.Empty, false, null);
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    })
    .For(get(typeof(DynamicMethod)), (type, code) =>
    {
        code.StaticMembers = transpiler => string.Empty;
        code.Members = transpiler => (string.Empty, false, null);
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    })
    .For(get(typeof(MethodBuilder)), (type, code) =>
    {
        code.Members = transpiler => ("\tbool v_m_5fbIsBaked;\n", false, null);
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    })
    .For(get(typeof(ModuleBuilder)), (type, code) =>
    {
        code.Members = transpiler => (string.Empty, false, null);
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    })
    .For(get(typeof(SignatureHelper)), (type, code) =>
    {
        code.Members = transpiler => (string.Empty, false, null);
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    })
    .For(get(typeof(TypeBuilder)), (type, code) =>
    {
        code.Members = transpiler => (string.Empty, false, null);
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    })
    .For(get(typeof(MetadataUpdater)), (type, code) =>
    {
        code.Members = transpiler => (string.Empty, false, null);
        code.For(
            type.GetProperty(nameof(MetadataUpdater.IsSupported))!.GetMethod,
            transpiler => ("\treturn false;\n", 1)
        );
        code.AnyToBody = (transpiler, method) => ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 0);
    });
}
