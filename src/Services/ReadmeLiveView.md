# SvcLiveView

> **NOTE:** This functionality is currently being provided manually through PS/Python scripts on a daily basis and this should be enabled ONLY after a discussion with grecoe to determine if that process is still ongoing.

LiveView data is used by ADME to determine what physical resources exist in our subscriptions, regardless of what Kusto says should be there.

This code is used to collect C1B, ADME and DCP instance information for the LiveView and Cleanup dashboards.

|Stage|Details|
|---|---|
|A|Retrieve all of the subscriptions for the given service in configuration ServiceTreeSettings.ServiceId. Filter out Production subs for now as this has only been tested with Non Production subscriptions.|
|B|For each subscription, get a list of all of the Resource Groups and then:|
||Using customized logic for ADME, break down the groups into ADME, C1B and DCP istances.|
||Collect the list of invalid instances.|
|F|Upload results to LiveView tables for use in dashboard reporting|
