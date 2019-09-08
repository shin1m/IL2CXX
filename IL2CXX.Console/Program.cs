using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace IL2CXX.Console
{
    using System;

    class Program
    {
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
        static int Test()
        {
            string[] lines = {
                "Hello, World!",
                "Hello, this is shin!",
                "Good bye, World!",
                "Bye bye."
            };
            var correct = Corrector(EnumerateWords(lines).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count()));
            var log = string.Empty;
            void test(string word) {
                var corrected = correct(word);
                log += $"corrected: {word}: {corrected}\n";
                Console.WriteLine($"{word}: {corrected}");
            }
            var ts = new[] {
                "hell",
                "shin1",
                "work",
                "wide",
                "wild"
            }.Select(x => new Thread(() => {
                for (var i = 0; i < 10; ++i) test(x);
            })).ToList();
            foreach (var x in ts) x.Start();
            foreach (var x in ts) x.Join();
            Console.WriteLine(log);
            return 0;
        }
        static void Main(string[] args)
        {
            new Transpiler(DefaultBuiltin.Create(), Console.Error.WriteLine).Do(typeof(Program).GetMethod(nameof(Test), BindingFlags.Static | BindingFlags.NonPublic), Console.Out);
        }
    }
}
