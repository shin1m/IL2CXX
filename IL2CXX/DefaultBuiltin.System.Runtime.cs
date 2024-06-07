using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Loader;

namespace IL2CXX;

partial class DefaultBuiltin
{
    private static void SetupIntrinsicsVector(Func<Type, Type> get, Type type, Builtin.Code code, Type typeofVectorOfT)
    {
        SetupVector(get, type, code, typeofVectorOfT, nameof(Vector64.Sqrt));
        foreach (var x in type.GetMethods().Where(x => x.Name == nameof(Vector64.Widen))) code.For(x, transpiler =>
        {
            var t = x.ReturnType.GenericTypeArguments[0];
            var v = transpiler.EscapeForStacked(t);
            var e = transpiler.EscapeForStacked(t.GenericTypeArguments[0]);
            return ($@"{'\t'}auto p0 = reinterpret_cast<{transpiler.EscapeForStacked(x.GetParameters()[0].ParameterType.GenericTypeArguments[0])}*>(&a_0);
{'\t'}auto n = sizeof({v}) / sizeof({e});
{'\t'}{v} x;
{'\t'}auto p1 = reinterpret_cast<{e}*>(&x);
{'\t'}for (size_t i = 0; i < n; ++i) p1[i] = p0[i];
{'\t'}{v} y;
{'\t'}auto p2 = reinterpret_cast<{e}*>(&y);
{'\t'}for (size_t i = 0; i < n; ++i) p2[i] = p0[n + i];
{'\t'}return {{x, y}};
", 1);
        });
        code.ForGeneric(
            type.GetMethod(nameof(Vector64.ExtractMostSignificantBits)),
            (transpiler, types) =>
            {
                var t = types[0];
                var e = transpiler.EscapeForStacked(t);
                return ($@"{'\t'}uint32_t value{{}};
{'\t'}auto p0 = reinterpret_cast<{(t == get(typeof(float)) ? "uint32_t" : t == get(typeof(double)) ? "uint64_t" : $"std::make_unsigned_t<{e}>")}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) value |= p0[i] >> (sizeof({e}) * 8 - 1) << i;
{'\t'}return value;
", 1);
            }
        );
    }
    private static void SetupIntrinsicsVectorOfT(Type type, Builtin.Code code)
    {
        SetupVectorOfT(type, code);
        // TODO
        code.ForGeneric(
            type.GetMethod(nameof(ToString)),
            (transpiler, types) => ($"\treturn f__new_string(u\"{type.MakeGenericType(types)}\"sv);\n", 0)
        );
    }
    private static Builtin ForIf(this Builtin @this, Type type, Action<Type, Builtin.Code> action) => type == null ? @this : @this.For(type, action);
    private static Builtin SetupSystemRuntime(this Builtin @this, Func<Type, Type> get) => @this
    .For(get(typeof(GCHandle)), (type, code) =>
    {
        code.For(
            type.GetMethod("InternalAlloc", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($"\treturn {transpiler.EscapeForStacked(get(typeof(IntPtr)))}{{a_1 < 2 ? static_cast<t__handle*>(new t__weak_handle(a_0, a_1)) : new t__normal_handle(a_0)}};\n", 1)
        );
        code.For(
            type.GetMethod("InternalFree", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tdelete static_cast<t__handle*>(a_0.v__5fvalue);\n", 1)
        );
        code.For(
            type.GetMethod("InternalGet", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\treturn static_cast<t__handle*>(a_0.v__5fvalue)->f_target();\n", 1)
        );
        code.For(
            type.GetMethod("InternalSet", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tstatic_cast<t__handle*>(a_0.v__5fvalue)->f_target__(a_1);\n", 1)
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
            type.GetMethod(nameof(Marshal.Copy), [get(typeof(IntPtr)), get(typeof(byte[])), get(typeof(int)), get(typeof(int))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_1") + "\tstd::memcpy(a_1->f_data() + a_2, a_0, a_3);\n", 1)
        );
        code.For(
            type.GetMethod(nameof(Marshal.DestroyStructure), [get(typeof(IntPtr)), get(typeof(Type))]),
            transpiler => ($@"{'\t'}if (a_1->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}static_cast<t__type*>(a_1)->f_destroy_unmanaged(a_0);
", 1)
        );
        var gdffpi = type.GetMethod("GetDelegateForFunctionPointerInternal", BindingFlags.Static | BindingFlags.NonPublic);
        code.For(gdffpi, transpiler => ($@"{'\t'}auto type = static_cast<t__type*>(a_1);
{'\t'}auto p = static_cast<{transpiler.EscapeForStacked(get(typeof(Delegate)))}>(type->f_new_zerod());
{'\t'}p->v__5ftarget = p;
{'\t'}p->v__5fmethodPtr = type->v__invoke_unmanaged;
{'\t'}p->v__5fmethodPtrAux = a_0;
{'\t'}return p;
", 0));
        code.For(
            type.GetMethod(nameof(Marshal.GetDelegateForFunctionPointer), [get(typeof(IntPtr)), get(typeof(Type))]),
            transpiler =>
            {
                var md = $"&t__type_of<{transpiler.Escape(get(typeof(MulticastDelegate)))}>::v__instance";
                transpiler.Enqueue(gdffpi);
                return (transpiler.GenerateCheckArgumentNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (a_1->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto type = static_cast<t__type*>(a_1);
{'\t'}if (type->v__generic_definition) throw std::runtime_error(""must be non generic"");
{'\t'}if (type->v__base != {md} && type != {md}) throw std::runtime_error(""must be delegate"");
{'\t'}return {transpiler.Escape(gdffpi)}(a_0, a_1);
", 0);
            }
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
            type.GetMethod(nameof(Marshal.GetExceptionForHR), [get(typeof(int)), get(typeof(IntPtr))]),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
        code.For(
            type.GetMethod("IsPinnable", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\treturn true;\n", 1)
        );
        var ptsh = type.GetMethod("PtrToStructureHelper", BindingFlags.Static | BindingFlags.NonPublic, null, [get(typeof(IntPtr)), get(typeof(object)), get(typeof(bool))], null);
        code.For(ptsh, transpiler => ($"\ta_1->f_type()->f_from_unmanaged(a_1, a_0);\n", 1));
        code.For(
            type.GetMethod(nameof(Marshal.PtrToStructure), [get(typeof(IntPtr)), get(typeof(Type))]),
            transpiler =>
            {
                var create = get(typeof(Activator)).GetMethod(nameof(Activator.CreateInstance), [get(typeof(Type)), get(typeof(bool))]);
                transpiler.Enqueue(create);
                transpiler.Enqueue(ptsh);
                return (transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0) return nullptr;
{'\t'}if (a_1->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto type = static_cast<t__type*>(a_1);
{'\t'}if (type->v__generic_definition) throw std::runtime_error(""must be non generic"");
{'\t'}auto RECYCLONE__SPILL p = {transpiler.Escape(create)}(a_1, true);
{'\t'}{transpiler.Escape(ptsh)}(a_0, p, true);
{'\t'}return p;
", 0);
            }
        );
        var soh = type.GetMethod("SizeOfHelper", BindingFlags.Static | BindingFlags.NonPublic);
        code.For(soh, transpiler => ($@"{'\t'}auto type = static_cast<t__type*>(a_0);
{'\t'}if (a_1 && type->v__unmanaged_size <= 0) throw std::runtime_error(""not marshalable"");
{'\t'}return type->v__unmanaged_size;
", 1));
        code.For(
            type.GetMethod(nameof(Marshal.SizeOf), [get(typeof(Type))]),
            transpiler =>
            {
                transpiler.Enqueue(soh);
                return (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (a_0->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto type = static_cast<t__type*>(a_0);
{'\t'}if (type->v__generic_definition) throw std::runtime_error(""must be non generic"");
{'\t'}return {transpiler.Escape(soh)}(a_0, true);
", 0);
            }
        );
        code.For(
            type.GetMethod(nameof(Marshal.StructureToPtr), [get(typeof(object)), get(typeof(IntPtr)), get(typeof(bool))]),
            transpiler => ($@"{'\t'}if (a_2) a_0->f_type()->f_destroy_unmanaged(a_1);
{'\t'}return a_0->f_type()->f_to_unmanaged(a_0, a_1);
", 1)
        );
    })
    .For(get(typeof(MemoryMarshal)), (type, code) =>
    {
        code.ForGeneric(
            type.GetMethod(nameof(MemoryMarshal.GetArrayDataReference), 1, [Type.MakeGenericMethodParameter(0).MakeArrayType()]),
            (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(a_0->f_data());\n", 1)
        );
        code.For(
            type.GetMethod(nameof(MemoryMarshal.GetArrayDataReference), [get(typeof(Array))]),
            transpiler => ("\treturn reinterpret_cast<uint8_t*>(a_0->f_bounds() + a_0->f_type()->v__rank);\n", 1)
        );
    })
    .For(get(typeof(NativeLibrary)), (type, code) =>
    {
        code.For(
            type.GetMethod("LoadLibraryByName", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    })
    .For(get(typeof(RuntimeFeature)), (type, code) =>
    {
        code.For(
            type.GetProperty(nameof(RuntimeFeature.IsDynamicCodeCompiled)).GetMethod,
            transpiler => ($"\treturn false;\n", 1)
        );
        code.For(
            type.GetProperty(nameof(RuntimeFeature.IsDynamicCodeSupported)).GetMethod,
            transpiler => ($"\treturn false;\n", 1)
        );
    })
    .For(get(typeof(RuntimeHelpers)), (type, code) =>
    {
        code.ForGeneric(
            type.GetMethod(nameof(RuntimeHelpers.CreateSpan)),
            (transpiler, types) => {
                var t = transpiler.EscapeForValue(types[0]);
                return ($@"{'\t'}auto p = static_cast<t__runtime_field_info*>(a_0.v__field);
{'\t'}return {{static_cast<{t}*>(p->f_address(nullptr)), p->v__field_type->v__size / sizeof({t})}};
", 1);
            }
        );
        code.For(
            type.GetMethod(nameof(GetHashCode), BindingFlags.Static | BindingFlags.Public),
            transpiler => ("\treturn reinterpret_cast<intptr_t>(static_cast<t__object*>(a_0));\n", 1)
        );
        code.For(
            type.GetMethod(nameof(RuntimeHelpers.GetUninitializedObject)),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (a_0->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}return static_cast<t__type*>(a_0)->f_new_zerod();
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
            type.GetMethod("ObjectHasComponentSize", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}auto type = a_0->f_type();
{'\t'}return type == &t__type_of<{transpiler.Escape(get(typeof(string)))}>::v__instance || type->v__array;
", 1)
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
            type.GetMethod("TryGetHashCode", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\treturn reinterpret_cast<intptr_t>(static_cast<t__object*>(a_0));\n", 1)
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
            transpiler => ("\treturn static_cast<t__object*>(static_cast<t__dependent_handle*>(a_0.v__5fvalue)->f_get().first);\n", 1)
        );
        code.For(
            type.GetMethod("InternalGetDependent", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\treturn static_cast<t__object*>(static_cast<t__dependent_handle*>(a_0.v__5fvalue)->f_get().second);\n", 1)
        );
        code.For(
            type.GetMethod("InternalGetTargetAndDependent", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}auto [target, dependent] = static_cast<t__dependent_handle*>(a_0.v__5fvalue)->f_get();
{'\t'}f__store(*a_1, static_cast<t__object*>(dependent));
{'\t'}return static_cast<t__object*>(target);
", 1)
        );
        code.For(
            type.GetMethod("InternalSetDependent", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tstatic_cast<t__dependent_handle*>(a_0.v__5fvalue)->f_dependent__(a_1);\n", 1)
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
    .For(get(Type.GetType("System.Runtime.Intrinsics.Scalar`1", true)), (type, code) =>
    {
        code.ForGeneric(
            type.GetProperty("AllBitsSet").GetMethod,
            (transpiler, types) =>
            {
                var e = transpiler.EscapeForStacked(types[0]);
                return ($@"{'\t'}{e} value;
{'\t'}std::memset(&value, 0xff, sizeof({e}));
{'\t'}return value;
", 1);
            }
        );
        code.ForGeneric(
            type.GetProperty("One").GetMethod,
            (transpiler, types) => ("\treturn 1;\n", 1)
        );
    })
    .For(get(typeof(Vector64)), (type, code) => SetupIntrinsicsVector(get, type, code, get(typeof(Vector64<>))))
    .For(get(typeof(Vector128)), (type, code) => SetupIntrinsicsVector(get, type, code, get(typeof(Vector128<>))))
    .For(get(typeof(Vector256)), (type, code) => SetupIntrinsicsVector(get, type, code, get(typeof(Vector256<>))))
    .For(get(typeof(Vector512)), (type, code) => SetupIntrinsicsVector(get, type, code, get(typeof(Vector512<>))))
    .For(get(typeof(Vector64<>)), SetupIntrinsicsVectorOfT)
    .For(get(typeof(Vector128<>)), SetupIntrinsicsVectorOfT)
    .For(get(typeof(Vector256<>)), SetupIntrinsicsVectorOfT)
    .For(get(typeof(Vector512<>)), SetupIntrinsicsVectorOfT)
    .ForIf(get(Type.GetType("System.Runtime.Versioning.CompatibilitySwitch", true)), (type, code) =>
    {
        // TODO
        code.For(
            type.GetMethod("GetValueInternal", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    })
    .For(get(typeof(Unsafe)), (type, code) =>
    {
        var t0 = Type.MakeGenericMethodParameter(0);
        var t0ref = t0.MakeByRefType();
        var methods = GenericMethods(type);
        void additive(string name, char @operator)
        {
            var voidp = type.GetMethod(name, [get(typeof(void*)), get(typeof(int))]);
            foreach (var m in methods.Where(x => x != voidp && x.Name == name))
                code.ForGeneric(m, (transpiler, types) => ($"\treturn a_0 {@operator} a_1;\n", 1));
            code.ForGeneric(voidp, (transpiler, types) => ($"\treturn static_cast<char*>(a_0) {@operator} a_1;\n", 1));
        }
        additive(nameof(Unsafe.Add), '+');
        additive(nameof(Unsafe.Subtract), '-');
        void offset(string name, char @operator)
        {
            foreach (var x in new[] { typeof(IntPtr), typeof(UIntPtr) }) code.ForGeneric(
                type.GetMethod(name, [t0ref, get(x)]),
                (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(reinterpret_cast<char*>(a_0) {@operator} a_1);\n", 1)
            );
        }
        offset(nameof(Unsafe.AddByteOffset), '+');
        offset(nameof(Unsafe.SubtractByteOffset), '-');
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.AreSame)),
            (transpiler, types) => ("\treturn a_0 == a_1;\n", 1)
        );
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.As), 1, [get(typeof(object))]),
            (transpiler, types) => ($"\treturn static_cast<{transpiler.EscapeForStacked(types[0])}>(a_0);\n", 1)
        );
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.As), 2, [t0ref]),
            (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[1])}*>(a_0);\n", 1)
        );
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.AsPointer)),
            (transpiler, types) => ("\treturn a_0;\n", 1)
        );
        foreach (var m in methods.Where(x => x.Name == nameof(Unsafe.AsRef)))
            code.ForGeneric(m, (transpiler, types) => ($"\treturn static_cast<{transpiler.EscapeForStacked(((MethodInfo)m).MakeGenericMethod(types).ReturnType)}>(a_0);\n", 1));
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.ByteOffset)),
            (transpiler, types) => ("\treturn reinterpret_cast<char*>(a_1) - reinterpret_cast<char*>(a_0);\n", 1)
        );
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.Copy), 1, [get(typeof(void*)), t0ref]),
            (transpiler, types) => ($"\t*static_cast<{transpiler.EscapeForStacked(types[0])}*>(a_0) = *a_1;\n", 1)
        );
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.Copy), 1, [t0ref, get(typeof(void*))]),
            (transpiler, types) => ($"\t*a_0 = *static_cast<{transpiler.EscapeForValue(types[0])}*>(a_1);\n", 1)
        );
        foreach (var x in new[] { typeof(void*), typeof(byte).MakeByRefType() })
        {
            foreach (var name in new[] { nameof(Unsafe.CopyBlock), nameof(Unsafe.CopyBlockUnaligned) }) code.For(
                type.GetMethod(name, [get(x), get(x), get(typeof(uint))]),
                transpiler => ("\tstd::memcpy(a_0, a_1, a_2);\n", 1)
            );
            foreach (var name in new[] { nameof(Unsafe.InitBlock), nameof(Unsafe.InitBlockUnaligned) }) code.For(
                type.GetMethod(name, [get(x), get(typeof(byte)), get(typeof(uint))]),
                transpiler => ("\tstd::memset(a_0, a_1, a_2);\n", 1)
            );
            code.ForGeneric(
                type.GetMethod(nameof(Unsafe.ReadUnaligned), [get(x)]),
                (transpiler, types) => ($"\treturn *reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(a_0);\n", 1)
            );
            code.ForGeneric(
                type.GetMethod(nameof(Unsafe.WriteUnaligned), [get(x), t0]),
                (transpiler, types) => ($"\t*reinterpret_cast<{transpiler.EscapeForMember(types[0])}*>(a_0) = a_1;\n", 1)
            );
        }
        void relation(string name, char @operator) => code.ForGeneric(
            type.GetMethod(name),
            (transpiler, types) => ($"\treturn reinterpret_cast<uintptr_t>(a_0) {@operator} reinterpret_cast<uintptr_t>(a_1);\n", 1)
        );
        relation(nameof(Unsafe.IsAddressGreaterThan), '>');
        relation(nameof(Unsafe.IsAddressLessThan), '<');
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.SizeOf)),
            (transpiler, types) => ($"\treturn sizeof({transpiler.EscapeForValue(types[0])});\n", 1)
        );
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.SkipInit)),
            (transpiler, types) => (string.Empty, 1)
        );
        code.ForGeneric(
            type.GetMethod(nameof(Unsafe.Unbox)),
            (transpiler, types) => ($"\treturn reinterpret_cast<{transpiler.EscapeForValue(types[0])}*>(a_0 + 1);\n", 1)
        );
    })
    .For(get(typeof(AssemblyLoadContext)), (type, code) =>
    {
        code.For(
            type.GetMethod("GetLoadedAssemblies", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}size_t n = 0;
{'\t'}for (auto p = v__assemblies; *p; ++p) ++n;
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(Assembly[])))}, {transpiler.Escape(get(typeof(Assembly)))}>(n);
{'\t'}std::copy_n(v__assemblies, n, p->f_data());
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(AssemblyLoadContext.LoadFromStream), [get(typeof(Stream)), get(typeof(Stream))]),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    });
}
