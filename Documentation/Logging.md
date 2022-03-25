# Logging

## Umbraco logs

Many operations log to the standard Umbraco logs, visible in the back office at Settings > Logs.

Query the logs with `StartsWith(SourceContext, 'Stoolball')` to see just the logs from custom code. You can save this search by clicking the star icon in the UI.

## Set up Application Insights locally

To set up reporting to Application Insights from your development machine, copy `ApplicationInsights.xdt.template.config` to `Secret-InstrumentationKey.ApplicationInsights.local.xdt.config` and add your instrumentation key.
