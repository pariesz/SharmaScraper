using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using System;
using System.Text.Json;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace SharmaScraper {
    public class LambdaEntryPoint {

        public const int MaxRetries = 24; // Try for 4 hours: 10min delay * 6 * 4

        public async Task<object> FunctionHandlerAsync(LambdaPayload? payload, ILambdaContext lambdaContext) {
            if (payload == null) {
                payload = new LambdaPayload();
            }

            LambdaLogger.Log(payload.ToString() + Environment.NewLine);

            var dateTime = payload.GetDateTime();
            var config = new Configuration();
            using (var client = new SharmaClient()) {
                await client.Login(config.GetEmail(), config.GetPassowrd());

                try {
                    await client.BookNextReservation(dateTime, payload.Mock);

                } catch (NoTimesException ex) {
                    LambdaLogger.Log(ex.Message);

                    if (payload.Attempt <= MaxRetries) {
                        await Task.Delay(TimeSpan.FromMinutes(10));

                        var invokePayload = new LambdaPayload {
                            Date = dateTime,
                            Attempt = ++payload.Attempt,
                            Mock = payload.Mock
                        };

                        await InvokeSelf(invokePayload, lambdaContext.FunctionName);
                        return new { Success = false };
                    }

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
