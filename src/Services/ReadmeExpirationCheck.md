# SvcExpirationCheck

> **WARNING:** The functionality to do cleanup in this service is too dangerous as there are too many unknowns about the structure of the subscriptions. **Do not add delete functionality to this service until an agreement has been made across the org.

This service, currently disabled, is meant to be used as an "expiration" tool on a subscription by

|Stage|Details|
|---|---|
|A|Retrieve all of the subscriptions for the given service in configuration ServiceTreeSettings.ServiceId. Filter out Production subs for now as this has only been tested with Non Production subscriptions.|
|B|For each subscription, get a list of all of the Resource Groups and then:|
||If an "expiration" tag is not present, add it to the resource group, this will also remove any locks on the resource group itself.|
||For all groups that did have the tag, report in the log (Kusto) which groups have expired.|
