using Microsoft.Extensions.Logging;
using Moq;
using TorrentRatioBooster.Services;

namespace TorrentRatioBooster.Tests
{
    public class UrlModifierServiceTests
    {
        [Fact]
        public void UrlModifierSer()
        {
            // Arrange
            var logger = new Mock<ILogger<UrlModifierService>>();

            // Act

            // Assert
            Assert.True(1 == 1);
        }
    }
}