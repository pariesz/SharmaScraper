using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using System;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace SharmaScraper {
    public class LambdaEntryPoint {
        public async Task<object> FunctionHandlerAsync() {
            var config = new Configuration();
            var date = DateTime.UtcNow.Add(SharmaClient.ReservationsReleasedSpan);
            using (var client = new SharmaClient()) {
                await client.Login(config.GetEmail(), config.GetPassowrd());
                await client.BookNextReservation(date);
            }
            return new { Success = true };
        }
    }
}
