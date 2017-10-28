using Xunit;
using static PageOfBob.Parsing.Sources;

namespace PageOfBob.Parsing.Tests
{
    public class SourceTests
    {
        [Fact]
        public void ListSourceWorks()
        {
            var source = ListSource(new[] { 1, 2, 3 });

            var a = source.Assume();
            Assert.NotNull(a);
            Assert.Equal(1, a.Token);

            var b = a.Next().Assume();
            Assert.NotNull(b);
            Assert.Equal(2, b.Token);

            var c = b.Next().Assume();
            Assert.NotNull(c);
            Assert.Equal(3, c.Token);

            c.Next().AssertAtEnd();
        }

        [Fact]
        public void EnumerableSourceWorks()
        {
            var source = EnumerableSource(new[] { 1, 2, 3 });

            var a = source.Assume();
            Assert.NotNull(a);
            Assert.Equal(1, a.Token);

            var b = a.Next().Assume();
            Assert.NotNull(b);
            Assert.Equal(2, b.Token);

            var c = b.Next().Assume();
            Assert.NotNull(c);
            Assert.Equal(3, c.Token);

            c.Next().AssertAtEnd();
        }

        [Fact]
        public void EnumerableSourceCanBacktrack()
        {
            var source = EnumerableSource(new[] { 1, 2, 3 });

            var a = source.Assume();
            Assert.NotNull(a);
            Assert.Equal(1, a.Token);

            var b = a.Next().Assume();
            Assert.NotNull(b);
            Assert.Equal(2, b.Token);

            var c = b.Next().Assume();
            Assert.NotNull(c);
            Assert.Equal(3, c.Token);

            c.Next().AssertAtEnd();

            var b2 = a.Next().Assume();
            Assert.NotNull(b2);
            Assert.Equal(2, b2.Token);

            var c2 = b2.Next().Assume();
            Assert.NotNull(c2);
            Assert.Equal(3, c2.Token);
        }
    }
}
