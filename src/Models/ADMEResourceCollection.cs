//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models
{
    using SubscriptionCleanupUtils.Domain;

    internal class ADMEResourceCollection
    {
        public String ResourceType { get; set; } = String.Empty;
        public string InstanceName { get; set; }
        public ADMEResource Parent { get; set; }
        public List<ADMEResource> Clusters { get; set; } = new List<ADMEResource>();
        public List<ADMEResource> Partitions { get; set; } = new List<ADMEResource>();

        public bool IsValid
        {
            get
            {
                bool obValid = this.ResourceType == ADMESubscriptionParser.SubTypeOneBox && 
                    this.Clusters.Count > 0;
                bool instValid = this.ResourceType == ADMESubscriptionParser.SubTypeInstance && 
                    this.Clusters.Count > 0 &&
                    this.Partitions.Count > 0;
                bool dcpValid = this.ResourceType == ADMESubscriptionParser.SubTypeDCP;

                return obValid || instValid || dcpValid;
            }
        }
    }
}
