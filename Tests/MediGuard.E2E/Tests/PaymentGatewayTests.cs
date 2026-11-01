using MediGuard.E2E.Pages;
using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// Online payments against the built-in sandbox gateway (no Stripe key configured, so
/// wallet/card charges settle synchronously). Covers a bKash wallet payment, a card
/// payment, and saving a method on one order then reusing that stored method on the
/// next. Every order uses store pickup so the instrument fields — not an address — are
/// the only thing checkout demands.
/// </summary>
public class PaymentGatewayTests : BrowserTestBase
{
    private const string KnownMedicine = "Napa";

    [Fact]
    public void WalletPayment_bKash_SettlesPaidWithGatewayReference()
    {
        var walletNumber = "017" + Random.Shared.Next(10000000, 99999999); // 11-digit BD mobile

        var confirmation = BuyWithCheckout(checkout => checkout
            .SelectWallet("BKASH")
            .SetWalletNumber(walletNumber));

        Assert.True(confirmation.IsPaid,
            $"A sandbox wallet charge should render the green Paid pill, banner: '{confirmation.StatusBannerText}'.");
        Assert.Contains("bKash", confirmation.PaymentMethodText, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(confirmation.GatewayReferenceText)
                     || confirmation.GatewayReferenceText.Equals("N/A", StringComparison.OrdinalIgnoreCase),
            "A settled payment should print a gateway transaction reference on the receipt.");
    }

    [Fact]
    public void CardPayment_SettlesPaidWithGatewayReference()
    {
        var confirmation = BuyWithCheckout(checkout => checkout
            .SelectCard()
            .SetCardNumber("4242424242424242"));

        Assert.True(confirmation.IsPaid,
            $"A sandbox card charge should render the green Paid pill, banner: '{confirmation.StatusBannerText}'.");
        Assert.Contains("Card", confirmation.PaymentMethodText, StringComparison.OrdinalIgnoreCase);
        Assert.False(confirmation.GatewayReferenceText.Equals("N/A", StringComparison.OrdinalIgnoreCase),
            "A settled card payment should carry a transaction reference, not N/A.");
    }

    [Fact]
    public void SaveWalletMethod_ThenReuseItOnNextOrder()
    {
        var walletNumber = "017" + Random.Shared.Next(10000000, 99999999);

        // Order 1: pay with bKash and tick "remember this method".
        var first = BuyWithCheckout(checkout => checkout
            .SelectWallet("BKASH")
            .SetWalletNumber(walletNumber)
            .EnableSaveMethod($"E2E bKash {walletNumber}"));
        Assert.True(first.IsPaid, "The first saved-method order should still settle as Paid.");

        // Order 2: the stored method must be offered and pay without re-entering digits.
        var second = BuyWithCheckout(checkout =>
        {
            checkout.SelectWallet("BKASH");
            Assert.True(checkout.HasSavedMethodFor("BKASH"),
                "Checkout should offer the bKash method saved on the previous order.");
            checkout.UseSavedMethod("BKASH");
        });

        Assert.True(second.IsPaid,
            $"Paying with the saved method should settle as Paid, banner: '{second.StatusBannerText}'.");
        Assert.Contains("bKash", second.PaymentMethodText, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Places a one-item pickup order as the demo customer, letting the caller drive the
    /// payment step (method choice + instrument fields) before the order is placed.
    /// Returns the settled receipt page.
    /// </summary>
    private ConfirmationPage BuyWithCheckout(Action<CheckoutPage> payStep)
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        Cart.ClearIfNotEmpty();
        Storefront.Open().Search(KnownMedicine).AddFirstProductToCart();

        var checkout = Cart.ProceedToCheckout();
        checkout.WaitForReady().SelectStorePickup();

        payStep(checkout);

        return checkout.PlaceOrder().WaitForReady();
    }
}
