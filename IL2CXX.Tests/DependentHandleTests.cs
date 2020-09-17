using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class DependentHandleTests
    {
        static Func<int> Default()
        {
            var type = Type.GetType("System.Runtime.CompilerServices.DependentHandle");
            var constructor = type.GetConstructor(new[] { typeof(object), typeof(object) });
            var free = type.GetMethod("Free");
            var get = type.GetMethod("GetPrimaryAndSecondary");
            var x = Expression.Variable(typeof(string));
            var y = Expression.Variable(typeof(string));
            var h = Expression.Variable(type);
            var xs = Expression.Variable(typeof(object[]));
            var z = Expression.Variable(typeof(object));
            var label = Expression.Label(typeof(int));
            return Expression.Lambda<Func<int>>(Expression.Block(typeof(int), new[] { x, y, h },
                Expression.Assign(x, Expression.Constant("Hello")),
                Expression.Assign(y, Expression.Constant("World")),
                Expression.Assign(h, Expression.New(constructor, x, y)),
                Expression.TryFinally(
                    Expression.Block(typeof(int), new[] { xs, z },
                        Expression.Assign(xs, Expression.NewArrayInit(typeof(object), Expression.Constant(null))),
                        Expression.Assign(z, Expression.Call(h, get, xs)),
                        Expression.IfThen(
                            Expression.OrElse(
                                Expression.NotEqual(z, x),
                                Expression.NotEqual(Expression.ArrayAccess(xs, Expression.Constant(0)), y)
                            ),
                            Expression.Return(label, Expression.Constant(1))
                        ),
                        Expression.Assign(x, Expression.Constant(null, typeof(string))),
                        Expression.Assign(y, Expression.Constant(null, typeof(string))),
                        Expression.Assign(z, Expression.Constant(null)),
                        Expression.Assign(Expression.ArrayAccess(xs, Expression.Constant(0)), Expression.Constant(null)),
                        Expression.Invoke(Expression.Constant((Action)GC.Collect)),
                        Expression.Assign(z, Expression.Call(h, get, xs)),
                        Expression.Label(label, Expression.Condition(
                            Expression.AndAlso(
                                Expression.Equal(z, Expression.Constant(null)),
                                Expression.Equal(Expression.ArrayAccess(xs, Expression.Constant(0)), Expression.Constant(null))
                            ),
                            Expression.Constant(0),
                            Expression.Constant(2)
                        ))
                    ),
                    Expression.Call(h, free)
                )
            )).Compile();
        }
        [Test, Ignore("Requires decoding DynamicMethod")]
        public void TestDefault() => Utilities.Test(Default());
    }
}
