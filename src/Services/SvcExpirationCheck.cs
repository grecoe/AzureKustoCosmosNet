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

    internal class SvcExpirationCheck : BackgroundService
    {
        #region Private ReadOnly variables passed in
        private readonly ILogger<SvcExpirationCheck> _logger;
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ServiceSettings ServiceSettings { get; set; }
        #endregion

        public SvcExpirationCheck(
            ILogger<SvcExpirationCheck> logger,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime,
            ITokenProvider tokenProvider,
            IMapper mapper)
        {
            this._applicationLifetime = appLifetime;
            this._configuration = configuration;
            this._tokenProvider = tokenProvider;
            this._mapper = mapper;
            this._logger = logger;

            this.ServiceSettings = new ServiceSettings(this._configuration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ////////////////////////////////////////////////////////////////////////////////////////
                /// Get ADME instances from Prod/NonProd Kusto stores. Data used when an expiring RG
                /// that is to be deleted is actually an ADME Instance. The data will be used to clear
                /// out the Cosmos data and DNS settings as is done in SvcAdmeCleanup, however, this 
                /// additional functionality may never make it here, so commented out for brevity in 
                /// execution.
                /// 
                /*
                _logger.LogInformation("Load ADME Data from Kusto...");
                List<ADMEResourcesDTO> admeResources = SvcUtilsCommon.GetADMEInstanceDataFromKusto(
                    this._tokenProvider,
                    this._mapper,
                    this.ServiceSettings);
                */


                ////////////////////////////////////////////////////////////////////////////////////////
                /// Collect Subscriptions to be processed.
                _logger.LogInformation("Get service tree listed subscription information...");
                // ODD string[] limitList = new string[] { "b0844137-4c2f-4091-b7f1-bc64c8b60e9c" };
                // ENGG string[] limitList = new string[] { "c99e2bf3-1777-412b-baba-d823676589c2" };
                // string[] limitList = new string[] { "015ab1e4-bd82-4c0d-ada9-0f9e9c68e0c4" };
                string[] limitList = null;


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
                /// Iterate each sub marking resource groups where neccesary. Cleanup, until better 
                /// defined, is not part of this process. 
                /// 
                foreach (AzureSubscription sub in subscriptionResults.Subscriptions)
                {
                    _logger.LogInformation(
                        "Managing subscription {subname}",
                        sub.ServiceSubscriptionsDTO.SubscriptionName
                        );

                    // ************************************** GROUP EXPIRATION ************************************** 

                    ////////////////////////////////////////////////////////////////////////////////////////
                    /// Manage expiration tagging and data collection of what is expired. 
                    ///
                    _logger.LogInformation("Verify group expiration data...");
                    GroupExpirationResult expirationTaggingResults = this.ManageResourceGroupExpirations(sub);

                    // ********** DEBUG **********
                    SvcUtilsCustomLogging.ReportGroupExpirationTagging(this._logger, expirationTaggingResults);
                    // ********** DEBUG **********

                    //*********************************************************************************************
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    // Un-commenting this code will delete anything that has expired. 
                    // this.DeleteResourceGroups(expirationTaggingResults.ExpiredGroups);
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    //*********************************************************************************************

                    // ************************************** GROUP EXPIRATION ************************************** 

                    /*
                      Example Output Running this on C99

                          {
                            *** WHAT HAD A EXPRIATION TAG ADDED TO IT ****
                            "Recently Tagged": [
                              "AzSecPackAutoConfigRG",
                              "Compute-rg-it1708591054335-hnnuij",
                              "Compute-rg-it1708595985203-tavuae",
                              "RGServiceITPvtEndpointAuto1170859566393212745",
                              "RGServiceITPvtEndpointAutoPA17086095040294927"
                            ],

                            *** ATTEMPTS TO ADD TAGS BUT ARE LOCKED ***
                            "Unable To Tag": [
                              "Compute-rg-Jan29test-dxamlg",
                              "DataPartition-rg-Jan29test-c123cghf456fgty67yh76-dxamlg",
                              "synapseworkspace-managedrg-32819e66-a0b5-4c37-9748-76dfe079c83d",
                              "synapseworkspace-managedrg-fe4b5e0a-84f1-4288-953d-c6f0d8ab1f5c",
                              "managed-rg-preeti",
                              "Compute-rg-it1700510575982-mptjkk",
                              "DataPartition-rg-it1700510575982-dp1-mptjkk",
                              "DataPartition-rg-sept4ck3-d-tlbtoc",
                              "DataPartition-rg-sept4ck3-c-tlbtoc",
                              "Compute-rg-sept4ck3-tlbtoc",
                              "Compute-rg-sept4ck4-xwuhew",
                              "DataPartition-rg-dagit3-dp1-rrjlxk",
                              "Compute-rg-dagit3-rrjlxk",
                              "laksfabric2",
                              "Compute-rg-it1708601583812-aeycir",
                              "DataPartition-rg-it1708604513727-dp1-qwmaao",
                              "Compute-rg-it1708604513727-qwmaao",
                              "MC_Compute-rg-it1708604513727-qwmaao_aks-6jmbb5qsq5xdq_eastus",
                              "DataPartition-rg-it1708604513727-dp2-qwmaao"
                            ],

                            *** EXPIRED BUT PROTECTED (with delete=False tag OR is managed group) ***
                            "Expired But Protected": [],

                            *** GROUPS WE TRIED TO DELETE ALREADY BUT THEY FAILED FOR SOME REASON MOST LIKELY NETWORKING***
                            "Previous Delete Attempts": [
                              "Compute-rg-dwlasdagtest1-xkjksl",
                              "Compute-rg-LASDAGtv23-vskben",
                              "Compute-rg-nikms145-uignim",
                              "aks80-cloud-onebox",
                              "Compute-rg-afwhj-uryikc"
                            ],
                            "Expired": []
                        }
                     */
                }

                // Wait a predetermined (configurable) amount of time befoe re-running the service or kill it with 
                // this._applicationLifetime.StopApplication();
                await Task.Delay(
                    this.ServiceSettings.ExecutionSettings.ExpirationService.GetTimeoutMilliseconds(),
                    stoppingToken
                    );
            }
        }

        /// <summary>
        /// The goal is to have all Resource Groups tagged with the tag "expiration" with a DateTime 
        /// value indicating when the group expires, i.e. can be deleted. 
        /// 
        /// Scan all resource groups of an Azure Subscription and perform the following actions/data
        /// collection tasks. 
        /// 
        /// - For each group, look for the expiration tag. If not present, add it to the group. 
        ///     NOTE: A locked resource group will reject the addition of a tag. 
        ///     
        /// Build up return data of
        /// - The list of resource groups that have expired. 
        ///     - A group is expired if the current time is passed the "expiration" tag value AND
        ///         1. The group does NOT have the "delete" tag set with a value of false
        ///         2. The group is NOT managed by any other group (i.e. AKS cluster)
        /// - The list of groups that have previously been attempted to be deleted by this process. 
        /// - The list of groups that have been tagged in the current pass
        /// - The list of groups that a tag attempt was made but it failed. 
        /// </summary>
        /// <param name="subscription">An Azure Subscription in which to scan</param>
        /// <returns>Expiration results</returns>
        private GroupExpirationResult ManageResourceGroupExpirations(AzureSubscription subscription)
        {
            GroupExpirationResult returnResult = new GroupExpirationResult();

            DateTime latestExpiration = DateTime.UtcNow.AddDays(
                this.ServiceSettings.ExecutionSettings.ExpirationService.DaysToExpiration
                );

            List<AzureResourceGroup> allSubGroups = subscription.GetResourceGroups();

            // Sweep over resource groups and 
            //  1. Give an expiration date to any group that doesn't have one
            //  2. Build a list of expired groups that don't have delete=false as a tag.
            foreach (AzureResourceGroup group in allSubGroups)
            {
                if (group.DeleteionAttempted)
                {
                    returnResult.PreviousDeleteAttemptGroups.Add(group.Name);
                }

                // Add expiration if not present
                if (!group.HasExpiration)
                {
                    bool result = group.SetExpiration(latestExpiration);
                    if (!result)
                    {
                        // One more attempt with the locks removed.
                        group.RemoveLocks();
                        if( (result = group.SetExpiration(latestExpiration)) == false)
                        {
                            returnResult.TagFailureGroups.Add(group.Name);
                        }
                    }
                    else
                    {
                        returnResult.TaggedGroups.Add(group.Name);
                    }
                }
                else if (group.IsExpired)
                {
                    if (group.IsProtected || group.IsManaged)
                    {
                        returnResult.ExpiredButProtectedGroups.Add(group.Name);
                    }
                    else
                    {
                        returnResult.ExpiredGroups.Add(group);
                    }
                }
            }

            return returnResult;
        }

    }
}
