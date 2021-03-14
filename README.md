# Sharma Reservation Scraper / Automated Bookings

## Automated Bookings

When `LambdaEntryPoint` is executed it will book the next availability
7 days and 13 hours from the current time - when bookings are released.

## Deploying to AWS Lambda

* Run `deploy.ps1`.
* Set `email` and `password` environment variables.
* Setup a CloudWatch events rule to execute the lambda at the desired times.

## CloudWatch events cron expressions

`45 5 ? * TUE,THU *`

Every Tuesday and Thursday at 5:45AM.
This will attempt to book the next session after 6:45PM the same weekday of next week.
