using System;
using System.Threading.Tasks;

namespace SharmaScraper {
    public class LambdaPayload {
        private static readonly TimeSpan DefaultTime = new TimeSpan(19, 0, 0); // 7PM
        private static readonly TimeSpan DefaultDelay = TimeSpan.FromMinutes(1);

        public DateTime? Date { get; set; }
        public string? Time { get; set; }
        public bool Mock { get; set; }
        public int Attempt { get; set; }
        public int MaxAttempts { get; set; } = 120;
        public string? Delay { get; set; }

        public DateTime GetDateTime() {
            var date = Date ?? GetDefaultDate();
            
            if (!string.IsNullOrEmpty(Time)) {
                date = date.Date + TimeSpan.Parse(Time);
            }

            return date;
        }

        public async Task<LambdaPayload> GetNextAttempt() {
            if (Attempt >= MaxAttempts) {
                throw new InvalidOperationException($"Exceeded {nameof(MaxAttempts)}: {MaxAttempts}");
            }

            await Task.Delay(GetDelayTimeSpan());

            return new LambdaPayload {
                Date = GetDateTime(),
                Attempt = ++Attempt,
                Mock = Mock,
                MaxAttempts = MaxAttempts,
                Delay = Delay
            };
        }

        private TimeSpan GetDelayTimeSpan() {
            if (Delay == null) {
                return DefaultDelay;
            }
            return TimeSpan.Parse(Delay);
        }

        private DateTime GetDefaultDate() {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SharmaClient.TZ.Value);
            var date = now.Add(SharmaClient.ReservationsReleasedSpan).Date;
            var dateTime = date + DefaultTime;
            return dateTime;
        }

        public override string ToString() {
            return $"date:{GetDateTime()} mock:{Mock} attempt:{Attempt}";
        }
    }
}
