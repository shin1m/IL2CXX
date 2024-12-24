namespace IL2CXX.Tests;

[Parallelizable]
class LinqTests
{
    static int Count()
    {
        var n = Enumerable.Range(0, 128).Count(x => x >= 'A' && x <= 'Z');
        Console.WriteLine($"# of alphabets: {n}");
        return n == 26 ? 0 : 1;
    }
    static IEnumerable<string> EnumerateWords(IEnumerable<string> lines)
    {
        var isWords = Enumerable.Range(0, 128).Select(c => c >= 48 && c < 58 || c >= 65 && c < 91 || c == 95 || c >= 97 && c < 123).ToArray();
        bool isWord(char c) => c < 128 && isWords[c];
        IEnumerable<string> matches(string line)
        {
            for (var i = 0; i < line.Length;)
            {
                while (!isWord(line[i])) if (++i >= line.Length) yield break;
                var j = i;
                do ++j; while (j < line.Length && isWord(line[j]));
                yield return line.Substring(i, j - i).ToLowerInvariant();
                i = j;
            }
        }
        return lines.SelectMany(matches);
    }
    static string[] lines =
    {
        "Hello, World!",
        "Hello, this is shin!",
        "Good bye, World!",
        "Bye bye."
    };
    static int CountWords()
    {
        var word2count = EnumerateWords(lines).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        Console.WriteLine($"# of words: {word2count.Count}");
        foreach (var x in word2count) Console.WriteLine($"\t{x.Key}: {x.Value}");
        return word2count.Count == 7 ? 0 : 1;
    }
    static Func<string, string> Corrector(IReadOnlyDictionary<string, int> word2count)
    {
        void edits1(string word, Action<string> action)
        {
            void edit(string left, string right0, string right1)
            {
                for (var i = 0; i < 26; ++i)
                {
                    var s = left + (char)('a' + i);
                    action(s + right0);
                    action(s + right1);
                }
                action(left + right1);
            }
            edit(string.Empty, word, word.Substring(1));
            for (var i = 1; i < word.Length; ++i)
            {
                var right1 = word.Substring(i + 1);
                edit(word.Substring(0, i), word.Substring(i), right1);
                action(word.Substring(0, i - 1) + word[i] + word[i - 1] + right1);
            }
            for (var i = 0; i < 26; ++i) action(word + (char)('a' + i));
        }
        return word =>
        {
            if (word2count.ContainsKey(word)) return word;
            var top = (Count: 0, Word: word);
            void subscribe(string x)
            {
                if (word2count.TryGetValue(x, out var p) && p > top.Count) top = (p, x);
            }
            var e1 = new HashSet<string>();
            edits1(word, x =>
            {
                subscribe(x);
                e1.Add(x);
            });
            if (top.Count <= 0) foreach (var x in e1) edits1(x, subscribe);
            return top.Word;
        };
    }
    static int Correct()
    {
        var correct = Corrector(EnumerateWords(lines).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count()));
        bool test(string word, string expected)
        {
            var corrected = correct(word);
            Console.WriteLine($"{word}: {corrected}");
            return corrected == expected;
        }
        if (!test("hell", "hello")) return 1;
        if (!test("work", "world")) return 2;
        if (!test("wide", "wide")) return 3;
        return 0;
    }

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(Count) => Count(),
        nameof(CountWords) => CountWords(),
        nameof(Correct) => Correct(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(Count),
            nameof(CountWords),
            nameof(Correct)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
