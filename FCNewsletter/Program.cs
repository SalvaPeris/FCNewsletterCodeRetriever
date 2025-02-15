using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

class Program
{
    private static readonly string dateString = "17/02/2025";
    private static readonly string FeedUrl = "https://newsletter.forocoches.com/feed";
    private static readonly string CodeUrl = "https://www.forocoches.com/codigo";
    private static readonly HttpClient client = new HttpClient();
    private static Timer timer;
    private static int seconds = 30;
    private static DateTime dateUser;
    private static IWebDriver driver;
    private static ChromeOptions chromeOptions = new ChromeOptions();

    static async Task Main(string[] args)
    {
        Console.WriteLine($"            FC Newsletter Codes Retriever ");
        Console.WriteLine($"===============================================================");
        Console.WriteLine($"Date to search {dateString}");
        Console.WriteLine($"Period {seconds} seconds");

        Console.WriteLine($"_______________________________________________________________");

        dateUser = DateTime.ParseExact(dateString, "dd/MM/yyyy", null);

        chromeOptions.AddArgument("log-level=3");

        var service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;

        var driver = new ChromeDriver(service, chromeOptions);

        driver.Navigate().GoToUrl(CodeUrl);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);

        timer = new Timer(async _ => await CheckNewPosts(), null, TimeSpan.Zero, TimeSpan.FromSeconds(seconds));

        Console.WriteLine($"Checking newsletter each {seconds} seconds");
        Console.ReadLine();
    }

    private static async Task CheckNewPosts()
    {
        try
        {
            var response = await client.GetStringAsync(FeedUrl);

            var xDoc = XDocument.Parse(response);
            var latestPost = xDoc.Descendants("item").FirstOrDefault();

            if (latestPost != null)
            {
                var dateString = latestPost.Element("pubDate")?.Value;
                string format = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";

                DateTime date = DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                if (date.Date == dateUser.Date)
                {
                    XNamespace ns = "http://purl.org/rss/1.0/modules/content/";
                    var content = latestPost.Element(ns + "encoded")?.Value;
                    var codesList = ExtractFormattedStrings(content);

                    foreach(var code in codesList)
                    {
                        if(code == codesList.FirstOrDefault())
                            FillCode(code);

                        OpenAndFillCode(code);
                    }
                }
                else
                {
                    Console.WriteLine($"No today newsletter found. {DateTime.Now}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting feed: {ex.Message}");
        }
    }

    static string[] ExtractFormattedStrings(string content)
    {
        string paragraphPattern = @"<p>(.*?)</p>";
        List<string> formattedStrings = new List<string>();

        MatchCollection paragraphMatches = Regex.Matches(content, paragraphPattern, RegexOptions.Singleline);

        foreach (Match paragraphMatch in paragraphMatches)
        {
            string paragraphContent = paragraphMatch.Groups[1].Value;

            string pattern = @"&#128064;?\s*([A-Za-z0-9]+(?:[^A-Za-z0-9\s]+[A-Za-z0-9]+)*)";
            MatchCollection matches = Regex.Matches(paragraphContent, pattern);

            foreach (Match match in matches)
            {
                string rawString = match.Groups[1].Value;
                string formatted = Regex.Replace(rawString, @"[^A-Za-z0-9]", "");
                formattedStrings.Add(formatted);
            }
        }

        return formattedStrings.ToArray();
    }

    static void FillCode(string code)
    {
        try
        {
            IWebElement inputField = driver.FindElement(By.Name("codigo"));
            inputField.SendKeys(code);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
        }
    }

    static void OpenAndFillCode(string code)
    {

        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open('');");

            var windowHandles = driver.WindowHandles;

            driver.SwitchTo().Window(windowHandles[windowHandles.Count - 1]);

            driver.Navigate().GoToUrl(CodeUrl);

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            IWebElement inputField = driver.FindElement(By.Name("codigo"));

            inputField.SendKeys(code);

            driver.SwitchTo().Window(windowHandles[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
        }
    }
}
