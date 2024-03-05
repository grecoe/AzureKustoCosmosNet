# AzureKustoCosmosNet

The services included are listed below with their intended purpose. All configuration is done through the appsettings.json file and passed along to each running service.

- Utilizes Azure.Identity, but currently the one and only ITokenProvider is backed by DefaultAzureCredential, which can work if deployed as a service who has an identity. If using a user/password/secret combination, this will need to be modified. 
- Uses the new Azure.ResourceManager libraries
- Connects to and uses both Kusto (various sources) and Cosmos (potential various sources).
- All services retrieve a list of Subscriptions for the given Service Tree ID in which to act upon.

> **NOTE:** Implied that caller (ITokenProvider) has the ability to work with identified Kusto and  Cosmos clusters AND access the list of returned subscriptions from Service Tree.

> **NOTE:** This project comes complete with an ADX Dashboard, look in the dasbhoard folder for more information.

## General Settings

Go to the **Models/AppSettings** folder and read the Readme.md file for general setting information.

For detailed service settings go to **Services**

## Services Available

Go to the **Services** folder and read the various Readme.md files.

## Kusto Table Management

For any table you are streaming to you

1. Portal to ADX Cluster -> Configuration -> Streaming ingestion : enable
2. Update each table as follows, in Kusto Explorer
    1. .alter table **TABLENAME** policy streamingingestion enable

Further, it's always a good idea to backup your table data if you are trying something new
so you don't blow stuff up.

> .create table **backuptable** ...spec from original table...
> .set-or-append **backuptable** <| **existing_table** | where ...

Then when your satisfied that the table data is valid, you can drop the backup.

### Event Log

> .create table CleanEventLog ( Timestamp:datetime, CorrelationId:string, Level:string, Service:string, Subscription:string, Message:string ,Data:string)

> .alter table CleanEventLog policy streamingingestion enable

### Live View Tables

#### InstanceView

> .create table InstanceView2 ( Timestamp:datetime, Subscription:string, Instance:string, Group:string)

> .alter table InstanceView2 policy streamingingestion enable

#### ClusterView

> .create table ClusterView2 ( Timestamp:datetime, Subscription:string, Instance:string, Cluster:string)

> .alter table ClusterView2 policy streamingingestion enable

#### PartitionView

> .create table PartitionView2 ( Timestamp:datetime, Subscription:string, Instance:string, Partition:string)

> .alter table PartitionView2  policy streamingingestion enable

#### DCPView

> .create table DCPView2 ( Timestamp:datetime, Subscription:string, User:string, Group:string, SubType:string, Version:string)

>.alter table DCPView2 policy streamingingestion enable
