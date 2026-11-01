using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Order/Details/{id} — the customer's order-tracking page. Renders the status badge,
/// a horizontal progress timeline (.mg-progress-step blocks) and a cancellation notice
/// when the order was cancelled.
/// </summary>
public class OrderDetailsPage : PageObjectBase
{
    public OrderDetailsPage(IWebDriver driver) : base(driver) { }

    private static By PageRoot => By.CssSelector(".track-page");
    private static By Heading => By.CssSelector(".track-page h1");
    private static By StatusBadge => By.CssSelector(".track-page .badge");
    private static By StepElements => By.CssSelector(".mg-progress-step strong");

    public OrderDetailsPage WaitForReady()
    {
        WaitFor(PageRoot, TimeSpan.FromSeconds(20));
        return this;
    }

    public string HeadingText => WaitFor(Heading).Text;

    public string StatusBadgeText =>
        IsDisplayed(StatusBadge) ? Driver.FindElements(StatusBadge)[0].Text.Trim() : string.Empty;

    public bool ShowsCancelledNotice =>
        Driver.PageSource.Contains("This order was cancelled", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> StepTitles
    {
        get
        {
            try
            {
                return Driver.FindElements(StepElements).Select(e => e.Text.Trim()).ToList();
            }
            catch (StaleElementReferenceException)
            {
                return Driver.FindElements(StepElements).Select(e => e.Text.Trim()).ToList();
            }
        }
    }

    public bool HasStep(string title) =>
        StepTitles.Any(t => t.Equals(title, StringComparison.OrdinalIgnoreCase));
}
