//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Services
{
    using AutoMapper;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Dns;
    using Azure.ResourceManager.Resources;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models;
    using SubscriptionCleanupUtils.Models.AppSettings;
    using SubscriptionCleanupUtils.Models.Cosmos;
    using SubscriptionCleanupUtils.Models.Kusto;
    using SubscriptionCleanupUtils.Services.Cache;
    using ITokenProvider = SubscriptionCleanupUtils.Domain.Interface.ITokenProvider;

    internal class SvcUtilsCommon
    {
        /// <summary>
        /// Given an ADME Instance (ADMEResourceDTO) check the appropriate DNS for it's environment 
        /// and remove any C or A records associated with the instance DNS name.
        /// </summary>
        /// <param name="tokenProvider">Provider to create ARMClient to subscription where DNS
        /// Zone is located.</param>
        /// <param name="settings">AppSettings for DNZ Zone information to look up based on ADME Environment</param>
        /// <param name="resource">The actual ADME Instance to pull the DNS name from.</param>
        public static void ClearADMEInstanceDNSSettings(
            ITokenProvider tokenProvider,
            DNSSettings settings,
            IEventLogger logger,
            ADMEResourcesDTO? resource)
        {
            if (resource != null)
            {
                if (settings.AcceptableInstanceEnvironment.Contains(resource.Environment))
                {
                    List<DNSEnvironment> dNSEnvironments = settings.Environments.Where(
                        x => x.Environments.Contains(resource.Environment))
                        .ToList();

                    DNSEnvironment? env = dNSEnvironments.FirstOrDefault();
                    if (env != null)
                    {
                        ServiceCache.TokenProvider = tokenProvider;
                        ServiceCache.EventLogger = logger;
                        DNSRecords? records = ServiceCache.GetCachedValues<DNSRecords>(env);

                        if (records != null)
                        {
                            List<DnsCnameRecordResource> deleteCRecords = records.CnameRecords
                                .Where(x => x.Data.Fqdn == resource.DNSName)
                                .ToList();
                            deleteCRecords.ForEach(x => x.Delete(Azure.WaitUntil.Started));

                            List<DnsARecordResource> deleteARecords = records.ARecords
                                .Where(x => x.Data.Fqdn == resource.DNSName)
                                .ToList();
                            deleteARecords.ForEach(x => x.Delete(Azure.WaitUntil.Started));

                            if (deleteARecords.Count > 0 || deleteCRecords.Count > 0)
                            {
                                logger.LogInfo($"Cleared DNS for {resource.DNSName}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load up all of the ADME instance data from Prod and NonProd Kusto databases 
        /// to be used for mapping to invalid instances. 
        /// 
        /// The data will be used to determine if there is more work to be done or not. 
        /// </summary>
        /// <returns></returns>
        public static List<ADMEResourcesDTO> GetADMEInstanceDataFromKusto(
            ITokenProvider tokenProvider,
            IMapper mapper,
            ServiceSettings serviceSettings)
        {
#pragma warning disable CS8602 
            KustoReader reader = new KustoReader(
                tokenProvider.Credential,
                mapper,
                serviceSettings.KustoSettings.ADMEEndpoint,
                serviceSettings.KustoSettings.ADMEDatabase);
#pragma warning restore CS8602 
            return reader.ReadData<ADMEResourcesDTO>(ADMEResourcesDTO.QUERY);
        }

 
        /// <summary>
        /// Deletes a resource group. 
        /// 
        /// First, the resource group has it's locks removed as a lock will prevent the tagging of the 
        /// group. A tag is added to signify that a delete attempt is occuring. In cases where the 
        /// group actually goes away, this is uneccesary, however many groups may linger and it's good 
        /// to know which ones require more attention.
        /// </summary>
        /// <param name="resourceGroups">A list of resource groups to delete</param>
        public static void DeleteResourceGroups(List<AzureResourceGroup> resourceGroups, IEventLogger logger)
        {
            // TODO: If there is one resoource as ADF or Autoscaling, which seem to be problematic
            // then we need to add additional steps here to clean up.
            // Further, network resources can also be problematic but it is unclear on how to clean those up. 
            foreach (AzureResourceGroup group in resourceGroups)
            {
                // Can't set a tag if it's locked so remove all locks forst, then tag
                // and delete. On failure, we'll see this tag later. 
                try
                {
                    logger.LogInfo($"Delete resource group {group.Name}");
                    group.RemoveLocks();
                    group.SetDeletionAttempt();
                    group.Delete();
                }
                catch(Exception ex)
                {
                    logger.LogInfo($"Exception deleting group {group.Name}", ex);
                }
            }
        }

        /// <summary>
        /// From the configuration settings create the Cosmos clients being requested.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tokenProvider"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static Dictionary<string, CosmosConnection> CreateCosmosClients(
                IEventLogger logger, 
                ITokenProvider tokenProvider, 
                CosmosSettings settings)
        {
            Dictionary<string, CosmosConnection> returnConnections = new Dictionary<string, CosmosConnection>();

            foreach(CosmosDetail env in settings.Environments) 
            {
                if (settings.AcceptableInstanceEnvironment.Contains(env.Environment))
                {
                    logger.LogInfo($"Creating Cosmos Connection for {env.Name}");

                    try
                    {
                        CosmosConnection connection = new CosmosConnection(tokenProvider, env);
                        connection.Connect();
                        returnConnections.Add(env.Name, connection);
                    }
                    catch (Exception ex)
                    {
                        logger.LogException($"Failed to create connection {env.Name}", ex);
                    }
                }
            }

            return returnConnections;
        }


        /// <summary>
        /// Given an ADME ResourceGroup, search for any OEPResource or DataPartition data 
        /// and force it all to Deleted status.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cosmosConnections"></param>
        /// <param name="resourceData"></param>
        public static void ClearADMEInstanceCosmosData(
            IEventLogger logger,
            Dictionary<string, CosmosConnection> cosmosConnections,
            ADMEResourcesDTO resourceData
            ) 
        {
            foreach (KeyValuePair<string, CosmosConnection> conns in cosmosConnections)
            {
                try
                {
                    OEPResourceEntity? entity = conns.Value.GetResource(resourceData.ResourceId);
                    List<DataPartitionsEntity> entities = conns.Value.GetDataPartitions(resourceData.ResourceId);
                    if (entity != null || entities.Count > 0)
                    {
                        logger.LogInfo($"Clear Cosmos Data for {resourceData.InstanceName} in {conns.Key}");
                        if (entity != null)
                        {
                            entity.ProvisioningState = ProvisioningState.Deleted;
                            var ignore = conns.Value.UpsertResource(entity).Result;
                        }

                        foreach (DataPartitionsEntity dp in entities)
                        {
                            dp.ProvisioningState = ProvisioningState.Deleted;
                            var ignore = conns.Value.UpsertDataPartition(dp).Result;
                        }
                        break;
                    }
                }
                catch(Exception ex)
                {
                    logger.LogException($"Failed to clear Cosmos Data for {resourceData.InstanceName} in {conns.Key}", ex);
                }
            }
        }
    }
}
