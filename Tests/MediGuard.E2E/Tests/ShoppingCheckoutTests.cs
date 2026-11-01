using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// The end-to-end buying funnel for a signed-in customer: search the catalogue, add a
/// medicine to the cart, review the cart, check out with cash on delivery, and land on
/// the order-confirmation receipt. This is the app's most important path, so it is
/// exercised as one continuous journey rather than mocked fragments.
/// </summary>
public class ShoppingCheckoutTests : BrowserTestBase
{
    // A stocked, prescription-free catalogue item so checkout never needs a prescription.
    private const string KnownMedicine = "Napa";

    [Fact]
    public void Search_KnownMedicine_ReturnsMatchingProducts()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        Storefront.Open().Search(KnownMedicine);

        Assert.True(Storefront.VisibleProductCount > 0,
            $"Searching for '{KnownMedicine}' should list at least one product.");
    }

    [Fact]
    public void Search_UnknownTerm_ShowsEmptyState()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        Storefront.Open().Search("zzz-not-a-medicine-zzz");

        Assert.True(Storefront.ShowsEmptyState,
            "A nonsense search should render the 'no matching products' empty state.");
    }

    [Fact]
    public void AddToCart_ThenCheckout_ConfirmationFlow_Completes()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        // Start from a clean cart so the assertions below are deterministic.
        Cart.ClearIfNotEmpty();

        // Search, add the first matching product, and confirm it landed in the cart.
        Storefront.Open().Search(KnownMedicine).AddFirstProductToCart();

        Cart.Open();
        Assert.False(Cart.IsEmpty, "The added medicine should appear in the cart.");

        // Proceed through checkout choosing cash on delivery (the default method).
        var checkout = Cart.ProceedToCheckout();
        checkout.WaitForReady()
                .SelectCashOnDelivery()
                .SetDeliveryAddress("House 21, Road 8, Dhanmondi, Dhaka")
                .SetPhone("01712345678");

        var confirmation = checkout.PlaceOrder().WaitForReady();

        // The receipt must show a real order number and an accepted status banner.
        Assert.False(string.IsNullOrWhiteSpace(confirmation.OrderNumberText),
            "Confirmation should render a stamped order number.");
        Assert.True(confirmation.IsAccepted,
            $"Confirmation banner should signal an accepted order, was: '{confirmation.StatusBannerText}'");
    }

    [Fact]
    public void Checkout_CashOnDelivery_OptionIsSelectable()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);
        Cart.ClearIfNotEmpty();

        Storefront.Open().Search(KnownMedicine).AddFirstProductToCart();
        var checkout = Cart.ProceedToCheckout();
        checkout.WaitForReady().SelectCashOnDelivery();

        // COD keeps the instrument panel hidden; placing the order still succeeds.
        checkout.SetDeliveryAddress("12/A Road 5 Block C Dhanmondi Dhaka");
        checkout.SetPhone("01700000000");
        var confirmation = checkout.PlaceOrder().WaitForReady();

        Assert.True(CurrentUrlContains("/Checkout/Confirmation"));
        Assert.NotEmpty(confirmation.OrderNumberText);
    }
}
