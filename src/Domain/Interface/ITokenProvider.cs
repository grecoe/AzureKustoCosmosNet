//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain.Interface
{
    using Azure.Core;
    using Azure.ResourceManager;

    public interface ITokenProvider
    {
        public TokenCredential Credential { get; }
        public ArmClient GetAzureArmClient(string subscriptionId);
    }
}
