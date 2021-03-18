# Sharma Reservation Scraper / Automated Bookings

## Automated Bookings

When `LambdaEntryPoint` is executed it will book the next availability
7 days from running at the specified time (Default: 19:00).

## Deploying to AWS Lambda

* Run `deploy.ps1`.
* Set `email` and `password` environment variables.
* Setup a CloudWatch events rule to execute the lambda at the desired times.

## CloudWatch events cron expressions

`45 4 ? * TUE,THU *`

Every Tuesday and Thursday at 4:45AM GMT.
This will attempt to book the 7PM session of the same weekday of next week.

## Test lambda payload

```json
{
  "Date": "2021-03-25",
  "Time": "19:00:00",
  "Mock": true
}
```
