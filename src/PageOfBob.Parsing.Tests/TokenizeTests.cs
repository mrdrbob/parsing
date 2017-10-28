using Xunit;
using static PageOfBob.Parsing.Rules;
using static PageOfBob.Parsing.StringRules;
using static PageOfBob.Parsing.Sources;
using System.Linq;
using System;

namespace PageOfBob.Parsing.Tests
{
    public class TokenizeTests
    {
        [Fact]
        public void CanTokenizeARule()
        {
            var source = CharSource("abcabcabc");
            var rule = Text("abc");
            var tokens = rule.Tokenize(source).ToArray();

            Assert.Equal(new string[] { "abc", "abc", "abc" }, tokens);
        }

        [Fact]
        public void TokenizingEmptyDoesNotFail()
        {
            var source = CharSource("");
            var rule = Text("abc");
            var tokens = rule.Tokenize(source).ToArray();

            Assert.Equal(new string[0], tokens);
        }

        [Fact]
        public void ByDefaultAFailingRuleThrowsAnException()
        {
            var source = CharSource("abcd");
            var rule = Text("abc");
            Assert.Throws<FormatException>(() =>
            {
                rule.Tokenize(source).ToArray();
            });
        }

        [Fact]
        public void TokenizerCanIgnoreFailingRule()
        {
            var source = CharSource("abcd");
            var rule = Text("abc");
            var result = rule.Tokenize(source, false).ToArray();
            Assert.Equal(new[] { "abc" }, result);
        }
    }
}
