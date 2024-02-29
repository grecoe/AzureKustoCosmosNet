using SubscriptionCleanupUtils.Domain;
using SubscriptionCleanupUtils.Domain.Interface;

namespace SubscriptionCleanupUtils.Models.Kusto
{
    /*
        .create table DCPView2 ( Timestamp:datetime, Subscription:string, User:string, Group:string, SubType:string, Version:string)

        .alter table DCPView2 policy streamingingestion enable
     */
    internal class LiveViewDCPDTO : LiveViewBaseDTO, IKustoRecord
    {
        public const string DCPGlobal = "Global";
        public const string DCPUser = "User";
        public const string TABLE = "DCPView2";

        public string User { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string SubType { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public LiveViewDCPDTO(DateTime timestamp, AzureSubscription subscription, ADMEResource resource)
        {
            this.Timestamp = timestamp;
            this.Subscription = subscription.ServiceSubscriptionsDTO.SubscriptionName;
            this.Group = resource.ResourceGroup.Name;

            if(this.Group.StartsWith(ADMESubscriptionParser.DevControlPlaneGroup))
            {
                this.SubType = DCPGlobal;
            }
            else
            {
                this.SubType = DCPUser;
                this.User = resource.InstanceName;
                string? version;
                resource.ResourceGroup.Tags.TryGetValue("VERSION", out version);
                this.Version = version ?? string.Empty;
            }
        }
        public string GetEntity()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

    }
}
