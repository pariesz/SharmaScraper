using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using System;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace SharmaScraper {
    public class LambdaEntryPoint {
        public async Task<object> FunctionHandlerAsync(LambdaPayload? payload) {
            var config = new Configuration();
            
            var date = payload?.Date ?? DateTime.UtcNow.Add(SharmaClient.ReservationsReleasedSpan);
            var mock = payload?.Mock ?? false;

            using (var client = new SharmaClient()) {
                await client.Login(config.GetEmail(), config.GetPassowrd());
                await client.BookNextReservation(date, mock);
            }

            return new { Success = true };
        }
    }
}
