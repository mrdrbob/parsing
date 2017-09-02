using System;

namespace PageOfBob.Parsing
{
    public interface IResult<TToken, TValue> { }

    public class Success<TToken, TValue> : IResult<TToken, TValue>
    {
        public TValue Value { get; }
        public ISource<TToken> Next { get; }

        public Success(TValue value, ISource<TToken> next)
        {
            Value = value;
            Next = next;
        }
    }

    public class Failure<TToken, TValue> : IResult<TToken, TValue>
    {
        public string Message { get; }

        public Failure(string message)
        {
            Message = message;
        }

        public IResult<TToken, O> Convert<O>() => new Failure<TToken, O>(Message);
    }

    public static class Result
    {
        public static IResult<T, K> Success<T, K>(K value, ISource<T> next) => new Success<T, K>(value, next);
        public static IResult<T, T> Fail<T>(string message) => new Failure<T, T>(message);
        public static IResult<T, K> Fail<T, K>(string message) => new Failure<T, K>(message);
    }

    public static class ResultExtensions
    {
        public static T Match<TToken, TValue, T>(
            this IResult<TToken, TValue> result,
            Func<Failure<TToken, TValue>, T> failure,
            Func<Success<TToken, TValue>, T> success)
        {
            switch (result)
            {
                case Success<TToken, TValue> s: return success.Invoke(s);
                case Failure<TToken, TValue> f: return failure.Invoke(f);
                default: throw new NotImplementedException();
            }
        }
    }
}
