//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models
{
    using SubscriptionCleanupUtils.Models.Kusto;

    internal class ADMEResourceCleanupResults
    {
        public List<ADMEResource> DeleteList { get; set; } = new List<ADMEResource>();
        public Dictionary<ADMEResourceCollection, List<ADMEResourcesDTO>> InvestigationList =
            new Dictionary<ADMEResourceCollection, List<ADMEResourcesDTO>>();
    }
}
