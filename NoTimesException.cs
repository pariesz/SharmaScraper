using System;

namespace SharmaScraper {
    public class NoTimesException : Exception {
        public NoTimesException(string message) : base(message) {}
    }
}
