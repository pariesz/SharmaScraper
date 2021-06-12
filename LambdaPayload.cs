using System;
using System.Threading.Tasks;

namespace SharmaScraper {
    public class LambdaPayload {
        private static readonly TimeSpan DefaultTime = new TimeSpan(19, 15, 0); // 7PM

        public DateTime? Date { get; set; }

        public string? Time { get; set; }

        public bool Mock { get; set; }

        // Each attempt increments the delay 1 sec so 120 sec max yields 
        // a 60sec average accross all attempts resulting in:
        // 120 attempts * 60 sec = 2 hour max runtime
        public int MaxDelaySeconds { get; set; } = 120;

        public int DelaySeconds { get; set; }

        public DateTime GetDateTime() {
            var date = Date ?? GetDefaultDate();
            
            if (!string.IsNullOrEmpty(Time)) {
                date = date.Date + TimeSpan.Parse(Time);
            }

            return date;
        }

        public async Task<LambdaPayload> GetNextAttempt() {
            if (DelaySeconds > MaxDelaySeconds) {
                throw new InvalidOperationException($"{nameof(DelaySeconds)} ({DelaySeconds}) Exceeded {nameof(MaxDelaySeconds)} ({MaxDelaySeconds})");
            }

            await Task.Delay(TimeSpan.FromSeconds(DelaySeconds));

            return new LambdaPayload {
                Date = GetDateTime(),
                Mock = Mock,
                MaxDelaySeconds = MaxDelaySeconds,
                DelaySeconds = DelaySeconds + 1
            };
        }

        private DateTime GetDefaultDate() {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SharmaClient.TZ.Value);
            var date = now.Add(SharmaClient.ReservationsReleasedSpan).Date;
            var dateTime = date + DefaultTime;
            return dateTime;
        }

        public override string ToString() {
            return $"{nameof(Date)}:{GetDateTime()} {nameof(Mock)}:{Mock} {nameof(DelaySeconds)}:{DelaySeconds}";
        }
    }
}
