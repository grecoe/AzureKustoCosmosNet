//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models
{
    using SubscriptionCleanupUtils.Domain;

    internal class GroupExpirationResult
    {
        public List<string> TaggedGroups { get; set; } = new List<string>();
        public List<string> TagFailureGroups { get; set; } = new List<string>();
        public List<string> ExpiredButProtectedGroups{ get; set; } = new List<string>();
        public List<string> PreviousDeleteAttemptGroups{ get; set; } = new List<string>();
        public List<AzureResourceGroup> ExpiredGroups { get; set; } = new List<AzureResourceGroup>();
    }
}
