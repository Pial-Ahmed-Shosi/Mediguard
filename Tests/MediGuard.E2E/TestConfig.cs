using System.Text.Json;

namespace MediGuard.E2E;

/// <summary>
/// Loads E2E settings from appsettings.json copied next to the test assembly.
/// Everything a test might need to point at a different environment lives here,
/// so nothing hard-codes localhost or a demo credential deep in a page object.
/// </summary>
public static class TestConfig
{
    private static readonly JsonDocument Document =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));

    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL")
        ?? Str("BaseUrl", "http://localhost:5251");

    // Headless can be flipped without editing the file (e.g. for CI) via E2E_HEADLESS=1/true.
    public static bool Headless
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("E2E_HEADLESS");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env is "1" or "true" or "True" or "yes";
            }
            return Document.RootElement.TryGetProperty("Headless", out var el)
                   && el.ValueKind == JsonValueKind.True;
        }
    }

    // Which browser to drive: "chrome", "edge", or "brave". Override per-run via
    // E2E_BROWSER without touching the settings file.
    public static string Browser
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("E2E_BROWSER");
            if (!string.IsNullOrWhiteSpace(env)) return env.Trim().ToLowerInvariant();
            return Str("Browser", "chrome");
        }
    }

    public static string DemoCustomerEmail => Str("DemoCustomerEmail", "customer@example.com");

    public static string DemoAdminEmail => Str("DemoAdminEmail", "admin@mediguard.app");

    public static string DemoManagerEmail => Str("DemoManagerEmail", "manager@citypharmacy.app");

    public static string DemoPharmacistEmail => Str("DemoPharmacistEmail", "pharmacist@citypharmacy.app");

    public static string DemoRiderEmail => Str("DemoRiderEmail", "rider@citypharmacy.app");

    // Display name of the seeded rider, used to pick them in the manager's assign dropdown.
    public static string DemoRiderName => Str("DemoRiderName", "Jubayer Ahmed");

    public static string DemoPassword => Str("DemoPassword", "Mediguard123");

    private static string Str(string key, string fallback) =>
        Document.RootElement.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? fallback
            : fallback;
}
