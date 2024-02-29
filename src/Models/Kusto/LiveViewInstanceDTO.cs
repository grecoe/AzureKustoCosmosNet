using SubscriptionCleanupUtils.Domain;
using SubscriptionCleanupUtils.Domain.Interface;

namespace SubscriptionCleanupUtils.Models.Kusto
{
    /*
        .create table InstanceView2 ( Timestamp:datetime, Subscription:string, Instance:string, Group:string)

        .alter table InstanceView2 policy streamingingestion enable

     */

    internal class LiveViewInstanceDTO : LiveViewInstanceBaseDTO, IKustoRecord
    {
        public const string TABLE = "InstanceView2";

        public string Group { get; set; } = string.Empty;

        public LiveViewInstanceDTO(DateTime timestamp, AzureSubscription subscription, ADMEResource resource)
        {
            this.Timestamp = timestamp;
            this.Subscription = subscription.ServiceSubscriptionsDTO.SubscriptionName;
            this.Instance = resource.InstanceName;
            this.Group= resource.ResourceGroup.Name;
        }

        public string GetEntity()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
