using System;
using Microsoft.Extensions.Configuration;

namespace SharmaScraper {
    public class Configuration {
        private string? email;
        private string? password;

        public string Output { get; }

        public string GetEmail() {
            while (string.IsNullOrEmpty(email)) {
                Console.Write("Username (email): ");
                email = Console.ReadLine();
            }
            return email;
        }

        public string GetPassowrd() {
            while (string.IsNullOrEmpty(password)) {
                Console.Write("Password: ");
                password = Console.ReadLine();
            }
            return password;
        }

        public Configuration(string[]? args = null) {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args ?? Array.Empty<string>())
                .Build();

            email = configuration.GetSection("email").Value;
            password = configuration.GetSection("password").Value;
            Output = configuration.GetSection("output").Value ?? "./results.csv";
        }
    }
}
