using Xunit;

namespace VcdiffLibrary
{
    public class XunitTests
    {
        [Fact]
        public void TrivialTest()
        {
            Assert.Equal(4, 2 + 2);
        }
    }
}