# Sharma Reservation Scraper / Automated Bookings

## Automated Bookings

When `LambdaEntryPoint` is executed it will book the next availability
7 days from running at the specified time (Default: 19:15).

## Requirements
* .NET Core 3.1 SDK: https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-3.1.410-windows-x64-installer
* Configure you AWS profile: https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-quickstart.html
* Install the Lambda .NET Core CLI: https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/lambda-cli-publish.html

## Deploying to AWS Lambda

1. Deploy the lambda function setting the `email` and `password` `environment-variables` to your sharma login: https://aws.amazon.com/blogs/developer/deploying-net-core-aws-lambda-functions-from-the-command-line/

```powershell
dotnet lambda deploy-function `
    --region "eu-west-3" `
    --configuration "Release" `
    --framework "netcoreapp3.1" `
    --function-description "Automated sharma climbing gym bookings" `
    --function-runtime "dotnetcore3.1" `
    --function-handler "SharmaScraper::SharmaScraper.LambdaEntryPoint::FunctionHandlerAsync" `
    --function-name "SharmaScraper" `
    --function-timeout 900 `
    --function-memory-size 256
    --environment-variables "email={email};password={password}"
```

3. Setup a CloudWatch events rule to execute the lambda at the desired times:

```powershell
aws events put-rule `
    --region "eu-west-3" `
    --name "SharmaScraper-test" `
    --schedule-expression "cron(45 4 ? * TUE,THU *)" `
    --description "Book climbing session every TUE and THU at 7:15PM" `
    --state ENABLED

aws events put-targets `
    --region "eu-west-3" `
    --rule "SharmaScraper" `
    --targets "Id"="1","Arn"="{lambda ARN from step 1}"
```

**NOTE:** The cron expression `45 4 ? * TUE,THU *` will run the lambda every Tuesday and Thursday at 4:45AM GMT.
This will attempt to book the 7:15PM CEST session of the same weekday of next week.  This does not account
for daylight savings.

Use the target payload to configure the desired time you would like to book:

```json
{ 
    "Time": "19:15:00"
}
```

## Test lambda payload

```json
{
  "Date": "2021-03-25",
  "Time": "19:15:00",
  "Mock": true
}
```
