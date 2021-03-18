using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SharmaScraper {


    public class SharmaClient : IDisposable {
        public static TimeSpan ReservationsReleasedSpan = new TimeSpan(7, 13, 0, 0);

        public static readonly Lazy<TimeZoneInfo> TZ = new Lazy<TimeZoneInfo>(() => {
            try {
                return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            } catch {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            }
        });

        const string ReservationType = "58";
        const string PunchCardPaymentId = "977";
        const string NewReservationFormId = "newreservation";
        const string PaymentFormId = "paymentForm";

        static readonly Encoding htmlEncoding = Encoding.GetEncoding("iso-8859-1");
        static readonly Regex startDateRegex = new Regex(@"""start"":{""DateTime"":new Date\(([0-9,]+)\)");

        readonly CookieContainer cookieContainer;
        readonly HttpClientHandler httpHandler;
        readonly HttpClient httpClient;

        public SharmaClient() {
            cookieContainer = new CookieContainer();
            httpHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
            httpClient = new HttpClient(httpHandler) {
                BaseAddress = new Uri("https://sharmaclimbingbarcelona.syltek.com")
            };
        }
        
        public Task Login(string email, string password, CancellationToken ct = default) {
            return PostAsync("/bookings/customer/login", new Dictionary<string, string> {
                ["email"] = email,
                ["password"] = password,
                ["safari"] = "",
                ["url"] = "/customerzone"
            }, secure: true, ct: ct);
        }

        public async Task<IEnumerable<DateTime>> GetTimes(DateTime date, CancellationToken ct) {
            var response = await PostAsync("/booking/getTimePeriod", new Dictionary<string, string> {
                ["sDate"] = date.ToString("yyyy-MM-dd"),
                ["type"] = ReservationType,
                ["checktype"] = "false"
            }, ct: ct);

            var json = await response.Content.ReadAsStringAsync();
            var result = DeserializeDay(json).Select(time => date.Add(time.TimeOfDay));
            return result;
        }

        public async Task BookNextReservation(DateTime date, bool mock = false, CancellationToken ct = default) {
            if (date.Kind == DateTimeKind.Utc) {
                date = TimeZoneInfo.ConvertTimeFromUtc(date, TZ.Value);
            }

            var reservation = await GetReservation(date, ct);
            if (reservation.Form == null) {

                var times = await GetTimes(date.Date, ct);
                if (!times.Any()) {
                    throw new NoTimesException($"{date:yyyy-MM-dd} has no available times.");
                }

                var time = times.FirstOrDefault(x => x == date);
                if (time == default) {
                    throw new NoTimesException($"{JsonSerializer.Serialize(times)} does not contain {date}.");
                }

                throw new Exception($"Unable to retrieve '{NewReservationFormId}' form values.");
            }

            var response = await PostAsync("/customerZone/newReservationPost", reservation.Form, ct: ct);
            var paymentUri = response.RequestMessage.RequestUri.ToString();
            var paymentForm = await ScrapeFormValues(response, PaymentFormId);
            if (paymentForm == null) {
                throw new Exception($"Unable to retrieve '{PaymentFormId}' form values.");
            }

            paymentForm["idPaymentMethod"] = PunchCardPaymentId;         

            if (mock) {
                WritePost(paymentUri, paymentForm);
                Console.WriteLine("MOCK");
            } else {
                await PostAsync(paymentUri, paymentForm, ct: ct);
            }
        }

        public async Task<List<Reservation>> GetReservations(CancellationToken ct) {
            var results = new List<Reservation>();
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TZ.Value);

            for (var i = 0; i <= 7; i++) {
                if (ct.IsCancellationRequested) {
                    break;
                }
                results.AddRange(await GetReservations(now.Date.AddDays(i), ct));
            }

            return results;
        }

        public async Task<List<Reservation>> GetReservations(DateTime date, CancellationToken ct) {
            var results = new List<Reservation>();
            var times = await GetTimes(date, ct);

            foreach (var time in times) {
                if (ct.IsCancellationRequested) {
                    break;
                }

                var result = await GetReservation(time, ct);
                results.Add(result);
            }

            return results;
        }

        public async Task<Reservation> GetReservation(DateTime time, CancellationToken ct) {
            var response = await PostAsync("/booking/index", new Dictionary<string, string> {
                ["date"] = time.ToString("dd-MM-yyyy"),
                ["hour"] = time.ToString("HH:mm"),
                ["type"] = ReservationType,
                ["duration"] = "120"
            }, ct: ct);

            using var stream = await response.Content.ReadAsStreamAsync();
            var doc = LoadHtmlDocument(stream);

            return new Reservation {
                Time = time,
                Info = ScrapeReservationGeneralInfo(doc),
                Form = ScrapeFormValues(doc, NewReservationFormId)
            };
        }

        public async Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> formValues, bool secure = false, CancellationToken ct = default) {
            WritePost(url, formValues, secure);
            var content = new FormUrlEncodedContent(formValues);
            var response = await httpClient.PostAsync(url, content, ct);

            Console.WriteLine(response.StatusCode);
            response.EnsureSuccessStatusCode();
            
            return response;
        }

        static void WritePost(string url, Dictionary<string, string> formValues, bool secure = false) {
            var sb = new StringBuilder();
            
            sb.Append("POST ");
            sb.Append(url);
            sb.Append(' ');

            if (!secure) {
                sb.Append('{');
                sb.Append(string.Join(",", formValues.Select(kv => $"{kv.Key}:{kv.Value}")));
                sb.Append('}');
                sb.Append(' ');
            }

            Console.Write(sb.ToString());
        }

        static IEnumerable<DateTime> DeserializeDay(string js) {
            return startDateRegex.Matches(js)
                .Select(match => {
                    var group = match.Groups[1];
                    var parts = group.Value.Split(",").Select(x => int.Parse(x)).ToArray();
                    var date = new DateTime(parts[0], parts[1] + 1, parts[2], parts[3], parts[4], parts[5]);
                    return date;
                })
                .Distinct()
                .OrderBy(x => x);
        }

        static string ScrapeReservationGeneralInfo(HtmlDocument doc) {
            var generalInfo = doc
                .GetElementbyId(NewReservationFormId)?
                .SelectSingleNode("//div[contains(@class, 'generalInfo')]");

            var text = generalInfo?.InnerText.Trim() ?? string.Empty;

            return WebUtility.HtmlDecode(text);
        }

         static async Task<Dictionary<string, string>?> ScrapeFormValues(HttpResponseMessage response, string formId) {
            using var stream = await response.Content.ReadAsStreamAsync();
            var doc = LoadHtmlDocument(stream);
            return ScrapeFormValues(doc, formId);
        }

        static Dictionary<string, string>? ScrapeFormValues(HtmlDocument doc, string formId) {
            var result = doc.GetElementbyId(formId)?
                .SelectNodes("//input")
                .Select(elm => elm.Attributes)
                .Where(attrs => attrs.Contains("name"))
                .ToDictionary(attrs => attrs["name"].Value, attrs => attrs["value"]?.Value ?? string.Empty);

            return result;
        }

        static HtmlDocument LoadHtmlDocument(Stream stream) {
            var doc = new HtmlDocument();
            doc.Load(stream, htmlEncoding);
            return doc;
        }

        public static void Debug() {
            var downloads = "../../../Downloads/";
            var stream = File.OpenRead(downloads + "NewReservationFromBooking.html");
            var doc = LoadHtmlDocument(stream);

            WriteDebug("GENERAL INFO", ScrapeReservationGeneralInfo(doc));
            WriteDebug("FORM VALUES", ScrapeFormValues(doc, NewReservationFormId));
            WriteDebug("GET TIME PERIOD", DeserializeDay(File.ReadAllText(downloads + "getTimePeriod.js")));
        }

        static void WriteDebug(string name, object? value) {
            Console.WriteLine(name);
            Console.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine();
        }

        public void Dispose() {
            httpClient.Dispose();
        }
    }
}
