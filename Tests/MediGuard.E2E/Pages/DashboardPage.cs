using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Dashboard — the shared landing surface for Admin, Pharmacy Manager, Pharmacist and
/// Cashier (riders get their dispatch board here too). Used to assert that a role signs
/// in to the operational area rather than the storefront.
/// </summary>
public class DashboardPage : PageObjectBase
{
    public DashboardPage(IWebDriver driver) : base(driver) { }

    public DashboardPage Open()
    {
        Go("/Dashboard");
        return WaitForReady();
    }

    public DashboardPage WaitForReady()
    {
        Wait(TimeSpan.FromSeconds(20)).Until(d =>
            d.Url.Contains("/Dashboard", StringComparison.OrdinalIgnoreCase)
            && d.Title.Contains("Dashboard", StringComparison.OrdinalIgnoreCase));
        return this;
    }

    public bool IsOnDashboard =>
        Driver.Url.Contains("/Dashboard", StringComparison.OrdinalIgnoreCase);
}
