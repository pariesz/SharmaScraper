﻿# Sharma Reservation Scraper / Automated Bookings

## Automated Bookings

When `LambdaEntryPoint` is executed it will book the next availability
7 days and 13 hours from the current time - when bookings are released.

## Deploying to AWS Lambda

* Run `deploy.ps1`.
* Set `email` and `password` environment variables.
* Setup a CloudWatch events rule to execute the lambda at the desired times.

## CloudWatch events cron expressions

`45 4 ? * TUE,THU *`

Every Tuesday and Thursday at 4:45AM GMT.
This will attempt to book the next session after 6:45PM CEST the same weekday of next week.

## Test lambda payload

```json
{
    "Date": "2021-03-25T18:45:00",
    "Mock": true
}
```
