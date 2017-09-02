using System.Collections.Generic;
using static PageOfBob.Parsing.Rules;

namespace PageOfBob.Parsing
{
    public static class StringRules
    {
        public static Rule<char, char> Char(char c) => Match(c);

        public static Rule<char, char> IChar(char c) => Match<char>(x => char.ToUpperInvariant(x) == char.ToUpperInvariant(c), $"Expected {c}");

        public static readonly Rule<char, char> IsLetter = Match<char>(char.IsLetter, "Expected Letter");

        public static readonly Rule<char, char> IsDigit = Match<char>(char.IsDigit, "Expected Digit");

        static IEnumerable<Rule<char, char>> SplitText(string text)
        {
            foreach(var c in text) {
                yield return Char(c);
            }
        }

        static IEnumerable<Rule<char, char>> SplitIText(string text)
        {
            foreach (var c in text)
            {
                yield return IChar(c);
            }
        }


        public static Rule<char, string> Text(string text, string message = null) => Sequence(SplitText(text), message ?? $"Expected '{text}'").JoinText();

        public static Rule<char, string> IText(string text, string message = null) => Sequence(SplitIText(text), message ?? $"Expected '{text}'").JoinText();

        public static Rule<char, string> JoinText(this Rule<char, char[]> rule) => rule.Map(x => string.Join("", x));
    }
}
