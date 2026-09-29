using Simultria.RustDeskCompanion;
using Xunit;

namespace RustDeskHop.Tests;

public sealed class SetupIdentityTests
{
    [Theory]
    [InlineData("S-1-5-21-123-1001", "S-1-5-21-123-1001", true)]
    [InlineData("S-1-5-21-123-1001", "S-1-5-21-123-1002", false)]
    [InlineData(null, "S-1-5-21-123-1001", false)]
    [InlineData("S-1-5-21-123-1001", null, false)]
    [InlineData("", "", false)]
    [InlineData(" ", " ", false)]
    public void PublicSetupRejectsDifferentOrMissingWindowsIdentity(string? caller, string? helper, bool allowed) =>
        Assert.Equal(allowed, RustDeskPublicProfileSetup.IsSameWindowsUser(caller, helper));
}
