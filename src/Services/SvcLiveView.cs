namespace SubscriptionCleanupUtils.Services
{
    using AutoMapper;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models;
    using SubscriptionCleanupUtils.Models.Kusto;
    using System.Collections.Generic;

    internal class SvcLiveView : BackgroundService
    {
        #region Private ReadOnly variables passed in
        private readonly ILogger<SvcLiveView> _consoleLogger;
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ServiceSettings ServiceSettings { get; set; }
        private BackgroundServiceRunningState RunningState { get; set; }
        #endregion

        public SvcLiveView(
            ILogger<SvcLiveView> logger,
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
            if (this.ServiceSettings.ExecutionSettings.LiveViewService.IsActive == false)
            {
                _consoleLogger.LogInformation("ADME LiveView Service is not active. Exiting...");
                await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                return;
            }
#pragma warning restore CS8602

            ////////////////////////////////////////////////////////////////////////////////////////
            /// Most data goes to cosmos so we have history of what we've done. 
#pragma warning disable CS8604
            IEventLogger eventLogger = new EventLogWriter(
                this._tokenProvider.Credential,
                this.ServiceSettings.EventLogSettings);
#pragma warning restore CS8604
            eventLogger.Service = "SvcLiveView";


            while (!stoppingToken.IsCancellationRequested)
            {
                _consoleLogger.LogInformation("Starting live view pass....");
                eventLogger.LogInfo("Starting execution");

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

                // To write to db
                List<IKustoRecord> instances = new List<IKustoRecord>();
                List<IKustoRecord> clusters = new List<IKustoRecord>();
                List<IKustoRecord> partitions = new List<IKustoRecord>();
                List<IKustoRecord> dcps = new List<IKustoRecord>();

                foreach (AzureSubscription sub in subscriptionResults.Subscriptions)
                {
                    _consoleLogger.LogInformation(
                        "Managing subscription {subname}",
                        sub.ServiceSubscriptionsDTO.SubscriptionName
                        );

                    ADMESubscriptionParser parser = new ADMESubscriptionParser(sub);
                    DateTime timestamp = DateTime.UtcNow;
                    List<ADMEResourceCollection> allInstanceResources = parser.CollectInstances();


                    // Now get each type
#pragma warning disable CS8602, CS8604 
                    List<ADMEResource?> dcpInstanceresources = allInstanceResources
                        .Where(x => x.Parent.SubType == ADMESubscriptionParser.SubTypeDCP)
                        .Select(x => x.Parent).ToList();
                    dcpInstanceresources.ForEach(x => dcps.Add(new LiveViewDCPDTO(timestamp, sub, x)));


                    List<ADMEResource?> instanceResources = allInstanceResources
                        .Where(x => x.Parent.SubType != ADMESubscriptionParser.SubTypeDCP)
                        .Select(x => x.Parent).ToList();
                    instanceResources.ForEach(x => instances.Add(new LiveViewInstanceDTO(timestamp, sub, x)));

                    List<ADMEResource> clusterResources = new List<ADMEResource>(); 
                    allInstanceResources.Select(x=>x.Clusters).ToList().ForEach(x => clusterResources.AddRange(x));
                    clusterResources.ForEach(x => clusters.Add(new LiveViewClusterDTO(timestamp, sub, x)));
                    
                    List<ADMEResource> partitionResources = new List<ADMEResource>();
                    allInstanceResources.Select(x => x.Partitions).ToList().ForEach(x => partitionResources.AddRange(x));
                    partitionResources.ForEach(x => partitions.Add(new LiveViewPartitionDTO(timestamp, sub, x)));
#pragma warning restore CS8602, CS8604
                }

                _consoleLogger.LogInformation("Uploading results to LiveView database");

                // Now upload all of the collected information in batches.
                Dictionary<string, List<IKustoRecord>> liveViewRecords = new Dictionary<string, List<IKustoRecord>>()
                    {
                        {LiveViewInstanceDTO.TABLE, instances },
                        {LiveViewClusterDTO.TABLE, clusters },
                        {LiveViewPartitionDTO.TABLE, partitions },
                        {LiveViewDCPDTO.TABLE, dcps }
                    };

                foreach (KeyValuePair<string, List<IKustoRecord>> records in liveViewRecords)
                {
                    if (records.Value.Count > 0)
                    {
#pragma warning disable CS8602
                        try
                        {
                            using (KustoIngest ingest = new KustoIngest(
                                this._tokenProvider.Credential,
                                new KustoIngestSettings(
                                    this.ServiceSettings.KustoSettings.LiveViewEndpoint,
                                    this.ServiceSettings.KustoSettings.LiveViewDatabase,
                                    records.Key
                                    )
                                ))
                            {
                                eventLogger.LogInfo($"Uploading {records.Value.Count} records to {records.Key} table.");
                                ingest.StreamRecords(records.Value);
                            }
#pragma warning restore CS8602
                        }
                        catch (Exception ex)
                        {
                            eventLogger.LogException($"Failed to upload {records.Key} records", ex);
                        }
                    }
                }

                // Let the log know we're done, then potentially bail. 
                eventLogger.LogInfo("Execution Complete");
                eventLogger.Dispose();

                // If we are not running continuously, one off jobs, then kill the service.
                if (this.ServiceSettings.ExecutionSettings.LiveViewService.RunContinuous == false)
                {
                    await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                    return;
                }
                else
                {
                    await Task.Delay(
                        this.ServiceSettings.ExecutionSettings.LiveViewService.GetTimeoutMilliseconds(),
                        stoppingToken
                        );
                }
            }
        }
    }
}
