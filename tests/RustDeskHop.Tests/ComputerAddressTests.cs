using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class ComputerAddressTests
    {
        #region Test Methods
        [Theory]
        [InlineData("123 456 789", "123456789")]
        [InlineData("  Workstation_01  ", "Workstation_01")]
        [InlineData("123\t456\n789", "123456789")]
        [InlineData("192.168.1.5", "192.168.1.5")]
        public void DisplayFormattingIsNormalizedConsistentlyForSavingAndConnecting(string input, string expected)
        {
            Assert.Equal(expected, ComputerAddress.Normalize(input));
            Assert.Null(ComputerAddress.Validate(input));
            ServerProfile profile = new ServerProfile { ServerAddress = "public" };
            Assert.Equal(expected,
                         ConnectionTargetBuilder.Build(new TargetDefinition { RustDeskId = input },
                                                       profile,
                                                       RustDeskDefaultRoute.PUBLIC
                                                      )
                        );
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123@server")]
        [InlineData("123?key=value")]
        [InlineData("123&key=value")]
        [InlineData("rustdesk://123")]
        [InlineData("--connect")]
        [InlineData("computer name")]
        public void InvalidIdsAreRejectedByBothInputValidationAndTheLauncher(string id)
        {
            Assert.NotNull(ComputerAddress.Validate(id));
            Assert.Throws<InvalidOperationException>(() =>
                                                         ConnectionTargetBuilder.Build(new TargetDefinition
                                                                      { RustDeskId = id },
                                                                  new ServerProfile { ServerAddress = "public" },
                                                                  RustDeskDefaultRoute.PUBLIC
                                                             )
                                                    );
        }
        #endregion
    }
}