//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using Azure.Core;
    using Azure.ResourceManager;
    using Azure.ResourceManager.DataFactory;
    using Azure.ResourceManager.Monitor;
    using Azure.ResourceManager.Resources;

    internal class RawResourceGroup
    {
        private readonly ResourceGroupResource _resourceGroup;
        private readonly ArmClient _client;

        public string Id
        {
            get
            {
#pragma warning disable CS8603 
                return _resourceGroup.Data.Id;
#pragma warning restore CS8603 
            }
        }

        public string Name
        {
            get
            {
                return _resourceGroup.Data.Name;
            }
        }

        public AzureLocation Location
        {
            get
            {
                return _resourceGroup.Data.Location;
            }
        }

        public Dictionary<string, string> Tags = new Dictionary<string, string>();

        public string ManagedBy
        {
            get
            {
                string managedBy = _resourceGroup.Data.ManagedBy;
                if (string.IsNullOrEmpty(managedBy) == false)
                {
                    if (managedBy.StartsWith("/") == false)
                    {
                        managedBy = string.Format("/{0}", managedBy);
                    }
                }
                return managedBy;
            }
        }

        public RawResourceGroup(ResourceGroupResource resourceGroup, ArmClient client)
        {
            _client = client;
            _resourceGroup = resourceGroup;
            this.Tags = this._resourceGroup.Data.Tags.ToDictionary();
        }

        public bool UpdateTags()
        {
            bool returnValue = false;
#pragma warning disable CS0168 
            try
            {
                var resp = this._resourceGroup.SetTags(this.Tags);
                returnValue = resp.GetRawResponse().Status == 200;
            }
            catch (Azure.RequestFailedException ex)
            {
                // Group is likely locked and we ignore for now
            }
            catch(Exception ex)
            {
                // We don't have a handy logger here, but setting tags is not a show stopper.
            }
#pragma warning restore CS0168 
            return returnValue;
        }

        public void Delete(bool validateResources=true)
        {
            // Locked group can't be deleted
            this.RemoveLocks();

            // Certain resources are known problems (ADF, autoscale, networking) so remove them
            this.RemoveProblematicResources(validateResources);

            // Finally, delete the resource group (or try to)
            this._resourceGroup.Delete(Azure.WaitUntil.Started);
        }

        public List<RawResource> GetResources()
        {
            List<RawResource> returnResources = new List<RawResource>();
            var genericresourceList = _resourceGroup.GetGenericResources();
            foreach (GenericResource gr in genericresourceList)
            {
                returnResources.Add(new RawResource(gr));
            }
            return returnResources;
        }

        public void RemoveLocks()
        {
            var managementLocks = _resourceGroup.GetManagementLocks();
            foreach (ManagementLockResource ml in managementLocks)
            {
                // Data.Name is what we're working on
                ml.Delete(Azure.WaitUntil.Completed);
            }
        }
    
        private void RemoveProblematicResources(bool validateResources)
        {
            // We are going to want to validate most of the time. Need a better solution and 
            // also how to clear out the network resources, but this does ADF and Autoscaling. 
            if (validateResources == true)
            {
                List<RawResource> resources = this.GetResources();

                foreach (RawResource resource in resources)
                {
                    if (resource.ResourceType == new ResourceType("microsoft.datafactory/factories"))
                    {
                        DataFactoryResource df = this._client.GetDataFactoryResource(new ResourceIdentifier(resource.Id));
                        var runtimes = df.GetDataFactoryIntegrationRuntimes();
                        foreach (var runtime in runtimes)
                        {
                            runtime.Delete(Azure.WaitUntil.Completed);
                        }
                    }
                    else if (resource.ResourceType == new ResourceType("microsoft.insights/autoscalesettings"))
                    {
                        var autoScale = this._client.GetAutoscaleSettingResource(new ResourceIdentifier(resource.Id));
                        autoScale.Delete(Azure.WaitUntil.Started);
                    }
                }
            }
        }
    }
}
