using OpenQA.Selenium;

namespace MediGuard.E2E.Pages;

/// <summary>
/// /Prescription/Upload?orderId=... — the customer's prescription drop-zone. The styled
/// drop area hides a real file input (#PrescriptionFile); sending it an absolute file path
/// fires the client validator (JPG/PNG/PDF, max 5 MB) which then enables the disabled
/// submit button (#submit-upload-btn). A successful POST redirects to the order tracking
/// page (/Order/Details/{id}). Reached from the "Waiting for your prescription copy" panel
/// on /Prescription/MyPrescriptions.
/// </summary>
public class PrescriptionUploadPage : PageObjectBase
{
    public PrescriptionUploadPage(IWebDriver driver) : base(driver) { }

    private static By Form => By.Id("prescription-upload-form");
    private static By FileInput => By.Id("PrescriptionFile");
    private static By Submit => By.Id("submit-upload-btn");
    private static By Notes => By.Id("Notes");
    private static By ClientError => By.Id("file-validation-error");
    private static By ServerError => By.CssSelector("span.field-validation-error");

    /// <summary>Navigates straight to the upload form for one order (its id from the checkout receipt).</summary>
    public PrescriptionUploadPage Open(Guid orderId)
    {
        Go($"/Prescription/Upload?orderId={orderId}");
        return WaitForReady();
    }

    public PrescriptionUploadPage WaitForReady()
    {
        WaitForPresent(Form, TimeSpan.FromSeconds(20));
        WaitForPresent(FileInput);
        return this;
    }

    /// <summary>
    /// Attaches a document by typing its absolute path into the hidden file input, then
    /// waits until the client-side validation accepts it (submit becomes enabled).
    /// </summary>
    public PrescriptionUploadPage AttachDocument(string absolutePath)
    {
        var input = WaitForPresent(FileInput);
        input.SendKeys(absolutePath);
        Wait(TimeSpan.FromSeconds(15)).Until(d =>
        {
            var buttons = d.FindElements(Submit);
            return buttons.Count > 0 && buttons[0].Enabled;
        });
        return this;
    }

    public PrescriptionUploadPage AddNote(string text)
    {
        if (IsDisplayed(Notes)) Type(Notes, text);
        return this;
    }

    /// <summary>True once the submit button is enabled (a valid file is attached).</summary>
    public bool IsSubmitEnabled
    {
        get
        {
            var buttons = Driver.FindElements(Submit);
            return buttons.Count > 0 && buttons[0].Enabled;
        }
    }

    /// <summary>Submits and follows the redirect to the order-tracking page.</summary>
    public OrderDetailsPage SubmitAndWait()
    {
        var button = WaitForClickable(Submit, TimeSpan.FromSeconds(15));
        // A fixed overlay sits over the bottom of the form, so a native click would be
        // intercepted; dispatch the click through script instead (handlers still fire).
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'}); arguments[0].click();", button);
        if (!WaitForUrlContains("/Order/Details/", TimeSpan.FromSeconds(40)))
        {
            throw new InvalidOperationException(
                "Prescription upload did not redirect to order tracking. At: " + Driver.Url +
                " Error: " + ValidationErrorText);
        }
        return new OrderDetailsPage(Driver).WaitForReady();
    }

    /// <summary>Text of any client- or server-side rejection shown for the file.</summary>
    public string ValidationErrorText
    {
        get
        {
            if (IsDisplayed(ClientError)) return Driver.FindElements(ClientError)[0].Text;
            if (IsDisplayed(ServerError)) return Driver.FindElements(ServerError)[0].Text;
            return string.Empty;
        }
    }
}
