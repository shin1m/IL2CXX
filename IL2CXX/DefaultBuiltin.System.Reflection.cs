using System;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemReflection(this Builtin @this) => @this
        .For(typeof(LocalVariableInfo), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForVariable(typeof(Type))} v_m_5ftype;
{'\t'}int32_t v_m_5fisPinned;
{'\t'}int32_t v_m_5flocalIndex;
", true);
        })
        .For(typeof(CustomAttributeData), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}t_slot_of<t_System_2eReflection_2eConstructorInfo> v_m_5fctor;
{'\t'}{transpiler.EscapeForVariable(typeof(Module))} v_m_5fscope;
{'\t'}//t_slot_of<t_System_2eReflection_2eMemberInfo_5b_5d> v_m_5fmembers;
{'\t'}//t_slot_of<t_System_2eReflection_2eCustomAttributeCtorParameter_5b_5d> v_m_5fctorParams;
{'\t'}//t_slot_of<t_System_2eReflection_2eCustomAttributeNamedParameter_5b_5d> v_m_5fnamedParams;
{'\t'}t_slot_of<t_System_2eObject> v_m_5ftypedCtorArgs;
{'\t'}t_slot_of<t_System_2eObject> v_m_5fnamedArgs;
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
