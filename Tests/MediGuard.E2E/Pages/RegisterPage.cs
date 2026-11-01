using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Account/Register — self-service signup for Customer and Delivery roles.
/// The two roles are radio cards (values "Regular User" and "Deliveryman").
/// </summary>
public class RegisterPage : PageObjectBase
{
    public RegisterPage(IWebDriver driver) : base(driver) { }

    public const string RoleCustomer = "Regular User";
    public const string RoleDelivery = "Deliveryman";

    private static By RoleRadio(string value) => By.CssSelector($"input[name='Role'][value='{value}']");
    private static By FullName => By.Name("FullName");
    private static By Email => By.Name("Email");
    private static By Password => By.Name("Password");
    private static By ConfirmPassword => By.Name("ConfirmPassword");
    private static By PhoneNumber => By.Name("PhoneNumber");
    private static By DeliveryAddress => By.Name("DeliveryAddress");
    private static By Submit => By.CssSelector("button.btn-submit[type='submit']");
    private static By ValidationSummary => By.CssSelector("div.validation-summary-errors");
    private static By FieldErrors => By.CssSelector("span.field-validation-error");

    public RegisterPage Open()
    {
        Go("/Account/Register");
        WaitFor(Submit);
        return this;
    }

    /// <summary>
    /// The role radios are visually hidden cards, so we select via JavaScript to avoid
    /// the "element not interactable" error a direct click on an off-screen input raises.
    /// </summary>
    public RegisterPage SelectRole(string roleValue)
    {
        // The role radios are styled off-screen inputs, so locate by presence (not
        // visibility) and drive them with a JS click.
        var el = WaitForPresent(RoleRadio(roleValue));
        if (!el.Selected)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", el);
        }
        return this;
    }

    public RegisterPage Fill(string fullName, string email, string password,
        string? phone = null, string? address = null)
    {
        Type(FullName, fullName);
        Type(Email, email);
        Type(Password, password);
        Type(ConfirmPassword, password);
        if (phone != null && IsDisplayed(PhoneNumber)) Type(PhoneNumber, phone);
        if (address != null && IsDisplayed(DeliveryAddress)) Type(DeliveryAddress, address);
        return this;
    }

    public bool SubmitAndWait()
    {
        Click(Submit);
        try
        {
            Wait().Until(d => !d.Url.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase)
                              || ((IJavaScriptExecutor)d).ExecuteScript(
                                  "return !!document.querySelector('div.validation-summary-errors');")
                              is true);
            return !Driver.Url.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    // Some failures (e.g. the [Compare] password check) are emitted as per-field spans
    // rather than in the ModelOnly summary, so treat either shape as "the form was rejected".
    public bool HasValidationError =>
        WaitForPresence(ValidationSummary, TimeSpan.FromSeconds(5))
        || WaitForPresence(FieldErrors, TimeSpan.FromSeconds(5));

    public string ValidationText =>
        IsDisplayed(ValidationSummary)
            ? WaitFor(ValidationSummary).Text
            : (IsDisplayed(FieldErrors) ? Driver.FindElements(FieldErrors)[0].Text : string.Empty);
}
