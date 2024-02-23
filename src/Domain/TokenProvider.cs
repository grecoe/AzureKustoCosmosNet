//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using Azure.Core;
    using Azure.Identity;
    using Azure.ResourceManager;
    using SubscriptionCleanupUtils.Domain.Interface;

    /// <summary>
    /// The provider that holds the authentication information for the process. 
    /// 
    /// Currently written to just run locally using personal credentials, which will
    /// also work with a Service Managed Identity. 
    /// 
    /// However, if any custom credential is to be used, an SP for example, then it 
    /// will need to be updated to create the appropriate TokenCredential. 
    /// 
    /// Thiere is ONE created for all services currently and passed into the services from
    /// the .NET Dependency Injection.
    /// </summary>
    internal class TokenProvider : ITokenProvider
    {
        public TokenCredential Credential { get; private set; }

        public TokenProvider()
        {
            Credential = new DefaultAzureCredential();
        }

        public ArmClient GetAzureArmClient(string subscriptionId)
        {
            ArmClient client = new ArmClient(Credential, subscriptionId);
            return client;
        }
    }
}
