using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Storefront — the customer catalogue. Search runs server-side through the query
/// string; add-to-cart fires an AJAX POST, so we confirm the request completed by
/// waiting for the toast the client renders in response.
/// </summary>
public class StorefrontPage : PageObjectBase
{
    public StorefrontPage(IWebDriver driver) : base(driver) { }

    private static By SearchBox => By.Id("search");
    private static By SearchButton => By.Id("btnFilter");
    private static By ProductGrid => By.Id("productGrid");
    private static By ProductCards => By.CssSelector("#productGrid .mg-card-exact");
    private static By AddButtons => By.CssSelector("button.mg-add-to-cart:not([disabled])");
    private static By EmptyState => By.Id("emptyState");
    private static By CartToast => By.Id("mg-cart-toast");

    public StorefrontPage Open()
    {
        Go("/Storefront");
        WaitFor(ProductGrid);
        return this;
    }

    public StorefrontPage Search(string term)
    {
        Type(SearchBox, term);
        Click(SearchButton);
        WaitFor(ProductGrid);
        return this;
    }

    public int VisibleProductCount => Driver.FindElements(ProductCards).Count;
    public bool ShowsEmptyState => IsDisplayed(EmptyState);

    /// <summary>
    /// Clicks the first enabled add-to-cart button and waits for the AJAX toast, so the
    /// caller can assume the line is on the server before navigating to the cart.
    /// </summary>
    public StorefrontPage AddFirstProductToCart()
    {
        var button = WaitForClickable(AddButtons);
        button.Click();
        // The client renders #mg-cart-toast (hidden=false) once the POST resolves.
        Wait(TimeSpan.FromSeconds(20)).Until(d =>
        {
            var toast = d.FindElements(CartToast);
            return toast.Count > 0 && toast[0].Displayed;
        });
        return this;
    }

    /// <summary>
    /// Adds a product whose card title contains <paramref name="brandContains"/>, so a
    /// test can deterministically pick a specific catalogue item (e.g. a non-Rx one).
    /// </summary>
    public StorefrontPage AddProductToCart(string brandContains)
    {
        var xpath =
            $"//article[contains(@class,'mg-card-exact')]//h6[contains(@title,'{brandContains}')]" +
            "/ancestor::article//button[contains(@class,'mg-add-to-cart') and not(@disabled)]";
        var button = WaitForClickable(By.XPath(xpath), TimeSpan.FromSeconds(20));
        button.Click();
        Wait(TimeSpan.FromSeconds(20)).Until(d =>
        {
            var toast = d.FindElements(CartToast);
            return toast.Count > 0 && toast[0].Displayed;
        });
        return this;
    }
}
