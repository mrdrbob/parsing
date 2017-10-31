using System;
using System.Collections.Generic;

namespace PageOfBob.Parsing
{
    public delegate IResult<TToken, TOutput> Rule<TToken, TOutput>(ISource<TToken> source);

    public static class Rules
    {
        public static Rule<T, T> Match<T>(Func<T, bool> match, string message = null) 
            => (source) => source.Match(
                empty => new Failure<T, T>("EOF"),
                content => match(content.Token)
                        ? Result.Success(content.Token, content.Next())
                        : Result.Fail<T>(message)
            );

        public static Rule<T, K> GetPosition<T, K>(Func<long, K> map)
            => (source) => source.Match(
                empty => Result.Success(map(empty.Position), source),
                content => Result.Success(map(content.Position), source)
            );

        public static Rule<T, long> GetPosition<T>() => GetPosition<T, long>(x => x);

        public static Rule<T, T> Any<T>() => Match<T>(x => true);

        public static Rule<T, T> Match<T>(T value, string message = null) => Match<T>(x => value.Equals(x), message ?? $"Exepected {value}");

        public static Rule<T, K[]> Sequence<T, K>(IEnumerable<Rule<T, K>> rules, string message = null)
            => source =>
            {
                var list = new List<K>();

                foreach(var rule in rules)
                {
                    var result = rule(source).Match(
                        fail => fail,
                        success =>
                        {
                            source = success.Next;
                            list.Add(success.Value);
                            return (IResult<T, K>)success;
                        });

                    if (result is Failure<T, K>)
                    {
                        return Result.Fail<T, K[]>(message ?? "Did not match all");
                    }
                }

                return Result.Success(list.ToArray(), source);
            };

        public static Rule<T, O> Map<T, I, O>(this Rule<T, I> rule, Func<I, O> map) 
            => source => rule(source).Match(
                fail => fail.Convert<O>(),
                success => Result.Success(map(success.Value), success.Next)
            );

        public static Rule<T, K> Any<T, K>(params Rule<T, K>[] rules)
            => source =>
            {
                IResult<T, K> result = Result.Fail<T, K>("no matching rule");
                for (int x = 0; x < rules.Length && result is Failure<T, K>; x++)
                {
                    result = rules[x](source).Match(
                        fail => result,
                        success => success);
                }

                return result;
            };

        public static Rule<T, O> Then<T, L, R, O>(this Rule<T, L> left, Rule<T, R> right, Func<L, R, O> map)
            => source =>
                left(source).Match(
                    failLeft => failLeft.Convert<O>(),
                    successLeft => right(successLeft.Next).Match(
                        failRight => failRight.Convert<O>(),
                        successRight => Result.Success(map(successLeft.Value, successRight.Value), successRight.Next)
                    )
                );

        public static Rule<T, L> ThenIgnore<T, L, R>(this Rule<T, L> left, Rule<T, R> right) => Then(left, right, (l, r) => l);

        public static Rule<T, R> ThenKeep<T, L, R>(this Rule<T, L> left, Rule<T, R> right) => Then(left, right, (l, r) => r);

        public static Rule<T, K> ThenSet<T, R, K>(this Rule<T, K> left, Rule<T, R> right, Action<K, R> action) => Then(left, right, (l, r) => { action(l, r); return l; });
        public static Rule<T, K> Always<T, K>(Func<K> action) => source => Result.Success(action(), source);

        public static Rule<T, K[]> Many<T, K>(this Rule<T, K> rule, bool required = false, string message = null)
            => source =>
            {
                var list = new List<K>();

                IResult<T, K> result = null;
                do
                {
                    result = rule(source).Match(
                        fail => (IResult<T, K>)fail,
                        success =>
                        {
                            list.Add(success.Value);
                            source = success.Next;
                            return success;
                        });
                } while (result is Success<T, K>);

                if (required && list.Count == 0)
                    return Result.Fail<T, K[]>(message ?? "Required");

                return Result.Success(list.ToArray(), source);
            };

        public static Rule<T, T> Not<T, K>(this Rule<T, K> rule, string message = null) 
            => (source) => source.Match(
                empty => Result.Fail<T>("EOF"),
                nonempty => rule(nonempty).Match(
                    fail => Result.Success(nonempty.Token, nonempty.Next()),
                    success => Result.Fail<T>(message ?? "Rule matched")
                )
            );


        public static Rule<T, K> Optional<T, K>(this Rule<T, K> rule, K defaultValue)
            => source => rule(source).Match(
                fail => Result.Success(defaultValue, source),
                success => success
            );

        public static Rule<T, K> WithMessage<T, K>(this Rule<T, K> rule, string message)
            => source => rule.Invoke(source).Match(
                fail => Result.Fail<T, K>(message),
                success => success
            );

        public static Rule<T, K> ThenEnd<T, K>(this Rule<T, K> rule)
            => source => rule.Invoke(source).Match(
                fail => fail,
                success => success.Next.Match(
                    empty => success,
                    hasContent => Result.Fail<T, K>("Expected EOF")
                )
            );
    }
}
