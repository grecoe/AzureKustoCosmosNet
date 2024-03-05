# SvcADMECleanup

Executes a scan across a subscription, and using internal knowledge of how a product instance is created, determines a list of resources that are invalid. These are:

This service will attempt to cleanup invalid ADME and C1B instances from a subscription. An invalid instance is defined as:

- ADME Instances with 0 clusters and/or partitions.
- C1B with 0 clusters
- **DCP is not affected by this logic at this time**

|Stage|Details|
|---|---|
|A|Retrieve all of the subscriptions for the given service in configuration ServiceTreeSettings.ServiceId. Filter out Production subs for now as this has only been tested with Non Production subscriptions.|
|B|For each subscription, get a list of all of the Resource Groups and then:|
||Using customized logic for ADME, break down the groups into ADME, C1B and DCP istances.|
||Collect the list of invalid instances.|
|C|Look up the ADME instances (C1B and DCP do not register with the CP) in the ADME CosmosToKusto tables, and:|
||For each instance that is not registered, add it's resource groups directly to a delete list.|
||For each instance that IS registered, perform the next 3 steps.|
|D|Using the ADME environment provided by the registered instance, attempt to remove DNC C and A records from the appropriate DNS Zone.|
|E|Using the ADME environment provided by the registered instance, attempt to move all Instance and DataPartition records to a provisioning state of Deleted in the appropriate Cosmos database.|
|X|Add all instance groups from the previous 2 steps to the delete list.|
|B|With the full delete list, and assuming the ExecuteCleanup flag is true, delete all of the associated resource groups by:|
||Remove all locks|
||Remove any ADF IR for any ADF in the resource group.|
||Remove any Autoscale settings from the resource group.|
||Delete the Resource Group|
