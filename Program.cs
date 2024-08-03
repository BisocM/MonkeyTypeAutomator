using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Spectre.Console;
using OpenQA.Selenium.Interactions;
using System;

namespace MonkeyTypeAutomator
{
    internal class Program
    {
        private static int desiredWPM;
        private static readonly string Url = "https://monkeytype.com/";
        private const int StandardWordLength = 5; //Standard word length for calculating WPM

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ShowSplashScreen();

            desiredWPM = AnsiConsole.Ask<int>("What is your target WPM?");
            int delay = CalculateKeystrokeDelay(desiredWPM);

            var driver = InitializeWebDriver();
            using (driver)
            {
                driver.Navigate().GoToUrl(Url);
                HandleCookieConsent(driver);
                AnsiConsole.Prompt(new TextPrompt<string>("Press [green]ENTER[/] to start typing...").AllowEmpty());
                TryTyping(driver, delay);
            }
        }

        private static void ShowSplashScreen() => AnsiConsole.Write(new FigletText("MonkeyType Bot").Color(Color.Blue));

        private static ChromeDriver InitializeWebDriver()
        {
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArguments("--log-level=3", "--silent", "--disable-logging", "--disable-dev-shm-usage");

            return new ChromeDriver(driverService, options);
        }

        private static void TryTyping(IWebDriver driver, int delay)
        {
            var actions = new Actions(driver);
            bool isTyping = true;

            while (isTyping)
            {
                try
                {
                    var activeWordElement = driver.FindElement(By.CssSelector(".word.active"));
                    string activeWord = string.Join("", activeWordElement.FindElements(By.TagName("letter")).Select(e => e.Text));
                    TypeWord(actions, activeWord, delay);
                    isTyping = CheckIfTestIsActive(driver);
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Adjusting search for active word...");
                    Thread.Sleep(1000);
                }
            }

            AnsiConsole.MarkupLine("[bold green]Test complete![/]");
        }

        private static void TypeWord(Actions actions, string word, int delay)
        {
            foreach (char c in word)
            {
                actions.SendKeys(c.ToString()).Perform();
                Thread.Sleep(delay);
            }
            actions.SendKeys(" ").Perform();
        }

        private static int CalculateKeystrokeDelay(int wpm)
        {
            if (wpm <= 0)
                throw new ArgumentException("WPM must be greater than 0.");
            return (int)Math.Round(60000 / (double)(wpm * StandardWordLength));
        }

        private static void HandleCookieConsent(ChromeDriver driver)
        {
            try
            {
                driver.FindElement(By.ClassName("rejectAll"))?.Click();
            }
            catch (NoSuchElementException) { }

            try
            {
                driver.FindElement(By.CssSelector("button[aria-label='Consent']"))?.Click();
            }
            catch (NoSuchElementException) { }
        }

        private static bool CheckIfTestIsActive(IWebDriver driver)
        {
            return driver.FindElements(By.CssSelector(".word.active")).Count > 0;
        }
    }
}