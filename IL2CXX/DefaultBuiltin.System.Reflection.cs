using System;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemReflection(this Builtin @this) => @this
        .For(typeof(Assembly), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Assembly.GetEntryAssembly)),
                transpiler => ("\treturn v__entry_assembly;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Assembly.GetExecutingAssembly)),
                transpiler => ("\treturn {};\n", 0)
            );
            code.For(
                type.GetMethod("IsRuntimeImplemented", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => ($"\treturn a_0 && !a_0->f_type()->f__is(&t__type_of<{transpiler.Escape(typeof(RuntimeAssembly))}>::v__instance);\n", 1)
            );
            code.For(
                type.GetProperty(nameof(Assembly.Location)).GetMethod,
                transpiler => ($@"{'\t'}char cs[PATH_MAX];
{'\t'}auto r = readlink(""/proc/self/exe"", cs, sizeof(cs));
{'\t'}if (r == -1) throw std::system_error(errno, std::generic_category());
{'\t'}return f__new_string(std::string_view(cs, static_cast<size_t>(r)));
", 0)
            );
        })
        .For(typeof(AssemblyName), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("ComputePublicKeyToken", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(CustomAttributeData), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(typeof(ConstructorInfo))} v_m_5fctor;
{'\t'}{transpiler.EscapeForMember(typeof(Module))} v_m_5fscope;
{'\t'}//{transpiler.EscapeForMember(typeof(MemberInfo[]))} v_m_5fmembers;
{'\t'}//{transpiler.EscapeForMember(Type.GetType("System.Reflection.CustomAttributeCtorParameter[]"))} v_m_5fctorParams;
{'\t'}//{transpiler.EscapeForMember(Type.GetType("System.Reflection.CustomAttributeNamedParameter[]"))} v_m_5fnamedParams;
{'\t'}{transpiler.EscapeForMember(typeof(object))} v_m_5ftypedCtorArgs;
{'\t'}{transpiler.EscapeForMember(typeof(object))} v_m_5fnamedArgs;
", true, null);
            // TODO
            code.For(
                type.GetProperty(nameof(CustomAttributeData.ConstructorArguments)).GetMethod,
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            // TODO
            code.For(
                type.GetProperty(nameof(CustomAttributeData.NamedArguments)).GetMethod,
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(LocalVariableInfo), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(typeof(Type))} v_m_5ftype;
{'\t'}int32_t v_m_5fisPinned;
{'\t'}int32_t v_m_5flocalIndex;
", true, null);
        });
    }
}
