using MediGuard.E2E;
using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// Self-service registration for the two public roles (Customer and Delivery) and the
/// client-side guardrails on the form. Each test registers a throwaway, uniquely-named
/// account so runs stay independent of leftover demo data.
/// </summary>
public class AccountRegistrationTests : BrowserTestBase
{
    private const string Password = "Test1234";

    [Fact]
    public void Register_Customer_RedirectsToStorefront()
    {
        var email = UniqueEmail("customer");

        var landed = Register
            .Open()
            .SelectRole(Pages.RegisterPage.RoleCustomer)
            .Fill("E2E Customer", email, Password, phone: "01700000001")
            .SubmitAndWait();

        Assert.True(landed, "Registration should submit and leave the register page.");
        Assert.True(CurrentUrlContains("/Storefront"),
            $"A new customer should land on the storefront, at: {Driver.Url}");
    }

    [Fact]
    public void Register_Delivery_RedirectsToRiderPortal()
    {
        var email = UniqueEmail("rider");

        var landed = Register
            .Open()
            .SelectRole(Pages.RegisterPage.RoleDelivery)
            .Fill("E2E Rider", email, Password)
            .SubmitAndWait();

        Assert.True(landed, "Registration should submit and leave the register page.");
        Assert.True(CurrentUrlContains("/Deliveryman"),
            $"A new delivery user should land on the rider portal, at: {Driver.Url}");
    }

    [Fact]
    public void Register_MismatchedConfirmationPassword_ShowsValidationError()
    {
        var email = UniqueEmail("mismatch");

        // Bypass the client "confirm password" check by posting straight to the form with
        // the two password fields differing; server-side [Compare] must reject it.
        Register.Open();
        Register.SelectRole(Pages.RegisterPage.RoleCustomer);
        var script =
            "var f=document.getElementById('authForm');" +
            "f.querySelector('input[name=FullName]').value='E2E Mismatch';" +
            $"f.querySelector('input[name=Email]').value='{email}';" +
            "f.querySelector('input[name=Password]').value='Test1234';" +
            "f.querySelector('input[name=ConfirmPassword]').value='Different1234';" +
            "f.submit();";
        ((OpenQA.Selenium.IJavaScriptExecutor)Driver).ExecuteScript(script);

        Assert.True(Register.HasValidationError,
            "Server should reject a password/confirm-password mismatch.");
    }

    [Fact]
    public void Register_SameEmailTwice_SecondAttemptShowsError()
    {
        var email = UniqueEmail("dupe");

        Register.Open().SelectRole(Pages.RegisterPage.RoleCustomer)
            .Fill("E2E Dupe", email, Password).SubmitAndWait();

        // Sign back in is automatic on success, so start clean and try the same email again.
        Driver.Manage().Cookies.DeleteAllCookies();
        var landedSecond = Register.Open().SelectRole(Pages.RegisterPage.RoleCustomer)
            .Fill("E2E Dupe", email, Password).SubmitAndWait();

        Assert.True(!landedSecond || Register.HasValidationError,
            "Registering an already-taken email must not silently succeed.");
    }
}
