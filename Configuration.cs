using System;
using Microsoft.Extensions.Configuration;

namespace SharmaScraper {
    public class Configuration {
        private string? email;
        private string? password;

        // 977 is Bono (10 pack)
        // 969 is Bono Prepago (30 day normal)
        private string? paymentTypeId;

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

        public string GetPaymentTypeId() {
            while(string.IsNullOrEmpty(paymentTypeId)) {
                Console.Write("Payment Type Id:");
                paymentTypeId = Console.ReadLine();
            }
            return paymentTypeId;
        }

        public Configuration(string[]? args = null) {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args ?? Array.Empty<string>())
                .Build();

            email = configuration.GetSection("email").Value;
            password = configuration.GetSection("password").Value;
            paymentTypeId = configuration.GetSection("paymentTypeId").Value ?? "977";
            Output = configuration.GetSection("output").Value ?? "./results.csv";
        }
    }
}
