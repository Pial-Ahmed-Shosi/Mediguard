namespace MediGuard.E2E;

/// <summary>
/// Produces real on-disk fixture files for upload-driven tests. The prescription drop-zone
/// demands a genuine document (client and server both check the MIME type), so instead of a
/// placeholder text file we materialise a valid 1x1 PNG in a temp folder and hand Selenium
/// its absolute path — SendKeys on a file input only accepts files that actually exist.
/// </summary>
internal static class TestAssets
{
    // The canonical minimal valid PNG (1x1 pixel) — passes extension + MIME checks.
    private const string OnePixelPng =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    /// <summary>
    /// Writes a unique prescription-like PNG (named so it looks like a scanned copy) and
    /// returns its absolute path. Unique names keep concurrent runs from sharing a handle.
    /// </summary>
    public static string WritePrescriptionPng()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mediguard-e2e", "prescriptions");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"rx-copy-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}.png");
        File.WriteAllBytes(path, Convert.FromBase64String(OnePixelPng));
        return path;
    }
}
