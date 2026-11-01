using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// Post-purchase order management for a signed-in customer: the tabbed order list, the
/// tracking timeline, the printable invoice, Re-order (which refills the cart from a past
/// order) and Cancel (a confirm-gated POST that flips the order into the Cancelled tab).
/// Each test places its own fresh cash-on-delivery order and then works against that
/// stamped number, so results never depend on rows left behind by earlier runs.
/// </summary>
public class CustomerOrderManagementTests : BrowserTestBase
{
    // A stocked, prescription-free item: the order lands in "Confirmed" (cancellable)
    // state immediately instead of waiting on a pharmacist decision.
    private const string KnownMedicine = "Napa";
    private const string DeliveryAddress = "House 21, Road 8, Dhanmondi, Dhaka";
    private const string DeliveryPhone = "01712345678";

    [Fact]
    public void OrderList_FreshOrder_VisibleInActiveAndAllTabs()
    {
        var orderNumber = PlaceCodOrder();

        Order.Open("active");
        Assert.True(Order.WaitForOrder(orderNumber),
            $"The new order {orderNumber} should show under the Active tab.");

        Order.Open("all");
        Assert.True(Order.HasOrder(orderNumber),
            $"The new order {orderNumber} should also show under the All tab.");
    }

    [Fact]
    public void TrackOrder_FreshOrder_OpensProgressTimelineWithStatus()
    {
        var orderNumber = PlaceCodOrder();

        var details = Order.Open("active").TrackOrder(orderNumber);

        Assert.Contains(orderNumber, details.HeadingText, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(details.StatusBadgeText),
            "The tracking page should render the order's current status badge.");
        Assert.True(details.StepTitles.Count >= 3,
            $"The progress timeline should show the fulfilment steps, saw: [{string.Join(", ", details.StepTitles)}].");
    }

    [Fact]
    public void Invoice_FreshOrder_RendersTaxInvoiceForThatOrder()
    {
        var orderNumber = PlaceCodOrder();

        Assert.True(Order.Open("active").OpenInvoice(orderNumber),
            "The Invoice link should open the printable invoice document.");
        Assert.True(Order.InvoiceShowsOrder(orderNumber),
            $"The invoice should be headed as a tax invoice for {orderNumber}.");
    }

    [Fact]
    public void ReOrder_PastOrder_RefillsCartAndNoticesCustomer()
    {
        var orderNumber = PlaceCodOrder();

        // Empty the cart first so the re-added lines are unambiguously from Re-order.
        Cart.ClearIfNotEmpty();

        var cart = Order.Open("active").ReOrder(orderNumber);

        // The notice is a TempData banner rendered on this single landing request, so it
        // must be read from the page Re-order redirected to — never by re-navigating.
        Assert.True(cart.WaitForNoticeContains("back to your cart"),
            "Re-order should notice the customer that items were added back to the cart.");
        Assert.False(cart.IsEmpty,
            "Re-order should push the past order's items back into the cart.");
    }

    [Fact]
    public void Cancel_ConfirmedOrder_MovesItToCancelledTab()
    {
        var orderNumber = PlaceCodOrder();

        var orders = Order.Open("active").CancelOrder(orderNumber);

        Assert.True(orders.WaitForOrder(orderNumber),
            $"After cancelling, {orderNumber} should be listed under the Cancelled tab.");
        Assert.False(string.IsNullOrWhiteSpace(orders.NoticeText),
            "Cancelling should leave a success notice on the cancelled tab.");

        orders.Open("active");
        Assert.False(orders.HasOrder(orderNumber),
            "A cancelled order should disappear from the Active tab.");
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Places a fresh cash-on-delivery home-delivery order as the demo customer and
    /// returns its stamped number. A non-Rx COD order is created in Approved state,
    /// which is both cancellable and reorderable — the states these tests act on.
    /// </summary>
    private string PlaceCodOrder()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        Cart.ClearIfNotEmpty();
        Storefront.Open().Search(KnownMedicine).AddFirstProductToCart();

        var checkout = Cart.ProceedToCheckout();
        checkout.WaitForReady()
                .SelectCashOnDelivery()
                .SelectHomeDelivery()
                .SetDeliveryAddress(DeliveryAddress)
                .SetPhone(DeliveryPhone);

        var confirmation = checkout.PlaceOrder().WaitForReady();
        var orderNumber = confirmation.OrderNumberText;

        Assert.False(string.IsNullOrWhiteSpace(orderNumber),
            "The receipt should carry an order number to manage afterwards.");
        return orderNumber;
    }
}
