using System;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemReflection(this Builtin @this) => @this
        .For(typeof(LocalVariableInfo), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(typeof(Type))} v_m_5ftype;
{'\t'}int32_t v_m_5fisPinned;
{'\t'}int32_t v_m_5flocalIndex;
", true);
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
", true);
            code.For(
                type.GetProperty(nameof(CustomAttributeData.ConstructorArguments)).GetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetProperty(nameof(CustomAttributeData.NamedArguments)).GetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        });
    }
}
