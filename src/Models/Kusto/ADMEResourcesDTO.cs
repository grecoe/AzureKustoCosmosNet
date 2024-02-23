//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models.Kusto
{
    internal class DataPartitionDTO
    {
        public string DataPartition { get; set; }
        public string ManagedRG { get; set; }
        public string ProvisioningState { get; set; }
    }

    internal class ADMEResourcesDTO
    {
        public static string[] ActiveStates = new string[] { "Succeeded", "Creating", "Updating" };


        public const string QUERY = @"
          let instances = materialize(
          union cluster('https://oepcpprodadx.eastus.kusto.windows.net').database('CosmosToKusto').OEPResource,
                cluster('https://oepcpprodadx.eastus.kusto.windows.net').database('CosmosToKusto').OEPResource2,
                cluster('https://oepcpdfadx.eastus.kusto.windows.net').database('CosmosToKusto').OEPResource,
                cluster('https://oepcpdfadx.eastus.kusto.windows.net').database('CosmosToKusto').OEPResource2
          | distinct resourceName, oepResourceId, provisioningState, Environment, dnsName, OEPRelease
          );
          let dataPartitions = materialize(
          union cluster('https://oepcpprodadx.eastus.kusto.windows.net').database('CosmosToKusto').DataPartitions,
                cluster('https://oepcpprodadx.eastus.kusto.windows.net').database('CosmosToKusto').DataPartitions2,
                cluster('https://oepcpdfadx.eastus.kusto.windows.net').database('CosmosToKusto').DataPartitions,
                cluster('https://oepcpdfadx.eastus.kusto.windows.net').database('CosmosToKusto').DataPartitions2
          | distinct Environment, Region, oepResourceId, dataPartitionName, managedRGName, provisioningState
          );
          let computeResources = materialize(
          union cluster('https://oepcpprodadx.eastus.kusto.windows.net').database('CosmosToKusto').DPComputeResources,
                cluster('https://oepcpprodadx.eastus.kusto.windows.net').database('CosmosToKusto').DPComputeResources2,
                cluster('https://oepcpdfadx.eastus.kusto.windows.net').database('CosmosToKusto').DPComputeResources,
                cluster('https://oepcpdfadx.eastus.kusto.windows.net').database('CosmosToKusto').DPComputeResources2
          | distinct Environment, Region, ResourceId=tostring(oepResourceId), computeRGName, provisioningState
          );
          instances
          | join kind=leftouter dataPartitions on oepResourceId
          | extend Partition = pack( 'DataPartition', dataPartitionName, 'ManagedRG', managedRGName, 'ProvisioningState', provisioningState1)
          | summarize Partitions=make_set(Partition) by 
                    InstanceName=resourceName, 
                    ResourceId=tostring(oepResourceId), 
                    ProvisioningState=provisioningState, 
                    DNSName=dnsName, 
                    Version=OEPRelease
          | join kind=leftouter computeResources on ResourceId          
          | project InstanceName,
                    Environment,
                    ResourceId, 
                    ProvisioningState, 
                    DNSName, 
                    Version, 
                    ComputeRg=computeRGName, 
                    ComputeState=provisioningState, 
                    Partitions=tostring(Partitions)
        ";

        public string InstanceName { get; set; }
        public string Environment { get; set; }
        public string ResourceId { get; set; }
        public string ProvisioningState { get; set; }
        public string DNSName { get; set; }
        //public List<DataPartitionDTO> Partitions { get; set; } = new List<DataPartitionDTO>();
        public string Version { get; set; }
        public string ComputeRg { get; set; }
        public string ComputeState { get; set; }
        public string Partitions { get; set; }

        public List<DataPartitionDTO>? GetDataPartitions()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<DataPartitionDTO>>(Partitions);
        }

        /// <summary>
        /// 
        /// An instance that is found to be invalid may or may not need investigation. 
        /// 
        /// If the instance or any of it's data partitions are in a valid state, as defined by the static 
        /// ActiveStates in this class definition, then we have to look further as there may be some 
        /// backend database information to remove. 
        ///
        /// This method lets you know if it's safe to just delete or not.
        /// 
        /// </summary>
        /// <param name="records">All records associated with an instance, there really only should be one
        /// but dealing with a >1 scenario is a follow on task. </param>
        /// <returns>True if ANYTHING is in a valid state that could create billing records.</returns>
        public static bool RequiresInvestigation(List<ADMEResourcesDTO> records)
        {
            bool investigate = false;
            foreach (ADMEResourcesDTO dto in records)
            {
                List<string> provisioningStates = dto.GetDataPartitions()
                    .Select(x => x.ProvisioningState)
                    .ToList();
                provisioningStates.Add(dto.ProvisioningState);

                List<string> validStates = provisioningStates
                    .Where(x => ActiveStates.Contains(x))
                    .Select(x => x)
                    .ToList();

                investigate = validStates.Count > 0;
                if (investigate == true)
                {
                    break;
                }
            }

            return investigate;
        }
    }
}
