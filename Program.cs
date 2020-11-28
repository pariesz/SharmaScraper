using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SharmaScraper {
    public class Reservation {
        public DateTime Time { get; set; }
        public string Info { get; set; }
    }

    class Program {
        const string ReservationType = "58";

        static Encoding htmlEncoding = Encoding.GetEncoding("iso-8859-1");
        static CancellationTokenSource ctSource = new CancellationTokenSource();
        static ConcurrentBag<Reservation> reservations = new ConcurrentBag<Reservation>();
        static HttpClient client;
        static Regex startDateRegex = new Regex(@"""start"":{""DateTime"":new Date\(([0-9,]+)\)"); 

        static async Task Main(string[] args) {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };

            Console.Write("Username (email): ");
            var email = Console.ReadLine();

            Console.Write("Password: ");
            var password = Console.ReadLine();

            client = new HttpClient(handler) {
                BaseAddress = new Uri("https://sharmaclimbingbarcelona.syltek.com")
            };

            try {
                await Login(email, password);

                for (var i=0; i<=7; i++) {
                    await GetDay(DateTime.Now.Date.AddDays(i));
                }

                // TESTING
                //Scrape(File.OpenRead("../../../NewReservationFromBooking.html"));
                //Deserialize(File.ReadAllText("../../../getTimePeriod.js")).ToArray();

            } finally {
                await WriteResults(args.Length > 0 ? args[0] : null);
                client.Dispose();
                handler.Dispose();
            }
        }

        static async Task Login(string email, string password) {
            await PostAsync("/bookings/customer/login", new Dictionary<string, string> {
                ["email"] = email,
                ["password"] = password,
                ["safari"] = "",
                ["url"] = "/customerzone"
            });
        }

        static async Task GetDay(DateTime date) {
            var response = await PostAsync("/booking/getTimePeriod", new Dictionary<string, string> {
                ["sDate"] = date.ToString("dd-MM-yyyy"),
                ["type"] = ReservationType,
                ["checktype"] = "true"
            });

            var json = await response.Content.ReadAsStringAsync();
            var times = Deserialize(json);

            foreach(var time in times) {
                await GetTime(date.Add(time.TimeOfDay));
            }
        }

        static async Task GetTime(DateTime time) {
            var response = await PostAsync("/booking/index", new Dictionary<string, string> {
                ["date"] = time.ToString("dd-MM-yyyy"),
                ["hour"] = time.ToString("HH:mm"),
                ["type"] = ReservationType,
                ["duration"] = "120"
            });

            var stream = await response.Content.ReadAsStreamAsync();
            reservations.Add(new Reservation {
                Time = time,
                Info = Scrape(stream)
            });
        }

        static IEnumerable<DateTime> Deserialize(string js) {
            return startDateRegex.Matches(js).Select(match => {
                var group = match.Groups[1];
                var parts = group.Value.Split(",").Select(x => int.Parse(x)).ToArray();
                var date = new DateTime(parts[0], parts[1] + 1, parts[2], parts[3], parts[4], parts[5]);
                return date;
            }).Distinct();
        }

        static string Scrape(Stream html) {
            var doc = new HtmlDocument();
            doc.Load(html, htmlEncoding);

            var generalInfo = doc
                .GetElementbyId("newreservation")?
                .SelectSingleNode("//div[contains(@class, 'generalInfo')]");

            var text = generalInfo?.InnerText.Trim() ?? string.Empty;

            return WebUtility.HtmlDecode(text);
        }

        static async Task WriteResults(string? file) {
            var lines = reservations
                .OrderBy(x => x.Time).Select(x => $"{x.Time:ddd dd/MM hh:mm},{x.Info}")
                .ToList();

            lines.Insert(0, "Time,Info");

            await File.WriteAllLinesAsync(file ?? "./results.csv", lines, Encoding.ASCII);
        }

        static async Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> formValues) {
            if (ctSource.Token.IsCancellationRequested) return null;

            var valuesString = "{" + string.Join(",", formValues.Select(kv => $"{kv.Key}:{kv.Value}")) + "}";

            Console.Write("POST: " + url + " " + valuesString + ": ");
            var content = new FormUrlEncodedContent(formValues);
            var response = await client.PostAsync(url, content, ctSource.Token);

            Console.WriteLine(response.StatusCode);
            if (!response.IsSuccessStatusCode) ctSource.Cancel();
            return response;

        }
    }
}
