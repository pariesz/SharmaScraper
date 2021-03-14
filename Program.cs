using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SharmaScraper {

    class Program {

        static async Task Main(string[] args) {
            // TESTING
            // SharmaClient.Debug(); return;

            var ctSource = new CancellationTokenSource();
            var config = new Configuration(args);

            using var client = new SharmaClient(); 
            await client.Login(config.GetEmail(), config.GetPassowrd(), ctSource.Token);

            // TESTING
            // await client.BookNext(new DateTime(2021, 3, 20, 12, 0, 0, DateTimeKind.Utc)); return;

            var reservations = await client.GetReservations(ctSource.Token);
            await WriteReservations(reservations, config.Output);
        }

        

        static async Task WriteReservations(IEnumerable<Reservation> reservations, string filePath) {
            var lines = reservations
                .OrderBy(x => x.Time).Select(x => $"{x.Time:ddd dd/MM hh:mm},{x.Info}")
                .ToList();

            lines.Insert(0, "Time,Info");

            await File.WriteAllLinesAsync(filePath, lines, Encoding.ASCII);
        }
    }
}
