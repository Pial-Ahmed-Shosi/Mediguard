using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MediGuard.E2E.Pages;

/// <summary>
/// Shared behaviour for every page object: navigation, explicit waits and safe
/// element lookups. All timing is explicit (no reliance on the small implicit wait)
/// so tests fail fast and predictably on real problems instead of hanging.
/// </summary>
public abstract class PageObjectBase
{
    protected readonly IWebDriver Driver;
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    protected PageObjectBase(IWebDriver driver) => Driver = driver;

    protected WebDriverWait Wait(TimeSpan? timeout = null)
    {
        var span = timeout ?? DefaultTimeout;
        return new WebDriverWait(Driver, span);
    }

    /// <summary>Navigates to an app-relative path (e.g. "/Storefront").</summary>
    protected void Go(string relativePath)
    {
        var url = relativePath.StartsWith("http")
            ? relativePath
            : $"{TestConfig.BaseUrl}/{relativePath.TrimStart('/')}";
        Driver.Navigate().GoToUrl(url);
    }

    protected IWebElement WaitFor(By by, TimeSpan? timeout = null)
        => Wait(timeout).Until(d => First(d, by, requireVisible: true))
           ?? throw new NoSuchElementException($"Element not visible: {by}");

    protected IWebElement WaitForClickable(By by, TimeSpan? timeout = null)
        => Wait(timeout).Until(d => First(d, by, requireClickable: true))
           ?? throw new NoSuchElementException($"Element not clickable: {by}");

    /// <summary>
    /// Locates an element even when it is visually hidden (e.g. the role radio cards,
    /// which are real inputs kept off-screen behind styled labels).
    /// </summary>
    protected IWebElement WaitForPresent(By by, TimeSpan? timeout = null)
        => Wait(timeout).Until(d => First(d, by))
           ?? throw new NoSuchElementException($"Element not present: {by}");

    /// <summary>
    /// Re-resolves the finder on every poll and swallows the transient exceptions a
    /// re-rendering page throws between locating an element and reading its state
    /// (Stale / not-interactable), so waits retry instead of failing mid-navigation.
    /// </summary>
    private static IWebElement? First(IWebDriver d, By by, bool requireVisible = false, bool requireClickable = false)
    {
        try
        {
            var matches = d.FindElements(by);
            if (matches.Count == 0) return null;
            var el = matches[0];
            if (requireVisible && !el.Displayed) return null;
            if (requireClickable && !(el.Displayed && el.Enabled)) return null;
            return el;
        }
        catch (StaleElementReferenceException) { return null; }
        catch (ElementNotInteractableException) { return null; }
    }

    protected bool WaitForPresence(By by, TimeSpan? timeout = null)
    {
        try
        {
            Wait(timeout).Until(d => SafeCount(d, by) > 0);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Blocks until no element matches <paramref name="by"/> - used to detect an item
    /// leaving a list that a background AJAX re-render just replaced.
    /// </summary>
    protected bool WaitForAbsent(By by, TimeSpan? timeout = null)
    {
        try
        {
            Wait(timeout).Until(d => SafeCount(d, by) == 0);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for an <see cref="IJavaScriptExecutor"/> predicate expressed as a JS
    /// function returning a boolean. Used for DOM states Selenium's locators cannot
    /// express directly (e.g. a data-act attribute changing after a re-render).
    /// </summary>
    protected bool WaitForJavaScript(string predicate, TimeSpan? timeout = null)
    {
        try
        {
            Wait(timeout).Until(d =>
            {
                var result = ((IJavaScriptExecutor)d).ExecuteScript(
                    "return Boolean(" + predicate + ");");
                return result is bool b ? b : result != null;
            });
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    protected static int SafeCount(IWebDriver d, By by)
    {
        try { return d.FindElements(by).Count; }
        catch (StaleElementReferenceException) { return 0; }
    }

    protected bool WaitForUrlContains(string fragment, TimeSpan? timeout = null)
    {
        try
        {
            Wait(timeout).Until(d => d.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    protected void Click(By by)
    {
        var el = WaitForClickable(by);
        try
        {
            el.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // Sticky headers / overlays can cover a target; scroll it into the middle first.
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});", el);
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", el);
        }
    }

    protected void Type(By by, string text)
    {
        var el = WaitFor(by);
        el.Clear();
        el.SendKeys(text);
    }

    protected void SetCheckbox(By by, bool value)
    {
        var el = WaitFor(by);
        if (el.Selected != value)
        {
            el.Click();
        }
    }

    protected bool IsDisplayed(By by)
    {
        try
        {
            var found = Driver.FindElements(by);
            return found.Count > 0 && found[0].Displayed;
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }
}
