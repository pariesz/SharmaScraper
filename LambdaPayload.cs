using System;

namespace SharmaScraper {
    public class LambdaPayload {
        public static readonly TimeSpan DefaultTime = new TimeSpan(19, 0, 0); // 7PM

        public DateTime? Date { get; set; }
        public string? Time { get; set; }
        public bool Mock { get; set; }
        public int Attempt { get; set; }

        public DateTime GetDateTime() {
            var date = Date ?? GetDefaultDate();
            
            if (!string.IsNullOrEmpty(Time)) {
                date = date.Date + TimeSpan.Parse(Time);
            }

            return date;
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
