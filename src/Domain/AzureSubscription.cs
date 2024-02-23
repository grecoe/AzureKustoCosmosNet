//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using Azure.ResourceManager;
    using Azure.ResourceManager.Resources;
    using SubscriptionCleanupUtils.Models.Kusto;

    internal class AzureSubscription : RawAzureSubscription
    {
        private List<AzureResourceGroup> _ResourceGroupList { get; set; } = new List<AzureResourceGroup>();

        public ServiceSubscriptionsDTO ServiceSubscriptionsDTO { get; private set; }

        public AzureSubscription(ServiceSubscriptionsDTO subDto, ArmClient client) 
            : base(client)
        {
            this.ServiceSubscriptionsDTO = subDto;
        }

        public List<AzureResourceGroup> GetResourceGroups()
        {
            if (this._ResourceGroupList.Count == 0)
            {
                List<ResourceGroupResource> rgs = this.GetResourceGroupResources();

                foreach (var rawRg in rgs)
                {
                    this._ResourceGroupList.Add(new AzureResourceGroup(rawRg, this._client));
                }
            }
            
            return this._ResourceGroupList;
        }
    }
}
