PageOfBob.Parsing
=================

A tiny library for writing tiny parsers in C#.  The core idea is building parsers from simple rules.

For example, parsing a basic quoted string:

```csharp
using static PageOfBob.Parsing.Rules;
using static PageOfBob.Parsing.StringRules;

// Define the two special characters: " and \
var QUOTE = Char('"');
var ESCAPE = Char('\\');

// Define classes of characters.  Characters that require escape, and those that do not.
var charThatRequiresEscape = Any(QUOTE, ESCAPE);
var charThatDoesNotRequireEsquape = charThatRequiresEscape.Not();

// Define what an escaped character looks like (slash followed by something that requires an escape)
var escapedChar = ESCAPE
    .ThenKeep(charThatRequiresEscape);

// Define the characters in the "middle" of the quoted string
// (either escaped characters or characters that do not require an escape).
// Accept zero or more of those characters.
// Then join all those characters into a string.
var center = Any(charThatDoesNotRequireEsquape, escapedChar).Many().JoinText();

// Define the final rule, a quote, middle of the quote, then a closing quote.
var quotedStringRule = QUOTE
    .ThenKeep(center)
    .ThenIgnore(QUOTE);

return quotedStringRule;
```

And here is how you would use this rule:

```csharp
var input = Sources.CharSource("\"this is a test\"");
var result = quotedStringRule(input);
var finalValue = result.Match(
  fail => {
    Console.Error.WriteLine($"Failed: {fail.Message}");
    return null;
  },
  success => {
    Console.Out.WriteLine($"Parsed String: {success.Value}");
    return success.Value;
  }
)
```

*Obligatory Disclaimer:* This parsing library is built to make writing small parsers directly in C# simple.  It is not optimized for speed or memory usage.  It works well for parsing relatively simple objects in memory.  For very large token streams/object graphs, or where performance is critical, other solutions are probably more appropriate.

Rules
-----

Parsers are built from simple rules.  The **parser rule** is a method that takes a token source (`ISource<TToken>`) and returns a result (`IResult<TToken, TOutput>`).  In general, rules should be pure functions.

A **token source** can either be empty (`Empty<TToken>`) or non-emtpy (`Source<TToken>`).  A non-empty source contains a single token (`TToken Token { get; }`) and a method to get the next token from the stream (`ISource<TToken> Next() `).

A **result** (`IResult<TToken, TOutput>`) can either be a failure (`Failure<TToken, TValue>`) which contains only an error message (`string Message { get; }`) or a success (`Success<TToken, TValue>`) which contains a parsed value (`TValue Value { get; }`) and the next token source from the stream (`ISource<TToken> Next { get; }`).

Rules are then combined to form complex parsers build from simple blocks.

Building a simple rule
----------------------

Here is a naïve example of a simple rule that accepts any even digit:

```csharp
var cast = input as Source<char>;
if (cast == null)
    return Result.Fail<char>("Input is empty");

var c = cast.Token;

if (c != '2' && c != '4' && c != '6' && c != '8')
    return Result.Fail<char>("Expected a digit");

return Result.Success(c, cast.Next());
```

This example has room for improvement.  The casting of input is not necessary, as there is an extension method that allows you to treat a source (`ISource<TToken>`) like a discriminated union.

This could be made shorter as:

```csharp
return input.Match(
    // Fail if the source is empty
    () => Result.Fail<char>("Input is empty"), 
    // Source is not empty, check it's token against known-good tokens
    source => 
    {
        if (source.Token != '2' && source.Token != '4' && source.Token != '6' && source.Token != '8')
            return Result.Fail<char>("Expected a digit");

        return Result.Success(source.Token, source.Next());
    });
```

But even this can be made shorter using built-in Rules.  This rule can be written a couple ways:

```csharp
// Check using Rules.Match and a delegate.
var evenDigit = Rules.Match<char>(x => x == '2' || x == '4' || x == '6' || x == '8');
// Check using Any to test a rule for each possible char.
var evenDigit = Rules.Any(StringRules.Char('2'), StringRules.Char('4'), StringRules.Char('6'), StringRules.Char('8'));
```

Note that in this example, `Rules.Match` and `StringRules.Char` both already have empty source check as part of their logic, so that can be omitted.

These can be made slightly shorter by `using static` the `Rules` and `StringRules` classes.

