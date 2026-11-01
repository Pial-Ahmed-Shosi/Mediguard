using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// The prescription journey across three roles: a customer buys an Rx medicine and must
/// attach a real prescription copy (a valid PNG document), a pharmacist then approves it
/// on the review terminal, and a manager rejects another one — with the empty-reason
/// attempt refused first. Decisions are AJAX, so the review badge locks in place and the
/// customer's own tracker plus the order status must reflect the outcome afterwards.
/// </summary>
public class PrescriptionLifecycleTests : BrowserTestBase
{
    // A prescription-only catalogue item (Novidat / Ciprofloxacin, Rx = true in the seeder).
    private const string RxMedicine = "Novidat";
    private const string DeliveryAddress = "House 21, Road 8, Dhanmondi, Dhaka";
    private const string DeliveryPhone = "01712345678";

    [Fact]
    public void UploadPrescription_ValidCopy_LandsOnTrackingAwaitingReview()
    {
        var (orderNumber, orderId) = PlaceRxOrder();

        var upload = PrescriptionUpload.Open(orderId)
            .AttachDocument(TestAssets.WritePrescriptionPng())
            .AddNote("E2E prescription copy for automated review");
        Assert.True(upload.IsSubmitEnabled, "A valid PNG should enable the submit button.");

        var details = upload.SubmitAndWait();

        Assert.True(CurrentUrlContains("/Order/Details/"),
            "A successful upload should redirect to the order tracking page.");
        Assert.Contains("prescription", details.StatusBadgeText, StringComparison.OrdinalIgnoreCase);

        Prescription.Open();
        Assert.True(Prescription.WaitForOrder(orderNumber),
            $"The uploaded copy for {orderNumber} should appear in the customer's tracker.");
    }

    [Fact]
    public void PharmacistApproves_BadgeLocksAndCustomerSeesApproval()
    {
        var (orderNumber, orderId) = PlaceRxOrder();
        PrescriptionUpload.Open(orderId).AttachDocument(TestAssets.WritePrescriptionPng()).SubmitAndWait();

        // The pharmacist (prescription.verify) works the pending queue and approves.
        Login.Logout();
        SignIn(TestConfig.DemoPharmacistEmail, TestConfig.DemoPassword);
        Assert.True(PrescriptionQueue.Open().WaitForOrder(orderNumber),
            $"The fresh prescription for {orderNumber} should wait in the PENDING queue.");

        var review = PrescriptionQueue.ReviewOrder(orderNumber);
        Assert.True(review.Approve(), "The status badge should flip to APPROVED after the decision.");

        // Back to the customer: the tracker reflects the pharmacist's approval.
        Login.Logout();
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);
        Prescription.Open();
        Assert.True(Prescription.WaitForOrder(orderNumber),
            "The decided prescription should still be listed for the customer.");
        Assert.Equal("Approved", Prescription.StatusForOrder(orderNumber));
    }

    [Fact]
    public void ManagerRejects_RequiresReason_AndCancelsTheOrder()
    {
        var (orderNumber, orderId) = PlaceRxOrder();
        PrescriptionUpload.Open(orderId).AttachDocument(TestAssets.WritePrescriptionPng()).SubmitAndWait();

        Login.Logout();
        SignIn(TestConfig.DemoManagerEmail, TestConfig.DemoPassword);
        Assert.True(PrescriptionQueue.Open().WaitForOrder(orderNumber),
            $"The fresh prescription for {orderNumber} should wait in the PENDING queue.");

        var review = PrescriptionQueue.ReviewOrder(orderNumber);

        // A rejection without a reason must be refused client-side (modal stays open).
        review.OpenRejectDialog();
        Assert.True(review.ConfirmRejectWithoutReasonIsRefused(),
            "Confirming an empty rejection reason should be refused.");

        Assert.True(review.Reject("Prescription copy is illegible"),
            "A reasoned rejection should lock the badge to REJECTED.");
        Assert.Contains("illegible", review.RejectionReasonText, StringComparison.OrdinalIgnoreCase);

        // The customer sees the rejection, and the order itself moved to Cancelled.
        Login.Logout();
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);
        Prescription.Open();
        Assert.True(Prescription.WaitForOrder(orderNumber),
            "The rejected prescription should still be listed for the customer.");
        Assert.Equal("Rejected", Prescription.StatusForOrder(orderNumber));

        Assert.True(Order.Open("cancelled").WaitForOrder(orderNumber),
            $"A rejected prescription should cancel order {orderNumber}.");
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Places a fresh COD order containing the Rx medicine. Such an order is created in
    /// "Under review" state and awaits a prescription copy, which is uploaded separately
    /// via /Prescription/Upload?orderId=... Returns the stamped number and the order id.
    /// </summary>
    private (string OrderNumber, Guid OrderId) PlaceRxOrder()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        Cart.ClearIfNotEmpty();
        Storefront.Open().Search(RxMedicine).AddFirstProductToCart();

        var checkout = Cart.ProceedToCheckout();
        checkout.WaitForReady()
                .SelectCashOnDelivery()
                .SelectHomeDelivery()
                .SetDeliveryAddress(DeliveryAddress)
                .SetPhone(DeliveryPhone);

        var confirmation = checkout.PlaceOrder().WaitForReady();
        var orderNumber = confirmation.OrderNumberText;
        var orderId = confirmation.OrderIdFromUrl;

        Assert.False(string.IsNullOrWhiteSpace(orderNumber), "The Rx order should be stamped.");
        Assert.NotEqual(Guid.Empty, orderId);
        return (orderNumber, orderId);
    }
}
