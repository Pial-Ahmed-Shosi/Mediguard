using OpenQA.Selenium;
using MediGuard.E2E.Pages;

namespace MediGuard.E2E;

/// <summary>
/// Base for every browser-driven test. Owns one WebDriver per test instance (xUnit
/// constructs a fresh class instance per [Fact]) and guarantees the browser is closed
/// even when an assertion throws. Also centralises the two things nearly every flow
/// needs: a signed-in session and a unique email for registrations.
/// </summary>
public abstract class BrowserTestBase : IDisposable
{
    protected IWebDriver Driver { get; }

    protected string BaseUrl => TestConfig.BaseUrl;

    protected BrowserTestBase()
    {
        Driver = BrowserFactory.Create();
    }

    /// <summary>
    /// A unique email per run so registration tests never collide with rows left behind
    /// by an earlier run (Identity enforces unique emails).
    /// </summary>
    protected static string UniqueEmail(string prefix) =>
        $"{prefix}.{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(1000, 9999)}@e2e.mediguard.test";

    protected LoginPage Login => new(Driver);
    protected RegisterPage Register => new(Driver);
    protected RegisterPharmacyPage RegisterPharmacy => new(Driver);
    protected StorefrontPage Storefront => new(Driver);
    protected CartPage Cart => new(Driver);
    protected CheckoutPage Checkout => new(Driver);
    protected ConfirmationPage Confirmation => new(Driver);
    protected DashboardPage Dashboard => new(Driver);
    protected PrescriptionPage Prescription => new(Driver);
    protected DeliveryBoardPage Delivery => new(Driver);
    protected RiderPortalPage Rider => new(Driver);
    protected OrderPage Order => new(Driver);
    protected PrescriptionUploadPage PrescriptionUpload => new(Driver);
    protected PrescriptionQueuePage PrescriptionQueue => new(Driver);
    protected PrescriptionReviewPage PrescriptionReview => new(Driver);
    protected AdminPharmaciesPage AdminPharmacies => new(Driver);

    /// <summary>Signs in as a seeded demo account and waits until the login page clears.</summary>
    protected void SignIn(string email, string password)
    {
        if (!Login.Login(email, password))
        {
            throw new InvalidOperationException(
                $"Could not sign in as {email}. Stuck on: {Driver.Url}");
        }
    }

    /// <summary>
    /// Registers a throwaway account (Customer or Delivery) and leaves the browser
    /// signed in on the role's landing surface.
    /// </summary>
    protected string RegisterAndSignIn(string roleValue, string fullName, string password)
    {
        var email = UniqueEmail(fullName.Split(' ')[0].ToLowerInvariant());
        Register.Open().SelectRole(roleValue).Fill(fullName, email, password).SubmitAndWait();
        return email;
    }

    protected void GoTo(string relativePath)
    {
        var url = relativePath.StartsWith("http") ? relativePath : $"{BaseUrl}/{relativePath.TrimStart('/')}";
        Driver.Navigate().GoToUrl(url);
    }

    protected bool CurrentUrlContains(string fragment) =>
        Driver.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Saves a PNG of the current browser state under TestResults/screenshots so a
    /// failed flow can be inspected without re-running it attached to a debugger.
    /// </summary>
    protected void CaptureScreenshot(string tag)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "TestResults", "screenshots");
            Directory.CreateDirectory(dir);
            var safe = string.Concat(tag.Split(Path.GetInvalidFileNameChars()));
            var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safe}.png");
            var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
            File.WriteAllBytes(file, screenshot.AsByteArray);
        }
        catch
        {
            // Never let diagnostics masking turn a real failure into a confusing one.
        }
    }

    public void Dispose()
    {
        Driver.Dispose();
        GC.SuppressFinalize(this);
    }
}
