//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using Azure.Core;
    using Azure.ResourceManager.Resources;

    internal class RawResource
    {
        private readonly GenericResource _resource;

        // Name-string, Tags (like above), ResourceType Azure.Core.resourceType, Location Azure.Core.AzureLocation
        public string Id
        {
            get
            {
                return _resource.Data.Id.ToString();
            }
        }

        public string Name
        {
            get
            {
                return _resource.Data.Name;
            }
        }

        public ResourceType ResourceType
        {
            get
            {
                return _resource.Data.ResourceType;
            }
        }

        public AzureLocation Location
        {
            get
            {
                return _resource.Data.Location;
            }
        }

        public Dictionary<string, string> Tags
        {
            get
            {
                return _resource.Data.Tags.ToDictionary();
            }
        }
        public RawResource(GenericResource resource)
        {
            _resource = resource;
        }
    }
}
