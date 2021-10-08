using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin ForIf(this Builtin @this, Type type, Action<Type, Builtin.Code> action) => type == null ? @this : @this.For(type, action);
        private static Builtin SetupSystemRuntime(this Builtin @this, Func<Type, Type> get) => @this
        .For(get(typeof(ExceptionDispatchInfo)), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod(nameof(ExceptionDispatchInfo.Capture)),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(get(typeof(GCHandle)), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalAlloc", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($"\treturn {transpiler.EscapeForValue(get(typeof(IntPtr)))}{{a_1 < 2 ? static_cast<t__handle*>(new t__weak_handle(a_0, a_1)) : new t__normal_handle(a_0)}};\n", 1)
            );
            code.For(
                type.GetMethod("InternalFree", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tdelete static_cast<t__handle*>(a_0.v__5fvalue);\n", 1)
            );
            code.For(
                type.GetMethod("InternalGet", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn static_cast<t__handle*>(a_0.v__5fvalue)->f_target();\n", 1)
            );
            // TODO
            code.For(
                type.GetMethod("InternalSet", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(get(typeof(GCSettings)), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(GCSettings.IsServerGC)).GetMethod,
                transpiler => ("\treturn false;\n", 1)
            );
            code.For(
                type.GetProperty(nameof(GCSettings.LatencyMode)).GetMethod,
                transpiler => ("\treturn 1;\n", 1)
            );
        })
        .For(get(typeof(Marshal)), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Marshal.Copy), new[] { get(typeof(IntPtr)), get(typeof(byte[])), get(typeof(int)), get(typeof(int)) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_1") + "\tstd::memcpy(a_1->f_data() + a_2, a_0, a_3);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Marshal.DestroyStructure), new[] { get(typeof(IntPtr)), get(typeof(Type)) }),
                transpiler => ($@"{'\t'}if (a_1->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}static_cast<t__type*>(a_1)->f_destroy_unmanaged(a_0);
", 1)
            );
            code.For(
                type.GetMethod("GetDelegateForFunctionPointerInternal", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}if (a_1->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto p = static_cast<t__type*>(a_1);
{'\t'}auto q = static_cast<{transpiler.EscapeForValue(get(typeof(Delegate)))}>(f_engine()->f_allocate(sizeof({transpiler.Escape(get(typeof(MulticastDelegate)))})));
{'\t'}q->v__5ftarget = q;
{'\t'}q->v__5fmethodPtr = p->v__invoke_unmanaged;
{'\t'}q->v__5fmethodPtrAux = a_0;
{'\t'}p->f_finish(q);
{'\t'}return q;
", 0)
            );
            code.For(
                type.GetMethod("GetFunctionPointerForDelegateInternal", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}if (a_0->v__5fmethodPtr == a_0->f_type()->v__invoke_unmanaged) return a_0->v__5fmethodPtrAux;
{'\t'}return v__managed_method_to_unmanaged.at(a_0->v__5fmethodPtrAux);
", 0)
            );
            code.For(
                type.GetMethod(nameof(Marshal.GetLastPInvokeError)),
                transpiler => ("\treturn v_last_unmanaged_error;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Marshal.SetLastPInvokeError)),
                transpiler => ("\tv_last_unmanaged_error = a_0;\n", 1)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Marshal.GetExceptionForHR), new[] { get(typeof(int)), get(typeof(IntPtr)) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            code.For(
                type.GetMethod("IsPinnable", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn true;\n", 1)
            );
            code.For(
                type.GetMethod("PtrToStructureHelper", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { get(typeof(IntPtr)), get(typeof(object)), get(typeof(bool)) }, null),
                transpiler => ($"\ta_1->f_type()->f_from_unmanaged(a_1, a_0);\n", 1)
            );
            code.For(
                type.GetMethod("SizeOfHelper", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}if (a_0->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto p = static_cast<t__type*>(a_0);
{'\t'}if (a_1 && p->v__unmanaged_size <= 0) throw std::runtime_error(""not marshalable"");
{'\t'}return p->v__unmanaged_size;
", 1)
            );
            code.For(
                type.GetMethod(nameof(Marshal.StructureToPtr), new[] { get(typeof(object)), get(typeof(IntPtr)), get(typeof(bool)) }),
                transpiler => ($@"{'\t'}if (a_2) a_0->f_type()->f_destroy_unmanaged(a_1);
{'\t'}return a_0->f_type()->f_to_unmanaged(a_0, a_1);
", 1)
            );
        })
        .For(get(typeof(MemoryMarshal)), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod(nameof(MemoryMarshal.GetArrayDataReference), 1, new[] { Type.MakeGenericMethodParameter(0).MakeArrayType() }),
                (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(a_0->f_data());\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MemoryMarshal.GetArrayDataReference), new[] { get(typeof(Array)) }),
                transpiler => ("\treturn reinterpret_cast<uint8_t*>(a_0->f_bounds() + a_0->f_type()->v__rank);\n", 1)
            );
        })
        .For(get(typeof(NativeLibrary)), (type, code) =>
        {
            code.For(
                type.GetMethod("LoadLibraryByName", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(get(typeof(RuntimeFeature)), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(RuntimeFeature.IsDynamicCodeSupported)).GetMethod,
                transpiler => ($"\treturn false;\n", 1)
            );
        })
        .For(get(typeof(RuntimeHelpers)), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(GetHashCode), BindingFlags.Static | BindingFlags.Public),
                transpiler => ("\treturn reinterpret_cast<intptr_t>(static_cast<t__object*>(a_0));\n", 0)
            );
            code.For(
                type.GetMethod(nameof(RuntimeHelpers.GetUninitializedObject)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (a_0->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto type = static_cast<t__type*>(a_0);
{'\t'}auto p = f_engine()->f_allocate(type->v__managed_size);
{'\t'}std::memset(p + 1, 0, type->v__unmanaged_size - sizeof(t_object<t__type>));
{'\t'}type->f_finish(p);
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod(nameof(RuntimeHelpers.InitializeArray)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\tstd::memcpy(a_0->f_bounds() + a_0->f_type()->v__rank, static_cast<t__runtime_field_info*>(a_1.v__field)->f_address(nullptr), a_0->f_type()->v__element->v__size * a_0->v__length);\n", 1)
            );
            {
                bool isBitwiseEquatable(Type t) => Type.GetTypeCode(t.IsEnum ? t.GetEnumUnderlyingType() : t) switch
                {
                    TypeCode.Boolean => true,
                    TypeCode.Char => true,
                    TypeCode.SByte => true,
                    TypeCode.Byte => true,
                    TypeCode.Int16 => true,
                    TypeCode.UInt16 => true,
                    TypeCode.Int32 => true,
                    TypeCode.UInt32 => true,
                    TypeCode.Int64 => true,
                    TypeCode.UInt64 => true,
                    _ => false
                } || t == get(typeof(IntPtr)) || t == get(typeof(UIntPtr));
                code.ForGeneric(
                    type.GetMethod("IsBitwiseEquatable", BindingFlags.Static | BindingFlags.NonPublic),
                    (transpiler, types) => ($"\treturn {(isBitwiseEquatable(types[0]) ? "true" : "false")};\n", 1)
                );
            }
            code.ForGeneric(
                type.GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences)),
                (transpiler, types) => ($"\treturn {(transpiler.Define(types[0]).IsManaged ? "true" : "false")};\n", 1)
            );
            code.For(
                type.GetProperty(nameof(RuntimeHelpers.OffsetToStringData)).GetMethod,
                transpiler => ($"\treturn offsetof({transpiler.Escape(get(typeof(string)))}, v__5ffirstChar);\n", 1)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(RuntimeHelpers.TryEnsureSufficientExecutionStack)),
                transpiler => ("\treturn true;\n", 1)
            );
            code.For(
                type.GetMethod("ObjectHasComponentSize", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto type = a_0->f_type();
{'\t'}return type == &t__type_of<{transpiler.Escape(get(typeof(string)))}>::v__instance || type->f_is(&t__type_of<{transpiler.Escape(get(typeof(Array)))}>::v__instance);
", 1)
            );
        })
        .For(get(typeof(DependentHandle)), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalInitialize", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn new t__dependent_handle(a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod("InternalGetTarget", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn static_cast<t__dependent_handle*>(a_0.v__5fvalue)->f_target();\n", 1)
            );
            code.For(
                type.GetMethod("InternalGetDependent", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn static_cast<t__dependent_handle*>(a_0.v__5fvalue)->v_dependent;\n", 1)
            );
            code.For(
                type.GetMethod("InternalGetTargetAndDependent", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = static_cast<t__dependent_handle*>(a_0.v__5fvalue);
{'\t'}auto target = p->f_target();
{'\t'}f__store(*a_1, target ? static_cast<t__object*>(p->v_dependent) : nullptr);
{'\t'}return target;
", 1)
            );
            code.For(
                type.GetMethod("InternalSetDependent", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = static_cast<t__dependent_handle*>(a_0.v__5fvalue);
{'\t'}if (auto target = p->f_target()) p->v_dependent = a_1;
", 1)
            );
            code.For(
                type.GetMethod("InternalSetTargetToNull", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tstatic_cast<t__dependent_handle*>(a_0.v__5fvalue)->f_target__(nullptr);\n", 1)
            );
            code.For(
                type.GetMethod("InternalFree", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tdelete static_cast<t__dependent_handle*>(a_0.v__5fvalue);\n", 1)
            );
        })
        .For(get(Type.GetType("System.Runtime.Intrinsics.Vector128`1", true)), (type, code) =>
        {
            // TODO
            code.ForGeneric(
                type.GetMethod(nameof(ToString)),
                (transpiler, types) => ($"\treturn f__new_string(u\"{type.MakeGenericType(types)}\"sv);\n", 0)
            );
        })
        .For(get(Type.GetType("System.Runtime.Intrinsics.Vector256`1", true)), (type, code) =>
        {
            // TODO
            code.ForGeneric(
                type.GetMethod(nameof(ToString)),
                (transpiler, types) => ($"\treturn f__new_string(u\"{type.MakeGenericType(types)}\"sv);\n", 0)
            );
        })
        .ForIf(get(Type.GetType("System.Runtime.Versioning.CompatibilitySwitch", true)), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("GetValueInternal", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(get(Type.GetType("Internal.Runtime.CompilerServices.Unsafe", true)), (type, code) =>
        {
            var methods = GenericMethods(type);
            code.ForGeneric(
                methods.Single(x => x.Name == "Add" && x.GetGenericArguments().Length == 1 && x.GetParameters().Select(x => x.ParameterType).SequenceEqual(new[] { get(typeof(void*)), get(typeof(int)) })),
                (transpiler, types) => ("\treturn static_cast<char*>(a_0) + a_1;\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "Add" && x.GetGenericArguments().Length == 1 && x.GetParameters().Select(x => x.ParameterType).SequenceEqual(new[] { x.GetGenericArguments()[0].MakeByRefType(), get(typeof(int)) })),
                (transpiler, types) => ("\treturn a_0 + a_1;\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "Add" && x.GetGenericArguments().Length == 1 && x.GetParameters()[1].ParameterType == get(typeof(IntPtr))),
                (transpiler, types) => ("\treturn a_0 + static_cast<intptr_t>(a_1);\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "AddByteOffset" && x.GetGenericArguments().Length == 1 && x.GetParameters()[1].ParameterType == get(typeof(IntPtr))),
                (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(reinterpret_cast<char*>(a_0) + static_cast<intptr_t>(a_1));\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("AreSame"),
                (transpiler, types) => ("\treturn a_0 == a_1;\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "As" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ($"\treturn static_cast<{transpiler.EscapeForValue(types[0])}>(a_0);\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "As" && x.GetGenericArguments().Length == 2),
                (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[1])}*>(a_0);\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "AsPointer" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ("\treturn a_0;\n", 1)
            );
            foreach (var m in methods.Where(x => x.Name == "AsRef" && x.GetGenericArguments().Length == 1))
                code.ForGeneric(m, (transpiler, types) => ($"\treturn static_cast<{transpiler.EscapeForValue(((MethodInfo)m).MakeGenericMethod(types).ReturnType)}>(a_0);\n", 1));
            code.ForGeneric(
                methods.Single(x => x.Name == "ByteOffset" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ("\treturn reinterpret_cast<char*>(a_1) - reinterpret_cast<char*>(a_0);\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "IsAddressLessThan" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ("\treturn reinterpret_cast<uintptr_t>(a_0) < reinterpret_cast<uintptr_t>(a_1);\n", 1)
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
                methods.Single(x => x.Name == "SizeOf" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ($"\treturn sizeof({transpiler.EscapeForValue(types[0])});\n", 1)
            );
            code.ForGeneric(
                methods.Single(x => x.Name == "SkipInit" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => (string.Empty, 1)
            );
        })
        .For(get(typeof(AssemblyLoadContext)), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(AssemblyLoadContext.LoadFromStream), new[] { get(typeof(Stream)), get(typeof(Stream)) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        });
    }
}
