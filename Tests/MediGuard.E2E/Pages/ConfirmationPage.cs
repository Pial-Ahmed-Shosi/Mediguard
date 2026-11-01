using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Checkout/Confirmation — the terminal step of the ordering funnel. Renders the
/// order number, a status banner and the payment method that was chosen.
/// </summary>
public class ConfirmationPage : PageObjectBase
{
    public ConfirmationPage(IWebDriver driver) : base(driver) { }

    private static By Banner => By.CssSelector(".confirmation-banner");
    private static By OrderNumber => By.CssSelector(".order-id-highlight");
    private static By SuccessPill => By.CssSelector(".pill-badge.pill-success");
    private static By WarningPill => By.CssSelector(".pill-badge.pill-warning");
    private static By GatewayRef => By.CssSelector(".tx-code");
    private static By PaymentMethodValue =>
        By.XPath("//span[contains(@class,'meta-label')][contains(text(),'Payment method')]/following-sibling::span[1]");

    public ConfirmationPage WaitForReady()
    {
        WaitFor(OrderNumber);
        return this;
    }

    /// <summary>The raw order number the server stamped on the receipt (without the leading #).</summary>
    public string OrderNumberText
    {
        get
        {
            var raw = WaitFor(OrderNumber).Text.Trim();
            return raw.TrimStart('#').Trim();
        }
    }

    public string StatusBannerText => IsDisplayed(Banner) ? WaitFor(Banner).Text : string.Empty;

    public bool IsAccepted => StatusBannerText.Contains("received", StringComparison.OrdinalIgnoreCase)
                              || StatusBannerText.Contains("placed", StringComparison.OrdinalIgnoreCase)
                              || StatusBannerText.Contains("success", StringComparison.OrdinalIgnoreCase)
                              || StatusBannerText.Contains("thank", StringComparison.OrdinalIgnoreCase);

    public bool ShowsOrderReference(string orderNumber) =>
        OrderNumberText.Equals(orderNumber, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the receipt shows the green "Paid" pill (a completed online payment).</summary>
    public bool IsPaid => IsDisplayed(SuccessPill) && WaitFor(SuccessPill).Text.Trim().Equals("Paid", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the amber "Pay on delivery" pill is shown (cash on delivery).</summary>
    public bool IsPayOnDelivery =>
        IsDisplayed(WarningPill) && WaitFor(WarningPill).Text.Contains("delivery", StringComparison.OrdinalIgnoreCase);

    /// <summary>The gateway reference printed on the receipt; "N/A" when there is none.</summary>
    public string GatewayReferenceText => IsDisplayed(GatewayRef) ? WaitFor(GatewayRef).Text.Trim() : string.Empty;

    /// <summary>The human-readable payment method label (bKash / Nagad / Rocket / Card / Cash on delivery).</summary>
    public string PaymentMethodText =>
        IsDisplayed(PaymentMethodValue) ? WaitFor(PaymentMethodValue).Text.Trim() : string.Empty;

    /// <summary>Parses the orderId query-stamp from the confirmation URL so a test can re-open the order.</summary>
    public Guid OrderIdFromUrl
    {
        get
        {
            var marker = "orderId=";
            var idx = Driver.Url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return Guid.Empty;
            var value = Driver.Url.Substring(idx + marker.Length);
            var amp = value.IndexOf('&');
            if (amp >= 0) value = value.Substring(0, amp);
            return Guid.TryParse(value, out var g) ? g : Guid.Empty;
        }
    }
}
