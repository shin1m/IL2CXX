using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

namespace IL2CXX
{
    using static MethodKey;

    partial class Transpiler
    {
        [StructLayout(LayoutKind.Explicit)]
        private class SingleUnion
        {
            [FieldOffset(0)]
            public float Single;
            [FieldOffset(0)]
            public int Int32;
        }
        [StructLayout(LayoutKind.Explicit)]
        private class DoubleUnion
        {
            [FieldOffset(0)]
            public double Double;
            [FieldOffset(0)]
            public long Int64;
        }
        public Transpiler(Func<Type, Type> get, IBuiltin builtin, Action<string> log, PlatformID target, bool is64, bool checkNull = true, bool checkRange = true)
        {
            this.builtin = builtin;
            this.log = log;
            Target = target;
            Is64Bit = is64;
            CheckNull = checkNull;
            CheckRange = checkRange;
            getType = get;
            typeofObject = get(typeof(object));
            typeofValueType = get(typeof(ValueType));
            typeofRuntimeAssembly = get(typeof(RuntimeAssembly));
            typeofRuntimeFieldInfo = get(typeof(RuntimeFieldInfo));
            typeofRuntimeConstructorInfo = get(typeof(RuntimeConstructorInfo));
            typeofRuntimeMethodInfo = get(typeof(RuntimeMethodInfo));
            typeofRuntimePropertyInfo = get(typeof(RuntimePropertyInfo));
            typeofType = get(typeof(Type));
            typeofRuntimeType = get(typeof(RuntimeType));
            typeofRuntimeGenericTypeParameter = get(typeof(RuntimeGenericTypeParameter));
            typeofRuntimeGenericMethodParameter = get(typeof(RuntimeGenericMethodParameter));
            typeofBoolean = get(typeof(bool));
            typeofByte = get(typeof(byte));
            typeofSByte = get(typeof(sbyte));
            typeofInt16 = get(typeof(short));
            typeofUInt16 = get(typeof(ushort));
            typeofInt32 = get(typeof(int));
            typeofUInt32 = get(typeof(uint));
            typeofInt64 = get(typeof(long));
            typeofUInt64 = get(typeof(ulong));
            typeofVoidPointer = get(typeof(void*));
            typeofChar = get(typeof(char));
            typeofDouble = get(typeof(double));
            typeofSingle = get(typeof(float));
            typeofVoid = get(typeof(void));
            typeofIntPtr = get(typeof(IntPtr));
            typeofUIntPtr = get(typeof(UIntPtr));
            typeofNullable = get(typeof(Nullable<>));
            typeofString = get(typeof(string));
            typeofStringBuilder = get(typeof(StringBuilder));
            typeofException = get(typeof(Exception));
            typeofTypedReference = get(typeof(TypedReference));
            typeofTypedReferenceTag = get(typeof(TypedReferenceTag));
            typeofDelegate = get(typeof(Delegate));
            typeofMulticastDelegate = get(typeof(MulticastDelegate));
            typeofSafeHandle = get(typeof(SafeHandle));
            typeofAttribute = get(typeof(Attribute));
            typeofOutAttribute = get(typeof(OutAttribute));
            typeofDllImportAttribute = get(typeof(DllImportAttribute));
            typeofFieldOffsetAttribute = get(typeof(FieldOffsetAttribute));
            typeofMarshalAsAttribute = get(typeof(MarshalAsAttribute));
            typeofThreadStaticAttribute = get(typeof(ThreadStaticAttribute));
            typeofRuntimeFieldHandle = get(typeof(RuntimeFieldHandle));
            typeofRuntimeMethodHandle = get(typeof(RuntimeMethodHandle));
            typeofRuntimeTypeHandle = get(typeof(RuntimeTypeHandle));
            typeofSZArrayHelper = get(typeof(SZArrayHelper<>));
            typeofUtilities = get(typeof(Utilities));
            builtinTypes = new Dictionary<Type, string>
            {
                [typeofObject] = "t__object",
                [get(typeof(Assembly))] = "t__assembly",
                [typeofRuntimeAssembly] = "t__runtime_assembly",
                [get(typeof(MemberInfo))] = "t__member_info",
                [get(typeof(FieldInfo))] = "t__field_info",
                [typeofRuntimeFieldInfo] = "t__runtime_field_info",
                [get(typeof(MethodBase))] = "t__method_base",
                [get(typeof(ConstructorInfo))] = "t__constructor_info",
                [typeofRuntimeConstructorInfo] = "t__runtime_constructor_info",
                [get(typeof(MethodInfo))] = "t__method_info",
                [typeofRuntimeMethodInfo] = "t__runtime_method_info",
                [get(typeof(PropertyInfo))] = "t__property_info",
                [typeofRuntimePropertyInfo] = "t__runtime_property_info",
                [typeofType] = "t__abstract_type",
                [typeofRuntimeType] = "t__type",
                [get(typeof(RuntimeGenericParameter))] = "t__generic_parameter",
                [typeofRuntimeGenericTypeParameter] = "t__generic_type_parameter",
                [typeofRuntimeGenericMethodParameter] = "t__generic_method_parameter",
                [get(typeof(CriticalFinalizerObject))] = "t__critical_finalizer_object"
            };
            primitives = new Dictionary<Type, string>
            {
                [typeofBoolean] = "bool",
                [typeofByte] = "uint8_t",
                [typeofSByte] = "int8_t",
                [typeofInt16] = "int16_t",
                [typeofUInt16] = "uint16_t",
                [typeofInt32] = "int32_t",
                [typeofUInt32] = "uint32_t",
                [typeofInt64] = "int64_t",
                [typeofUInt64] = "uint64_t",
                [typeofVoidPointer] = "void*",
                [typeofChar] = "char16_t",
                [typeofDouble] = "double",
                [typeofSingle] = "float",
                [typeofVoid] = "void"
            };
            typedReferenceByRefType = get(typeof(TypedReferenceTag)).MakeByRefType();
            typeOfAdd = new Dictionary<(string, string), Type>
            {
                [("int32_t", "int32_t")] = typeofInt32,
                [("int32_t", "void*")] = typeofVoidPointer,
                [("int64_t", "int64_t")] = typeofInt64,
                [("void*", "int32_t")] = typeofVoidPointer,
                [("void*", "void*")] = typeofVoidPointer,
                [("double", "double")] = typeofDouble
            };
            typeOfDiv_Un = new Dictionary<(string, string), Type>
            {
                [("int32_t", "int32_t")] = typeofInt32,
                [("int32_t", "void*")] = typeofVoidPointer,
                [("int64_t", "int64_t")] = typeofInt64,
                [("void*", "int32_t")] = typeofVoidPointer,
                [("void*", "void*")] = typeofVoidPointer
            };
            typeOfShl = new Dictionary<(string, string), Type>
            {
                [("int32_t", "int32_t")] = typeofInt32,
                [("int32_t", "void*")] = typeofInt32,
                [("int64_t", "int32_t")] = typeofInt64,
                [("int64_t", "void*")] = typeofInt64,
                [("void*", "int32_t")] = typeofVoidPointer,
                [("void*", "void*")] = typeofVoidPointer
            };
            finalizeOfObject = FinalizeOf(typeofObject);
            for (int i = 0; i < 256; ++i)
            {
                instructions1[i] = new Instruction { OpCode = opcodes1[i] };
                instructions2[i] = new Instruction { OpCode = opcodes2[i] };
            }
            instructions1[OpCodes.Nop.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    return index;
                };
            });
            new[]
            {
                OpCodes.Ldarg_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldarg_3
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(GetArgumentType(i)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = const_cast<std::remove_volatile_t<decltype(a_{i})>&>(a_{i});");
                    return index;
                };
            }));
            new[]
            {
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldloc_2,
                OpCodes.Ldloc_3
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(method.GetMethodBody().LocalVariables[i].LocalType));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = l{i};");
                    return index;
                };
            }));
            new[]
            {
                OpCodes.Stloc_0,
                OpCodes.Stloc_1,
                OpCodes.Stloc_2,
                OpCodes.Stloc_3
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\tl{i} = {CastValue(method.GetMethodBody().LocalVariables[i].LocalType, stack.Variable)};");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldarg_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    return (index, stack.Push(GetArgumentType(i)));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = const_cast<std::remove_volatile_t<decltype(a_{i})>&>(a_{i});");
                    return index;
                };
            });
            instructions1[OpCodes.Ldarga_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    return (index, stack.Push(MakePointerType(GetArgumentType(i))));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = const_cast<std::remove_volatile_t<decltype(a_{i})>*>(&a_{i});");
                    return index;
                };
            });
            instructions1[OpCodes.Starg_S.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 1, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    writer.WriteLine($" {i}\n\ta_{i} = {CastValue(GetArgumentType(i), stack.Variable)};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldloc_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    return (index, stack.Push(method.GetMethodBody().LocalVariables[i].LocalType));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = l{i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldloca_S.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    var type = method.GetMethodBody().LocalVariables[i].LocalType;
                    return (index, stack.Push(MakePointerType(type)));
                };
                x.Generate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = const_cast<std::remove_volatile_t<decltype(l{i})>*>(&l{i});");
                    return index;
                };
            });
            instructions1[OpCodes.Stloc_S.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 1, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var i = ParseU1(ref index);
                    writer.WriteLine($" {i}\n\tl{i} = {CastValue(method.GetMethodBody().LocalVariables[i].LocalType, stack.Variable)};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldnull.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(typeofObject));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = nullptr;");
                    return index;
                };
            });
            new[]
            {
                OpCodes.Ldc_I4_M1,
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_3,
                OpCodes.Ldc_I4_4,
                OpCodes.Ldc_I4_5,
                OpCodes.Ldc_I4_6,
                OpCodes.Ldc_I4_7,
                OpCodes.Ldc_I4_8
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(typeofInt32));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {i - 1};");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldc_I4_S.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 1, stack.Push(typeofInt32));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_I4.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeofInt32));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI4(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_I8.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 8, stack.Push(typeofInt64));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI8(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {(i > long.MinValue ? $"{i}" : $"{i + 1} - 1")};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_R4.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeofSingle));
                x.Generate = (index, stack) =>
                {
                    var r = ParseR4(ref index);
                    var i = new SingleUnion { Single = r }.Int32;
                    var literal = i == 0 ? "0.0f" : $"{(i < 0 ? "-" : string.Empty)}0x1.{(i & 0x7fffff) << 1:x6}p{(i >> 23 & 0xff) - 127}f";
                    writer.Write($" {literal}\n\t{indexToStack[index].Variable} = ");
                    if (float.IsPositiveInfinity(r))
                        writer.WriteLine("std::numeric_limits<float>::infinity();");
                    else if (float.IsNegativeInfinity(r))
                        writer.WriteLine("-std::numeric_limits<float>::infinity();");
                    else if (float.IsNaN(r))
                        writer.WriteLine("std::numeric_limits<float>::quiet_NaN();");
                    else
                        writer.WriteLine($"{literal};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_R8.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 8, stack.Push(typeofDouble));
                x.Generate = (index, stack) =>
                {
                    var r = ParseR8(ref index);
                    var i = new DoubleUnion { Double = r }.Int64;
                    var literal = i == 0 ? "0.0" : $"{(i < 0 ? "-" : string.Empty)}0x1.{i & 0xfffffffffffff:x13}p{(i >> 52 & 0x7ff) - 1023}";
                    writer.Write($" {literal}\n\t{indexToStack[index].Variable} = ");
                    if (double.IsPositiveInfinity(r))
                        writer.WriteLine("std::numeric_limits<double>::infinity();");
                    else if (double.IsNegativeInfinity(r))
                        writer.WriteLine("-std::numeric_limits<double>::infinity();");
                    else if (double.IsNaN(r))
                        writer.WriteLine("std::numeric_limits<double>::quiet_NaN();");
                    else
                        writer.WriteLine($"{literal};");
                    return index;
                };
            });
            instructions1[OpCodes.Dup.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(stack.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Variable};");
                    return index;
                };
            });
            instructions1[OpCodes.Pop.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    return index;
                };
            });
            instructions1[OpCodes.Call.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    return (index, EstimateCall(m, stack));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    writer.WriteLine($" {m.DeclaringType}::[{m}]");
                    GenerateCall(m, Escape(m), stack, indexToStack[index]);
                    return index;
                };
            });
            instructions1[OpCodes.Calli.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var (_, @return, parameters) = ParseSignature(ref index);
                    stack = stack.ElementAt(parameters.Length + 1);
                    return (index, @return == typeofVoid ? stack : stack.Push(@return));
                };
                x.Generate = (index, stack) =>
                {
                    var (cc, @return, parameters) = ParseSignature(ref index);
                    writer.WriteLine($@" {cc} {@return}({string.Join(", ", parameters.AsEnumerable())})
{'\t'}{(
    @return == typeofVoid ? string.Empty : $"{indexToStack[index].Variable} = "
)}reinterpret_cast<{EscapeForStacked(@return)}(*)({
    string.Join(", ", parameters.Select(EscapeForStacked))
})>({stack.Variable})({string.Join(",", parameters.Zip(
    stack.Skip(1).Take(parameters.Length).Reverse(),
    (p, s) => $"\n\t\t{CastValue(p, s.Variable)}"
))}
{'\t'});");
                    return index;
                };
            });
            instructions1[OpCodes.Ret.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    hasReturn = true;
                    return (int.MaxValue, GetReturnType(method) == typeofVoid ? stack : stack.Pop);
                };
                x.Generate = (index, stack) =>
                {
                    //if (!method.DeclaringType.Name.StartsWith("AllowedBmpCodePointsBitmap")) writer.Write($"\n\tprintf(\"return {Escape(method)}\\n\");");
                    writer.Write("\n\treturn");
                    var @return = GetReturnType(method);
                    if (@return != typeofVoid)
                    {
                        writer.Write($" {CastValue(@return, stack.Variable)}");
                        stack = stack.Pop;
                    }
                    writer.WriteLine(";");
                    Trace.Assert(stack.Pop == null);
                    return index;
                };
            });
            string condition_Un(Stack stack, string @operator) => stack.VariableType == "double"
                ? string.Format("{0} {1} {2} || std::isunordered({0}, {2})", stack.Pop.Variable, @operator, stack.Variable)
                : $"{stack.Pop.AsUnsigned} {@operator} {stack.AsUnsigned}";
            string @goto(int index, int target) => target < index ? $@"{{
{'\t'}{'\t'}f_epoch_point();
{'\t'}{'\t'}goto L_{target:x04};
{'\t'}}}" : $"goto L_{target:x04};";
            new[]
            {
                (OpCode: OpCodes.Br_S, Target: (ParseBranchTarget)ParseBranchTargetI1),
                (OpCode: OpCodes.Br, Target: (ParseBranchTarget)ParseBranchTargetI4)
            }.ForEach(baseSet =>
            {
                instructions1[baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack);
                        return (int.MaxValue, stack);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        writer.WriteLine($" {target:x04}\n\t{@goto(index, target)}");
                        return index;
                    };
                });
                new[]
                {
                    (OpCode: OpCodes.Brfalse_S, Operator: "!"),
                    (OpCode: OpCodes.Brtrue_S, Operator: string.Empty)
                }.ForEach(set => instructions1[set.OpCode.Value - OpCodes.Br_S.Value + baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack.Pop);
                        return (index, stack.Pop);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        writer.WriteLine($" {target:x04}\n\tif ({set.Operator}{stack.Variable}) {@goto(index, target)}");
                        return index;
                    };
                }));
                new[]
                {
                    (OpCode: OpCodes.Beq_S, Operator: "=="),
                    (OpCode: OpCodes.Bge_S, Operator: ">="),
                    (OpCode: OpCodes.Bgt_S, Operator: ">"),
                    (OpCode: OpCodes.Ble_S, Operator: "<="),
                    (OpCode: OpCodes.Blt_S, Operator: "<")
                }.ForEach(set => instructions1[set.OpCode.Value - OpCodes.Br_S.Value + baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack.Pop.Pop);
                        return (index, stack.Pop.Pop);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        writer.WriteLine($" {target:x04}\n\tif ({stack.Pop.AsSigned} {set.Operator} {stack.AsSigned}) {@goto(index, target)}");
                        return index;
                    };
                }));
                new[]
                {
                    (OpCode: OpCodes.Bne_Un_S, Operator: "!="),
                    (OpCode: OpCodes.Bge_Un_S, Operator: ">="),
                    (OpCode: OpCodes.Bgt_Un_S, Operator: ">"),
                    (OpCode: OpCodes.Ble_Un_S, Operator: "<="),
                    (OpCode: OpCodes.Blt_Un_S, Operator: "<")
                }.ForEach(set => instructions1[set.OpCode.Value - OpCodes.Br_S.Value + baseSet.OpCode.Value].For(x =>
                {
                    x.Estimate = (index, stack) =>
                    {
                        Estimate(baseSet.Target(ref index), stack.Pop.Pop);
                        return (index, stack.Pop.Pop);
                    };
                    x.Generate = (index, stack) =>
                    {
                        var target = baseSet.Target(ref index);
                        writer.WriteLine($" {target:x04}\n\tif ({condition_Un(stack, set.Operator)}) {@goto(index, target)}");
                        return index;
                    };
                }));
            });
            instructions1[OpCodes.Switch.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var n = ParseI4(ref index);
                    var next = index + n * 4;
                    for (; n > 0; --n) Estimate(next + ParseI4(ref index), stack.Pop);
                    return (index, stack.Pop);
                };
                x.Generate = (index, stack) =>
                {
                    var n = ParseI4(ref index);
                    var next = index + n * 4;
                    writer.WriteLine($" {n}\n\tswitch({stack.Variable}) {{");
                    for (var i = 0; i < n; ++i) writer.WriteLine($@"{'\t'}case {i}:
{'\t'}{'\t'}goto L_{next + ParseI4(ref index):x04};");
                    writer.WriteLine("\t}");
                    return index;
                };
            });
            void withVolatile(Action action)
            {
                if (@volatile) writer.WriteLine("\tstd::atomic_thread_fence(std::memory_order_consume);");
                action();
                if (@volatile) writer.WriteLine("\tstd::atomic_thread_fence(std::memory_order_consume);");
                @volatile = false;
            }
            new[]
            {
                (OpCode: OpCodes.Ldind_I1, Type: typeofSByte),
                (OpCode: OpCodes.Ldind_U1, Type: typeofByte),
                (OpCode: OpCodes.Ldind_I2, Type: typeofInt16),
                (OpCode: OpCodes.Ldind_U2, Type: typeofUInt16),
                (OpCode: OpCodes.Ldind_I4, Type: typeofInt32),
                (OpCode: OpCodes.Ldind_U4, Type: typeofUInt32),
                (OpCode: OpCodes.Ldind_I8, Type: typeofInt64),
                (OpCode: OpCodes.Ldind_I, Type: typeofVoidPointer),
                (OpCode: OpCodes.Ldind_R4, Type: typeofSingle),
                (OpCode: OpCodes.Ldind_R8, Type: typeofDouble)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t{indexToStack[index].Variable} = *reinterpret_cast<{primitives[set.Type]}*>({stack.Variable});"));
                    return index;
                };
            }));
            instructions1[OpCodes.Ldind_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(GetElementType(stack.Type)));
                x.Generate = (index, stack) =>
                {
                    var after = indexToStack[index];
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t{after.Variable} = *static_cast<{EscapeForValue(after.Type)}*>({stack.Variable});"));
                    return index;
                };
            });
            instructions1[OpCodes.Stind_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\tf__store(*static_cast<{EscapeForValue(typeofObject)}*>({stack.Pop.Variable}), {stack.Variable});"));
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Stind_I1, Type: typeofSByte),
                (OpCode: OpCodes.Stind_I2, Type: typeofInt16),
                (OpCode: OpCodes.Stind_I4, Type: typeofInt32),
                (OpCode: OpCodes.Stind_I8, Type: typeofInt64),
                (OpCode: OpCodes.Stind_R4, Type: typeofSingle),
                (OpCode: OpCodes.Stind_R8, Type: typeofDouble)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t*static_cast<{primitives[set.Type]}*>({stack.Pop.Variable}) = {stack.AsSigned};"));
                    return index;
                };
            }));
            instructions1[OpCodes.Stind_I.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    withVolatile(() => writer.WriteLine($"\t*static_cast<intptr_t*>({stack.Pop.Variable}) = {stack.AsSigned};"));
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Add, Operator: "+", Type: typeOfAdd),
                (OpCode: OpCodes.Sub, Operator: "-", Type: typeOfAdd),
                (OpCode: OpCodes.Mul, Operator: "*", Type: typeOfAdd),
                (OpCode: OpCodes.Div_Un, Operator: "/", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Rem_Un, Operator: "%", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Shr_Un, Operator: ">>", Type: typeOfShl)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(set.Type[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Assign($"{stack.Pop.AsUnsigned} {set.Operator} {stack.AsUnsigned}")};");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Div, Operator: "/", Type: typeOfAdd),
                (OpCode: OpCodes.Rem, Operator: "%", Type: typeOfAdd),
                (OpCode: OpCodes.And, Operator: "&", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Or, Operator: "|", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Xor, Operator: "^", Type: typeOfDiv_Un),
                (OpCode: OpCodes.Shl, Operator: "<<", Type: typeOfShl),
                (OpCode: OpCodes.Shr, Operator: ">>", Type: typeOfShl)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(set.Type[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    var after = indexToStack[index];
                    var result = set.OpCode == OpCodes.Rem && after.VariableType == "double"
                        ? $"std::fmod({stack.Pop.Variable}, {stack.Variable})"
                        : $"{stack.Pop.AsSigned} {set.Operator} {stack.AsSigned}";
                    writer.WriteLine($"\n\t{after.Assign(result)};");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Neg, Operator: "-"),
                (OpCode: OpCodes.Not, Operator: "~")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{stack.Assign($"{set.Operator}{stack.AsSigned}")};");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Conv_I1, Type: typeofSByte),
                (OpCode: OpCodes.Conv_I2, Type: typeofInt16),
                (OpCode: OpCodes.Conv_I4, Type: typeofInt32),
                (OpCode: OpCodes.Conv_I8, Type: typeofInt64),
                (OpCode: OpCodes.Conv_R4, Type: typeofSingle),
                (OpCode: OpCodes.Conv_R8, Type: typeofDouble)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{primitives[set.Type]}>({stack.AsSigned});");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Conv_I, Type: "intptr_t"),
                (OpCode: OpCodes.Conv_U, Type: "uintptr_t")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeofVoidPointer));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Assign($"static_cast<{set.Type}>({stack.AsSigned})")};");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Conv_U4, Type: typeofUInt32),
                (OpCode: OpCodes.Conv_U8, Type: typeofUInt64),
                (OpCode: OpCodes.Conv_U2, Type: typeofUInt16),
                (OpCode: OpCodes.Conv_U1, Type: typeofByte),
                (OpCode: OpCodes.Conv_R_Un, Type: typeofDouble)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{primitives[set.Type]}>({stack.AsUnsigned});");
                    return index;
                };
            }));
            instructions1[OpCodes.Callvirt.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    return (index, EstimateCall(m, stack));
                };
                x.Generate = (index, stack) =>
                {
                    var m = (MethodInfo)ParseMethod(ref index);
                    writer.WriteLine($" {m.DeclaringType}::[{m}]");
                    var after = indexToStack[index];
                    string generateVirtual(string target) => GenerateVirtualCall(m, target,
                        stack.Take(m.GetParameters().Length).Select(y => y.Variable).Reverse(),
                        y => $"\t{(GetReturnType(m) == typeofVoid ? string.Empty : $"{after.Variable} = ")}{y};\n"
                    );
                    void generateConcrete(MethodBase cm) => GenerateCall(cm, Escape(cm), stack, after);
                    var isConcrete = !m.DeclaringType.IsInterface && (!m.IsVirtual || m.IsFinal);
                    var @this = stack.ElementAt(m.GetParameters().Length);
                    if (constrained == null)
                    {
                        if (isConcrete)
                            generateConcrete(m);
                        else if (@this.Type.IsSealed)
                            generateConcrete(GetConcrete(m, @this.Type));
                        else
                            writer.Write(GenerateCheckNull(@this.Variable) + generateVirtual(@this.Variable));
                    }
                    else
                    {
                        string generate(MethodBase cm)
                        {
                            var call = GenerateCall(cm, Escape(cm), stack.Take(cm.GetParameters().Length).Select(y => y.Variable).Append("p").Reverse());
                            return $"\t{(GetReturnType(cm) == typeofVoid ? string.Empty : $"{after.Variable} = ")}{call};\n";
                        }
                        if (constrained.IsValueType)
                        {
                            var cm = isConcrete ? m : GetConcrete(m, constrained);
                            if (cm.DeclaringType == constrained)
                            {
                                generateConcrete(cm);
                            }
                            else
                            {
                                void generateValueMethod(string name)
                                {
                                    var rm = typeofRuntimeType.GetMethod(name);
                                    writer.WriteLine($"\t{after.Variable} = {GenerateCall(rm, Escape(rm), stack.Take(rm.GetParameters().Length - 2).Select(y => y.Variable).Append(@this.Variable).Append($"&t__type_of<{Escape(constrained)}>::v__instance").Reverse())};");
                                }
                                if (cm == typeofObject.GetMethod(nameof(GetType)))
                                    writer.WriteLine($"\t{after.Variable} = &t__type_of<{Escape(constrained)}>::v__instance;");
                                else if (cm == typeofValueType.GetMethod(nameof(Equals)))
                                    generateValueMethod(nameof(RuntimeType.ValueEquals));
                                else if (cm == typeofValueType.GetMethod(nameof(GetHashCode)))
                                    generateValueMethod(nameof(RuntimeType.ValueGetHashCode));
                                else if (cm == typeofValueType.GetMethod(nameof(ToString)))
                                    generateValueMethod(nameof(RuntimeType.ValueToString));
                                else
                                    writer.WriteLine($@"{'\t'}{{auto p = f__new_constructed<{Escape(constrained)}>(*{CastValue(MakePointerType(constrained), @this.Variable)});
{(isConcrete ? generate(m) : generateVirtual("p"))}{'\t'}}}");
                            }
                        }
                        else
                        {
                            writer.WriteLine($@"{'\t'}{{auto p = *static_cast<{Escape(constrained.IsInterface ? typeofObject : constrained)}**>({@this.Variable});
{(
    isConcrete ? generate(m) :
    constrained.IsSealed ? generate(GetConcrete(m, constrained)) :
    GenerateCheckNull("p") + generateVirtual("p")
)}{'\t'}}}");
                        }
                        constrained = null;
                    }
                    return index;
                };
            });
            instructions1[OpCodes.Ldobj.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    withVolatile(() => writer.WriteLine($"\t{indexToStack[index].Variable} = *static_cast<{EscapeForValue(t)}*>({stack.Variable});"));
                    return index;
                };
            });
            instructions1[OpCodes.Ldstr.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeofString));
                x.Generate = (index, stack) =>
                {
                    var s = ParseString(ref index);
                    writer.Write($"\n\t{indexToStack[index].Variable} = ");
                    WriteNewString(writer, s);
                    writer.WriteLine(';');
                    return index;
                };
            });
            instructions1[OpCodes.Newobj.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    return (index, stack.ElementAt(m.GetParameters().Length).Push(m.DeclaringType));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    var t = m.DeclaringType;
                    writer.WriteLine($@" {t}::[{m}]");
                    var after = indexToStack[index];
                    Enqueue(m);
                    string call(IEnumerable<string> xs) => $"{Escape(m)}({string.Join(",", xs)}\n\t)";
                    var parameters = m.GetParameters();
                    var arguments = parameters.Zip(stack.Take(parameters.Length).Reverse(), (p, s) => $"\n\t\t{CastValue(p.ParameterType, s.Variable)}");
                    if (t == typeofIntPtr || t == typeofUIntPtr)
                        writer.WriteLine($@"{'\t'}{{{EscapeForValue(t)} p{{}};
{'\t'}{call(arguments.Prepend("\n\t\t&p"))};
{'\t'}{after.Variable} = p;}}");
                    else if (t.IsValueType)
                        writer.WriteLine($@"{'\t'}{after.Variable} = {EscapeForValue(t)}{{}};
{'\t'}{call(arguments.Prepend($"\n\t\tconst_cast<std::remove_volatile_t<decltype({after.Variable})>*>(&{after.Variable})"))};");
                    else if (builtin.GetBody(this, ToKey(m)).body != null)
                        writer.WriteLine($"\t{after.Variable} = {call(arguments)};");
                    else
                        writer.WriteLine($@"{'\t'}{{auto RECYCLONE__SPILL p = f__new_zerod<{Escape(t)}>();
{'\t'}{call(arguments.Prepend("\n\t\tp"))};
{'\t'}{after.Variable} = p;}}");
                    return index;
                };
            });
            instructions1[OpCodes.Castclass.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{GenerateIsAssignableTo(stack.Variable, t)}) {GenerateThrow("InvalidCast")};");
                    return index;
                };
            });
            instructions1[OpCodes.Isinst.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    ParseType(ref index);
                    return (index, stack.Pop.Push(typeofObject));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var after = indexToStack[index];
                    Trace.Assert(after.Variable == stack.Variable);
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{GenerateIsAssignableTo(stack.Variable, t)}) {after.Variable} = nullptr;");
                    return index;
                };
            });
            instructions1[OpCodes.Unbox.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(MakeByRefType(t)));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    Trace.Assert(t.IsValueType);
                    writer.WriteLine($" {t}");
                    if (GetNullableUnderlyingType(t) != null) throw new NotSupportedException();
                    GenerateCheckNull(stack);
                    writer.WriteLine($@"{'\t'}if ({stack.Variable}->f_type() != &t__type_of<{Escape(t)}>::v__instance) [[unlikely]] {GenerateThrow("InvalidCast")};
{'\t'}{indexToStack[index].Variable} = &static_cast<{Escape(t)}*>({stack.Variable})->v__value;");
                    return index;
                };
            });
            instructions1[OpCodes.Throw.Value].For(x =>
            {
                x.Estimate = (index, stack) => (int.MaxValue, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\tthrow {stack.Variable};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldfld.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Pop.Push(f.FieldType));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() =>
                    {
                        GenerateCheckNull(stack);
                        writer.Write($"\t{indexToStack[index].Variable} = ");
                        writer.Write(stack.Type.IsValueType
                            ? $"const_cast<std::remove_volatile_t<decltype({stack.Variable})>&>({stack.Variable})."
                            : $"static_cast<{Escape(f.DeclaringType)}{(f.DeclaringType.IsValueType ? "::t_value" : string.Empty)}*>({stack.Variable})->"
                        );
                        writer.WriteLine($"{Escape(f)};");
                    });
                    return index;
                };
            });
            instructions1[OpCodes.Ldflda.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Pop.Push(MakePointerType(f.FieldType)));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    GenerateCheckNull(stack);
                    writer.Write($"\t{indexToStack[index].Variable} = &");
                    writer.Write(stack.Type.IsValueType
                        ? $"{stack.Variable}."
                        : $"static_cast<{Escape(f.DeclaringType)}{(f.DeclaringType.IsValueType ? "::t_value" : string.Empty)}*>({stack.Variable})->"
                    );
                    writer.WriteLine($"{Escape(f)};");
                    return index;
                };
            });
            instructions1[OpCodes.Stfld.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() =>
                    {
                        GenerateCheckNull(stack.Pop);
                        writer.WriteLine(
                            f.DeclaringType.IsByRefLike ? "\tf__copy({0}, {1});" :
                            f.DeclaringType.IsValueType && Define(f.FieldType).IsManaged ? "\tf__store({0}, {1});" :
                            "\t{0} = {1};",
                            $"static_cast<{Escape(f.DeclaringType)}{(f.DeclaringType.IsValueType ? "::t_value" : string.Empty)}*>({stack.Pop.Variable})->{Escape(f)}",
                            CastValue(f.FieldType, stack.Variable)
                        );
                    });
                    return index;
                };
            });
            string @static(FieldInfo x) => x.GetCustomAttributesData().Any(x => x.AttributeType == typeofThreadStaticAttribute)
                ? $"t_thread_static::v_instance->v_{Escape(x.DeclaringType)}.{Escape(x)}"
                : $"t_static::v_instance->v_{Escape(x.DeclaringType)}->{Escape(x)}";
            instructions1[OpCodes.Ldsfld.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Push(f.FieldType));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() => writer.WriteLine($"\t{indexToStack[index].Variable} = {@static(f)};"));
                    return index;
                };
            });
            instructions1[OpCodes.Ldsflda.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    return (index, stack.Push(MakePointerType(f.FieldType)));
                };
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.Write($" {f.DeclaringType}::[{f}]\n\t{indexToStack[index].Variable} = ");
                    writer.WriteLine(f.Attributes.HasFlag(FieldAttributes.HasFieldRVA)
                        ? $"v__field_{Escape(f.DeclaringType)}__{Escape(f.Name)}__data;"
                        : $"&{@static(f)};"
                    );
                    return index;
                };
            });
            instructions1[OpCodes.Stsfld.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var f = ParseField(ref index);
                    writer.WriteLine($" {f.DeclaringType}::[{f}]");
                    withVolatile(() => writer.WriteLine($"\t{@static(f)} = {CastValue(f.FieldType, stack.Variable)};"));
                    return index;
                };
            });
            instructions1[OpCodes.Stobj.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    withVolatile(() => writer.WriteLine(
                        t.IsByRefLike ? "\tf__copy({0}, {1});" :
                        Define(t).IsManaged ? "\tf__store({0}, {1});" :
                        "\t{0} = {1};",
                        $"*static_cast<{EscapeForValue(t)}*>({stack.Pop.Variable})",
                        CastValue(t, stack.Variable)
                    ));
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Conv_Ovf_I1_Un, Type: typeofSByte, Primitive: "int8_t"),
                (OpCode: OpCodes.Conv_Ovf_I2_Un, Type: typeofInt16, Primitive: "int16_t"),
                (OpCode: OpCodes.Conv_Ovf_I4_Un, Type: typeofInt32, Primitive: "int32_t"),
                (OpCode: OpCodes.Conv_Ovf_I8_Un, Type: typeofInt64, Primitive: "int64_t"),
                (OpCode: OpCodes.Conv_Ovf_U1_Un, Type: typeofByte, Primitive: "uint8_t"),
                (OpCode: OpCodes.Conv_Ovf_U2_Un, Type: typeofUInt16, Primitive: "uint16_t"),
                (OpCode: OpCodes.Conv_Ovf_U4_Un, Type: typeofUInt32, Primitive: "uint32_t"),
                (OpCode: OpCodes.Conv_Ovf_U8_Un, Type: typeofUInt64, Primitive: "uint64_t"),
                (OpCode: OpCodes.Conv_Ovf_I_Un, Type: typeofVoidPointer, Primitive: "intptr_t"),
                (OpCode: OpCodes.Conv_Ovf_U_Un, Type: typeofVoidPointer, Primitive: "uintptr_t")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    var s = stack.AsUnsigned;
                    var t = set.Primitive;
                    writer.WriteLine($@"
{'\t'}if ({s} > std::numeric_limits<{t}>::max()) {GenerateThrow("Overflow")};
{'\t'}{indexToStack[index].Assign($"static_cast<{t}>({s})")};");
                    return index;
                };
            }));
            instructions1[OpCodes.Box.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Push(typeofObject));
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    var after = indexToStack[index];
                    var next = instructions1[bytes[index]].OpCode;
                    if (t.IsValueType && (next == OpCodes.Brtrue_S || next == OpCodes.Brfalse_S || next == OpCodes.Brtrue || next == OpCodes.Brfalse))
                        writer.WriteLine($"\t{after.Variable} = reinterpret_cast<t__object*>(1);");
                    else if (next == OpCodes.Unbox_Any)
                        constrained = t;
                    else if (GetNullableUnderlyingType(t) is Type u)
                        writer.WriteLine($"\t{after.Variable} = {stack.Variable}.v_hasValue ? f__new_constructed<{Escape(u)}>(const_cast<std::remove_volatile_t<decltype({stack.Variable})>&>({stack.Variable}).v_value) : nullptr;");
                    else if (t.IsValueType)
                        writer.WriteLine($"\t{after.Variable} = f__new_constructed<{Escape(t)}>(const_cast<std::remove_volatile_t<decltype({stack.Variable})>&>({stack.Variable}));");
                    return index;
                };
            });
            instructions1[OpCodes.Newarr.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t.MakeArrayType()));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    if (CheckRange) writer.WriteLine($"\tif ({stack.AsSigned} < 0) [[unlikely]] {GenerateThrow("Overflow")};");
                    writer.WriteLine($"\t{indexToStack[index].Variable} = f__new_array<{Escape(t.MakeArrayType())}, {EscapeForMember(t)}>({stack.AsSigned});");
                    return index;
                };
            });
            instructions1[OpCodes.Ldlen.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeofVoidPointer));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    GenerateCheckNull(stack);
                    writer.WriteLine($"\t{indexToStack[index].Assign($"static_cast<{Escape(stack.Type)}*>({stack.Variable})->v__length")};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldelema.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Pop.Push(MakePointerType(t)));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var array = stack.Pop;
                    Trace.Assert(array.Type == t.MakeArrayType());
                    writer.WriteLine($" {t}");
                    GenerateArrayAccess(array, stack, y => $"{indexToStack[index].Variable} = &{y}");
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Ldelem_I1, Type: typeofSByte),
                (OpCode: OpCodes.Ldelem_U1, Type: typeofByte),
                (OpCode: OpCodes.Ldelem_I2, Type: typeofInt16),
                (OpCode: OpCodes.Ldelem_U2, Type: typeofUInt16),
                (OpCode: OpCodes.Ldelem_I4, Type: typeofInt32),
                (OpCode: OpCodes.Ldelem_U4, Type: typeofUInt32),
                (OpCode: OpCodes.Ldelem_I8, Type: typeofInt64),
                (OpCode: OpCodes.Ldelem_R4, Type: typeofSingle),
                (OpCode: OpCodes.Ldelem_R8, Type: typeofDouble)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    GenerateArrayAccess(stack.Pop, stack, y => indexToStack[index].Assign($"static_cast<{primitives[set.Type]}>({y})"));
                    return index;
                };
            }));
            instructions1[OpCodes.Ldelem_I.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeofVoidPointer));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    GenerateArrayAccess(stack.Pop, stack, y => indexToStack[index].Assign($"{(stack.Pop.Type.GetElementType().IsPointer ? "reinterpret_cast" : "static_cast")}<intptr_t>({y})"));
                    return index;
                };
            });
            instructions1[OpCodes.Ldelem_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(GetElementType(stack.Pop.Type)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    GenerateArrayAccess(stack.Pop, stack, y => $"{indexToStack[index].Variable} = {y}");
                    return index;
                };
            });
            instructions1[OpCodes.Stelem_I.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    GenerateArrayAccess(stack.Pop.Pop, stack.Pop, y => $"{(stack.Pop.Pop.Type.GetElementType().IsPointer ? $"reinterpret_cast<intptr_t&>({y})" : y)} = reinterpret_cast<intptr_t>({stack.Variable})");
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Stelem_I1, Type: typeofSByte),
                (OpCode: OpCodes.Stelem_I2, Type: typeofInt16),
                (OpCode: OpCodes.Stelem_I4, Type: typeofInt32),
                (OpCode: OpCodes.Stelem_I8, Type: typeofInt64),
                (OpCode: OpCodes.Stelem_R4, Type: typeofSingle),
                (OpCode: OpCodes.Stelem_R8, Type: typeofDouble)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    GenerateArrayAccess(stack.Pop.Pop, stack.Pop, y => $"{y} = static_cast<{EscapeForValue(set.Type)}>({stack.Variable})");
                    return index;
                };
            }));
            instructions1[OpCodes.Stelem_Ref.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    var array = stack.Pop.Pop;
                    GenerateArrayAccess(array, stack.Pop, y => $"{y} = {CastValue(GetElementType(array.Type), stack.Variable)}");
                    return index;
                };
            });
            instructions1[OpCodes.Ldelem.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var array = stack.Pop;
                    Trace.Assert(array.Type == t.MakeArrayType());
                    writer.WriteLine($" {t}");
                    GenerateArrayAccess(array, stack, y => $"{indexToStack[index].Variable} = {y}");
                    return index;
                };
            });
            instructions1[OpCodes.Stelem.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var array = stack.Pop.Pop;
                    Trace.Assert(array.Type == t.MakeArrayType());
                    writer.WriteLine($" {t}");
                    GenerateArrayAccess(array, stack.Pop, y => $"{y} = {CastValue(t, stack.Variable)}");
                    return index;
                };
            });
            instructions1[OpCodes.Unbox_Any.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    return (index, stack.Pop.Push(t));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    var after = indexToStack[index];
                    if (constrained != null)
                    {
                        var s = constrained;
                        constrained = null;
                        if (t == s || s.IsEnum && t == s.GetEnumUnderlyingType()) return index;
                        var before = indexToStack[index - 10];
                        if (GetNullableUnderlyingType(t) == s)
                        {
                            writer.WriteLine($"\t{after.Variable} = {{true, {before.Variable}}};");
                            return index;
                        }
                        if (s.IsAssignableTo(t))
                        {
                            if (s.IsValueType) writer.WriteLine($"\t{after.Variable} = f__new_constructed<{Escape(s)}>(const_cast<std::remove_volatile_t<decltype({before.Variable})>&>({before.Variable}));");
                            return index;
                        }
                        if (s.IsValueType)
                        {
                            writer.WriteLine($"\t{GenerateThrow("InvalidCast")};");
                            return index;
                        }
                    }
                    if (t.IsValueType)
                    {
                        if (GetNullableUnderlyingType(t) is Type u)
                        {
                            writer.WriteLine($@"{'\t'}if (!{stack.Variable})
{'\t'}{'\t'}{after.Variable} = {{false}};
{'\t'}else if ({stack.Variable}->f_type() == &t__type_of<{Escape(u)}>::v__instance)
{'\t'}{'\t'}{after.Variable} = {{true, static_cast<{Escape(u)}*>({stack.Variable})->v__value}};");
                        }
                        else
                        {
                            GenerateCheckNull(stack);
                            writer.Write($"\tif ({stack.Variable}->f_type() == &t__type_of<{Escape(t)}>::v__instance");
                            if (t == typeofByte || t == typeofSByte || t == typeofInt16 || t == typeofUInt16 || t == typeofInt32 || t == typeofUInt32 || t == typeofInt64 || t == typeofUInt64) writer.Write($" || {stack.Variable}->f_type()->v__enum && {stack.Variable}->f_type()->v__underlying == &t__type_of<{Escape(t)}>::v__instance");
                            writer.WriteLine($")\n\t\t{after.Variable} = static_cast<{Escape(t)}*>({stack.Variable})->v__value;");
                        }
                        writer.WriteLine($@"{'\t'}else
{'\t'}{'\t'}[[unlikely]] {GenerateThrow("InvalidCast")};");
                    }
                    else if (t != typeofObject)
                    {
                        writer.WriteLine($"\tif ({stack.Variable} && !{GenerateIsAssignableTo(stack.Variable, t)}) [[unlikely]] {GenerateThrow("InvalidCast")};");
                    }
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Conv_Ovf_I1, Type: typeofSByte, Primitive: "int8_t"),
                (OpCode: OpCodes.Conv_Ovf_U1, Type: typeofByte, Primitive: "uint8_t"),
                (OpCode: OpCodes.Conv_Ovf_I2, Type: typeofInt16, Primitive: "int16_t"),
                (OpCode: OpCodes.Conv_Ovf_U2, Type: typeofUInt16, Primitive: "uint16_t"),
                (OpCode: OpCodes.Conv_Ovf_I4, Type: typeofInt32, Primitive: "int32_t"),
                (OpCode: OpCodes.Conv_Ovf_U4, Type: typeofUInt32, Primitive: "uint32_t"),
                (OpCode: OpCodes.Conv_Ovf_I8, Type: typeofInt64, Primitive: "int64_t"),
                (OpCode: OpCodes.Conv_Ovf_U8, Type: typeofUInt64, Primitive: "uint64_t"),
                (OpCode: OpCodes.Conv_Ovf_I, Type: typeofVoidPointer, Primitive: "intptr_t"),
                (OpCode: OpCodes.Conv_Ovf_U, Type: typeofVoidPointer, Primitive: "uintptr_t")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    var s = stack.AsSigned;
                    var t = set.Primitive;
                    writer.WriteLine($@"
{'\t'}if ({s} < static_cast<std::make_signed_t<{t}>>(std::numeric_limits<{t}>::min()) || {s} > std::numeric_limits<{t}>::max()) {GenerateThrow("Overflow")};
{'\t'}{indexToStack[index].Assign($"static_cast<{t}>({s})")};");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldtoken.Value].For(x =>
            {
                x.Estimate = (index, stack) => ParseMember(ref index) switch
                {
                    FieldInfo f => (index, stack.Push(typeofRuntimeFieldHandle)),
                    MethodBase m => (index, stack.Push(typeofRuntimeMethodHandle)),
                    Type t => (index, stack.Push(typeofRuntimeTypeHandle)),
                    _ => throw new Exception()
                };
                x.Generate = (index, stack) =>
                {
                    var member = ParseMember(ref index);
                    writer.WriteLine($@" {member}
{'\t'}{indexToStack[index].Variable} = {member switch
{
    FieldInfo f => $"&v__field_{Escape(f.DeclaringType)}__{Escape(f.Name)}",
    MethodBase m => $"&v__method_{Escape(m)}",
    Type t => $"&t__type_of<{Escape(t)}>::v__instance",
    _ => throw new Exception()
}};");
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Add_Ovf, Operator: "add"),
                (OpCode: OpCodes.Mul_Ovf, Operator: "mul"),
                (OpCode: OpCodes.Sub_Ovf, Operator: "sub")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeOfDiv_Un[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($@"
{'\t'}{{decltype({stack.Pop.AsSigned} + {stack.AsSigned}) x;
{'\t'}if (__builtin_{set.Operator}_overflow({stack.Pop.AsSigned}, {stack.AsSigned}, &x)) {GenerateThrow("Overflow")};
{'\t'}{indexToStack[index].Assign("x")};}}");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Add_Ovf_Un, Operator: "add"),
                (OpCode: OpCodes.Mul_Ovf_Un, Operator: "mul"),
                (OpCode: OpCodes.Sub_Ovf_Un, Operator: "sub")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeOfDiv_Un[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($@"
{'\t'}{{decltype({stack.Pop.AsUnsigned} + {stack.AsUnsigned}) x;
{'\t'}if (__builtin_{set.Operator}_overflow({stack.Pop.AsUnsigned}, {stack.AsUnsigned}, &x)) {GenerateThrow("Overflow")};
{'\t'}{indexToStack[index].Assign("x")};}}");
                    return index;
                };
            }));
            instructions1[OpCodes.Endfinally.Value].For(x =>
            {
                x.Estimate = (index, stack) => (int.MaxValue, stack);
                x.Generate = (index, stack) => 
                {
                    if (tries.Peek().Flags == ExceptionHandlingClauseOptions.Finally)
                        writer.WriteLine("\n\treturn;");
                    else
                        writer.WriteLine("\n\tthrow;");
                    return index;
                };
            });
            new[]
            {
                (OpCode: OpCodes.Leave, Target: (ParseBranchTarget)ParseBranchTargetI4),
                (OpCode: OpCodes.Leave_S, Target: (ParseBranchTarget)ParseBranchTargetI1)
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    Estimate(set.Target(ref index), stack);
                    return (int.MaxValue, stack);
                };
                x.Generate = (index, stack) =>
                {
                    var target = set.Target(ref index);
                    writer.WriteLine($" {target:x04}\n\tgoto L_{target:x04};");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Ceq, Operator: "=="),
                (OpCode: OpCodes.Cgt, Operator: ">"),
                (OpCode: OpCodes.Clt, Operator: "<")
            }.ForEach(set => instructions2[set.OpCode.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeofInt32));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Pop.AsSigned} {set.Operator} {stack.AsSigned} ? 1 : 0;");
                    return index;
                };
            }));
            new[]
            {
                (OpCode: OpCodes.Cgt_Un, Operator: ">"),
                (OpCode: OpCodes.Clt_Un, Operator: "<")
            }.ForEach(set => instructions2[set.OpCode.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeofInt32));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {condition_Un(stack, set.Operator)} ? 1 : 0;");
                    return index;
                };
            }));
            instructions2[OpCodes.Ldftn.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    ParseMethod(ref index);
                    return (index, stack.Push(typeofVoidPointer));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    writer.WriteLine($" {m.DeclaringType}::[{m}]\n\t{indexToStack[index].Variable} = reinterpret_cast<void*>(&{Escape(m)});");
                    Enqueue(m);
                    ldftnMethods.Add(m);
                    return index;
                };
            });
            instructions2[OpCodes.Ldvirtftn.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    ParseMethod(ref index);
                    return (index, stack.Pop.Push(typeofVoidPointer));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
                    Enqueue(m);
                    var function = m.DeclaringType.IsInterface
                        ? $@"{GetInterfaceFunction(m,
                            y => $"f__resolve<{y}>",
                            y => $"f__generic_resolve<{y}>"
                        )}({stack.Variable})"
                        : $"reinterpret_cast<void*>({GetVirtualFunction(m, stack.Variable)})";
                    writer.WriteLine($" {m.DeclaringType}::[{m}]\n\t{indexToStack[index].Variable} = {function};");
                    return index;
                };
            });
            instructions2[OpCodes.Stloc.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop);
                x.Generate = (index, stack) =>
                {
                    var i = ParseI4(ref index);
                    writer.WriteLine($" {i}\n\tl{i} = {CastValue(method.GetMethodBody().LocalVariables[i].LocalType, stack.Variable)};");
                    return index;
                };
            });
            instructions2[OpCodes.Localloc.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeofByte.MakePointerType()));
                x.Generate = (index, stack) =>
                {
                    writer.Write($@"
{'\t'}{{auto n = {stack.AsUnsigned};
{'\t'}{indexToStack[index].Variable} = alloca(n);");
                    if (method.GetMethodBody().InitLocals) writer.Write($"\n\tstd::memset({indexToStack[index].Variable}, 0, n);");
                    writer.WriteLine('}');
                    return index;
                };
            });
            instructions2[OpCodes.Endfilter.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeofException));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($@"
{'\t'}if ({stack.Variable} == 0) throw;
{'\t'}{indexToStack[index].Variable} = e;");
                    return index;
                };
            });
            instructions2[OpCodes.Unaligned.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    ParseU1(ref index);
                    return (index, stack);
                };
                x.Generate = (index, stack) =>
                {
                    var alignment = ParseU1(ref index);
                    writer.WriteLine($" {alignment}");
                    return index;
                };
            });
            instructions2[OpCodes.Volatile.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    @volatile = true;
                    writer.WriteLine();
                    return index;
                };
            });
            instructions2[OpCodes.Initobj.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    ParseType(ref index);
                    return (index, stack.Pop);
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}");
                    var type = EscapeForValue(t);
                    writer.WriteLine(
                        t.IsByRefLike ? "\tf__store({0}, {1});" :
                        Define(t).IsManaged ? "\tf__store({0}, {1});" :
                        "\t{0} = {1};",
                        $"*static_cast<{type}*>({stack.Variable})",
                        type.EndsWith("*") ? $"static_cast<{type}>(nullptr)" : $"{type}{{}}"
                    );
                    return index;
                };
            });
            instructions2[OpCodes.Constrained.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    ParseType(ref index);
                    return (index, stack);
                };
                x.Generate = (index, stack) =>
                {
                    constrained = ParseType(ref index);
                    writer.WriteLine($" {constrained}");
                    return index;
                };
            });
            instructions2[OpCodes.Cpblk.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\tstd::memcpy({stack.Pop.Pop.Variable}, {stack.Pop.Variable}, {stack.Variable});");
                    return index;
                };
            });
            instructions2[OpCodes.Initblk.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\tstd::memset({stack.Pop.Pop.Variable}, {stack.Pop.Variable}, {stack.Variable});");
                    return index;
                };
            });
            instructions2[OpCodes.Rethrow.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (int.MaxValue, stack);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine("\n\tthrow;");
                    return index;
                };
            });
            instructions2[OpCodes.Sizeof.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeofUInt32));
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = sizeof({EscapeForValue(t)});");
                    return index;
                };
            });
            instructions2[OpCodes.Refanytype.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeofRuntimeTypeHandle));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {EscapeForValue(typeofRuntimeTypeHandle)}{{static_cast<t__type*>({stack.Variable}.v__5ftype.v__5fvalue)}};");
                    return index;
                };
            });
            instructions2[OpCodes.Readonly.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    return index;
                };
            });
        }
    }
}
