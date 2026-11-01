using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Prescription/PendingQueue — the pharmacy's prescription review terminal (requires the
/// prescription.verify permission, held by managers and pharmacists). Rows are tagged with
/// the order number in .rxq-order and each carries a Review link into the decision page.
/// Tabs filter by status: ?status=PENDING|APPROVED|REJECTED|ALL.
/// </summary>
public class PrescriptionQueuePage : PageObjectBase
{
    public PrescriptionQueuePage(IWebDriver driver) : base(driver) { }

    private static By Table => By.CssSelector(".rxq-table");
    private static By Empty => By.CssSelector(".rxq-empty");

    public PrescriptionQueuePage Open(string status = "PENDING")
    {
        Go($"/Prescription/PendingQueue?status={status}");
        Wait(TimeSpan.FromSeconds(20)).Until(d =>
            d.Url.Contains("/Prescription/PendingQueue", StringComparison.OrdinalIgnoreCase)
            && (SafeCount(d, Table) > 0 || SafeCount(d, Empty) > 0));
        return this;
    }

    private static By RowBy(string orderNumber) =>
        By.XPath($"//tr[.//div[contains(@class,'rxq-order')][normalize-space()='#{orderNumber}']]");

    public bool ListsOrder(string orderNumber) => SafeCount(Driver, RowBy(orderNumber)) > 0;

    public bool WaitForOrder(string orderNumber, TimeSpan? timeout = null) =>
        WaitForPresence(RowBy(orderNumber), timeout);

    public int RowCount => Driver.FindElements(By.CssSelector(".rxq-table tbody tr")).Count;

    /// <summary>Opens the Review page for the row carrying that order number.</summary>
    public PrescriptionReviewPage ReviewOrder(string orderNumber)
    {
        var row = Wait().Until(d =>
        {
            var rows = d.FindElements(RowBy(orderNumber));
            return rows.Count > 0 ? rows[0] : null;
        }) ?? throw new NoSuchElementException($"No queue row for order #{orderNumber}.");

        var link = row.FindElement(By.CssSelector("a[href^='/Prescription/Review/']"));
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", link);
        link.Click();
        if (!WaitForUrlContains("/Prescription/Review/", TimeSpan.FromSeconds(20)))
        {
            throw new InvalidOperationException($"Review link did not open the review page (at {Driver.Url}).");
        }
        return new PrescriptionReviewPage(Driver).WaitForReady();
    }
}
