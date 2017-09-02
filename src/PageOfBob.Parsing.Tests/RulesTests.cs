using System;
using System.Linq;
using Xunit;
using static PageOfBob.Parsing.Rules;
using static PageOfBob.Parsing.Sources;

namespace PageOfBob.Parsing.Tests
{
    public class RulesTest
    {
        [Fact]
        public void StringSouceWords()
        {
            var source = CharSource("abc");

            var a = source.Assume();
            Assert.NotNull(a);
            Assert.Equal('a', a.Token);

            var b = a.Next().Assume();
            Assert.NotNull(b);
            Assert.Equal('b', b.Token);

            var c = b.Next().Assume();
            Assert.NotNull(c);
            Assert.Equal('c', c.Token);

            c.Next().AssertAtEnd();
        }

        [Fact]
        public void AnyMatchesAnything()
        {
            var source = CharSource("abc");
            var any = Any<char>();

            var a = any(source).AssertEquals('a');
            var b = any(a).AssertEquals('b');
            var c = any(b).AssertEquals('c');
            c.AssertAtEnd();
        }

        [Fact]
        public void ExactMatchesMatchesExactly()
        {
            var source = CharSource("abc");
            var aRule = Match('a');
            var zRule = Match('z');

            aRule(source).AssertEquals('a');
            zRule(source).AssertFails();
        }

        [Fact]
        public void SequenceMatchWorks()
        {
            var source = CharSource("abc");
            var rules = "abc".Select(x => Match(x));
            var rule = Sequence(rules);

            var next = rule(source).AssertEquals(new[] { 'a', 'b', 'c' });
            next.AssertAtEnd();
        }

        [Fact]
        public void CanMapResult()
        {
            var source = CharSource("a");
            var rule = Any<char>().Map(char.GetNumericValue);

            var next = rule(source).AssertEquals(char.GetNumericValue('a'));
            next.AssertAtEnd();
        }

        [Fact]
        public void AnyMatchesFirstRule()
        {
            var source = CharSource("a");
            var doesNotMatch = Match('z').Map(x => "DNM");
            var firstMatch = Any<char>().Map(x => "FIRST");
            var secondMatch = Match('a').Map(x => "SECOND");

            // Make sure second rule DOES match.
            secondMatch(source).AssertEquals("SECOND");

            // Make sure doesNot rule DOESN'T match.
            doesNotMatch(source).AssertFails();

            // Combine and test all 3
            var rule = Any(doesNotMatch, firstMatch, secondMatch);
            var next = rule(source).AssertEquals("FIRST");

            next.AssertAtEnd();
        }

        [Fact]
        public void AnyRuleShortCircuits()
        {
            var source = CharSource("a");
            var firstRule = Any<char>();
            var secondRule = Any<char>().Map<char, char, char>(x => { throw new InvalidOperationException(); });
            var rule = Any(firstRule, secondRule);

            rule(source).AssertEquals('a');
        }

        [Fact]
        public void ThenProcessesBothRules()
        {
            var source = CharSource("ab");
            var firstRule = Match('a');
            var secondRule = Match('b');
            var rule = firstRule.Then(secondRule, (a, b) => new string(new char[] { a, b }));
            var next = rule(source).AssertEquals("ab");
            next.AssertAtEnd();
        }

        [Fact]
        public void SecondRuleCanFailThenRule()
        {
            var source = CharSource("ab");
            var firstRule = Match('a');
            var secondRule = Match('z');

            // First rule must match
            var firstMatch = firstRule(source).AssertEquals('a');

            // Second rule must fail
            secondRule(firstMatch).AssertFails();

            // Therefor combined rule must fail
            var rule = firstRule.Then(secondRule, (a, b) => true);
            rule(source).AssertFails();
        }

        [Fact]
        public void FirstRuleCanFailThenRule()
        {
            var source = CharSource("ab");
            var firstRule = Match('z');
            var secondRule = Match('b');

            // First rule must fail
            firstRule(source).AssertFails();

            // Second rule must succeed
            var next = source.Assume().Next();
            secondRule(next).AssertEquals('b');

            // Combined rule must fail
            var rule = firstRule.Then(secondRule, (a, b) => true);
            rule(source).AssertFails();
        }

