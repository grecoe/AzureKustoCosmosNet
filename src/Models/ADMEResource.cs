//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models
{
    using SubscriptionCleanupUtils.Domain;

    internal class ADMEResource
    {
        public string InstanceName { get; set; }
        public string SubType { get; set; }
        public AzureResourceGroup ResourceGroup { get; set; }

        public ADMEResource(string name, string subType, AzureResourceGroup group)
        {
            this.InstanceName = name;
            this.SubType = subType;
            this.ResourceGroup = group;
        }
    }
}
