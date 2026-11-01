using MediGuard.E2E.Pages;
using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// Sign-in behaviour across the credential and account-state guard rails: correct
/// credentials reach the right landing surface, bad credentials are refused, and a
/// not-yet-approved account is blocked even with the correct password.
/// </summary>
public class LoginTests : BrowserTestBase
{
    [Fact]
    public void Login_CustomerWithValidCredentials_LandsOnStorefront()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        Assert.True(CurrentUrlContains("/Storefront"),
            $"A signed-in customer should be on the storefront, at: {Driver.Url}");
    }

    [Fact]
    public void Login_WrongPassword_IsRejected()
    {
        Login.Open().SubmitCredentials(TestConfig.DemoCustomerEmail, "WrongPass1234");

        Assert.True(Login.HasValidationError, "Invalid credentials must produce an error.");
        Assert.True(CurrentUrlContains("/Account/Login"),
            "Failed sign-in should keep the user on the login page.");
    }

    [Fact]
    public void Login_ManagerLandsOnDashboard()
    {
        SignIn("manager@citypharmacy.app", TestConfig.DemoPassword);

        Assert.True(Dashboard.IsOnDashboard,
            $"A pharmacy manager should land on the operational dashboard, at: {Driver.Url}");
    }

    [Fact]
    public void Login_AdminLandsOnDashboard()
    {
        SignIn("admin@mediguard.app", TestConfig.DemoPassword);

        Assert.True(Dashboard.IsOnDashboard,
            $"An administrator should land on the dashboard, at: {Driver.Url}");
    }

    [Fact]
    public void RegisterPharmacy_ThenLogin_IsBlockedUntilApproved()
    {
        // A fresh pharmacy application creates an INACTIVE manager, so signing in with the
        // exact credentials just chosen must be refused by the account-state guard.
        var domain = $"e2{DateTime.UtcNow:yyyyMMddHHmmss}.test";
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var managerEmail = $"manager@{domain}";
        const string password = "Pharm1234";

        var submitted = RegisterPharmacy
            .Open()
            .Fill("E2E Test Pharmacy", domain, $"E2E-LIC-{stamp}", $"E2E-TIN-{stamp}",
                "House 1, Road 1", "Dhaka", "E2E Manager", managerEmail, password,
                phone: "01700000009")
            .SubmitAndWait();

        Assert.True(submitted, "Pharmacy registration should redirect to the acknowledgement page.");
        Assert.True(RegisterPharmacy.OnPendingApprovalPage,
            $"Expected the pending-approval page, at: {Driver.Url}");

        // Now attempt to sign in with those credentials.
        var signedIn = Login.Login(managerEmail, password);

        Assert.True(!signedIn, "A pending manager account must not be able to sign in.");
        Assert.True(Login.HasValidationError,
            "Blocked sign-in should explain that the account is pending approval.");
    }
}
