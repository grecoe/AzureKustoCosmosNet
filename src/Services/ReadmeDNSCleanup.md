# SvcDNSCleanup

DNS cleanup can be something that can cause some bigger issues in the organization and a reserved approach has been taken here, even though we are pretty confident there are thousands of dead records in the Dogfood/Staging DNS Zone.

The tasks are broken down into 3 distinct stages, each with thier own settings in ExecutionSettings.DNSCleanupService that extend the base service settings. The following sections explain what each flag means.

The DNS that will be used is in the settings file under DNS.Environments where the sub item has "Dogfood" in it's own Environments.

```json
      "ResolveCnameOption": true,
      "UnmatchedARecordsOption": true,
      "FilterITInstancesOption": true
```

## Name Matching

Throughout the following steps, name matching is mentioned. Names are determined by taking the record name and producing a common name that would be associated with other CNAME records (backup entries) and A records (which may also be backups).

The following example on how names are determined.

```bash
CNAME::name = aayushibcdrbug3bkp ==> aayushibcdrbug3
A::name = aayushibcdrbug3.privatelink.bkp ==> aayushibcdrbug3
```

## ResolveCnameOption

Removing unresolvable DNS entries helps with security in preventing subdomain takeovers.

- Retrieve all DNS CNAME and A records from the DNS server.
- Try and resolve each CNAME.cname with DNS, if it fails
  - Add the CNAME record to a list to delete
  - Search for all A records where there is a name match with the CNAME record and add them to the delete list.
- Delete all records found.

## UnmatchedARecordsOption

These will become fewer over time, but records can get left behind when an instance is deleted by hand OR the delete API fails.

- Search for all A records in which the name has a "." separator. These will typically be:
  - NAME.privatelink.... , NAME.internal.... with potentially backups as well.
- For any of these types of A records, try and match the name to a CNAME record.
- For any A record in this pattern that does not match a CNAME record, add it to the list to delete.
- Delete all records found.

## FilterITInstancesOption

There are literally hundreds of itXXX instances created monthly. Hundreds of these have left behind abandoned records in DNS. This step simple searches ALL CNAME and A records, trying to match the name pattern "itNN...." to record names.

All records matching the pattern, regardless of class type, are added to a list and then deleted.
