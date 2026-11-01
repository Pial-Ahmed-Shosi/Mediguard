using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;

namespace MediGuard.E2E;

/// <summary>
/// Creates a configured WebDriver for the browser named by TestConfig.Browser
/// ("chrome", "edge", or "brave"). Selenium 4.6+ bundles Selenium Manager, which
/// resolves and downloads the matching driver (chromedriver / msedgedriver)
/// automatically, so there is no manual driver install or PATH setup required here.
/// When Headless is off the browser opens as a normal, maximized window so the
/// automation can be watched live.
/// </summary>
public static class BrowserFactory
{
    public static IWebDriver Create()
    {
        return TestConfig.Browser switch
        {
            "edge" or "msedge" or "microsoftedge" or "microsoft edge" => CreateEdge(),
            "brave" => CreateBrave(),
            _ => CreateChrome()
        };
    }

    private static IWebDriver CreateChrome()
    {
        var options = new ChromeOptions();
        ApplyCommonChromeArguments(options);
        return Finalize(new ChromeDriver(options));
    }

    /// <summary>
    /// Brave is Chromium-based and is driven by chromedriver; only the browser
    /// binary location differs, so we point ChromeOptions at the Brave executable.
    /// Selenium Manager still supplies the matching driver.
    /// </summary>
    private static IWebDriver CreateBrave()
    {
        var binary = FindBraveBinary()
            ?? throw new InvalidOperationException(
                "Brave browser was not found in the usual install locations. " +
                "Install Brave, or set Browser to 'edge'/'chrome', or adjust FindBraveBinary().");

        var options = new ChromeOptions();
        options.BinaryLocation = binary;
        ApplyCommonChromeArguments(options);
        return Finalize(new ChromeDriver(options));
    }

    private static IWebDriver CreateEdge()
    {
        var options = new EdgeOptions();

        if (TestConfig.Headless)
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1440,2000");
        }

        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-session-crashed-bubble");
        options.AddArgument("--disable-infobars");
        // Suppress the "Microsoft Edge is being controlled by automated test software"
        // info bar and the automation-extension prompt so clicks are never blocked.
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);

        return Finalize(new EdgeDriver(options));
    }

    private static void ApplyCommonChromeArguments(ChromeOptions options)
    {
        if (TestConfig.Headless)
        {
            options.AddArgument("--headless=new");
            // A fixed viewport keeps headless layout identical to a normal window,
            // so elements that only appear past a bootstrap breakpoint stay reachable.
            options.AddArgument("--window-size=1440,2000");
        }

        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        // Stop the browser from stealing focus / popping the "restore pages?" bubble on
        // the crash badge between runs, which otherwise intermittently blocks clicks.
        options.AddArgument("--disable-session-crashed-bubble");
        options.AddArgument("--disable-infobars");
        options.AddExcludedArgument("enable-automation");
    }

    private static IWebDriver Finalize(IWebDriver driver)
    {
        // Keep the implicit wait small: page objects use explicit WebDriverWait for
        // the real synchronization, and a large implicit wait only slows failures down.
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        if (!TestConfig.Headless)
        {
            driver.Manage().Window.Maximize();
        }
        return driver;
    }

    private static string? FindBraveBinary()
    {
        var candidates = new[]
        {
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\BraveSoftware\Brave-Browser\Application\brave.exe"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\BraveSoftware\Brave-Browser\Application\brave.exe"),
            Environment.ExpandEnvironmentVariables(@"%LocalAppData%\BraveSoftware\Brave-Browser\Application\brave.exe"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\BraveSoftware\Brave\Browser\brave.exe"),
            Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Programs\Brave\brave.exe")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }
        return null;
    }
}
