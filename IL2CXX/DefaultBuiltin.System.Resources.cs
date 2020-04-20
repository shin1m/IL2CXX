using System;
using System.Collections.Generic;
using System.IO;
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
                transpiler => ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}return p;
", false)
            );
        })
        .For(typeof(ResourceReader), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(typeof(BinaryReader))} v__5fstore;
{'\t'}{transpiler.EscapeForMember(typeof(Dictionary<,>).MakeGenericType(typeof(string), Type.GetType("System.Resources.ResourceLocator")))} v__5fresCache;
{'\t'}int64_t v__5fnameSectionOffset;
{'\t'}int64_t v__5fdataSectionOffset;
{'\t'}{transpiler.EscapeForMember(typeof(int[]))} v__5fnameHashes;
{'\t'}int32_t* v__5fnameHashesPtr;
{'\t'}{transpiler.EscapeForMember(typeof(int[]))} v__5fnamePositions;
{'\t'}int32_t* v__5fnamePositionsPtr;
{'\t'}{transpiler.EscapeForMember(typeof(Type[]))} v__5ftypeTable;
{'\t'}{transpiler.EscapeForMember(typeof(int[]))} v__5ftypeNamePositions;
{'\t'}int32_t v__5fnumResources;
{'\t'}{transpiler.EscapeForMember(typeof(UnmanagedMemoryStream))} v__5fums;
{'\t'}int32_t v__5fversion;
", true);
            code.For(
                type.GetMethod("_LoadObjectV1", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", false)
            );
        })
        .For(Type.GetType("System.Resources.RuntimeResourceSet"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetString", new[] { typeof(string), typeof(bool) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", false)
            );
        });
    }
}
