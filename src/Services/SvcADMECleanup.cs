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
        private readonly ILogger<SvcADMECleanup> _consoleLogger;
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ServiceSettings ServiceSettings { get; set; }
        private BackgroundServiceRunningState RunningState { get; set; }
        #endregion

        public SvcADMECleanup(
            ILogger<SvcADMECleanup> logger,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime,
            ITokenProvider tokenProvider,
            IMapper mapper,
            BackgroundServiceRunningState runningState)
        {
            this._applicationLifetime = appLifetime;
            this._configuration = configuration;
            this._mapper = mapper;
            this._tokenProvider = tokenProvider;
            this._consoleLogger = logger;

            this.ServiceSettings = new ServiceSettings(this._configuration);
            this.RunningState = runningState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
#pragma warning disable CS8602 
            if (this.ServiceSettings.ExecutionSettings.ADMECleanupService.IsActive == false)
            {
                _consoleLogger.LogInformation("ADME Cleanup Service is not active. Exiting...");
                await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                return;
            }
#pragma warning restore CS8602 

            while (!stoppingToken.IsCancellationRequested)
            {
                _consoleLogger.LogInformation("Starting cleanup pass....");

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Most data goes to cosmos so we have history of what we've done. 
#pragma warning disable CS8604 
                IEventLogger eventLogger = new EventLogWriter(
                    this._tokenProvider.Credential,
                    this.ServiceSettings.EventLogSettings);
#pragma warning restore CS8604 
                eventLogger.Service = "SvcADMECleanup";

                eventLogger.LogInfo("Starting execution");

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Get ADME instances from Prod/NonProd Kusto stores. Data used when an invalid instance
                /// is detected for deleteion. The data will be used to clear
                /// out the Cosmos data and DNS settings.
                /// 
                _consoleLogger.LogInformation("Load ADME Data from Kusto...");
                List<ADMEResourcesDTO> admeResources = SvcUtilsCommon.GetADMEInstanceDataFromKusto(
                    this._tokenProvider,
                    this._mapper,
                    this.ServiceSettings);
                eventLogger.LogInfo($"Loaded {admeResources.Count} resources from Kusto");

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Get the Cosmos instances to have on hand for cleaning up invalid instances that 
                /// either have the instance or any data partition in a succeeded state as it may have 
                /// impact on billing and is just good form to keep cleaned.
                /// 
                _consoleLogger.LogInformation("Preparing Cosmos connections...");
#pragma warning disable CS8604 
                Dictionary<string, CosmosConnection> cosmosConnections =
                    SvcUtilsCommon.CreateCosmosClients(
                    eventLogger,
                    this._tokenProvider,
                    this.ServiceSettings.CosmosSettings
                    );
#pragma warning restore CS8604 

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Collect Subscriptions to be processed.
                _consoleLogger.LogInformation("Get service tree listed subscription information...");

                // ***IMPORTANT*** Currently limited ONLY to Engg sub
                //string[] limitList = new string[] { "b0844137-4c2f-4091-b7f1-bc64c8b60e9c" };
                string[]? limitList = null;
                SubscriptionResults subscriptionResults = SvcUtilsCommon.GetNonProdServiceSubscriptions(
                    this._tokenProvider,
                    this._mapper,
                    this.ServiceSettings,
                    limitList
                    );

                eventLogger.LogInfo("Options Subscription filter", limitList);
                eventLogger.LogInfo("Reachable Subscriptions", 
                    subscriptionResults.Subscriptions.Select(x => x.ServiceSubscriptionsDTO.SubscriptionName));
                eventLogger.LogInfo("UnReachable Subscriptions", 
                    subscriptionResults.UnreachableSubscriptions.Select(x => x.SubscriptionName));

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Process each subscription
                /// 
                foreach (AzureSubscription sub in subscriptionResults.Subscriptions)
                {
                    eventLogger.Subscription = sub.ServiceSubscriptionsDTO.SubscriptionName;
                    _consoleLogger.LogInformation(
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

                    eventLogger.LogInfo($"Total ADME Instances found {instanceCollections.Count}");
                    if (invalidColletion.Count > 0)
                    {
#pragma warning disable CS8602 
                        eventLogger.LogInfo("Invalid ADME Instances", invalidColletion.Select(x => x.Parent.ResourceGroup.Name).ToList());
#pragma warning restore CS8602 
                    }
                    if (abandoned.Count > 0)
                    {
                        eventLogger.LogInfo("Abandoned ADME Resources", abandoned.Select(x => x.ResourceGroup.Name).ToList());
                    }

                    ////////////////////////////////////////////////////////////////////////////////////////
                    /// Collect data for instances that need investigation along with a list of raw 
                    /// resource groups that can be deleted without further action. 
                    /// 
                    ADMEResourceCleanupResults cleanupResults = this.GetCleanupResults(invalidColletion, admeResources);

                    // Abandoned have always presented as and ADMResource Partition, and ar not present in Kusto,
                    // so if we have those add them its safe to just remove them.
                    cleanupResults.DeleteList.AddRange(
                        abandoned.Where(x => x.SubType == ADMESubscriptionParser.SubTypePartition)
                    );

                    if (cleanupResults.DeleteList.Count > 0)
                    {
                        eventLogger.LogInfo("ResourceGroup To Delete", cleanupResults.DeleteList.Select(x => x.ResourceGroup.Name).ToList());
                    }
                    if  (cleanupResults.InvestigationList.Count > 0)
                    {
                        eventLogger.LogInfo("Instances To Investigate", cleanupResults.InvestigationList.Keys.Select(x => x.InstanceName).ToList());
                    }


                    //*********************************************************************************************
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    // This section will delete actual resource groups, run with fals first to ensure no mistakes.
                    if (this.ServiceSettings.ExecutionSettings.ADMECleanupService.ExecuteCleanup)
                    {
                        _consoleLogger.LogWarning("Execution state has been set to true, cleaning up resource groups.");

                        // For instances we need to investigate, this means they were found active in 
                        // Kusto, for each of these, clear out the compute and partition data from 
                        // Cosmos, delete the DNS records, if any, and add any resource groups to the list
                        // for deletion. 
                        List<AzureResourceGroup> cleanedUpInstanceGroups = this.CleanupInstanceResources(
                            cleanupResults.InvestigationList,
                            eventLogger,
                            cosmosConnections);

                        // Get the delete outright groups then add the ones where we cleaned up resources.
                        List<AzureResourceGroup> deleteGroups = cleanupResults.DeleteList.Select(x => x.ResourceGroup).ToList();
                        deleteGroups.AddRange(cleanedUpInstanceGroups);

                        // Then finally, delete them.
                        _consoleLogger.LogInformation("Deleting {count} resource groups as final step", deleteGroups.Count);
                        SvcUtilsCommon.DeleteResourceGroups(deleteGroups, eventLogger);
                    }
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    //*********************************************************************************************
                }

                eventLogger.Subscription = string.Empty;
                eventLogger.LogInfo("ADME Cleanup Service has completed successfully.");
                eventLogger.Dispose();

                // If we are not running continuously, one off jobs, then kill the service.
                if( this.ServiceSettings.ExecutionSettings.ADMECleanupService.RunContinuous == false)
                {
                    await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                    return;
                }
                else
                {
                    await Task.Delay(
                        this.ServiceSettings.ExecutionSettings.ADMECleanupService.GetTimeoutMilliseconds(),
                        stoppingToken
                        );

                }
            }
        }

        /// <summary>
        /// For each instance that is invalid, check and update when neccesary the Cosmos records associated
        /// with it and remove any DNS settings.
        /// </summary>
        /// <param name="invalidInstances"></param>
        /// <param name="eventLogger"></param>
        /// <param name="cosmosConnections"></param>
        /// <returns></returns>
        private List<AzureResourceGroup> CleanupInstanceResources(
            Dictionary<ADMEResourceCollection, List<ADMEResourcesDTO>> invalidInstances,
            IEventLogger eventLogger,
            Dictionary<string, CosmosConnection> cosmosConnections
            )
        {
            List<AzureResourceGroup> cleanedUpInstanceGroups = new List<AzureResourceGroup>();
            if (invalidInstances.Count > 0)
            {
                this._consoleLogger.LogInformation("Cleaning up {count} resources from Cosmos and DNS ", invalidInstances.Count);

                // For the investgate list, can we clear out the DNS records?
                foreach (KeyValuePair<ADMEResourceCollection, List<ADMEResourcesDTO>> kvp in invalidInstances)
                {
                    // Add these groups to the list of those to wipe because they are no longer needed.
#pragma warning disable CS8602 
                    cleanedUpInstanceGroups.Add(kvp.Key.Parent.ResourceGroup);
#pragma warning restore CS8602 
                    kvp.Key.Partitions.Select(x => x.ResourceGroup).ToList().ForEach(x => cleanedUpInstanceGroups.Add(x));

                    // If we do have a Kusto record, which we should, set instance and partition
                    // data to deleted then clear out DNS settings.
                    ADMEResourcesDTO? resourceData = kvp.Value.FirstOrDefault();
                    if (resourceData != null)
                    {
                        try
                        {
                            SvcUtilsCommon.ClearCosmosData(
                                eventLogger,
                                cosmosConnections,
                                resourceData);
                        }
                        catch (Exception ex)
                        {
                            eventLogger.LogException($"Exception clearing Cosmos for {resourceData.InstanceName}", ex);
                        }

                        try
                        {
#pragma warning disable CS8604 
                            SvcUtilsCommon.ClearDNSSettings(
                                this._tokenProvider,
                                this.ServiceSettings.DNSSettings,
                                eventLogger,
                                resourceData);
#pragma warning restore CS8604 
                        }
                        catch (Exception ex)
                        {
                            eventLogger.LogException($"Exception clearing DNS for {resourceData.InstanceName}", ex);
                        }
                    }
                }
            }
            return cleanedUpInstanceGroups;
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
#pragma warning disable CS8604
                        returnResults.DeleteList.Add(collection.Parent);
#pragma warning restore CS8604
                        returnResults.DeleteList.AddRange(collection.Partitions);
                    }
                    else
                    {
                        returnResults.InvestigationList.Add(collection, kustoRecords);
                    }
                }
                else
                {
                    //  TODO: Remove by March 9 for Marija and Pritish
#pragma warning disable CS8604, CS8602 
                    bool lastDitchSaveForPritish = false;
                    if(collection.ResourceType == ADMESubscriptionParser.SubTypeOneBox)
                    {
                        lastDitchSaveForPritish = collection.Parent.ResourceGroup.Tags.ContainsKey("ImABananna");
                    }
                    // DCP or onebox, no record so a straight out delete
                    if( lastDitchSaveForPritish == false)
                    {
                        returnResults.DeleteList.Add(collection.Parent);
                    }
#pragma warning restore CS8604, CS8602
                    //  TODO: Remove by March 9 for Marija and Pritish

                    // REINSTATE AFTER MARCH 9: returnResults.DeleteList.Add(collection.Parent);
                }
            }
            return returnResults;
        }

    }
}
