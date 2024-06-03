using System;
using System.Text.Json;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class JsonTests
    {
        class Foo
        {
            public int ID;
            public string Value;
        }
        enum Bar
        {
            X, Y
        }

        static int Deserialize()
        {
            var foo = JsonSerializer.Deserialize<Foo>("{\"ID\":1,\"Value\":\"foo\"}", new JsonSerializerOptions { IncludeFields = true });
            return foo.ID == 1 && foo.Value == "foo" ? 0 : 1;
        }
        static int Serialize()
        {
            var json = JsonSerializer.Serialize(new Foo { ID = 1, Value = "foo" }, new JsonSerializerOptions { IncludeFields = true });
            return json == "{\"ID\":1,\"Value\":\"foo\"}" ? 0 : 1;
        }
        static int SerializeEnum()
        {
            var json = JsonSerializer.Serialize(Bar.Y);
            return json == "1" ? 0 : 1;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Deserialize) => Deserialize(),
            nameof(Serialize) => Serialize(),
            nameof(SerializeEnum) => SerializeEnum(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var jpi = Type.GetType("System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1, System.Text.Json", true);
            var odc = Type.GetType("System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1, System.Text.Json", true);
            var odcOfFoo = odc.MakeGenericType(typeof(Foo));
            var ec = Type.GetType("System.Text.Json.Serialization.Converters.EnumConverter`1, System.Text.Json", true);
            var ecOfBar = ec.MakeGenericType(typeof(Bar));
            build = Utilities.Build(Run, [
                odcOfFoo,
                ecOfBar
            ], [
                jpi.MakeGenericType(typeof(int)),
                jpi.MakeGenericType(typeof(string)),
                odc,
                typeof(Foo),
                odcOfFoo,
                typeof(Bar),
                ecOfBar
            ]);
        }
        [Test]
        public void Test(
            [Values(
                nameof(Deserialize),
                nameof(Serialize),
                nameof(SerializeEnum)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
