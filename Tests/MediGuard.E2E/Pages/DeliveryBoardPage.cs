using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Delivery/Index — the manager dispatch board. Lists every still-open delivery for the
/// branch. A parcel with no rider yet renders an "Unassigned" cell plus a per-row rider
/// <select> and an Assign button; clicking Assign posts to /Delivery/Assign over AJAX
/// (antiforgery token supplied by the layout global) and reloads the page once it succeeds.
/// </summary>
public class DeliveryBoardPage : PageObjectBase
{
    public DeliveryBoardPage(IWebDriver driver) : base(driver) { }

    private static By Page => By.CssSelector(".delivery-page");
    private static By Rows => By.CssSelector(".delivery-page tbody tr");
    private static By EmptyState => By.CssSelector(".delivery-card.text-center");

    /// <summary>Opens the board and waits until it has clearly rendered (rows or empty state).</summary>
    public DeliveryBoardPage Open()
    {
        Go("/Delivery/Index");
        WaitFor(Page);
        // Either a populated table or the "No open deliveries" card proves the board settled.
        Wait().Until(d => d.FindElements(Rows).Count > 0 || d.FindElements(EmptyState).Count > 0);
        return this;
    }

    public bool HasRows => SafeCount(Driver, Rows) > 0;
    public int RowCount => SafeCount(Driver, Rows);
    public bool ShowsEmptyState => IsDisplayed(EmptyState);

    /// <summary>True once the board lists a delivery row for the given order number.</summary>
    public bool ListsOrder(string orderNumber)
        => SafeCount(Driver, RowByOrder(orderNumber)) > 0;

    public bool WaitForOrder(string orderNumber, TimeSpan? timeout = null)
        => WaitForPresence(RowByOrder(orderNumber), timeout ?? TimeSpan.FromSeconds(25));

    private By RowByOrder(string orderNumber)
        // The Order cell renders "#@OrderNumber" in a bold div; match the whole row on it.
        => By.XPath($"//table//tbody/tr[contains(.,'{orderNumber}')]");

    /// <summary>
    /// Picks <paramref name="riderName"/> in the row's rider dropdown and clicks Assign.
    /// The client posts to /Delivery/Assign over AJAX and then reloads the whole page,
    /// which wipes the transient success banner - so the reliable confirmation is the
    /// reloaded board showing this row as ASSIGNED to that rider.
    /// </summary>
    public bool AssignRider(string orderNumber, string riderName)
    {
        var row = WaitFor(RowByOrder(orderNumber), TimeSpan.FromSeconds(25));

        var select = row.FindElement(By.CssSelector("select.d-rider"));
        new SelectElement(select).SelectByText(riderName);

        var assign = row.FindElement(By.CssSelector("button.d-assign-btn"));
        // Sticky chrome can overlap the row button (or leave it outside the viewport on
        // long boards), so dispatch the click through script instead of a native one.
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'}); arguments[0].click();", assign);

        return IsAssignedTo(orderNumber, riderName);
    }

    /// <summary>
    /// Confirms the row now reports the rider and an Assigned status badge, tolerating the
    /// full-page reload the client performs a moment after a successful assignment.
    /// </summary>
    public bool IsAssignedTo(string orderNumber, string riderName, TimeSpan? timeout = null)
    {
        var limit = timeout ?? TimeSpan.FromSeconds(35);
        try
        {
            Wait(limit).Until(d =>
            {
                try
                {
                    var matches = d.FindElements(RowByOrder(orderNumber));
                    if (matches.Count == 0) return false;
                    var text = matches[0].Text;
                    return text.Contains(riderName, StringComparison.OrdinalIgnoreCase)
                           && text.Contains("ASSIGNED", StringComparison.OrdinalIgnoreCase);
                }
                catch (StaleElementReferenceException)
                {
                    return false; // mid-reload; the next poll reads the fresh table
                }
                catch (WebDriverException)
                {
                    return false; // document swapped during the client-side reload
                }
            });
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
}
