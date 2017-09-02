using Xunit;
using static PageOfBob.Parsing.Sources;

namespace PageOfBob.Parsing.Tests
{
    public class ExampleTests
    {
        static readonly Rule<char, string> QuotedInstance = ExampleParsers.QuotedStringParser();
        static readonly Rule<char, char> NaiveEven = ExampleParsers.NaiveEvenDigit();

        [Fact]
        public void QuotedStringWorks()
        {
            var source = CharSource("\"this is a test\"");
            QuotedInstance(source).AssertEquals("this is a test");
        }

        [Fact]
        public void QuotedStringEscapesWork()
        {
            var source = CharSource("\"this \\\\ is a \\\"test\\\"\"");
            QuotedInstance(source).AssertEquals("this \\ is a \"test\"");
        }

        [Fact]
        public void QuotedStringInvalidQuotesFail()
        {
            var source = CharSource("\"test\\a\"");
            QuotedInstance(source).AssertFails();
        }

        [Fact]
        public void QuotedStringMustStartWithQuote()
        {
            var source = CharSource("a\"");
            QuotedInstance(source).AssertFails();
        }

        [Fact]
        public void QuotedStringMustEndWithQuote()
        {
            var source = CharSource("\"a");
            QuotedInstance(source).AssertFails();
        }

        [Fact]
        public void NaiveEvenCheckWorks()
        {
            var source = CharSource("2468");
            var rule = NaiveEven.Many().JoinText().ThenEnd();
            rule(source).AssertEquals("2468");
        }

        [Fact]
        public void NaiveEvenFailsOdd()
        {
            var source = CharSource("3");
            NaiveEven(source).AssertFails();
        }

        [Fact]
        public void NaiveEvenFailsNonDigit()
        {
            var source = CharSource("a");
            NaiveEven(source).AssertFails();
        }
    }
}
