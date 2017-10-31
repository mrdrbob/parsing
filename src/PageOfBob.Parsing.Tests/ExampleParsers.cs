
using static PageOfBob.Parsing.Rules;
using static PageOfBob.Parsing.StringRules;

namespace PageOfBob.Parsing.Tests
{
    public static class ExampleParsers
    {
        public static Rule<char, string> QuotedStringParser()
        {
            // Define the two special characters: " and \
            var QUOTE = Char('"');
            var ESCAPE = Char('\\');

            // Define classes of characters.  Characters that require escape, and those that do not.
            var charThatRequiresEscape = Any(QUOTE, ESCAPE);
            var charThatDoesNotRequireEsquape = charThatRequiresEscape.Not();

            // Define what an escaped character looks like (slash followed by something that requires an escape)
            var escapedChar = ESCAPE
                .ThenKeep(charThatRequiresEscape);

            // Define what the characters in the "middle" of the quoted string
            // (either escaped characters or characters that do not require an escape).
            // Accept zero or more of those characters.
            // Then join all those characters into a string.
            var center = Any(charThatDoesNotRequireEsquape, escapedChar).Many().JoinText();

            // Define the final rule, a quote, middle of the quote, then a closing quote.
            var quotedStringRule = QUOTE
                .ThenKeep(center)
                .ThenIgnore(QUOTE);

            return quotedStringRule;
        }

        public static Rule<char, char> NaiveEvenDigit()
            => (input) =>
            {
                var cast = input as Source<char>;
                if (cast == null)
                    return Result.Fail<char>("Input is empty");

                var c = cast.Token;
                if (!char.IsDigit(c))
                    return Result.Fail<char>("Expected a digit");

                if (c != '2' && c != '4' && c != '6' && c != '8')
                    return Result.Fail<char>("Expected a digit");

                return Result.Success(c, cast.Next());
            };

        public static Rule<char, char> BetterEvenDigit()
            => (input) =>
                input.Match(
                    empty => Result.Fail<char>("Input is empty"),
                    source =>
                    {
                        if (source.Token != '2' && source.Token != '4' && source.Token != '6' && source.Token != '8')
                            return Result.Fail<char>("Expected a digit");

                        return Result.Success(source.Token, source.Next());
                    });
    }
}
