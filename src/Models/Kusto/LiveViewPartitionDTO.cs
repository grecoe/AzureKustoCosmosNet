using SubscriptionCleanupUtils.Domain;
using SubscriptionCleanupUtils.Domain.Interface;

namespace SubscriptionCleanupUtils.Models.Kusto
{
    /*
        .create table PartitionView2 ( Timestamp:datetime, Subscription:string, Instance:string, Partition:string)

        .alter table PartitionView2  policy streamingingestion enable
     */

    internal class LiveViewPartitionDTO : LiveViewInstanceBaseDTO, IKustoRecord
    {
        public const string TABLE = "PartitionView2";

        public string Partition { get; set; }  =  string.Empty;

        public LiveViewPartitionDTO(DateTime timestamp, AzureSubscription subscription, ADMEResource resource)
        {
            this.Timestamp = timestamp;
            this.Subscription = subscription.ServiceSubscriptionsDTO.SubscriptionName;
            this.Instance = resource.InstanceName;
            this.Partition = resource.ResourceGroup.Name;
        }
        public string GetEntity()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
