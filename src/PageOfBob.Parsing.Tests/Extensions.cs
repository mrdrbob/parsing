using System;
using Xunit;

namespace PageOfBob.Parsing.Tests
{
    public static class Extensions
    {
        public static Source<TToken> Assume<TToken>(this ISource<TToken> source)
            => source.Match(
                () => null,
                t => t
            );

        public static Success<TToken, TValue> Assume<TToken, TValue>(this IResult<TToken, TValue> result)
            => result.Match(
                f => null,
                s => s
            );

        public static void AssertAtEnd<TToken>(this ISource<TToken> source)
        {
            bool isEof = source.Match(() => true, s => false);
            Assert.True(isEof);
        }

        public static void AssertNotAtEnd<TToken>(this ISource<TToken> source)
        {
            bool isEof = source.Match(() => true, s => false);
            Assert.True(!isEof);
        }

        public static ISource<TToken> AssertSuccess<TToken, TValue>(this IResult<TToken, TValue> results, Action<TValue> assert)
        {
            var val = results.Assume();
            Assert.NotNull(val);
            assert(val.Value);
            return val.Next;
        }

        public static ISource<TToken> AssertEquals<TToken, TValue>(this IResult<TToken, TValue> results, TValue value)
            => AssertSuccess(results, v => Assert.Equal(value, v));

        public static void AssertFails<TToken, TValue>(this IResult<TToken, TValue> results, string withMessage = null)
        {
            bool success = results.Match(
                msg =>
                {
                    if (withMessage != null)
                    {
                        Assert.Equal(withMessage, msg.Message);
                    }
                    return false;
                },
                s => true);

            Assert.False(success);
        }
    }
}
