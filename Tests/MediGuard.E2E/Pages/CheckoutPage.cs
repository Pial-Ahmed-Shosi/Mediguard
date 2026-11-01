using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Checkout/Payment — the payment &amp; fulfilment step. Cash on delivery is the default
/// method, so an order can be completed without touching an instrument panel. For home
/// delivery the client demands a full address and a contact number before submitting.
/// Wallet/card methods reveal the instrument panel (or a saved-method picker); the
/// sandbox gateway settles them synchronously, so placing the order lands on the paid
/// receipt.
/// </summary>
public class CheckoutPage : PageObjectBase
{
    public CheckoutPage(IWebDriver driver) : base(driver) { }

    private static By Form => By.Id("checkout-form");
    private static By CodMethod => By.CssSelector("input[name='Method'][value='COD']");
    private static By WalletAccount => By.Id("walletAccount");
    private static By CardNumber => By.Id("cardNumber");
    private static By SaveMethodCheck => By.Id("saveMethod");
    private static By MethodLabel => By.Id("methodLabel");
    private static By InstrumentPanel => By.Id("instrumentPanel");
    private static By FulfillmentDelivery => By.CssSelector("input[name='Fulfillment'][value='delivery']");
    private static By FulfillmentPickup => By.CssSelector("input[name='Fulfillment'][value='pickup']");
    private static By Address => By.Id("address");
    private static By Phone => By.Id("phone");
    private static By Instructions => By.Id("instructions");
    private static By PlaceButton => By.Id("placeOrderButton");
    private static By CheckoutError => By.Id("checkoutError");

    public CheckoutPage OpenFor(Guid orderId)
    {
        Go($"/Checkout/Payment?orderId={orderId}");
        WaitFor(Form);
        return this;
    }

    /// <summary>Waits for the payment form to render after arriving from the cart.</summary>
    public CheckoutPage WaitForReady()
    {
        WaitFor(PlaceButton);
        return this;
    }

    public CheckoutPage SelectCashOnDelivery()
    {
        var cod = WaitForPresent(CodMethod);
        if (!cod.Selected)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", cod);
        }
        return this;
    }

    /// <summary>
    /// Picks a payment method radio and re-triggers the page's change handler, clearing
    /// any server-rendered "default" saved-method radio so the fresh instrument panel
    /// (wallet/card number entry) is guaranteed to be shown.
    /// </summary>
    private void ChooseMethod(string value)
    {
        WaitForPresent(By.CssSelector("input[name='Method']"));
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "var m = document.querySelector('input[name=\"Method\"][value=\"' + arguments[0] + '\"]');" +
            "if (!m) { throw new Error('Payment method ' + arguments[0] + ' not found'); }" +
            "m.checked = true;" +
            "document.querySelectorAll('input[name=\"SavedMethodId\"]').forEach(function (r) { r.checked = false; });" +
            "m.dispatchEvent(new Event('change', { bubbles: true }));",
            value);
    }

    /// <summary>Selects a mobile-wallet method (BKASH/NAGAD/ROCKET) and waits for the wallet-number field.</summary>
    public CheckoutPage SelectWallet(string provider)
    {
        ChooseMethod(provider);
        WaitFor(WalletAccount);
        return this;
    }

    /// <summary>Selects the card method and waits for the card-number field.</summary>
    public CheckoutPage SelectCard()
    {
        ChooseMethod("CARD");
        WaitFor(CardNumber);
        return this;
    }

    public CheckoutPage SetWalletNumber(string number)
    {
        Type(WalletAccount, number);
        return this;
    }

    public CheckoutPage SetCardNumber(string number)
    {
        Type(CardNumber, number);
        return this;
    }

    /// <summary>Ticks "remember this method" (optionally with a nickname) so it is stored masked.</summary>
    public CheckoutPage EnableSaveMethod(string? label = null)
    {
        var box = WaitFor(SaveMethodCheck);
        if (!box.Selected)
        {
            // The raw checkbox is covered by its styled label, so toggle it via script.
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'}); arguments[0].click();", box);
        }
        if (label != null && IsDisplayed(MethodLabel)) Type(MethodLabel, label);
        return this;
    }

    private static By SavedRadioFor(string provider) =>
        By.CssSelector($"div[data-saved-for='{provider}'] input[name='SavedMethodId']");

    /// <summary>True once the provider's saved-method block renders a visible stored option.</summary>
    public bool HasSavedMethodFor(string provider) => WaitForJavaScript(
        $"Array.prototype.slice.call(document.querySelectorAll('div[data-saved-for=\"{provider}\"] input[name=\"SavedMethodId\"]'))" +
        ".some(function (i) {{ return i.offsetParent !== null; }})",
        TimeSpan.FromSeconds(10));

    /// <summary>
    /// Chooses the first stored option for the provider; the client then hides the manual
    /// instrument panel and pays with the saved (masked) method.
    /// </summary>
    public CheckoutPage UseSavedMethod(string provider)
    {
        if (!HasSavedMethodFor(provider))
        {
            throw new InvalidOperationException($"No saved {provider} method was offered at checkout.");
        }
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            $"document.querySelector('div[data-saved-for=\"{provider}\"] input[name=\"SavedMethodId\"]').click();");
        if (!WaitForJavaScript("document.getElementById('instrumentPanel').hidden === true", TimeSpan.FromSeconds(10)))
        {
            throw new InvalidOperationException("Choosing the saved method did not collapse the instrument panel.");
        }
        return this;
    }

    public CheckoutPage SelectStorePickup()
    {
        var pickup = WaitForPresent(FulfillmentPickup);
        if (!pickup.Selected)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", pickup);
        }
        return this;
    }

    /// <summary>
    /// Chooses home delivery (the fulfilment radio is hidden behind a styled label),
    /// which makes checkout create a PENDING delivery row the manager can dispatch.
    /// </summary>
    public CheckoutPage SelectHomeDelivery()
    {
        var delivery = WaitForPresent(FulfillmentDelivery);
        if (!delivery.Selected)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", delivery);
        }
        return this;
    }

    public CheckoutPage SetDeliveryAddress(string address)
    {
        Type(Address, address);
        return this;
    }

    public CheckoutPage SetPhone(string phone)
    {
        Type(Phone, phone);
        return this;
    }

    public CheckoutPage SetInstructions(string text)
    {
        if (IsDisplayed(Instructions)) Type(Instructions, text);
        return this;
    }

    public ConfirmationPage PlaceOrder()
    {
        var button = WaitForClickable(PlaceButton);
        button.Click();
        if (!WaitForUrlContains("/Checkout/Confirmation", TimeSpan.FromSeconds(30)))
        {
            throw new InvalidOperationException(
                "Place order did not navigate to confirmation. Client error: " +
                (IsDisplayed(CheckoutError) ? WaitFor(CheckoutError).Text : "(none)"));
        }
        return new ConfirmationPage(Driver);
    }

    public bool HasClientError => IsDisplayed(CheckoutError);
}
