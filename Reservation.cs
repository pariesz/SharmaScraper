using System;
using System.Collections.Generic;

namespace SharmaScraper {
    public class Reservation {
        public DateTime Time { get; set; }
        public string? Info { get; set; }
        public Dictionary<string, string>? Form { get; set; }
    }
}
