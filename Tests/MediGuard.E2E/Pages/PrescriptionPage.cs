using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Prescription/MyPrescriptions — the customer's prescription tracker. Renders the
/// "no prescriptions yet" empty state or the list of submitted copies with their
/// review status, plus a "Waiting for your prescription copy" panel holding one upload
/// link per order that still needs a prescription before preparation.
/// </summary>
public class PrescriptionPage : PageObjectBase
{
    public PrescriptionPage(IWebDriver driver) : base(driver) { }

    private static By Heading => By.CssSelector("h1");
    private static By EmptyState => By.CssSelector("h2");

    public PrescriptionPage Open()
    {
        Go("/Prescription/MyPrescriptions");
        Wait(TimeSpan.FromSeconds(20)).Until(d =>
            d.Url.Contains("/Prescription/MyPrescriptions", StringComparison.OrdinalIgnoreCase));
        WaitFor(Heading);
        return this;
    }

    public bool IsLoaded =>
        Driver.Url.Contains("/Prescription/MyPrescriptions", StringComparison.OrdinalIgnoreCase)
        && Driver.PageSource.Contains("prescription", StringComparison.OrdinalIgnoreCase);

    public bool ShowsEmptyState =>
        Driver.PageSource.Contains("No prescriptions submitted yet", StringComparison.OrdinalIgnoreCase);

    public string HeadingText => WaitFor(Heading).Text;

    private static By CardBy(string orderNumber) =>
        By.XPath($"//div[contains(@class,'rx-card')][.//div[normalize-space()='#{orderNumber}']]");

    /// <summary>True when a submitted prescription card exists for that order.</summary>
    public bool ShowsOrder(string orderNumber) => SafeCount(Driver, CardBy(orderNumber)) > 0;

    public bool WaitForOrder(string orderNumber, TimeSpan? timeout = null) =>
        WaitForPresence(CardBy(orderNumber), timeout);

    /// <summary>The pharmacist-decision badge text ("Under review" / "Approved" / "Rejected").</summary>
    public string StatusForOrder(string orderNumber)
    {
        var card = Wait().Until(d =>
        {
            var cards = d.FindElements(CardBy(orderNumber));
            return cards.Count > 0 ? cards[0] : null;
        }) ?? throw new NoSuchElementException($"No prescription card for order #{orderNumber}.");
        return card.FindElement(By.CssSelector(".badge")).Text.Trim();
    }

    private static By UploadLinkFor(string orderNumber) =>
        By.XPath($"//a[contains(@href,'/Prescription/Upload')][contains(normalize-space(),'#{orderNumber}')]");

    /// <summary>True while the order still waits in the "Waiting for your prescription copy" panel.</summary>
    public bool ShowsUploadPrompt(string orderNumber) => SafeCount(Driver, UploadLinkFor(orderNumber)) > 0;

    /// <summary>Follows the awaiting-upload link into the customer upload form.</summary>
    public PrescriptionUploadPage OpenUploadFor(string orderNumber)
    {
        var link = WaitForClickable(UploadLinkFor(orderNumber), TimeSpan.FromSeconds(20));
        link.Click();
        if (!WaitForUrlContains("/Prescription/Upload", TimeSpan.FromSeconds(20)))
        {
            throw new InvalidOperationException($"Upload link for #{orderNumber} did not open (at {Driver.Url}).");
        }
        return new PrescriptionUploadPage(Driver).WaitForReady();
    }
}
