using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Spectre.Console;
using OpenQA.Selenium.Interactions;
using System;
using System.Diagnostics;

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

            var driver = InitializeWebDriver();
            using (driver)
            {
                driver.Navigate().GoToUrl(Url);
                HandleCookieConsent(driver);
                AnsiConsole.Prompt(new TextPrompt<string>("Press [green]ENTER[/] to start typing...").AllowEmpty());
                TryTyping(driver);
                
                //Prevent from closing until the user is happy.
                AnsiConsole.WriteLine("Press [green]ENTER[/] to exit...");
                Console.ReadLine();
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

        private static void TryTyping(IWebDriver driver)
        {
            var actions = new Actions(driver);
            bool isTyping = true;
            Stopwatch stopwatch = new Stopwatch(); // For more accurate timing

            while (isTyping)
            {
                try
                {
                    var activeWordElement = driver.FindElement(By.CssSelector(".word.active"));
                    string activeWord = string.Join("", activeWordElement.FindElements(By.TagName("letter")).Select(e => e.Text));

                    stopwatch.Restart(); //Start timer for the word
                    TypeWord(actions, activeWord);
                    stopwatch.Stop();

                    //Dynamically calculate delay based on actual typing time
                    int elapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds;
                    int desiredMilliseconds = CalculateWordTime(activeWord.Length);
                    int delay = Math.Max(0, desiredMilliseconds - elapsedMilliseconds); //Ensure delay isn't negative

                    Thread.Sleep(delay);
                    isTyping = CheckIfTestIsActive(driver);

                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Adjusting search for active word...");
                    Thread.Sleep(1000);
                }
                catch (StaleElementReferenceException)
                {
                    Console.WriteLine("Stale element exception - ignoring...");
                    Thread.Sleep(1000);
                }
            }

            AnsiConsole.MarkupLine("[bold green]Test complete![/]");
        }

        private static void TypeWord(Actions actions, string word)
        {
            foreach (var c in word)
                actions.SendKeys(c.ToString()).Perform();
            
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

        private static int CalculateWordTime(int wordLength)
        {
            //Adjust word length for spaces (each word effectively has one more character due to the space)
            double effectiveWordLength = wordLength + 1;
            return (int)Math.Round(60000.0 / (desiredWPM * (StandardWordLength / effectiveWordLength)));
        }
    }
}