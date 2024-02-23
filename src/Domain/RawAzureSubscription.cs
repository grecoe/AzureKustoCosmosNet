//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using Azure.ResourceManager;
    using Azure.ResourceManager.Resources;

    internal class RawAzureSubscription
    {
        protected readonly ArmClient _client;
        private readonly SubscriptionResource _subscription;
        public RawAzureSubscription(ArmClient client)
        {
            _client = client;
            _subscription = _client.GetDefaultSubscription();
        }

        public List<ResourceGroupResource> GetResourceGroupResources()
        {
            // ResourceGroupResource - Data.Id, Data.Name, Data.Tags (dictionary string,string), Data.ManagedBy, Data.Location (AzureLocation)
            // ManagedBy == subscritions/...... missing first / for a full ID. 
            ResourceGroupCollection resourceGroups = _subscription.GetResourceGroups();
            return resourceGroups.ToList();
        }
    }
}
