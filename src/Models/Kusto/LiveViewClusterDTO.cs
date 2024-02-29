using SubscriptionCleanupUtils.Domain;
using SubscriptionCleanupUtils.Domain.Interface;

namespace SubscriptionCleanupUtils.Models.Kusto
{
    /*
        .create table ClusterView2 ( Timestamp:datetime, Subscription:string, Instance:string, Cluster:string)

        .alter table ClusterView2 policy streamingingestion enable
     */
    internal class LiveViewClusterDTO : LiveViewInstanceBaseDTO, IKustoRecord
    {
        public const string TABLE = "ClusterView2";

        public string Cluster { get; set; } = string.Empty;

        public LiveViewClusterDTO(DateTime timestamp, AzureSubscription subscription, ADMEResource resource)
        {
            this.Timestamp = timestamp;
            this.Subscription = subscription.ServiceSubscriptionsDTO.SubscriptionName;
            this.Instance = resource.InstanceName;
            this.Cluster = resource.ResourceGroup.Name;
        }

        public string GetEntity()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
