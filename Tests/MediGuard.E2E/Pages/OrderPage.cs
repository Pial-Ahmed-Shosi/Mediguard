using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Order — the customer's order list. Orders render as .order-card blocks tagged with
/// their stamped number (#MG-...), and the tabs are plain links (?filter=active|
/// completed|cancelled|all). Every row action lives inside its own card: Track (link),
/// Invoice (link), Re-order (POST form) and Cancel (POST form guarded by confirm()).
/// </summary>
public class OrderPage : PageObjectBase
{
    public OrderPage(IWebDriver driver) : base(driver) { }

    private static By PageRoot => By.CssSelector(".order-page");
    private static By Notice => By.CssSelector(".alert-success");
    private static By Warning => By.CssSelector(".alert-warning");

    public OrderPage Open(string filter = "active")
    {
        Go($"/Order?filter={filter}");
        WaitFor(PageRoot, TimeSpan.FromSeconds(20));
        return this;
    }

    private static By CardBy(string orderNumber) =>
        By.XPath($"//div[contains(@class,'order-card')][.//div[normalize-space()='#{orderNumber}']]");

    /// <summary>True when a card for that exact order number is rendered in the current tab.</summary>
    public bool HasOrder(string orderNumber) => SafeCount(Driver, CardBy(orderNumber)) > 0;

    public bool WaitForOrder(string orderNumber, TimeSpan? timeout = null) =>
        WaitForPresence(CardBy(orderNumber), timeout);

    public bool WaitForOrderGone(string orderNumber, TimeSpan? timeout = null) =>
        WaitForAbsent(CardBy(orderNumber), timeout);

    private IWebElement Card(string orderNumber)
        => Wait().Until(d =>
        {
            var cards = d.FindElements(CardBy(orderNumber));
            return cards.Count > 0 ? cards[0] : null;
        }) ?? throw new NoSuchElementException($"No order card for #{orderNumber} on {Driver.Url}");

    /// <summary>Follows the card's "Track order" link into the progress page.</summary>
    public OrderDetailsPage TrackOrder(string orderNumber)
    {
        var link = Card(orderNumber).FindElement(By.CssSelector("a[href^='/Order/Details/']"));
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", link);
        link.Click();
        if (!WaitForUrlContains("/Order/Details/", TimeSpan.FromSeconds(20)))
        {
            throw new InvalidOperationException($"Track order did not open the details page (at {Driver.Url}).");
        }
        return new OrderDetailsPage(Driver).WaitForReady();
    }

    /// <summary>Opens the printable invoice document for that order in the same tab.</summary>
    public bool OpenInvoice(string orderNumber)
    {
        var link = Card(orderNumber).FindElement(By.CssSelector("a[href^='/Order/Invoice/']"));
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", link);
        link.Click();
        return WaitForUrlContains("/Order/Invoice/", TimeSpan.FromSeconds(20));
    }

    /// <summary>Renders the invoice document and shows the stamped order number on it.</summary>
    public bool InvoiceShowsOrder(string orderNumber) =>
        Driver.PageSource.Contains("Tax invoice", StringComparison.OrdinalIgnoreCase)
        && Driver.PageSource.Contains(orderNumber, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Submits the card's Re-order form; the server re-adds the items and redirects to
    /// the cart with a TempData notice.
    /// </summary>
    public CartPage ReOrder(string orderNumber)
    {
        var button = Card(orderNumber).FindElement(
            By.CssSelector("form[action^='/Order/ReOrder/'] button[type='submit']"));
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", button);
        button.Click();
        if (!WaitForUrlContains("/Cart", TimeSpan.FromSeconds(25)))
        {
            throw new InvalidOperationException($"Re-order did not land on the cart (at {Driver.Url}).");
        }
        return new CartPage(Driver);
    }

    /// <summary>
    /// Cancels a still-cancellable order. The form is guarded by window.confirm() so the
    /// dialog is auto-accepted via script before the submit button is pressed; the server
    /// redirects back to the Cancelled tab with a success notice.
    /// </summary>
    public OrderPage CancelOrder(string orderNumber)
    {
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.confirm = function () { return true; };");
        var button = Card(orderNumber).FindElement(
            By.CssSelector("form[action^='/Order/Cancel/'] button[type='submit']"));
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", button);
        button.Click();
        if (!WaitForUrlContains("filter=cancelled", TimeSpan.FromSeconds(30)))
        {
            throw new InvalidOperationException($"Cancel did not redirect to the cancelled tab (at {Driver.Url}).");
        }
        WaitFor(PageRoot, TimeSpan.FromSeconds(20));
        return this;
    }

    public string NoticeText => IsDisplayed(Notice) ? WaitFor(Notice, TimeSpan.FromSeconds(10)).Text : string.Empty;
    public string WarningText => IsDisplayed(Warning) ? Driver.FindElements(Warning)[0].Text : string.Empty;
}
