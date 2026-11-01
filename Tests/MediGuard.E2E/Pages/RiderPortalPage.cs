using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Deliveryman — the rider portal. The dispatch board, COD ledger and history are all
/// rendered client-side by delivery-tasks.js, which pulls buckets from
/// /Deliveryman/GetMyDeliveries and re-renders after each status change, so every
/// interaction here is an AJAX round-trip rather than a page navigation.
///
/// The active cards carry a single primary action button (data-act = the next status:
/// PICKED_UP, then IN_TRANSIT, then DONE). Confirming DONE fires a native window.confirm
/// in the app; the portal neutralises confirm/alert so the automated click never blocks
/// on a dialog that Selenium would otherwise have to catch mid-request.
/// </summary>
public class RiderPortalPage : PageObjectBase
{
    public RiderPortalPage(IWebDriver driver) : base(driver) { }

    private static By ActiveList => By.Id("rpActiveList");
    private static By ActiveCards => By.CssSelector("#rpActiveList article.rp-order");
    private static By EmptyState => By.CssSelector("#rpActiveList .rp-empty");
    private static By PrimaryAction => By.CssSelector("button.rp-btn.is-primary[data-act]");
    private static By StatActive => By.Id("rpStatActive");
    private static By CodTotal => By.Id("rpCodTotal");
    private static By CodRows => By.CssSelector("#rpCodRows tr");

    public RiderPortalPage Open()
    {
        Go("/Deliveryman");
        WaitFor(ActiveList);

        // Neutralise native dialogs before any action button exists so a DONE
        // click cannot hang on the "Confirm delivery?" confirm dialog.
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "window.confirm = function(){ return true; }; window.alert = function(){};");

        // Wait for the skeleton to be replaced by real cards or the empty state.
        WaitForJavaScript(
            "document.querySelector('#rpActiveList article.rp-order')" +
            " || document.querySelector('#rpActiveList .rp-empty')",
            TimeSpan.FromSeconds(30));
        return this;
    }

    /// <summary>The "Active dispatches" KPI number shown on the rider's counter strip.</summary>
    public string ActiveDispatchCount =>
        IsDisplayed(StatActive) ? WaitFor(StatActive).Text.Trim() : string.Empty;

    public int ActiveCardCount => SafeCount(Driver, ActiveCards);

    public bool ShowsNoActiveDispatches => IsDisplayed(EmptyState);

    public bool HasActiveOrder(string orderNumber) => ActiveCard(orderNumber) != null;

    /// <summary>
    /// Advances the rider's delivery for <paramref name="orderNumber"/> through every
    /// remaining step until it leaves the active board (i.e. is delivered). Returns the
    /// ordered list of actions clicked (e.g. PICKED_UP, IN_TRANSIT, DONE).
    /// </summary>
    public IList<string> AdvanceToDelivered(string orderNumber)
    {
        var clicked = new List<string>();

        for (var step = 0; step < 5; step++)
        {
            var card = ActiveCard(orderNumber);
            if (card == null) break; // the parcel has left the active board

            string action;
            try
            {
                var button = card.FindElement(PrimaryAction);
                action = button.GetAttribute("data-act") ?? string.Empty;
                ((IJavaScriptExecutor)Driver).ExecuteScript(
                    "arguments[0].scrollIntoView({block:'center'});arguments[0].click();", button);
            }
            catch (StaleElementReferenceException)
            {
                continue; // a re-render happened mid-read; retry next poll
            }
            catch (NoSuchElementException)
            {
                break; // no forward action left (delivered / cancelled)
            }

            if (string.IsNullOrEmpty(action)) break;
            clicked.Add(action);

            // Wait until THIS order's card either disappears or offers a different
            // action, which is how we know the status POST + re-render has landed.
            WaitForCardToChange(orderNumber, action);
        }

        return clicked;
    }

    private void WaitForCardToChange(string orderNumber, string previousAction)
    {
        try
        {
            Wait(TimeSpan.FromSeconds(30)).Until(d =>
            {
                var card = FindActiveCard(d, orderNumber);
                if (card == null) return true; // gone from the active board
                try
                {
                    var next = card.FindElement(PrimaryAction).GetAttribute("data-act");
                    return !string.Equals(next, previousAction, StringComparison.OrdinalIgnoreCase);
                }
                catch (NoSuchElementException)
                {
                    return true; // card now has no forward action
                }
                catch (StaleElementReferenceException)
                {
                    return false; // mid re-render; keep polling
                }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // Caller inspects the returned action list; leave state as-is.
        }
    }

    /// <summary>Activates the Delivery History panel and waits for it to render.</summary>
    public RiderPortalPage OpenHistory()
    {
        ClickTab("history");
        return this;
    }

    /// <summary>
    /// True once the history list (currently filtered to Delivered) renders a card for
    /// <paramref name="orderNumber"/>.
    /// </summary>
    public bool OrderAppearsInHistory(string orderNumber, TimeSpan? timeout = null)
    {
        var by = By.XPath($"//div[@id='rpHistoryList']//article[contains(.,'{orderNumber}')]");
        return WaitForPresence(by, timeout ?? TimeSpan.FromSeconds(30));
    }

    /// <summary>Activates the COD &amp; settlement panel.</summary>
    public RiderPortalPage OpenCodSettlement()
    {
        ClickTab("cod");
        WaitForJavaScript(
            "!!document.getElementById('rpCodRows')",
            TimeSpan.FromSeconds(10));
        return this;
    }

    public string CodTotalText => IsDisplayed(CodTotal) ? WaitFor(CodTotal).Text.Trim() : string.Empty;
    public int CodRowCount => SafeCount(Driver, CodRows);

    // ------------------------------------------------------------------ helpers

    private void ClickTab(string target)
    {
        var tab = WaitForPresent(By.XPath($"//*[@data-rp-tab='{target}']"), TimeSpan.FromSeconds(10));
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", tab);
    }

    private IWebElement? ActiveCard(string orderNumber) => FindActiveCard(Driver, orderNumber);

    private static IWebElement? FindActiveCard(IWebDriver d, string orderNumber)
    {
        try
        {
            var by = By.XPath($"//div[@id='rpActiveList']//article[contains(.,'{orderNumber}')]");
            var matches = d.FindElements(by);
            return matches.Count > 0 ? matches[0] : null;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }
}
