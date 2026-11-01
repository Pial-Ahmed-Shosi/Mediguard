using Xunit;

namespace MediGuard.E2E.Tests;

/// <summary>
/// The pharmacy verification gate: a self-registered manager waits in the platform
/// admin's Pending Pharmacies queue, and only after the admin approves the application
/// (activating manager + branch) may that manager sign in and reach the operational
/// dashboard. Complements LoginTests' blocked-login case, which proves the other half
/// of the same gate.
/// </summary>
public class PharmacyApprovalTests : BrowserTestBase
{
    private const string ManagerPassword = "Pharm1234";

    [Fact]
    public void AdminApprovesApplication_ManagerCanSignInAndReachesDashboard()
    {
        // 1) A fresh pharmacy application lands an INACTIVE manager account.
        var domain = $"e2{DateTime.UtcNow:yyyyMMddHHmmss}.test";
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var managerEmail = $"manager@{domain}";

        RegisterPharmacy
            .Open()
            .Fill("E2E Approval Pharmacy", domain, $"E2E-LIC-{stamp}", $"E2E-TIN-{stamp}",
                "House 2, Road 4", "Dhaka", "E2E Approve Me", managerEmail, ManagerPassword,
                phone: "01700000011")
            .SubmitAndWait();

        // 2) The platform admin finds it in the verification queue and approves it.
        Login.Logout();
        SignIn(TestConfig.DemoAdminEmail, TestConfig.DemoPassword);

        var queue = AdminPharmacies.Open();
        Assert.True(queue.WaitForApplication(managerEmail),
            $"The new application for {managerEmail} should wait in the pending queue.");
        Assert.True(queue.Approve(managerEmail),
            "After approval the notice should render and the row should leave the queue.");
        Assert.False(queue.ListsApplication(managerEmail),
            "An approved pharmacy must no longer appear among pending applications.");

        // 3) The very same credentials that were blocked before approval now work,
        //    and land the manager on the operational dashboard.
        Login.Logout();
        Assert.True(Login.Login(managerEmail, ManagerPassword),
            "An approved manager should be able to sign in.");
        Assert.True(Dashboard.IsOnDashboard,
            $"An approved manager should land on the dashboard, at: {Driver.Url}");
    }
}
