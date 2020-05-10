using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemRuntime(this Builtin @this) => @this
        .For(typeof(ExceptionDispatchInfo), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(ExceptionDispatchInfo.Capture)),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(GCHandle), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalAlloc", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($"\treturn {transpiler.EscapeForValue(typeof(IntPtr))}{{a_1 < 2 ? static_cast<t__handle*>(new t__weak_handle(a_0, a_1)) : new t__normal_handle(a_0)}};\n", 1)
            );
            code.For(
                type.GetMethod("InternalFree", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tdelete static_cast<t__handle*>(a_0.v__5fvalue);\n", 1)
            );
            code.For(
                type.GetMethod("InternalGet", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn static_cast<t__handle*>(a_0.v__5fvalue)->f_target();\n", 1)
            );
        })
        .For(Type.GetType("System.Runtime.CompilerServices.DependentHandle"), (type, code) =>
        {
            code.For(
                type.GetConstructor(new[] { typeof(object), typeof(object) }),
                transpiler => ($@"{'\t'}{transpiler.EscapeForValue(type)} value;
{'\t'}value.v__5fhandle = new t__dependent_handle(a_0, a_1);
{'\t'}return value;
", 1)
            );
            code.For(
                type.GetMethod("GetPrimary"),
                transpiler => ("\treturn static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue)->f_target();\n", 1)
            );
            code.For(
                type.GetMethod("GetPrimaryAndSecondary"),
                transpiler => ($@"{'\t'}auto p = static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue);
{'\t'}auto primary = p->f_target();
{'\t'}f__store(*a_1, primary ? static_cast<t_object*>(p->v_secondary) : nullptr);
{'\t'}return primary;
", 1)
            );
            code.For(
                type.GetMethod("SetPrimary"),
                transpiler => ("\tstatic_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue)->f_target__(a_1);\n", 1)
            );
            code.For(
                type.GetMethod("SetSecondary"),
                transpiler => ($@"{'\t'}auto p = static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue);
{'\t'}if (auto primary = p->f_target()) p->v_secondary = a_1;
", 1)
            );
            code.For(
                type.GetMethod("Free"),
                transpiler => ($@"{'\t'}if (auto p = static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue)) {{
{'\t'}{'\t'}a_0->v__5fhandle.v__5fvalue = nullptr;
{'\t'}{'\t'}delete p;
{'\t'}}}
", 1)
            );
        })
        .For(Type.GetType("System.Runtime.CompilerServices.JitHelpers"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetRawSzArrayData", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn reinterpret_cast<uint8_t*>(a_0->f__bounds() + 1);\n", 1)
            );
        })
        .For(Type.GetType("System.Runtime.Versioning.CompatibilitySwitch"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetValueInternal", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(RuntimeHelpers), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(RuntimeHelpers.GetHashCode), BindingFlags.Static | BindingFlags.Public),
                transpiler => ("\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n", 1)
            );
            code.For(
                type.GetMethod(nameof(RuntimeHelpers.InitializeArray)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\tstd::memcpy(a_0->f__bounds() + a_0->f_type()->v__rank, a_1.v__field, a_0->f_type()->v__element->v__size * a_0->v__length);\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("IsReferenceOrContainsReferences"),
                (transpiler, types) => ($"\treturn {(transpiler.Define(types[0]).IsManaged ? "true" : "false")};\n", 1)
            );
            code.For(
                type.GetProperty(nameof(RuntimeHelpers.OffsetToStringData)).GetMethod,
                transpiler => ($"\treturn offsetof({transpiler.Escape(typeof(string))}, v__5ffirstChar);\n", 1)
            );
            code.For(
                type.GetMethod("TryEnsureSufficientExecutionStack"),
                transpiler => ("\treturn true;\n", 1)
            );
            code.For(
                type.GetMethod("ObjectHasComponentSize", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto type = a_0->f_type();
{'\t'}return type == &t__type_of<{transpiler.Escape(typeof(string))}>::v__instance || type->f__is(&t__type_of<{transpiler.Escape(typeof(Array))}>::v__instance);
", 1)
            );
        })
        .For(typeof(Marshal), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Marshal.Copy), new[] { typeof(IntPtr), typeof(byte[]), typeof(int), typeof(int) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_1") + "\tstd::memcpy(a_1->f__data() + a_2, a_0, a_3);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Marshal.GetLastWin32Error)),
                transpiler => ("\treturn errno;\n", 1)
            );
            code.For(
                type.GetMethod("SetLastWin32Error", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\terrno = a_0;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Marshal.GetExceptionForHR), new[] { typeof(int), typeof(IntPtr) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            code.For(
                type.GetMethod("IsPinnable", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn true;\n", 1)
            );
            code.For(
                type.GetMethod("SizeOfHelper", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}if (a_1 && a_0->v__managed) throw std::runtime_error(""not marshalable"");
{'\t'}return a_0->v__size;
", 1)
            );
        })
        .For(Type.GetType("Internal.Runtime.CompilerServices.Unsafe"), (type, code) =>
        {
            var methods = GenericMethods(type);
            code.ForGeneric(
                methods.First(x => x.Name == "Add" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ("\treturn a_0 + a_1;\n", 1)
            );
            code.ForGeneric(
                methods.First(x => x.Name == "AddByteOffset" && x.GetGenericArguments().Length == 1 && x.GetParameters()[1].ParameterType == typeof(ulong)),
                (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(reinterpret_cast<char*>(a_0) + a_1);\n", 1)
            );
            code.ForGeneric(
                methods.First(x => x.Name == "AddByteOffset" && x.GetGenericArguments().Length == 1 && x.GetParameters()[1].ParameterType == typeof(IntPtr)),
                (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(reinterpret_cast<char*>(a_0) + reinterpret_cast<intptr_t>(a_1.v__5fvalue));\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("AreSame"),
                (transpiler, types) => ("\treturn a_0 == a_1;\n", 1)
            );
            code.ForGeneric(
                methods.First(x => x.Name == "As" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ($"\treturn static_cast<{transpiler.EscapeForValue(types[0])}>(a_0);\n", 1)
            );
            code.ForGeneric(
                methods.First(x => x.Name == "As" && x.GetGenericArguments().Length == 2),
                (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[1])}*>(a_0);\n", 1)
            );
            code.ForGeneric(
                methods.First(x => x.Name == "AsPointer" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ("\treturn a_0;\n", 1)
            );
            foreach (var m in methods.Where(x => x.Name == "ReadUnaligned" && x.GetGenericArguments().Length == 1))
                code.ForGeneric(m,
                    (transpiler, types) => ($"\treturn *reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(a_0);\n", 1)
                );
            foreach (var m in methods.Where(x => x.Name == "WriteUnaligned" && x.GetGenericArguments().Length == 1))
                code.ForGeneric(m,
                    (transpiler, types) => ($"\t*reinterpret_cast<{transpiler.EscapeForMember(types[0])}*>(a_0) = a_1;\n", 1)
                );
            code.ForGeneric(
                methods.First(x => x.Name == "SizeOf" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ($"\treturn sizeof({transpiler.EscapeForValue(types[0])});\n", 1)
            );
        });
    }
}
