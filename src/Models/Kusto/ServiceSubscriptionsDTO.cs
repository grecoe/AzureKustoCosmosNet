//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models.Kusto
{
    using SubscriptionCleanupUtils.Models.AppSettings;

    internal class ServiceSubscriptionsDTO
    {

        public const string QUERYHEADER = @"
            // Service Information
            // ID: 461b68e8-f849-47b4-8b85-3182a2def8a7
            // Name: Azure Data Manager for Energy
            let GetStatus = (index:int) {
                case(index == 1, 'Active', index== 0,'Inactive', '')
            };
            let GetEnvironment = (index:int) {
                case(index == 1, 'NonProd', index== 0,'Prod', '')
            };";

        public const string QUERY = @"
            cluster('https://servicetreepublic.westus.kusto.windows.net/').database('Shared').ServiceTree_ServiceHierarchy_Snapshot 
            | where Status < 2 and Id == '{0}'
            | lookup kind=leftouter (
                cluster('https://servicetreepublic.westus.kusto.windows.net/').database('Shared').ServiceTree_AzureSubscription_Snapshot 
                     | where Status < 2) on $left.InternalId == $right.ServiceInternalId
            | project SubscriptionId, SubscriptionName, Environment=GetEnvironment(Environment), Status=GetStatus(Status)
        ";

        public string SubscriptionId { get; set; }
        public string SubscriptionName { get; set; }
        public string Environment { get; set; }
        public string Status { get; set; }

        public static string GetServiceTreeQuery(ServiceTreeSettings settings)
        {
            string body = string.Format(QUERY, settings.ServiceId);
            return QUERYHEADER + body;
        }
    }
}
