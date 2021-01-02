using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace IL2CXX
{
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
        public Transpiler(IBuiltin builtin, Action<string> log, bool checkNull = true, bool checkRange = true)
        {
            this.builtin = builtin;
            this.log = log;
            CheckNull = checkNull;
            CheckRange = checkRange;
            for (int i = 0; i < 256; ++i)
            {
                instructions1[i] = new Instruction { OpCode = opcodes1[i] };
                instructions2[i] = new Instruction { OpCode = opcodes2[i] };
            }
            instructions1[OpCodes.Nop.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack);
                x.Generate = (index, stack) => {
                    writer.WriteLine();
                    return index;
                };
            });
            new[] {
                OpCodes.Ldarg_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldarg_3
            }.ForEach((opcode, i) => instructions1[opcode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Push(GetArgumentType(i)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = a_{i};");
                    return index;
                };
            }));
            new[] {
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
            new[] {
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
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = a_{i};");
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
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = &a_{i};");
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
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = &l{i};");
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
                x.Estimate = (index, stack) => (index, stack.Push(typeof(object)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = nullptr;");
                    return index;
                };
            });
            new[] {
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
                x.Estimate = (index, stack) => (index, stack.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {i - 1};");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldc_I4_S.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 1, stack.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI1(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_I4.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI4(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {i};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_I8.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 8, stack.Push(typeof(long)));
                x.Generate = (index, stack) =>
                {
                    var i = ParseI8(ref index);
                    writer.WriteLine($" {i}\n\t{indexToStack[index].Variable} = {(i > long.MinValue ? $"{i}" : $"{i + 1} - 1")};");
                    return index;
                };
            });
            instructions1[OpCodes.Ldc_R4.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(float)));
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
                x.Estimate = (index, stack) => (index + 8, stack.Push(typeof(double)));
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
                    Enqueue(m);
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
                    return (index, @return == typeof(void) ? stack : stack.Push(@return));
                };
                x.Generate = (index, stack) =>
                {
                    var (cc, @return, parameters) = ParseSignature(ref index);
                    writer.WriteLine($@" {cc} {@return}({string.Join(", ", parameters.AsEnumerable())})
{'\t'}{(
    @return == typeof(void) ? string.Empty : $"{indexToStack[index].Variable} = "
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
                x.Estimate = (index, stack) => (int.MaxValue, GetReturnType(method) == typeof(void) ? stack : stack.Pop);
                x.Generate = (index, stack) =>
                {
                    writer.Write("\n\treturn");
                    var @return = GetReturnType(method);
                    if (@return != typeof(void))
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
            new[] {
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
                        writer.WriteLine($" {target:x04}\n\tgoto L_{target:x04};");
                        return index;
                    };
                });
                new[] {
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
                        writer.WriteLine($" {target:x04}\n\tif ({set.Operator}{stack.Variable}) goto L_{target:x04};");
                        return index;
                    };
                }));
                new[] {
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
                        writer.WriteLine($" {target:x04}\n\tif ({stack.Pop.AsSigned} {set.Operator} {stack.AsSigned}) goto L_{target:x04};");
                        return index;
                    };
                }));
                new[] {
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
                        writer.WriteLine($" {target:x04}\n\tif ({condition_Un(stack, set.Operator)}) goto L_{target:x04};");
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
            new[] {
                (OpCode: OpCodes.Ldind_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Ldind_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Ldind_I2, Type: typeof(short)),
                (OpCode: OpCodes.Ldind_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Ldind_I4, Type: typeof(int)),
                (OpCode: OpCodes.Ldind_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Ldind_I8, Type: typeof(long)),
                (OpCode: OpCodes.Ldind_I, Type: typeof(void*)),
                (OpCode: OpCodes.Ldind_R4, Type: typeof(float)),
                (OpCode: OpCodes.Ldind_R8, Type: typeof(double))
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
                    withVolatile(() => writer.WriteLine($"\tf__store(*static_cast<{EscapeForValue(typeof(object))}*>({stack.Pop.Variable}), {stack.Variable});"));
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Stind_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Stind_I2, Type: typeof(short)),
                (OpCode: OpCodes.Stind_I4, Type: typeof(int)),
                (OpCode: OpCodes.Stind_I8, Type: typeof(long)),
                (OpCode: OpCodes.Stind_R4, Type: typeof(float)),
                (OpCode: OpCodes.Stind_R8, Type: typeof(double))
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
            new[] {
                (OpCode: OpCodes.Add, Operator: "+", Type: typeOfAdd),
                (OpCode: OpCodes.Sub, Operator: "-", Type: typeOfAdd),
                (OpCode: OpCodes.Mul, Operator: "*", Type: typeOfAdd),
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
            new[] {
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
            new[] {
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
            new[] {
                (OpCode: OpCodes.Conv_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Conv_I2, Type: typeof(short)),
                (OpCode: OpCodes.Conv_I4, Type: typeof(int)),
                (OpCode: OpCodes.Conv_I8, Type: typeof(long)),
                (OpCode: OpCodes.Conv_R4, Type: typeof(float)),
                (OpCode: OpCodes.Conv_R8, Type: typeof(double))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{primitives[set.Type]}>({stack.AsSigned});");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Conv_I, Type: "intptr_t"),
                (OpCode: OpCodes.Conv_U, Type: "uintptr_t")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(void*)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Assign($"static_cast<{set.Type}>({stack.AsSigned})")};");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Conv_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Conv_U8, Type: typeof(ulong)),
                (OpCode: OpCodes.Conv_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Conv_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Conv_R_Un, Type: typeof(double))
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
                    var m = ParseMethod(ref index);
                    writer.WriteLine($" {m.DeclaringType}::[{m}]");
                    var after = indexToStack[index];
                    string generateVirtual(string target) => GenerateVirtualCall(m, target,
                        stack.Take(m.GetParameters().Length).Select(y => y.Variable),
                        y => $"\t{(GetReturnType(m) == typeof(void) ? string.Empty : $"{after.Variable} = ")}{y};\n"
                    );
                    void generateConcrete(MethodBase cm)
                    {
                        Enqueue(cm);
                        GenerateCall(cm, Escape(cm), stack, after);
                    }
                    MethodInfo concrete(Type type)
                    {
                        var ct = (TypeDefinition)Define(type);
                        var methods = m.DeclaringType.IsInterface ? ct.InterfaceToMethods[m.DeclaringType] : (IReadOnlyList<MethodInfo>)ct.Methods;
                        var dt = Define(m.DeclaringType);
                        return m.IsGenericMethod
                            ? methods[dt.GetIndex(((MethodInfo)m).GetGenericMethodDefinition())].MakeGenericMethod(m.GetGenericArguments())
                            : methods[dt.GetIndex(m)];
                    }
                    var isConcrete = !m.DeclaringType.IsInterface && (!m.IsVirtual || m.IsFinal);
                    var @this = stack.ElementAt(m.GetParameters().Length);
                    if (constrained == null)
                    {
                        if (isConcrete)
                            generateConcrete(m);
                        else if (@this.Type.IsSealed)
                            generateConcrete(concrete(@this.Type));
                        else
                            writer.Write(GenerateCheckNull(@this.Variable) + generateVirtual(@this.Variable));
                    }
                    else
                    {
                        string generate(MethodBase cm)
                        {
                            Enqueue(cm);
                            var call = GenerateCall(cm, Escape(cm), stack.Take(cm.GetParameters().Length).Select(y => y.Variable).Append("p"));
                            return $"\t{(GetReturnType(cm) == typeof(void) ? string.Empty : $"{after.Variable} = ")}{call};\n";
                        }
                        if (constrained.IsValueType)
                        {
                            var cm = isConcrete ? m : concrete(constrained);
                            if (cm.DeclaringType == constrained)
                                generateConcrete(cm);
                            else
                                writer.WriteLine($@"{'\t'}{{auto p = f__new_constructed<{Escape(constrained)}>(*{CastValue(MakePointerType(constrained), @this.Variable)});
{(isConcrete ? generate(m) : generateVirtual("p"))}{'\t'}}}");
                        }
                        else
                        {
                            writer.WriteLine($@"{'\t'}{{auto p = *static_cast<{Escape(constrained.IsInterface ? typeof(object) : constrained)}**>({@this.Variable});
{(
    isConcrete ? generate(m) :
    constrained.IsSealed ? generate(concrete(constrained)) :
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
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(string)));
                var escapes = new Dictionary<char, string> {
                    ['\''] = "\\'",
                    ['"'] = "\\\"",
                    ['?'] = "\\?",
                    ['\\'] = "\\\\",
                    ['\a'] = "\\a",
                    ['\b'] = "\\b",
                    ['\f'] = "\\f",
                    ['\n'] = "\\n",
                    ['\r'] = "\\r",
                    ['\t'] = "\\t",
                    ['\v'] = "\\v"
                };
                x.Generate = (index, stack) =>
                {
                    var s = method.Module.ResolveString(ParseI4(ref index));
                    writer.Write($"\n\t{indexToStack[index].Variable} = f__new_string(u\"");
                    foreach (var c in s)
                        if (escapes.TryGetValue(c, out var e))
                            writer.Write(e);
                        else if (c < 0x20 || c >= 0x7f)
                            writer.Write($"\\x{(ushort)c:X}\"\"");
                        else
                            writer.Write(c);
                    writer.WriteLine($"\"sv);");
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
                    if (builtin.GetBody(this, m).body != null)
                        writer.WriteLine($"\t{after.Variable} = {call(arguments)};");
                    else if (t == typeof(IntPtr) || t == typeof(UIntPtr))
                        writer.WriteLine($@"{'\t'}{{{EscapeForValue(t)} p{{}};
{'\t'}{call(arguments.Prepend("\n\t\t&p"))};
{'\t'}{after.Variable} = p;}}");
                    else if (t.IsValueType)
                        writer.WriteLine($@"{'\t'}{after.Variable} = {EscapeForValue(t)}{{}};
{'\t'}{call(arguments.Prepend($"\n\t\t&{after.Variable}"))};");
                    else
                        writer.WriteLine($@"{'\t'}{{auto p = f__new_zerod<{Escape(t)}>();
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
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{stack.Variable}->f_type()->{(t.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(t)}>::v__instance)) f__throw_invalid_cast();");
                    return index;
                };
            });
            instructions1[OpCodes.Isinst.Value].For(x =>
            {
                x.Estimate = (index, stack) =>
                {
                    ParseType(ref index);
                    return (index, stack.Pop.Push(typeof(object)));
                };
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    var after = indexToStack[index];
                    Trace.Assert(after.Variable == stack.Variable);
                    writer.WriteLine($" {t}\n\tif ({stack.Variable} && !{stack.Variable}->f_type()->{(t.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(t)}>::v__instance)) {after.Variable} = nullptr;");
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
                    GenerateCheckNull(stack);
                    writer.WriteLine($@"{'\t'}if (!{stack.Variable}->f_type()->f__is(&t__type_of<{Escape(t)}>::v__instance)) [[unlikely]] f__throw_invalid_cast();
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
                            ? $"{stack.Variable}."
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
                            f.DeclaringType.IsValueType && Define(f.FieldType).IsManaged
                                ? "\tf__store({0}, {1});"
                                : "\t{0} = {1};",
                            $"static_cast<{Escape(f.DeclaringType)}{(f.DeclaringType.IsValueType ? "::t_value" : string.Empty)}*>({stack.Pop.Variable})->{Escape(f)}",
                            CastValue(f.FieldType, stack.Variable)
                        );
                    });
                    return index;
                };
            });
            string @static(FieldInfo x) => Attribute.IsDefined(x, typeof(ThreadStaticAttribute))
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
                    writer.WriteLine(f.DeclaringType.Name == "<PrivateImplementationDetails>"
                        ? $"f__field_{Escape(f.DeclaringType)}__{Escape(f.Name)}();"
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
                        Define(t).IsManaged
                            ? "\tf__store({0}, {1});"
                            : "\t{0} = {1};",
                        $"*static_cast<{EscapeForValue(t)}*>({stack.Pop.Variable})",
                        CastValue(t, stack.Variable)
                    ));
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Conv_Ovf_I1_Un, Type: typeof(sbyte)),
                (OpCode: OpCodes.Conv_Ovf_I2_Un, Type: typeof(short)),
                (OpCode: OpCodes.Conv_Ovf_I4_Un, Type: typeof(int)),
                (OpCode: OpCodes.Conv_Ovf_I8_Un, Type: typeof(long)),
                (OpCode: OpCodes.Conv_Ovf_U1_Un, Type: typeof(byte)),
                (OpCode: OpCodes.Conv_Ovf_U2_Un, Type: typeof(ushort)),
                (OpCode: OpCodes.Conv_Ovf_U4_Un, Type: typeof(uint)),
                (OpCode: OpCodes.Conv_Ovf_U8_Un, Type: typeof(ulong))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{primitives[set.Type]}>({stack.AsUnsigned});");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Conv_Ovf_I_Un, Type: "intptr_t"),
                (OpCode: OpCodes.Conv_Ovf_U_Un, Type: "uintptr_t")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(void*)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Assign($"static_cast<{set.Type}>({stack.AsUnsigned})")};");
                    return index;
                };
            }));
            instructions1[OpCodes.Box.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index + 4, stack.Pop.Push(typeof(object)));
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = {(t.IsValueType ? $"f__new_constructed<{Escape(t)}>({stack.Variable})" : stack.Variable)};");
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
                    if (CheckRange) writer.WriteLine($"\tif ({stack.AsSigned} < 0) [[unlikely]] f__throw_overflow();");
                    writer.WriteLine($"\t{indexToStack[index].Variable} = f__new_array<{Escape(t.MakeArrayType())}, {EscapeForMember(t)}>({stack.AsSigned});");
                    return index;
                };
            });
            instructions1[OpCodes.Ldlen.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(void*)));
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
            new[] {
                (OpCode: OpCodes.Ldelem_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Ldelem_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Ldelem_I2, Type: typeof(short)),
                (OpCode: OpCodes.Ldelem_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Ldelem_I4, Type: typeof(int)),
                (OpCode: OpCodes.Ldelem_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Ldelem_I8, Type: typeof(long)),
                (OpCode: OpCodes.Ldelem_R4, Type: typeof(float)),
                (OpCode: OpCodes.Ldelem_R8, Type: typeof(double))
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
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeof(void*)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine();
                    GenerateArrayAccess(stack.Pop, stack, y => indexToStack[index].Assign($"static_cast<intptr_t>({y})"));
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
            new[] {
                (OpCode: OpCodes.Stelem_I, Type: typeof(IntPtr)),
                (OpCode: OpCodes.Stelem_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Stelem_I2, Type: typeof(short)),
                (OpCode: OpCodes.Stelem_I4, Type: typeof(int)),
                (OpCode: OpCodes.Stelem_I8, Type: typeof(long)),
                (OpCode: OpCodes.Stelem_R4, Type: typeof(float)),
                (OpCode: OpCodes.Stelem_R8, Type: typeof(double))
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
                x.Estimate = (index, stack) => {
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
                    if (t.IsValueType)
                    {
                        GenerateCheckNull(stack);
                        var after = indexToStack[index];
                        writer.WriteLine($@"{'\t'}if ({stack.Variable}->f_type()->f__is(&t__type_of<{Escape(t)}>::v__instance))
{'\t'}{'\t'}{after.Variable} = static_cast<{Escape(t)}*>({stack.Variable})->v__value;");
                        if (t.IsPrimitive && t != typeof(float) && t != typeof(double))
                            writer.WriteLine($@"{'\t'}else if ({stack.Variable}->f_type()->f__is(&t__type_of<{Escape(typeof(Enum))}>::v__instance))
{'\t'}{'\t'}switch ({stack.Variable}->f_type()->v__size) {{
{'\t'}{'\t'}case 1:
{'\t'}{'\t'}{'\t'}{after.Variable} = static_cast<{EscapeForStacked(t)}>(*reinterpret_cast<int8_t*>({stack.Variable} + 1));
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}case 2:
{'\t'}{'\t'}{'\t'}{after.Variable} = static_cast<{EscapeForStacked(t)}>(*reinterpret_cast<int16_t*>({stack.Variable} + 1));
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}case 4:
{'\t'}{'\t'}{'\t'}{after.Variable} = static_cast<{EscapeForStacked(t)}>(*reinterpret_cast<int32_t*>({stack.Variable} + 1));
{'\t'}{'\t'}{'\t'}break;
{'\t'}{'\t'}default:
{'\t'}{'\t'}{'\t'}{after.Variable} = static_cast<{EscapeForStacked(t)}>(*reinterpret_cast<int64_t*>({stack.Variable} + 1));
{'\t'}{'\t'}}}");
                        writer.WriteLine($@"{'\t'}else
{'\t'}{'\t'}[[unlikely]] f__throw_invalid_cast();");
                    }
                    else
                    {
                        writer.WriteLine($"\tif ({stack.Variable} && !{stack.Variable}->f_type()->{(t.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(t)}>::v__instance)) [[unlikely]] f__throw_invalid_cast();");
                    }
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Conv_Ovf_I1, Type: typeof(sbyte)),
                (OpCode: OpCodes.Conv_Ovf_U1, Type: typeof(byte)),
                (OpCode: OpCodes.Conv_Ovf_I2, Type: typeof(short)),
                (OpCode: OpCodes.Conv_Ovf_U2, Type: typeof(ushort)),
                (OpCode: OpCodes.Conv_Ovf_I4, Type: typeof(int)),
                (OpCode: OpCodes.Conv_Ovf_U4, Type: typeof(uint)),
                (OpCode: OpCodes.Conv_Ovf_I8, Type: typeof(long)),
                (OpCode: OpCodes.Conv_Ovf_U8, Type: typeof(ulong))
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(set.Type));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = static_cast<{primitives[set.Type]}>({stack.AsSigned});");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Conv_Ovf_I, Type: "intptr_t"),
                (OpCode: OpCodes.Conv_Ovf_U, Type: "uintptr_t")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(void*)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Assign($"static_cast<{set.Type}>({stack.AsSigned})")};");
                    return index;
                };
            }));
            instructions1[OpCodes.Ldtoken.Value].For(x =>
            {
                x.Estimate = (index, stack) => method.Module.ResolveMember(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments()) switch
                {
                    FieldInfo f => (index, stack.Push(typeof(RuntimeFieldHandle))),
                    MethodInfo m => (index, stack.Push(typeof(RuntimeMethodHandle))),
                    Type t => (index, stack.Push(typeof(RuntimeTypeHandle))),
                    _ => throw new Exception()
                };
                x.Generate = (index, stack) =>
                {
                    var member = method.Module.ResolveMember(ParseI4(ref index), method.DeclaringType?.GetGenericArguments(), GetGenericArguments());
                    writer.WriteLine($@" {member}
{'\t'}{indexToStack[index].Variable} = {member switch
{
    FieldInfo f => $"f__field_{Escape(f.DeclaringType)}__{Escape(f.Name)}()",
    MethodInfo m => $"{Escape(m)}::v__handle",
    Type t => $"&t__type_of<{Escape(t)}>::v__instance",
    _ => throw new Exception()
}};");
                    return index;
                };
            });
            new[] {
                (OpCode: OpCodes.Add_Ovf, Operator: "+"),
                (OpCode: OpCodes.Mul_Ovf, Operator: "*"),
                (OpCode: OpCodes.Sub_Ovf, Operator: "-")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeOfDiv_Un[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Assign($"{stack.Pop.AsSigned} {set.Operator} {stack.AsSigned}")};");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Add_Ovf_Un, Operator: "+"),
                (OpCode: OpCodes.Mul_Ovf_Un, Operator: "*"),
                (OpCode: OpCodes.Sub_Ovf_Un, Operator: "-")
            }.ForEach(set => instructions1[set.OpCode.Value].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeOfDiv_Un[(stack.Pop.VariableType, stack.VariableType)]));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Assign($"{stack.Pop.AsUnsigned} {set.Operator} {stack.AsUnsigned}")};");
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
            new[] {
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
            new[] {
                (OpCode: OpCodes.Ceq, Operator: "=="),
                (OpCode: OpCodes.Cgt, Operator: ">"),
                (OpCode: OpCodes.Clt, Operator: "<")
            }.ForEach(set => instructions2[set.OpCode.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeof(int)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {stack.Pop.AsSigned} {set.Operator} {stack.AsSigned} ? 1 : 0;");
                    return index;
                };
            }));
            new[] {
                (OpCode: OpCodes.Cgt_Un, Operator: ">"),
                (OpCode: OpCodes.Clt_Un, Operator: "<")
            }.ForEach(set => instructions2[set.OpCode.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Pop.Push(typeof(int)));
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
                    return (index, stack.Push(typeof(void*)));
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
                    return (index, stack.Pop.Push(typeof(void*)));
                };
                x.Generate = (index, stack) =>
                {
                    var m = ParseMethod(ref index);
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
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(byte*)));
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
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(Exception)));
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
                    writer.WriteLine(
                        Define(t).IsManaged
                            ? "\tf__store({0}, {1});"
                            : "\t{0} = {1};",
                        $"*static_cast<{EscapeForValue(t)}*>({stack.Variable})",
                        $"({EscapeForValue(t)}){{}}"
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
                x.Estimate = (index, stack) => (index + 4, stack.Push(typeof(uint)));
                x.Generate = (index, stack) =>
                {
                    var t = ParseType(ref index);
                    writer.WriteLine($" {t}\n\t{indexToStack[index].Variable} = sizeof({EscapeForValue(t)});");
                    return index;
                };
            });
            instructions2[OpCodes.Refanytype.Value & 0xff].For(x =>
            {
                x.Estimate = (index, stack) => (index, stack.Pop.Push(typeof(RuntimeTypeHandle)));
                x.Generate = (index, stack) =>
                {
                    writer.WriteLine($"\n\t{indexToStack[index].Variable} = {EscapeForValue(typeof(RuntimeTypeHandle))}{{static_cast<t__type*>({stack.Variable}.v__5ftype.v__5fvalue)}};");
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
