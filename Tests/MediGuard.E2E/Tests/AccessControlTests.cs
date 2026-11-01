using OpenQA.Selenium;
using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// Verifies the authentication boundary: the public storefront is reachable without a
/// session, while every customer/staff surface behind [Authorize] sends an anonymous
/// visitor to the login page instead of leaking content.
/// </summary>
public class AccessControlTests : BrowserTestBase
{
    // These tests deliberately run WITHOUT signing in, so they do not call SignIn().

    [Fact]
    public void Storefront_IsPubliclyAccessible()
    {
        GoTo("/Storefront");

        Assert.True(CurrentUrlContains("/Storefront"),
            $"The catalogue should load for an anonymous visitor, at: {Driver.Url}");
        Assert.True(Driver.FindElements(By.Id("productGrid")).Count > 0,
            "The storefront product grid should render without a session.");
    }

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Cart")]
    [InlineData("/Prescription/MyPrescriptions")]
    public void ProtectedPage_RedirectsAnonymousToLogin(string path)
    {
        GoTo(path);

        Assert.True(CurrentUrlContains("/Account/Login"),
            $"{path} should bounce an anonymous visitor to the login page, landed on: {Driver.Url}");
    }
}
