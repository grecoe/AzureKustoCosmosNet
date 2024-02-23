//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Services
{
    using AutoMapper;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models;
    using SubscriptionCleanupUtils.Models.Kusto;
    using System.Collections.Generic;

    public class SvcADMECleanup : BackgroundService
    {
        #region Private ReadOnly variables passed in
        private readonly ILogger<SvcADMECleanup> _logger;
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ServiceSettings ServiceSettings { get; set; }

        #endregion

        public SvcADMECleanup(
            ILogger<SvcADMECleanup> logger,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime,
            ITokenProvider tokenProvider,
            IMapper mapper)
        {
            this._applicationLifetime = appLifetime;
            this._configuration = configuration;
            this._mapper = mapper;
            this._tokenProvider = tokenProvider;
            this._logger = logger;

            this.ServiceSettings = new ServiceSettings(this._configuration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ////////////////////////////////////////////////////////////////////////////////////////
                /// Get ADME instances from Prod/NonProd Kusto stores. Data used when an invalid instance
                /// is detected for deleteion. The data will be used to clear
                /// out the Cosmos data and DNS settings.
                /// 
                _logger.LogInformation("Load ADME Data from Kusto...");
                List<ADMEResourcesDTO> admeResources = SvcUtilsCommon.GetADMEInstanceDataFromKusto(
                    this._tokenProvider,
                    this._mapper,
                    this.ServiceSettings);

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Get the Cosmos instances to have on hand for cleaning up invalid instances that 
                /// either have the instance or any data partition in a succeeded state as it may have 
                /// impact on billing and is just good form to keep cleaned.
                /// 
                Dictionary<string, CosmosConnection> cosmosConnections =
                    SvcUtilsCommon.CreateCosmosClients(
                    this._logger,
                    this._tokenProvider,
                    this.ServiceSettings.CosmosSettings
                    );

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Collect Subscriptions to be processed.
                _logger.LogInformation("Get service tree listed subscription information...");
                //string[] limitList = new string[] { "c99e2bf3-1777-412b-baba-d823676589c2" };
                // ODD string[] limitList = new string[] { "b0844137-4c2f-4091-b7f1-bc64c8b60e9c" }; // ODD
                // ENGG string[] limitList = new string[] { "c99e2bf3-1777-412b-baba-d823676589c2" }; // ENGG
                string[] limitList = new string[] { "71356a6d-a339-4bce-bf4e-f76d3ecfc09d" }; // EXPLORERS

                SubscriptionResults subscriptionResults = SvcUtilsCommon.GetNonProdServiceSubscriptions(
                    this._tokenProvider,
                    this._mapper,
                    this.ServiceSettings,
                    limitList
                    );

                // ********** DEBUG **********
                SvcUtilsCustomLogging.ReportSubscriptions(this._logger, subscriptionResults);
                // ********** DEBUG **********

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Process each subscription
                /// 
                foreach (AzureSubscription sub in subscriptionResults.Subscriptions)
                {
                    _logger.LogInformation(
                        "Managing subscription {subname}", 
                        sub.ServiceSubscriptionsDTO.SubscriptionName
                        );

                    ////////////////////////////////////////////////////////////////////////////////////////
                    /// Parse down subscription resource groups into ADME instances (see CollectInstances) 
                    /// for further processing/detection.
                    /// 
                    ADMESubscriptionParser parser = new ADMESubscriptionParser(sub);
                    List<ADMEResourceCollection> instanceCollections = parser.CollectInstances();
                    List<ADMEResource> abandoned = parser.GetAbandonedResources(instanceCollections);
                    List<ADMEResourceCollection> invalidColletion = instanceCollections
                        .Where(x => x.IsValid ==  false )
                        .ToList();

                    // ********** DEBUG **********
                    SvcUtilsCustomLogging.ReportProblematicADMEResources(this._logger, invalidColletion, abandoned);
                    // ********** DEBUG **********

                    ////////////////////////////////////////////////////////////////////////////////////////
                    /// Collect data for instances that need investigation along with a list of raw 
                    /// resource groups that can be deleted without further action. 
                    /// 
                    ADMEResourceCleanupResults cleanupResults = this.GetCleanupResults(invalidColletion, admeResources);
                    
                    // ********** DEBUG **********
                    SvcUtilsCustomLogging.ReportCleanupResults(this._logger, cleanupResults);
                    // ********** DEBUG **********


                    ////////////////////////////////////////////////////////////////////////////////////////
                    /// For instances we need to investigate, this means they were found active in 
                    /// Kusto, for each of these, clear out the compute and partition data from 
                    /// Cosmos, delete the DNS records, if any, and add any resource groups to the list
                    /// for deletion. 
                    /// 
                    List<AzureResourceGroup> cleanedUpInstanceGroups = new List<AzureResourceGroup>();
                    if (cleanupResults.InvestigationList.Count > 0)
                    {
                        this._logger.LogInformation("Cleaning up {count} resources from Cosmos and DNS ", cleanupResults.InvestigationList.Count);

                        // For the investgate list, can we clear out the DNS records?
                        foreach (KeyValuePair<ADMEResourceCollection, List<ADMEResourcesDTO>> kvp in cleanupResults.InvestigationList)
                        {
                            // Add these groups to the list of those to wipe because they are no longer needed.
                            cleanedUpInstanceGroups.Add(kvp.Key.Parent.Resource);
                            kvp.Key.Partitions.Select(x => x.Resource).ToList().ForEach(x => cleanedUpInstanceGroups.Add(x));

                            // If we do have a Kusto record, which we should, set instance and partition
                            // data to deleted then clear out DNS settings.
                            ADMEResourcesDTO? resourceData = kvp.Value.FirstOrDefault();
                            if (resourceData != null)
                            {
                                try
                                {
                                    SvcUtilsCommon.ClearCosmosData(
                                        this._logger, 
                                        cosmosConnections, 
                                        resourceData);
                                }
                                catch(Exception ex)
                                {
                                    this._logger.LogError("Failed to delete cosmos record: {message}", ex.Message);
                                }

                                try 
                                { 
                                    SvcUtilsCommon.ClearDNSSettings(
                                        this._tokenProvider,
                                        this.ServiceSettings.DNSSettings,
                                        this._logger,
                                        resourceData);
                                }
                                catch (Exception ex)
                                {
                                    this._logger.LogError("Failed to delete DNS record: {message}", ex.Message);
                                }
                            }
                        }
                    }


                    //*********************************************************************************************
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    // This section will delete actual resource groups, run with fals first to ensure no mistakes.
                    if (this.ServiceSettings.ExecutionSettings.ADMECleanupService.ExecuteCleanup)
                    {
                        _logger.LogWarning("Execution state has been set to true, cleaning up resource groups.");

                        List<AzureResourceGroup> deleteGroups = cleanupResults.DeleteList.Select(x => x.Resource).ToList();
                        deleteGroups.AddRange(cleanedUpInstanceGroups);

                        _logger.LogInformation("Deleting {count} resource groups as final step", deleteGroups.Count);
                        SvcUtilsCommon.DeleteResourceGroups(deleteGroups);
                    }
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    //*********************************************************************************************
                }


                // Wait a predetermined (configurable) amount of time befoe re-running the service or kill it with 
                // this._applicationLifetime.StopApplication();
                await Task.Delay(
                    this.ServiceSettings.ExecutionSettings.ADMECleanupService.GetTimeoutMilliseconds(), 
                    stoppingToken
                    );
            }
        }

        /// <summary>
        /// Takes in a list of instances that are invalid for one reason or another. 
        /// 
        /// C1B and DCP instances will have no other tracking information in the system and can be deleted
        /// outright without further validation. 
        /// 
        /// ADME Instances that are not found in the ADME Kusto data similarly can be deleted outright. These 
        /// may be DCP instances or can be just headless instances. 
        /// 
        /// ADME Instances that ARE found in the Kusto data are added to a different part of the rsults. These
        /// instances may need more validation to be deleted. 
        /// 
        /// - Kusto is a reflection of Cosmos, and if an instance or ANY of it's data partitions are not in 
        ///   a failed state, billing may be occuring. 
        /// - With the KUSTO record, the DNS record can be looked up for deletion. 
        /// 
        /// </summary>
        /// <param name="invalidInstanceCollections">This is the list of resources that are detected as invalid. 
        /// These are either
        /// - ADME Instances in which there are zero Clusters, Partitions or both.
        /// - C1B instances without a cluster
        /// </param>
        /// <param name="admeResources">These are the instance data collected from the Prod and NonProd
        /// Kusto data for ADME that identify instances and their associated Compute RG and DataPartition RG(s)
        /// </param>
        /// <returns>Cleanup results </returns>
        private ADMEResourceCleanupResults GetCleanupResults(
            List<ADMEResourceCollection> invalidInstanceCollections,
            List<ADMEResourcesDTO> admeResources)
        {
            ADMEResourceCleanupResults returnResults = new ADMEResourceCleanupResults();

            foreach (ADMEResourceCollection collection in invalidInstanceCollections)
            {
                if (collection.ResourceType != ADMESubscriptionParser.SubTypeDCP &&
                    collection.ResourceType != ADMESubscriptionParser.SubTypeOneBox)
                {
                    List<ADMEResourcesDTO> kustoRecords = admeResources
                        .Where(x => x.InstanceName.ToLower() == collection.InstanceName.ToLower())
                        .ToList();

                    bool investigate = ADMEResourcesDTO.RequiresInvestigation(kustoRecords);

                    if (investigate == false)
                    {
                        // Clusters will be sorted out by the parent, partitions must
                        // be managed seperately. 
                        returnResults.DeleteList.Add(collection.Parent);
                        returnResults.DeleteList.AddRange(collection.Partitions);
                    }
                    else
                    {
                        returnResults.InvestigationList.Add(collection, kustoRecords);
                    }
                }
                else
                {
                    // DCP or onebox, no record so a straight out delete
                    returnResults.DeleteList.Add(collection.Parent);
                }
            }
            return returnResults;
        }
    }
}
