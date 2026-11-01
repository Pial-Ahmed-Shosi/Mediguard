using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Admin/PendingPharmacies — the platform admin's verification queue (Admin role only).
/// Inactive self-registered managers are listed as .rw-table rows with the manager email in
/// .rw-cell-sub; each row posts an ApprovePharmacy form which activates the manager account
/// and its branch, then redirects back with a green .rw-badge notice. An approved manager
/// leaves the queue (the query only lists !IsActive accounts).
/// </summary>
public class AdminPharmaciesPage : PageObjectBase
{
    public AdminPharmaciesPage(IWebDriver driver) : base(driver) { }

    private static By Table => By.CssSelector(".rw-table");
    private static By Notice => By.CssSelector(".rw-badge.is-ok");
    private static By EmptyNote => By.CssSelector(".rw-card p.text-muted");

    public AdminPharmaciesPage Open()
    {
        Go("/Admin/PendingPharmacies");
        Wait(TimeSpan.FromSeconds(20)).Until(d =>
            SafeCount(d, Table) > 0 || SafeCount(d, Notice) > 0 || SafeCount(d, EmptyNote) > 0);
        return this;
    }

    private static By RowByEmail(string managerEmail) =>
        By.XPath($"//tr[.//span[contains(@class,'rw-cell-sub')][normalize-space()='{managerEmail}']]");

    /// <summary>True while the application still waits in the verification queue.</summary>
    public bool ListsApplication(string managerEmail) => SafeCount(Driver, RowByEmail(managerEmail)) > 0;

    public bool WaitForApplication(string managerEmail, TimeSpan? timeout = null) =>
        WaitForPresence(RowByEmail(managerEmail), timeout);

    public bool WaitForApplicationGone(string managerEmail, TimeSpan? timeout = null) =>
        WaitForAbsent(RowByEmail(managerEmail), timeout);

    public int PendingRowCount =>
        Driver.FindElements(By.CssSelector(".rw-table tbody tr")).Count;

    /// <summary>
    /// Approves the application belonging to that manager email and waits for the
    /// redirect plus the success notice. The row must disappear afterwards, since the
    /// queue only lists still-inactive managers.
    /// </summary>
    public bool Approve(string managerEmail)
    {
        var row = Wait().Until(d =>
        {
            var rows = d.FindElements(RowByEmail(managerEmail));
            return rows.Count > 0 ? rows[0] : null;
        }) ?? throw new NoSuchElementException($"No pending application for {managerEmail}.");

        var button = row.FindElement(By.CssSelector("form[action*='ApprovePharmacy'] button[type='submit']"));
        // Chrome-level overlays (sticky sidebar/footer) intercept a native click on row
        // buttons, so submit through script instead (the form POST fires normally).
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'}); arguments[0].click();", button);

        var redirected = WaitForUrlContains("/Admin/PendingPharmacies", TimeSpan.FromSeconds(25));
        if (!redirected) return false;
        WaitForPresence(Notice, TimeSpan.FromSeconds(10));
        return WaitForApplicationGone(managerEmail, TimeSpan.FromSeconds(15));
    }

    public string NoticeText => IsDisplayed(Notice) ? Driver.FindElements(Notice)[0].Text.Trim() : string.Empty;
}
