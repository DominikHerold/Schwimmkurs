using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using HtmlAgilityPack;

namespace Schwimmkurs
{
    public class Program
    {
        private static Timer Timer;

        private static string PushOverToken;
        private static string PushOverUser;

        private static string SwimContent = "foo";

        private static void Main()
        {
            Console.WriteLine("Start");

            var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", false).Build();
            PushOverToken = configuration["PushOverToken"];
            PushOverUser = configuration["PushOverUser"];

            Timer = new Timer((int)TimeSpan.FromHours(5).TotalMilliseconds, false);
            Timer.Elapsed += CheckContent;

            CheckContent(null, null);

            Console.ReadLine();
            Timer.Dispose();
        }

        private static void CheckContent(object sender, EventArgs e)
        {
            try
            {
                var content = GetContent();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);
                var captionDiv = htmlDoc.DocumentNode.SelectNodes("//div[@class='slide_opener noselectmark']").Single();
                var parentDiv = captionDiv.ParentNode;
                var innerText = parentDiv.InnerText;
                if (innerText == SwimContent)
                    return;

                SendToPushoverApi();
                SwimContent = innerText;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Timer.Start();
            }
        }

        private static string GetContent()
        {
            using (var webClient = new HttpClient())
            {
                HttpResponseMessage response;
                do
                {
                    webClient.DefaultRequestHeaders.Clear();
                    response = webClient.GetAsync("https://www.l.de/sportbaeder/kurse/kurse-fuer-kinder").GetAwaiter().GetResult();
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(response.IsSuccessStatusCode);
                }
                while (!response.IsSuccessStatusCode && ThreadSleep15Minutes());

                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return content;
            }
        }

        private static bool ThreadSleep15Minutes()
        {
            Thread.Sleep(TimeSpan.FromMinutes(15));

            return true;
        }

        private static void SendToPushoverApi()
        {
            var client = new HttpClient();
            var toSend = $"token={PushOverToken}&user={PushOverUser}&message=🏊 https://www.l.de/sportbaeder/kurse/kurse-fuer-kinder 🏊&title=Schwimmkurs";
            var now = DateTime.Now;
            if (now.Hour >= 22 || now.Hour < 7)
                toSend = $"{toSend}&sound=none";

            client.PostAsync("https://api.pushover.net/1/messages.json", new StringContent(toSend, Encoding.UTF8, "application/x-www-form-urlencoded")).GetAwaiter().GetResult();
        }
    }
}
