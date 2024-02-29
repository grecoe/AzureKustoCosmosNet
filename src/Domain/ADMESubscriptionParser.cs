//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using SubscriptionCleanupUtils.Models;

    internal class ADMESubscriptionParser
    {
        public const string InstanceGroup = "compute-rg-";
        public const string ClusterGroup = "mc_compute-rg-";
        public const string PartitionGroup = "datapartition-rg-";
        public const string OneBoxGroup = "cloud-onebox";
        public const string DevControlPlaneGroup = "dev-controlplane";

        public const string SubTypeInstance = "Instance";
        public const string SubTypeCluster = "Cluster";
        public const string SubTypePartition = "Partition";
        public const string SubTypeOneBox = "OneBox";
        public const string SubTypeDCP = "DCP";

        public AzureSubscription AzureSubscription { get; set; }

        public ADMESubscriptionParser(AzureSubscription azureSubscription)
        {
            this.AzureSubscription = azureSubscription;
        }

        /// <summary>
        /// Abandoned resources are rare, but they occur. An abandoned resource will likely be only
        /// data partitions since a cluster cannot be headless if it's managing group is deleted. 
        /// 
        /// While this WAS detected in the ODD1 sub, it's very rare, but more work will need to be
        /// done to clean these up in the future.
        /// </summary>
        /// <param name="instanceCollection">Collection likely coming from CollectInstances() in
        /// this class.</param>
        /// <returns>A list of ADMEResource objects that should be deleted.</returns>
        public List<ADMEResource> GetAbandonedResources(List<ADMEResourceCollection> instanceCollection)
        {
            List<ADMEResource> abandoned = new  List<ADMEResource>();

            List<ADMEResource> resources = this.ParseInstanceData();
            List<ADMEResource> potentials = resources
                .Where(x =>
                    x.SubType == ADMESubscriptionParser.SubTypeCluster ||
                    x.SubType == ADMESubscriptionParser.SubTypePartition
                    )
                .ToList();

            List<string> instanceNames = instanceCollection.Select(x=> x.InstanceName).ToList();
            foreach (var rsrc in potentials)
            {
                if( !instanceNames.Contains(rsrc.InstanceName) )
                {
                    abandoned.Add(rsrc);
                }
            }

            return abandoned;
        }

        /// <summary>
        /// Gets all of the ADME instance data for a subscription and peices them together. 
        /// 
        /// Only DCP, C1B and Instances are viable targets for this connection with 
        /// 
        /// Instance -> Should have a cluster AND at least one data partition.
        /// C1B -> Should have a cluster but will never have a data partition
        /// DCP -> Will have neither a Cluster or data partition
        ///
        /// Mapping of sub resources (cluster, partition) to a parent instance is done with name
        /// mapping of the instance. Nothing more nothing less.
        /// </summary>
        /// <returns>A list of all resources found in the subscription.</returns>
        public List<ADMEResourceCollection> CollectInstances()
        {
            List<ADMEResourceCollection> returnCollection = new List<ADMEResourceCollection>();

            // Get the data
            List<ADMEResource> resources = this.ParseInstanceData();

            // Now bucketize it
            List<ADMEResource> rsInst = resources
                .Where(x => 
                    x.SubType == ADMESubscriptionParser.SubTypeInstance ||
                    x.SubType == ADMESubscriptionParser.SubTypeOneBox ||
                    x.SubType == ADMESubscriptionParser.SubTypeDCP
                    )
                .ToList();

            foreach(var inst in rsInst)
            {
                ADMEResourceCollection fullResource = new ADMEResourceCollection();
                fullResource.InstanceName = inst.InstanceName;
                fullResource.ResourceType = inst.SubType;
                fullResource.Parent = inst;

                fullResource.Clusters = resources
                    .Where(x => x.SubType == ADMESubscriptionParser.SubTypeCluster && x.InstanceName == inst.InstanceName)
                    .ToList();

                fullResource.Partitions = resources
                    .Where(x => x.SubType == ADMESubscriptionParser.SubTypePartition && x.InstanceName == inst.InstanceName)
                    .ToList();

                returnCollection.Add(fullResource);
            }

            return returnCollection;
        }

        /// <summary>
        /// Parse down all resource groups from a subscription to identify the ADME specific
        /// resource groups for an ADME Instance, C1B or DCP.
        /// 
        /// Bundles the underlying Azure Rsource Group into an ADME ResourceGroup and passes these 
        /// back for futher processing. 
        /// </summary>
        /// <returns>List of actual ADME resources in a subscrption.</returns>
        private List<ADMEResource> ParseInstanceData()
        {
            List<ADMEResource> returnResources = new List<ADMEResource>();

            List<AzureResourceGroup> groups = this.AzureSubscription.GetResourceGroups();
            foreach (AzureResourceGroup group in groups)
            {
                string groupName = group.Name.ToLower();
                string subType = string.Empty;

                if( groupName.StartsWith(ADMESubscriptionParser.InstanceGroup))
                {
                    subType = ADMESubscriptionParser.SubTypeInstance;
                }
                else if( groupName.Contains(ADMESubscriptionParser.OneBoxGroup))
                {
                    if( !String.IsNullOrEmpty(group.ManagedBy))
                    {
                        subType = ADMESubscriptionParser.SubTypeCluster;
                    }
                    else
                    {
                        subType = ADMESubscriptionParser.SubTypeOneBox;
                    }
                }
                else if(groupName.StartsWith(ADMESubscriptionParser.ClusterGroup))
                {
                    subType = ADMESubscriptionParser.SubTypeCluster;
                }
                else if (groupName.StartsWith(ADMESubscriptionParser.PartitionGroup))
                {
                    subType = ADMESubscriptionParser.SubTypePartition;
                }
                else if (groupName.Contains(ADMESubscriptionParser.DevControlPlaneGroup ))
                {
                    subType = ADMESubscriptionParser.SubTypeDCP;
                }

                if ( !string.IsNullOrEmpty(subType))
                {
                    string instanceName = string.Empty;
                    string[] parts = groupName.Split("-");
                    if ( groupName.Contains(ADMESubscriptionParser.OneBoxGroup) ||
                        groupName.EndsWith(ADMESubscriptionParser.DevControlPlaneGroup) )
                    {
                        if (parts[0].Contains("_"))
                        {
                            instanceName = parts[0].Split("_")[1];
                        }
                        else
                        {
                            instanceName = parts[0];
                        }
                    }   
                    else
                    {
                        instanceName = parts[2];
                    }

                    if( !string.IsNullOrEmpty(instanceName))
                    {
                        returnResources.Add(new ADMEResource(instanceName, subType, group));
                    }
                }
            }

            return returnResources;
        }
    }
}
