using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Account/Login — the single sign-in surface for every role. Also exposes a
/// logout helper used to reset browser sessions between tests.
/// </summary>
public class LoginPage : PageObjectBase
{
    public LoginPage(IWebDriver driver) : base(driver) { }

    private static By Email => By.Name("Email");
    private static By Password => By.Name("Password");
    private static By RememberMe => By.Id("rememberMe");
    private static By Submit => By.CssSelector("button.btn-login[type='submit']");
    private static By ValidationSummary => By.CssSelector("div.validation-summary-errors");
    private static By FieldErrors => By.CssSelector("span.field-validation-error");

    public LoginPage Open()
    {
        Go("/Account/Login");
        if (!TryWaitSubmit(TimeSpan.FromSeconds(8)))
        {
            // A competing navigation (an in-flight logout redirect from a previous
            // role switch) can interrupt this first load; reload once and wait again.
            Go("/Account/Login");
            WaitFor(Submit);
        }
        return this;
    }

    private bool TryWaitSubmit(TimeSpan timeout)
    {
        try
        {
            WaitFor(Submit, timeout);
            return true;
        }
        catch (NoSuchElementException) { return false; }
        catch (WebDriverTimeoutException) { return false; }
    }

    /// <summary>Fills and submits the form. Does not assert where the app lands.</summary>
    public LoginPage SubmitCredentials(string email, string password, bool? remember = null)
    {
        Type(Email, email);
        Type(Password, password);
        if (remember.HasValue)
        {
            SetCheckbox(RememberMe, remember.Value);
        }
        Click(Submit);
        return this;
    }

    /// <summary>
    /// Signs in and waits for the app to leave the login page. The controller redirects
    /// each role to its own landing surface, so we only assert that we left /Account/Login.
    /// </summary>
    public bool Login(string email, string password)
    {
        Open();
        SubmitCredentials(email, password);
        return WaitForLeaveLogin();
    }

    private bool WaitForLeaveLogin()
    {
        try
        {
            Wait().Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase)
                              || IsDisplayed(ValidationSummary));
            return !Driver.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public bool HasValidationError =>
        WaitForPresence(ValidationSummary, TimeSpan.FromSeconds(5)) || IsDisplayed(FieldErrors);

    public string ValidationText =>
        IsDisplayed(ValidationSummary)
            ? WaitFor(ValidationSummary).Text
            : (IsDisplayed(FieldErrors) ? Driver.FindElements(FieldErrors)[0].Text : string.Empty);

    public void Logout()
    {
        // The sidebar/header renders a logout form posting to /Account/Logout.
        var forms = Driver.FindElements(By.CssSelector("form[action='/Account/Logout']"));
        if (forms.Count > 0)
        {
            try
            {
                forms[0].FindElement(By.CssSelector("button, input[type='submit']")).Click();
                // Let the logout redirect finish so it cannot race the next navigation.
                Wait(TimeSpan.FromSeconds(10)).Until(d =>
                    d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase)
                    || SafeCount(d, Submit) > 0);
            }
            catch (WebDriverException) { /* navigation settled the page; handled below */ }
        }

        // Guarantee a signed-out state even when no logout control was on the page
        // (auth is cookie-based, so dropping the cookie is a complete logout).
        try
        {
            if (!CurrentUrlHasLoginForm())
            {
                Driver.Manage().Cookies.DeleteAllCookies();
            }
        }
        catch (WebDriverException)
        {
            Driver.Manage().Cookies.DeleteAllCookies();
        }
    }

    private bool CurrentUrlHasLoginForm()
    {
        try { return Driver.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase)
                       && SafeCount(Driver, Submit) > 0; }
        catch (WebDriverException) { return false; }
    }
}
