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
                var identifier = transpiler.Escape(concrete);
                var constructor = concrete.GetConstructor(Type.EmptyTypes);
                transpiler.Enqueue(constructor);
                return $@"{'\t'}auto p = f__new_zerod<{identifier}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}t_static::v_instance->v_{transpiler.Escape(gt)}->v__3cDefault_3ek_5f_5fBackingField = std::move(p);
";
            });
        })
        .For(Type.GetType("System.Collections.Generic.ArraySortHelper`1"), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("CreateArraySortHelper", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => "\treturn {};\n"
            );
        })
        .For(Type.GetType("System.Collections.Generic.ComparerHelpers"), (type, code) =>
        {
            code.For(
                type.GetMethod("CreateDefaultComparer", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn {};\n"
            );
        });
    }
}