        [Fact]
        public void ThenShortCircuitsOnFailure()
        {
            var source = CharSource("ab");
            var firstRule = Match('z');
            var secondRule = Match('b').Map<char, char, char>(x => { throw new InvalidOperationException(); });

            // First rule must fail
            firstRule(source).AssertFails();

            // Combined rule must fail without triggering InvalidOperationException
            var rule = firstRule.Then(secondRule, (a, b) => true);
            rule(source).AssertFails();
        }

        [Fact]
        public void ThenIgnoreWorks()
        {
            var source = CharSource("ab");
            var rule = Match('a')
                .ThenIgnore(Match('b'));

            rule(source).AssertEquals('a').AssertAtEnd();
        }

        [Fact]
        public void ThenKeepWorks()
        {
            var source = CharSource("ab");
            var rule = Match('a')
                .ThenKeep(Match('b'));

            rule(source).AssertEquals('b').AssertAtEnd();
        }

        public class TestPoco
        {
            public object Value { get; set; }
        }

        [Fact]
        public void ThenSetWorks()
        {
            var source = CharSource("ab");
            var rule = Match('a').Map(x => new TestPoco { Value = x })
                .ThenSet(Match('b'), (t, newValue) => t.Value = newValue );

            rule(source).AssertSuccess(x => Assert.Equal('b', x.Value)).AssertAtEnd();
        }

        [Fact]
        public void AlwaysSucceedsEventWhenSourceIsEmpty()
        {
            var source = CharSource("");
            var rule = Always<char, int>(() => 1);
            rule(source).AssertEquals(1).AssertAtEnd();
        }


        [Fact]
        public void AlwaysConsumesNoInput()
        {
            var source = CharSource("a");
            var rule = Always<char, int>(() => 1);
            var next = rule(source).AssertEquals(1);
            next.AssertNotAtEnd();

            var rule2 = Match('a');
            rule2(next).AssertEquals('a').AssertAtEnd();
        }

        [Fact]
        public void AlwaysAlwaysCreatesNewObject()
        {
            var source = CharSource("");

            int counter = 0;
            var rule = Always<char, int>(() => { counter += 1; return counter; });
            var next = rule(source).AssertEquals(1);
            next = rule(next).AssertEquals(2);
            next = rule(next).AssertEquals(3);
            next.AssertAtEnd();

            Assert.Equal(3, counter);
        }

        [Fact]
        public void ManyCanBeOptional()
        {
            var source = CharSource("a");
            var rule = Match('z').Many();
            var next = rule(source).AssertEquals(new char[0]);

            var rule2 = Match('a');
            rule2(next).AssertEquals('a').AssertAtEnd();
        }

        [Fact]
        public void ManyCanBeRequired()
        {
            var source = CharSource("a");
            var rule = Match('z').Many(required: true);
            rule(source).AssertFails();

            var rule2 = Match('a');
            rule2(source).AssertEquals('a').AssertAtEnd();
        }

        [Fact]
        public void ManyMatchesManyTimes()
        {
            var source = CharSource("aaaz");
            var rule = Match('a').Many();
            var next = rule(source).AssertEquals(new[] { 'a', 'a', 'a' });

            var rule2 = Match('z');
            rule2(next).AssertEquals('z').AssertAtEnd();
        }

        [Fact]
        public void NotInvertsTokenRules()
        {
            var source = CharSource("abc");
            var rule = Match('c').Not();

            var next = rule(source).AssertEquals('a');
            next = rule(next).AssertEquals('b');
            rule(next).AssertFails();
        }

        [Fact]
        public void RulesCanBeOptional()
        {
            var source = CharSource("a");
            var rule = Match('z').Optional('-');

            var next = rule(source).AssertEquals('-');

            var rule2 = Match('a');
            rule2(next).AssertEquals('a').AssertAtEnd();
        }

        [Fact]
        public void CanOverrideMessage()
        {
            var source = CharSource("a");
            var rule = Match('z').WithMessage("MSG");

            rule(source).AssertFails("MSG");
        }

        [Fact]
        public void ExpectEndWorksAtEnd()
        {
            var source = CharSource("a");
            var rule = Match('a').ThenEnd();
            rule(source).AssertEquals('a');
        }

        [Fact]
        public void ExpectEndWorksFailsPriorToEnd()
        {
            var source = CharSource("ab");
            var rule = Match('a').ThenEnd();
            rule(source).AssertFails();
        }
    }
}
