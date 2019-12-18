using System;
using System.Reflection;
using System.Resources;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemResources(this Builtin @this) => @this
        .For(typeof(ResourceManager), (type, code) =>
        {
            code.For(
                type.GetConstructor(new[] { typeof(string), typeof(Assembly) }),
                transpiler => $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}return p;
"
            );
        })
        .For(typeof(ResourceReader), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}t_slot_of<t_System_2eIO_2eBinaryReader> v__5fstore;
{'\t'}t_slot_of<t_System_2eCollections_2eGeneric_2eDictionary_602_5bSystem_2eString_2cSystem_2eResources_2eResourceLocator_5d> v__5fresCache;
{'\t'}int64_t v__5fnameSectionOffset;
{'\t'}int64_t v__5fdataSectionOffset;
{'\t'}t_slot_of<t_System_2eInt32_5b_5d> v__5fnameHashes;
{'\t'}int32_t* v__5fnameHashesPtr;
{'\t'}t_slot_of<t_System_2eInt32_5b_5d> v__5fnamePositions;
{'\t'}int32_t* v__5fnamePositionsPtr;
{'\t'}{transpiler.EscapeForVariable(typeof(Type[]))} v__5ftypeTable;
{'\t'}t_slot_of<t_System_2eInt32_5b_5d> v__5ftypeNamePositions;
{'\t'}int32_t v__5fnumResources;
{'\t'}t_slot_of<t_System_2eIO_2eUnmanagedMemoryStream> v__5fums;
{'\t'}int32_t v__5fversion;
", true);
            code.For(
                type.GetMethod("_LoadObjectV1", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(Type.GetType("System.Resources.RuntimeResourceSet"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetString", new[] { typeof(string), typeof(bool) }),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        });
    }
}
