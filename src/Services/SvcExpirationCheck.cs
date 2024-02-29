//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Services
{
    using AutoMapper;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models;

    internal class SvcExpirationCheck : BackgroundService
    {
        #region Private ReadOnly variables passed in
        private readonly ILogger<SvcExpirationCheck> _consoleLogger;
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ServiceSettings ServiceSettings { get; set; }
        private BackgroundServiceRunningState RunningState { get; set; }
        #endregion

        public SvcExpirationCheck(
            ILogger<SvcExpirationCheck> logger,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime,
            ITokenProvider tokenProvider,
            IMapper mapper,
            BackgroundServiceRunningState runningState)
        {
            this._applicationLifetime = appLifetime;
            this._configuration = configuration;
            this._tokenProvider = tokenProvider;
            this._mapper = mapper;
            this._consoleLogger = logger;

            this.ServiceSettings = new ServiceSettings(this._configuration);
            this.RunningState = runningState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
#pragma warning disable CS8602 
            if (this.ServiceSettings.ExecutionSettings.ExpirationService.IsActive == false)
            {
                _consoleLogger.LogInformation("ResourceGroup Group Expiration Service is not active. Exiting...");
                await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                return;
            }
#pragma warning restore CS8602 

            while (!stoppingToken.IsCancellationRequested)
            {
                ////////////////////////////////////////////////////////////////////////////////////////
                /// Most data goes to cosmos so we have history of what we've done. 
#pragma warning disable CS8604 // Possible null reference argument.
                IEventLogger eventLogger = new EventLogWriter(
                    this._tokenProvider.Credential,
                    this.ServiceSettings.EventLogSettings);
#pragma warning restore CS8604 // Possible null reference argument.
                eventLogger.Service = "SvcExpirationCheck";

                eventLogger.LogInfo("Starting execution");

                ////////////////////////////////////////////////////////////////////////////////////////
                /// Collect Subscriptions to be processed.
                _consoleLogger.LogInformation("Get service tree listed subscription information...");
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
                /// Iterate each sub marking resource groups where neccesary. Cleanup, until better 
                /// defined, is not part of this process. 
                /// 
                foreach (AzureSubscription sub in subscriptionResults.Subscriptions)
                {
                    eventLogger.Subscription = sub.ServiceSubscriptionsDTO.SubscriptionName;
                    _consoleLogger.LogInformation(
                        "Managing subscription {subname}",
                        sub.ServiceSubscriptionsDTO.SubscriptionName
                        );

                    ////////////////////////////////////////////////////////////////////////////////////////
                    /// Manage expiration tagging and data collection of what is expired. 
                    ///
                    _consoleLogger.LogInformation("Verify group expiration data...");
                    GroupExpirationResult expirationTaggingResults;
                    try
                    {
                        expirationTaggingResults = this.ManageResourceGroupExpirations(sub);
                    }
                    catch(Exception ex)
                    {
                        eventLogger.LogException($"Exception checking groups for {sub.ServiceSubscriptionsDTO.SubscriptionName}", ex);
                        continue;
                    } 

                    // ********** DEBUG **********
                    List<string> expired = expirationTaggingResults.ExpiredGroups.Select(x => x.Name).ToList();
                    Dictionary<string, List<string>> subLayout = new Dictionary<string, List<string>>()
                    {
                        { "Recently Tagged" , expirationTaggingResults.TaggedGroups},
                        { "Unable To Tag" , expirationTaggingResults.TagFailureGroups},
                        { "Expired But Protected" , expirationTaggingResults.ExpiredButProtectedGroups},
                        { "Previous Delete Attempts" , expirationTaggingResults.PreviousDeleteAttemptGroups},
                        { "Expired" , expired},
                    };
                    eventLogger.LogInfo("Expiration Scanning Results", subLayout);
                    // ********** DEBUG **********

                    //*********************************************************************************************
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    // Un-commenting this code will delete anything that has expired. 
                    // this.DeleteResourceGroups(expirationTaggingResults.ExpiredGroups);
                    // **** WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
                    //*********************************************************************************************
                }

                eventLogger.Subscription = string.Empty;
                eventLogger.LogInfo("Expiration Service Execution Complete");
                eventLogger.Dispose();

                if( this.ServiceSettings.ExecutionSettings.ExpirationService.RunContinuous == false)
                {
                    await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                    return; 
                }
                else
                {
                    await Task.Delay(
                        this.ServiceSettings.ExecutionSettings.ExpirationService.GetTimeoutMilliseconds(),
                        stoppingToken
                        );
                }
            }
        }

        /// <summary>
        /// The goal is to have all ResourceGroup Groups tagged with the tag "expiration" with a DateTime 
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

#pragma warning disable CS8602 
            DateTime latestExpiration = DateTime.UtcNow.AddDays(
                this.ServiceSettings.ExecutionSettings.ExpirationService.DaysToExpiration
                );
#pragma warning restore CS8602 

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
