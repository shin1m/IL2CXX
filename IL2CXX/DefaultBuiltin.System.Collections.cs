using System;
using System.Collections.Generic;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemCollections(this Builtin @this) => @this
        .For(typeof(EqualityComparer<>), (type, code) =>
        {
            code.ForGeneric(type.TypeInitializer, (transpiler, types) =>
            {
                var gt = type.MakeGenericType(types);
                var concrete = gt.GetProperty(nameof(EqualityComparer<object>.Default)).GetValue(null).GetType();
                var constructor = concrete.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                transpiler.Enqueue(constructor);
                return ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(concrete)}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}t_static::v_instance->v_{transpiler.Escape(gt)}->v__3cDefault_3ek_5f_5fBackingField = p;
", 0);
            });
        })
        .For(Type.GetType("System.Collections.Generic.ArraySortHelper`1"), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("CreateArraySortHelper", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) =>
                {
                    var concrete = (typeof(IComparable<>).MakeGenericType(types).IsAssignableFrom(types[0]) ? Type.GetType("System.Collections.Generic.GenericArraySortHelper`1") : type).MakeGenericType(types);
                    var constructor = concrete.GetConstructor(Type.EmptyTypes);
                    transpiler.Enqueue(constructor);
                    return ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(concrete)}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}return p;
", 0);
                }
            );
        })
        .For(Type.GetType("System.Collections.Generic.ComparerHelpers"), (type, code) =>
        {
            code.For(
                type.GetMethod("CreateDefaultComparer", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn {};\n", 0)
            );
        });
    }
}
