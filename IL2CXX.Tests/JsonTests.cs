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

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Deserialize) => Deserialize(),
            nameof(Serialize) => Serialize(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var type = Type.GetType("System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1,System.Text.Json", true);
            build = Utilities.Build(Run, new[] {
                type.MakeGenericType(typeof(Foo))
            }, new[] {
                type,
                typeof(Foo)
            });
        }
        [Test]
        public void Test(
            [Values(
                nameof(Deserialize),
                nameof(Serialize)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
