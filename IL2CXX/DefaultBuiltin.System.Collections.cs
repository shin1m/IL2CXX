using System;
using System.Collections.Generic;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemCollections(this Builtin @this, Func<Type, Type> get) => @this
        .For(get(typeof(EqualityComparer<>)), (type, code) =>
        {
            code.ForGeneric(type.TypeInitializer, (transpiler, types) =>
            {
                var t = types[0];
                Type concrete = null;
                if (t == get(typeof(bool)))
                {
                    concrete = get(Type.GetType("System.Collections.Generic.ByteEqualityComparer", true));
                }
                else if (t.IsAssignableTo(get(typeof(IEquatable<>)).MakeGenericType(types)))
                {
                    concrete = get(Type.GetType("System.Collections.Generic.GenericEqualityComparer`1", true)).MakeGenericType(types);
                }
                else if (t.IsGenericType)
                {
                    if (t.GetGenericTypeDefinition() == get(typeof(Nullable<>)))
                    {
                        var gas = t.GetGenericArguments();
                        if (gas[0].IsAssignableTo(get(typeof(IEquatable<>)).MakeGenericType(gas))) concrete = get(Type.GetType("System.Collections.Generic.NullableEqualityComparer`1", true)).MakeGenericType(gas);
                    }
                }
                else if (t.IsEnum)
                {
                    switch (Type.GetTypeCode(t.GetEnumUnderlyingType()))
                    {
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.UInt16:
                            concrete = get(Type.GetType("System.Collections.Generic.EnumEqualityComparer`1", true)).MakeGenericType(types);
                            break;
                    }
                }
                if (concrete == null) concrete = get(Type.GetType("System.Collections.Generic.ObjectEqualityComparer`1", true)).MakeGenericType(types);
                var constructor = concrete.GetConstructor(declaredAndInstance, null, Type.EmptyTypes, null);
                transpiler.Enqueue(constructor);
                return ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(concrete)}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}t_static::v_instance->v_{transpiler.Escape(type.MakeGenericType(types))}->v__3cDefault_3ek_5f_5fBackingField = p;
", 0);
            });
        })
        .For(get(Type.GetType("System.Collections.Generic.ArraySortHelper`1")), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("CreateArraySortHelper", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) =>
                {
                    var concrete = (get(typeof(IComparable<>)).MakeGenericType(types).IsAssignableFrom(types[0]) ? get(Type.GetType("System.Collections.Generic.GenericArraySortHelper`1")) : type).MakeGenericType(types);
                    var constructor = concrete.GetConstructor(Type.EmptyTypes);
                    transpiler.Enqueue(constructor);
                    return ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(concrete)}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}return p;
", 0);
                }
            );
        })
        .For(get(Type.GetType("System.Collections.Generic.ComparerHelpers")), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("CreateDefaultComparer", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn {};\n", 0)
            );
        });
    }
}
