using OpenQA.Selenium;
using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// The delivery journey: a customer places a home-delivery order, the branch manager
/// dispatches it to a rider from the delivery board, and the rider works the parcel
/// through pick-up -> in-transit -> delivered on their own portal. A fresh order is
/// placed per run so the flow is deterministic and repeatable instead of depending on
/// mutable demo rows that a previous run may have already advanced.
/// </summary>
public class DeliveryJourneyTests : BrowserTestBase
{
    // A stocked, prescription-free item so checkout never needs a pharmacist decision.
    private const string KnownMedicine = "Napa";
    private const string DeliveryAddress = "House 21, Road 8, Dhanmondi, Dhaka";
    private const string DeliveryPhone = "01712345678";

    [Fact]
    public void DeliveryJourney_ManagerAssigns_ThenRiderDelivers_CompletesEndToEnd()
    {
        var orderNumber = PlaceHomeDeliveryOrder();

        // 1) The manager dispatches the fresh parcel to the branch rider.
        SignOutAndSignInAs(TestConfig.DemoManagerEmail);
        var board = Delivery.Open();
        Assert.True(board.ListsOrder(orderNumber),
            $"The dispatch board should list the new delivery {orderNumber}.");
        Assert.True(board.AssignRider(orderNumber, TestConfig.DemoRiderName),
            "After assigning, the board should show the parcel ASSIGNED to the rider.");

        // 2) The rider picks the parcel up and drives it to delivered.
        SignOutAndSignInAs(TestConfig.DemoRiderEmail);
        var portal = Rider.Open();
        Assert.True(portal.HasActiveOrder(orderNumber),
            $"The assigned parcel {orderNumber} should appear on the rider's active board.");

        var actions = portal.AdvanceToDelivered(orderNumber);
        Assert.Contains("PICKED_UP", actions);
        Assert.Contains("IN_TRANSIT", actions);
        Assert.Contains("DONE", actions);

        // 3) A delivered parcel leaves the active board and is archived in history.
        Assert.False(portal.HasActiveOrder(orderNumber),
            "A delivered parcel should disappear from the rider's active board.");
        Assert.True(portal.OpenHistory().OrderAppearsInHistory(orderNumber),
            "The delivered parcel should be archived in the rider's history log.");
    }

    [Fact]
    public void RiderPortal_LoadsAndShowsActiveDispatches()
    {
        SignIn(TestConfig.DemoRiderEmail, TestConfig.DemoPassword);

        var portal = Rider.Open();

        Assert.True(portal.ActiveCardCount > 0,
            "The seeded rider should have at least one active dispatch card.");
        Assert.True(int.TryParse(portal.ActiveDispatchCount, out var active) && active > 0,
            $"The 'Active dispatches' counter should be a positive number, was: '{portal.ActiveDispatchCount}'.");
    }

    [Fact]
    public void RiderPortal_CodSettlementPanelRendersTotal()
    {
        SignIn(TestConfig.DemoRiderEmail, TestConfig.DemoPassword);

        var portal = Rider.Open().OpenCodSettlement();

        Assert.False(string.IsNullOrWhiteSpace(portal.CodTotalText),
            "The COD settlement ledger should render a running cash total.");
        Assert.True(portal.CodRowCount > 0,
            "The COD ledger should render at least one row (a cash order or the 'nothing to collect' note).");
    }

    [Fact]
    public void ManagerBoard_LoadsOpenDeliveries()
    {
        SignIn(TestConfig.DemoManagerEmail, TestConfig.DemoPassword);

        var board = Delivery.Open();

        Assert.True(board.HasRows,
            "The seeded branch has open deliveries, so the dispatch board should list rows.");
        Assert.True(board.RowCount >= 1,
            $"Board row count should be positive, was: {board.RowCount}.");
    }

    // The delivery surfaces are role-gated: the dispatch board is manager-only and the
    // rider portal is deliveryman-only, so a signed-in user of the wrong kind is sent to
    // the access-denied page rather than being silently shown the content.
    [Theory]
    [InlineData("rider@citypharmacy.app", "/Delivery/Index")]      // rider is not a manager
    [InlineData("customer@example.com", "/Deliveryman")]           // customer is not a rider
    [InlineData("customer@example.com", "/Delivery/Index")]        // customer is not a manager
    public void DeliverySurface_WrongRole_IsDenied(string email, string path)
    {
        SignIn(email, TestConfig.DemoPassword);

        GoTo(path);

        Assert.True(CurrentUrlContains("/Account/AccessDenied"),
            $"{email} hitting {path} should be blocked, landed on: {Driver.Url}");
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Places a fresh cash-on-delivery HOME-DELIVERY order as the demo customer and
    /// returns its stamped order number. Choosing delivery makes checkout create a
    /// PENDING delivery row that then surfaces on the manager's dispatch board.
    /// </summary>
    private string PlaceHomeDeliveryOrder()
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
            "The receipt should carry an order number to track through delivery.");
        return orderNumber;
    }

    /// <summary>
    /// Ends the current session and signs in as another demo account, letting one test
    /// walk the same parcel through every role's surface in a single browser.
    /// </summary>
    private void SignOutAndSignInAs(string email)
    {
        Login.Logout();
        SignIn(email, TestConfig.DemoPassword);
    }
}
