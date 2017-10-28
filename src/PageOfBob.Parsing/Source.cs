using System;
using System.Collections.Generic;

namespace PageOfBob.Parsing
{
    public interface ISource<TToken> { }

    public class Empty<TToken> : ISource<TToken>
    {
        Empty() { }
        public static readonly Empty<TToken> Instance = new Empty<TToken>();
    }

    public class Source<TToken> : ISource<TToken>
    {
        public TToken Token { get; }
        readonly Func<ISource<TToken>> next;
        public Source(TToken token, Func<ISource<TToken>> next)
        {
            Token = token;
            this.next = next;
        }

        public ISource<TToken> Next() => next.Invoke();
    }

    public static class SourceExtensions
    {
        public static TValue Match<TToken, TValue>(this ISource<TToken> isource, Func<TValue> empty, Func<Source<TToken>, TValue> hasContent)
        {
            switch (isource)
            {
                case Source<TToken> source: return hasContent(source);
                case Empty<TToken> fin: return empty();
                default: throw new NotImplementedException();
            }
        }
    }

    public static class Sources
    {
        public static ISource<char> CharSource(string text, int position = 0)
            => position >= text.Length
                ? (ISource<char>)Empty<char>.Instance
                : new Source<char>(text[position], () => CharSource(text, position + 1));

        public static ISource<T> ListSource<T>(IList<T> collection, int position = 0)
            => position >= collection.Count
                ? (ISource<T>)Empty<T>.Instance
                : new Source<T>(collection[position], () => ListSource(collection, position + 1));

        public static ISource<T> EnumerableSource<T>(IEnumerable<T> enumerable) => EnumeratorSource(enumerable.GetEnumerator());

        static ISource<T> EnumeratorSource<T>(IEnumerator<T> enumerator)
        {
            if (!enumerator.MoveNext())
                return Empty<T>.Instance;

            T current = enumerator.Current;
            ISource<T> nextCache = null;
            return new Source<T>(current, () => nextCache ?? (nextCache = EnumeratorSource(enumerator)));
        }



        public static ISource<byte> StreamSource(System.IO.Stream str)
            => _StreamSource(str, 0, 0, null, 0);

        static ISource<byte> _StreamSource(System.IO.Stream str, long streamPosition, int bufferPosition, byte[] buffer, int bufferLength)
        {
            if (streamPosition >= str.Length)
                return Empty<byte>.Instance;

            if (buffer == null || bufferPosition >= bufferLength)
            {
                buffer = new byte[4096];
                str.Seek(streamPosition, System.IO.SeekOrigin.Begin);
                bufferLength = str.Read(buffer, 0, buffer.Length);
                bufferPosition = 0;
            }

            return new Source<byte>(buffer[bufferPosition], () => _StreamSource(str, streamPosition + 1, bufferPosition + 1, buffer, bufferLength));
        }
    }
}
