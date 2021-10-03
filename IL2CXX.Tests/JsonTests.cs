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
        public void OneTimeSetUp() => build = Utilities.Build(Run, new[] {
            Type.GetType("System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1,System.Text.Json", true).MakeGenericType(typeof(Foo))
        });
        [TestCase(nameof(Deserialize))]
        [TestCase(nameof(Serialize))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
