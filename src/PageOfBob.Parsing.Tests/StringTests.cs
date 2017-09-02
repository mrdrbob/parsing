using Xunit;
using static PageOfBob.Parsing.Sources;
using static PageOfBob.Parsing.StringRules;

namespace PageOfBob.Parsing.Tests
{
    public class StringTests
    {
        [Fact]
        public void CharIsCaseSensitive()
        {
            var source = CharSource("aA");
            var rule = Char('a');

            var next = rule(source).AssertEquals('a');
            rule(next).AssertFails();
        }

        [Fact]
        public void ICharIsCaseInsensitive()
        {
            var source = CharSource("aA");
            var rule = IChar('a');

            var next = rule(source).AssertEquals('a');
            rule(next).AssertEquals('A').AssertAtEnd();
        }

        [Fact]
        public void IsLetterMatchesOnlyLetters()
        {
            var source = CharSource("a~");

            var next = IsLetter(source).AssertEquals('a');
            IsLetter(next).AssertFails();
        }

        [Fact]
        public void IsDigitMatchesOnlyDigits()
        {
            var source = CharSource("1a");

            var next = IsDigit(source).AssertEquals('1');
            IsDigit(next).AssertFails();
        }

        [Fact]
        public void JoinTextJoinsText()
        {
            var source = CharSource("aaa");
            var rule = Char('a').Many().JoinText();

            rule(source).AssertEquals("aaa").AssertAtEnd();
        }

        [Fact]
        public void TextMatchesCaseSensitive()
        {
            var rule = Text("abc");

            rule(CharSource("abc")).AssertEquals("abc").AssertAtEnd();
            rule(CharSource("ABC")).AssertFails();
        }

        [Fact]
        public void ITextMatchesCaseInsensitive()
        {
            var rule = IText("abc");

            rule(CharSource("abc")).AssertEquals("abc").AssertAtEnd();
            rule(CharSource("ABC")).AssertEquals("ABC").AssertAtEnd();
        }
    }
}