```csharp
using static PageOfBob.Parsing.Rules;
using static PageOfBob.Parsing.StringRules;

var evenDigit = Match<char>(x => x == '2' || x == '4' || x == '6' || x == '8');
// OR:
var evenDigit = Any(Char('2'), Char('4'), Char('6'), Char('8'));
```

Basic Built-in Rules
--------------------

Below is a list of built-in rules.  Probably the fastest way to familiarize yourself with these rules and how to combine them would be to look through the unit tests.

* `Rule<T, T> Match<T>(Func<T, bool> match, string message = null)` - Basis for most single-token parsing rules.  Succeeds for non-emtpy source and when `match` returns true, otherwise fails.
* `Rule<T, T> Any<T>()` - Always succeeds with non-empty source and consumes/returns 1 token.
* `Rule<T, T> Match<T>(T value, string message = null)` - Succeeds if non-empty source and token matches `value` exactly.
* `Rule<T, K[]> Sequence<T, K>(IEnumerable<Rule<T, K>> rules, string message = null)` - Each rule in the `rules` sequence must succeed. 
* `Rule<T, O> Map<T, I, O>(this Rule<T, I> rule, Func<I, O> map)` - If `rule` succeeds, then the value from the `rule` result is transformed by the `map` function.
* `Rule<T, K> Any<T, K>(params Rule<T, K>[] rules)` - Returns the first successful result from `rules`.  Otherwise, fails.
* `Rule<T, O> Then<T, L, R, O>(this Rule<T, L> left, Rule<T, R> right, Func<L, R, O> map)` - Attempts the `left` rule, then if that succeeds the `right` rule.  If both rules succeed, then the results from each are passed to `map` to create the final result.  Otherwise, fails.
* `ThenIgnore<T, L, R>(this Rule<T, L> left, Rule<T, R> right)` - If both rules succeed, returns the result of the `left` rule (ignores value from `right` rule).
* `Rule<T, R> ThenKeep<T, L, R>(this Rule<T, L> left, Rule<T, R> right)` - If both rules succeed, returns the result of the `right` rule (keeps value from `right` rule).
* `Rule<T, K> ThenSet<T, R, K>(this Rule<T, K> left, Rule<T, R> right, Action<K, R> action)` - If both rules succeed, calls `action` to mutate the result of the `left` rule using the value from the `right` rule.  Useful for building complex objects, even it's not exactly a pure function.
* `Always<T, K>(Func<K> action)`  - Always succeeds with the value of `action` without consuming any input.
* `Rule<T, K[]> Many<T, K>(this Rule<T, K> rule, bool required = false, string message = null)` - Applies `rule` repeated until it fails, returns all resulting values as an array.  If `required` is true, then `rule` must match at least once.  If `required` is false, then this will succeed event if `rule` never succeeds -- will return an empty array as a value.
* `Rule<T, T> Not<T>(this Rule<T, T> rule, string message = null)` - Only works on token matching, but will return any tokens that do not succeed against `rule`.
* `Optional<T, K>(this Rule<T, K> rule, K defaultValue)` - Makes `rule` optional.  If `rule` does not succeed, a success value of `defaultvalue` is returned and no input is consumed.
* `WithMessage<T, K>(this Rule<T, K> rule, string message)` - Allows one to override the error message of any rule.
* `ThenEnd<T, K>(this Rule<T, K> rule)` - Succeeds only if `rule` succeeds and the input is empty after `rule` succeeds.

Built-in String Rules
---------------------

These rules are in the `StringRules` static class:

* `Rule<char, char> Char(char c)` - Matches a specific character (case-sensitive).
* `Rule<char, char> IChar(char c)` - Matches a specific character (case-insensitive).
* `Rule<char, char> IsLetter` - Matches any letter.
* `Rule<char, char> IsDigit` - Matches any digit.
* `Rule<char, string> Text(string text, string message = null)` - Matches a string of text (case-sensitive).
* `Rule<char, string> IText(string text, string message = null)` - Matches a string of text (case-insensitive).
* `Rule<char, string> JoinText(this Rule<char, char[]> rule)` - Maps a char array result into a string.

Results
-------

Like `ISource<TToken>`, `IResult<TToken, TOutput>` has an extension method that allows one to treat it like a discriminated union.  For example:

```csharp
var results = rule(input);

bool wasSuccess = results.Match(
  fail => {
    Console.Error.WriteLine("Oh no! " + fail.Message);
    return false;
  },
  success => {
    Console.Out.WriteLine("Success! " + success.Value.ToString());
    return true;
  });
```




