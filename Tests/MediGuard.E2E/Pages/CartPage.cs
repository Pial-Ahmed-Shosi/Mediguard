using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Cart — the review-and-checkout page. Line items carry a data-line-id, quantities
/// are edited over AJAX, and "Proceed to checkout" posts the whole cart to
/// /Checkout/PlaceOrder, which reserves stock and forwards to the payment step.
/// </summary>
public class CartPage : PageObjectBase
{
    public CartPage(IWebDriver driver) : base(driver) { }

    private static By Lines => By.CssSelector("[data-line-id]");
    private static By Subtotal => By.Id("cartSubtotal");
    private static By CheckoutForm => By.CssSelector("form[action='/Checkout/PlaceOrder']");
    private static By CheckoutButton => By.CssSelector("form[action='/Checkout/PlaceOrder'] button[type='submit']");
    private static By EmptyClearButton => By.CssSelector("[data-cart-action='clear']");
    private static By EmptyCta => By.CssSelector("a[href='/Storefront']");

    public CartPage Open()
    {
        Go("/Cart");
        // Either the lines/checkout area or the empty-cart CTA is always present.
        Wait().Until(d => d.FindElements(Lines).Count > 0 || d.FindElements(EmptyCta).Count > 0);
        return this;
    }

    public bool IsEmpty => Driver.FindElements(Lines).Count == 0;
    public int LineCount => Driver.FindElements(Lines).Count;
    public string SubtotalText => IsDisplayed(Subtotal) ? WaitFor(Subtotal).Text : string.Empty;

    /// <summary>
    /// Waits until the rendered page contains that notice fragment (TempData banners
    /// survive exactly one request, so callers must not re-navigate to see them).
    /// </summary>
    public bool WaitForNoticeContains(string fragment) =>
        Wait(TimeSpan.FromSeconds(20)).Until(d =>
            d.PageSource.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Empties the cart if it holds anything, accepting the browser confirm() the
    /// client raises. Keeps ordering tests independent of leftover state.
    /// </summary>
    public void ClearIfNotEmpty()
    {
        Open();
        if (IsEmpty) return;

        var clear = Driver.FindElement(EmptyClearButton);
        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", clear);
        clear.Click();

        try
        {
            Driver.SwitchTo().Alert().Accept();
        }
        catch (NoAlertPresentException)
        {
            // client used its own dialog; nothing to accept
        }

        Wait(TimeSpan.FromSeconds(20)).Until(d => d.FindElements(Lines).Count == 0);
    }

    /// <summary>Proceeds to the payment step and returns once the URL has changed.</summary>
    public CheckoutPage ProceedToCheckout()
    {
        // Ensure we are on the cart page regardless of where the caller navigated from.
        if (!Driver.Url.Contains("/Cart", StringComparison.OrdinalIgnoreCase))
        {
            Open();
        }

        var button = WaitForClickable(CheckoutButton, TimeSpan.FromSeconds(20));
        button.Click();
        WaitForUrlContains("/Checkout/Payment", TimeSpan.FromSeconds(25));
        return new CheckoutPage(Driver);
    }
}
