using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// Prescription-only purchases. Covers the customer's prescription tracker and the
/// checkout gate that flags an order as requiring pharmacist verification before the
/// pharmacy can prepare it.
/// </summary>
public class PrescriptionFlowTests : BrowserTestBase
{
    // A prescription-only catalogue item (Novidat / Ciprofloxacin, Rx = true in the seeder).
    private const string RxMedicine = "Novidat";

    [Fact]
    public void MyPrescriptions_PageLoadsForSignedInCustomer()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);

        Prescription.Open();

        Assert.True(Prescription.IsLoaded,
            $"The prescription tracker should load for a signed-in customer, at: {Driver.Url}");
    }

    [Fact]
    public void AddingPrescriptionMedicine_FlagsCheckoutAsRequiringVerification()
    {
        SignIn(TestConfig.DemoCustomerEmail, TestConfig.DemoPassword);
        Cart.ClearIfNotEmpty();

        Storefront.Open().Search(RxMedicine).AddFirstProductToCart();
        var checkout = Cart.ProceedToCheckout();
        checkout.WaitForReady();

        // The payment step surfaces an "holds prescription medicine" notice and a link to
        // the customer's prescription tracker when the basket contains an Rx item.
        var source = Driver.PageSource;
        Assert.Contains("prescription", source, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("My prescriptions", source, System.StringComparison.OrdinalIgnoreCase);
    }
}
