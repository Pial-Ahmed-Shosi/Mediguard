using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Prescription/Review/{id} — the pharmacist's decision page. Approve and Reject post
/// JSON via fetch and lock the UI in place: #rxStatusBadge flips to APPROVED/REJECTED
/// without a reload. Approving first raises window.confirm(); rejecting opens a Bootstrap
/// modal whose reason textarea is mandatory (empty reason marks the field invalid and
/// keeps the modal open).
/// </summary>
public class PrescriptionReviewPage : PageObjectBase
{
    public PrescriptionReviewPage(IWebDriver driver) : base(driver) { }

    private static By Root => By.Id("reviewRoot");
    private static By StatusBadge => By.Id("rxStatusBadge");
    private static By ApproveButton => By.Id("btnApprove");
    private static By RejectButton => By.Id("btnReject");
    private static By RejectReason => By.Id("rejectReason");
    private static By ConfirmReject => By.Id("btnConfirmReject");

    public PrescriptionReviewPage WaitForReady()
    {
        WaitForPresent(Root, TimeSpan.FromSeconds(20));
        WaitForPresent(StatusBadge);
        return this;
    }

    public string StatusBadgeText => WaitForPresent(StatusBadge).Text.Trim();

    private bool IsModalOpen => ((IJavaScriptExecutor)Driver).ExecuteScript(
        "return document.getElementById('rejectModal').classList.contains('show');") is true;

    private bool WaitForBadge(string expected) => WaitForJavaScript(
        $"document.getElementById('rxStatusBadge').textContent.trim() === '{expected}'",
        TimeSpan.FromSeconds(30));

    /// <summary>
    /// Approves the prescription: auto-accepts the confirm() gate, clicks Approve and
    /// waits for the badge to lock to APPROVED (the AJAX decision, no page reload).
    /// </summary>
    public bool Approve()
    {
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.confirm = function () { return true; };");
        var button = WaitForClickable(ApproveButton, TimeSpan.FromSeconds(20));
        // The sticky action bar overlaps the button's centre point, so a native click
        // gets intercepted; dispatch it through script (handlers and confirm gate fire).
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'}); arguments[0].click();", button);
        return WaitForBadge("APPROVED");
    }

    /// <summary>Opens the rejection modal and waits until Bootstrap shows it.</summary>
    public PrescriptionReviewPage OpenRejectDialog()
    {
        Click(RejectButton);
        if (!WaitForJavaScript(
                "document.getElementById('rejectModal').classList.contains('show')",
                TimeSpan.FromSeconds(10)))
        {
            throw new InvalidOperationException("The rejection modal did not open.");
        }
        return this;
    }

    public PrescriptionReviewPage SetRejectionReason(string reason)
    {
        Type(RejectReason, reason);
        return this;
    }

    /// <summary>
    /// Clicks Confirm with the reason left empty; the client must refuse (field flagged
    /// invalid, modal still open, badge still PENDING). Returns true when refused.
    /// </summary>
    public bool ConfirmRejectWithoutReasonIsRefused()
    {
        Click(ConfirmReject);
        return WaitForJavaScript(
            "document.getElementById('rejectModal').classList.contains('show')" +
            " && document.getElementById('rejectReason').classList.contains('is-invalid')" +
            " && document.getElementById('rxStatusBadge').textContent.trim() !== 'REJECTED'",
            TimeSpan.FromSeconds(8));
    }

    /// <summary>
    /// Full reject path: dialog -> reason -> confirm -> badge locks to REJECTED. Reuses
    /// the modal when an earlier empty-reason attempt already left it open, since a
    /// click on the backdrop-covered reject button would be intercepted.
    /// </summary>
    public bool Reject(string reason)
    {
        if (!IsModalOpen) OpenRejectDialog();
        SetRejectionReason(reason);
        Click(ConfirmReject);
        return WaitForBadge("REJECTED");
    }

    /// <summary>The reason echoed back inside the rejection box after a decision.</summary>
    public string RejectionReasonText =>
        IsDisplayed(By.Id("rxRejectionText")) ? Driver.FindElement(By.Id("rxRejectionText")).Text.Trim() : string.Empty;
}
