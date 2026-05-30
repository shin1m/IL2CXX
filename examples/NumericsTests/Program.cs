using System.Globalization;
using System.Reflection;
using System.Numerics.Tests;
using Xunit.Sdk;

var coerce = new Coerce();
var count = 0;
foreach (var type in Assembly.GetEntryAssembly().GetExportedTypes())
{
    var facts = type.GetMethods().Where(x => x.IsDefined(typeof(FactAttribute)));
    if (!facts.Any()) continue;
    Console.WriteLine(type);
    var tests = type.GetConstructor([]).Invoke(null);
    try
    {
        foreach (var method in facts)
            if (method.IsDefined(typeof(TheoryAttribute)))
            {
                Console.WriteLine($"\t{method.Name}");
                foreach (var data in method.GetCustomAttributes<DataAttribute>().SelectMany(x => x.GetData(method)))
                {
                    Console.WriteLine($"{count}\t\t{string.Join(", ", data)}");
                    method.Invoke(tests, default, coerce, data, null);
                    ++count;
                }
            }
            else
            {
                Console.WriteLine($"{count}\t{method.Name}");
                method.Invoke(tests, null);
                ++count;
            }
    }
    finally
    {
        (tests as IDisposable)?.Dispose();
    }
}
Console.WriteLine($"count: {count}");

class Coerce : Binder
{
    public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state) => Type.DefaultBinder.BindToMethod(bindingAttr, match, ref args, modifiers, culture, names, out state);
    public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture) => Type.DefaultBinder.BindToField(bindingAttr, match, value, culture);
    public override void ReorderArgumentArray(ref object[] args, object state) => Type.DefaultBinder.ReorderArgumentArray(ref args, state);
    public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers) => Type.DefaultBinder.SelectMethod(bindingAttr, match, types, modifiers);
    public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers) => Type.DefaultBinder.SelectProperty(bindingAttr, match, returnType, indexes, modifiers);
    public override object ChangeType(object value, Type type, CultureInfo culture) => Convert.ChangeType(value, type);
}
