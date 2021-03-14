using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixsper.DmxDotNet.Tests
{
    [TestClass]
    public class DmxUniverseTests
    {
        [TestMethod]
        public void CanHtpMerge()
        {
            var universe1 = new DmxUniverse(1, new byte[] { 255, 0, 255, 0, 255, 0});
            var universe2 = new DmxUniverse(1, new byte[] { 0, 255, 0, 255, 50, 255, 255, 255 });

            var result1 = universe1.MergeHtp(universe2);
            var result2 = universe2.MergeHtp(universe1);

            result1.Should().Be(result2, "because merging in either direction should produce the same result");

            result1.ChannelCount.Should()
                .Be(universe2.ChannelCount,
                    "because the result channel count should be equal to the maximum channel count of the two sources");

            result2.ChannelCount.Should()
                .Be(universe2.ChannelCount,
                    "because the result channel count should be equal to the maximum channel count of the two sources");

            result1.Data.Should()
                .OnlyContain(b => b == 255,
                    "because the two source universes should produce only values of 255 when HTP merged");

            result2.Data.Should()
               .OnlyContain(b => b == 255,
                   "because the two source universes should produce only values of 255 when HTP merged");
        }
    }
}
