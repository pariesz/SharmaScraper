using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using System;
using System.Text.Json;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace SharmaScraper {
    public class LambdaEntryPoint {

        public async Task<object> FunctionHandlerAsync(LambdaPayload? payload, ILambdaContext lambdaContext) {
            if (payload == null) {
                payload = new LambdaPayload();
            }

            LambdaLogger.Log(payload.ToString() + Environment.NewLine);

            var config = new Configuration();
            var email = config.GetEmail();
            var password = config.GetPassowrd();
            var dateTime = payload.GetDateTime();

            using (var client = new SharmaClient()) {
                await client.Login(email, password);

                try {
                    await client.BookNextReservation(dateTime, payload.Mock);

                } catch (NoTimesException ex) {
                    LambdaLogger.Log(ex.Message);
                    var invokePayload = await payload.GetNextAttempt();
                    await InvokeSelf(invokePayload, lambdaContext.FunctionName);

                } catch (Exception ex) {
                    LambdaLogger.Log(ex.Message);
                    return new { Success = false };
                }
            }

            return new { Success = true };
        }

        private static async Task InvokeSelf(LambdaPayload payload, string functionName) {
            var payloadJson = JsonSerializer.Serialize(payload);

            using var client = new AmazonLambdaClient();

            await client.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest {
                FunctionName = functionName,
                InvocationType = InvocationType.Event,
                Payload = payloadJson
            });
        }
    }
}
