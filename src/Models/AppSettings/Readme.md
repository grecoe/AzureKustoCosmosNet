# Appsettings

Exactly what it indicates, these are the objects used in the appsettings.json file and read for processing.

For specific service settings, look to the service readme file in the /Services folder.

This document describes, high level, what these settings are and what they mean.

## ServiceTreeSettings

This value indicates the Service Tree ID on where to search for non production subscriptions.

**The user of this application MUST have access to at least SOME of the subscriptions for any of the steps to be of any use.**

## ExecutionSettings

These are settings for each of the services. Each service recognizes the base settings and performs accordingly.

|Setting|Meaning|
|----|----|
|IsActive|Boolean flag indicating whether to run this service or not.|
|ExecuteCleanup|Boolean flag indicating that actual deletions should occur given whatever that service would be deleting.|
|RunContinuous|Whether the service should be a one and done, or run continuously. Generally, if running with this flag false, all executing services should be set to false.|
|TimeoutHours|If run continuously is true, how many hours should pass before running the service again.|

## EventLog

These settings should point to a Kusto cluster/db in which logging records for each service run will appear. The table creation settings are below, streaming must be enabled.

```bash
.create table CleanEventLog ( Timestamp:datetime, CorrelationId:string, Level:string, Service:string, Subscription:string, Message:string ,Data:string)

.alter table CleanEventLog policy streamingingestion enable
```

## KustoSettings

A series of other Kusto databases that are accessed by the services, mostly for reading except in the LiveView case. See the Models/Kusto/*DTO.cs files for table creation where needed.

## CosmosSettings

Cosmos database information for when we are cleaning up ADME instances. Make sure that they are flagged with a provisioning state of Deleted to prevent billing or other processes from thinking that the instance is still alive and well.

## DNS

DNS Zone information to either clean out DNS for an instance being deleted OR for general DNS cleanup (Dogfood/Staging only).
