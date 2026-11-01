using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Account/RegisterPharmacy — the pharmacy + manager application form. The created
/// manager account is inactive, so a successful submit redirects to the pending-approval
/// acknowledgement page rather than signing the user in.
/// </summary>
public class RegisterPharmacyPage : PageObjectBase
{
    public RegisterPharmacyPage(IWebDriver driver) : base(driver) { }

    private static By PharmacyName => By.Name("PharmacyName");
    private static By PharmacyDomain => By.Name("PharmacyDomain");
    private static By LicenseNumber => By.Name("LicenseNumber");
    private static By TaxID => By.Name("TaxID");
    private static By Address => By.Name("Address");
    private static By City => By.Name("City");
    private static By FullName => By.Name("FullName");
    private static By ManagerEmail => By.Name("ManagerEmail");
    private static By Phone => By.Name("Phone");
    private static By Password => By.Name("Password");
    private static By ConfirmPassword => By.Name("ConfirmPassword");
    private static By Submit => By.CssSelector("#pharmacyRegisterForm button[type='submit']");
    private static By ValidationSummary => By.CssSelector("div.validation-summary-errors");
    private static By ManagerEmailFieldError => By.CssSelector("span#ManagerEmail-error");

    public RegisterPharmacyPage Open()
    {
        Go("/Account/RegisterPharmacy");
        WaitFor(Submit);
        return this;
    }

    public RegisterPharmacyPage Fill(string pharmacyName, string domain, string license,
        string taxId, string address, string city, string managerName,
        string managerEmail, string password, string? phone = null)
    {
        Type(PharmacyName, pharmacyName);
        Type(PharmacyDomain, domain);
        Type(LicenseNumber, license);
        Type(TaxID, taxId);
        Type(Address, address);
        Type(City, city);
        Type(FullName, managerName);
        Type(ManagerEmail, managerEmail);
        if (phone != null && IsDisplayed(Phone)) Type(Phone, phone);
        Type(Password, password);
        Type(ConfirmPassword, password);
        return this;
    }

    public bool SubmitAndWait()
    {
        Click(Submit);
        try
        {
            Wait(TimeSpan.FromSeconds(20)).Until(d =>
                !d.Url.Contains("/Account/RegisterPharmacy", StringComparison.OrdinalIgnoreCase)
                && !d.Url.Contains("/Pharmacy/Register", StringComparison.OrdinalIgnoreCase)
                || ((IJavaScriptExecutor)d).ExecuteScript(
                    "return !!document.querySelector('div.validation-summary-errors');") is true);
            return !Driver.Url.Contains("RegisterPharmacy", StringComparison.OrdinalIgnoreCase)
                   && !Driver.Url.Contains("Pharmacy/Register", StringComparison.OrdinalIgnoreCase);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public bool OnPendingApprovalPage => WaitForUrlContains("/Account/ManagerPendingApproval", TimeSpan.FromSeconds(20));
    public bool HasValidationError => WaitForPresence(ValidationSummary, TimeSpan.FromSeconds(5));
    public bool HasManagerEmailError => IsDisplayed(ManagerEmailFieldError);
    public string ManagerEmailErrorText => IsDisplayed(ManagerEmailFieldError)
        ? WaitFor(ManagerEmailFieldError).Text : string.Empty;
}
