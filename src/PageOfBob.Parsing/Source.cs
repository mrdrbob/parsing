using System;
using System.Collections.Generic;

namespace PageOfBob.Parsing
{
    public interface ISource<TToken> { }

    public class Empty<TToken> : ISource<TToken>
    {
        public long Position { get; }
        public Empty(long position)
        {
            Position = position;
        }
    }

    public class Source<TToken> : ISource<TToken>
    {
        public TToken Token { get; }
        public long Position { get; }
        readonly Func<ISource<TToken>> next;
        public Source(TToken token, long position, Func<ISource<TToken>> next)
        {
            Token = token;
            Position = position;
            this.next = next;
        }

        public ISource<TToken> Next() => next.Invoke();
    }

    public static class SourceExtensions
    {
        public static TValue Match<TToken, TValue>(this ISource<TToken> isource, Func<Empty<TToken>, TValue> empty, Func<Source<TToken>, TValue> hasContent)
        {
            switch (isource)
            {
                case Source<TToken> source: return hasContent(source);
                case Empty<TToken> fin: return empty(fin);
                default: throw new NotImplementedException();
            }
        }
    }

    public static class Sources
    {
        public static ISource<char> CharSource(string text, int position = 0)
            => position >= text.Length
                ? (ISource<char>)new Empty<char>(position)
                : new Source<char>(text[position], position, () => CharSource(text, position + 1));

        public static ISource<T> ListSource<T>(IList<T> collection, int position = 0)
            => position >= collection.Count
                ? (ISource<T>)new Empty<T>(position)
                : new Source<T>(collection[position], position, () => ListSource(collection, position + 1));

        public static ISource<T> EnumerableSource<T>(IEnumerable<T> enumerable) => EnumeratorSource(enumerable.GetEnumerator(), 0);

        static ISource<T> EnumeratorSource<T>(IEnumerator<T> enumerator, long position)
        {
            if (!enumerator.MoveNext())
                return new Empty<T>(position);

            T current = enumerator.Current;
            ISource<T> nextCache = null;
            return new Source<T>(current, position, () => nextCache ?? (nextCache = EnumeratorSource(enumerator, position + 1)));
        }



        public static ISource<byte> StreamSource(System.IO.Stream str)
            => _StreamSource(str, 0, 0, null, 0);

        static ISource<byte> _StreamSource(System.IO.Stream str, long streamPosition, int bufferPosition, byte[] buffer, int bufferLength)
        {
            if (streamPosition >= str.Length)
                return new Empty<byte>(streamPosition);

            if (buffer == null || bufferPosition >= bufferLength)
            {
                buffer = new byte[4096];
                str.Seek(streamPosition, System.IO.SeekOrigin.Begin);
                bufferLength = str.Read(buffer, 0, buffer.Length);
                bufferPosition = 0;
            }

            return new Source<byte>(buffer[bufferPosition], streamPosition, () => _StreamSource(str, streamPosition + 1, bufferPosition + 1, buffer, bufferLength));
        }
    }
}
