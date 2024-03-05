namespace SubscriptionCleanupUtils.Services
{
    using AutoMapper;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Dns;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models;
    using SubscriptionCleanupUtils.Models.AppSettings;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;

    internal class SvcDNSCleanup : BackgroundService
    {
        #region Private ReadOnly variables passed in
        private readonly ILogger<SvcDNSCleanup> _consoleLogger;
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ServiceSettings ServiceSettings { get; set; }
        private BackgroundServiceRunningState RunningState { get; set; }
        #endregion

        public SvcDNSCleanup(
            ILogger<SvcDNSCleanup> logger,
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
            if (this.ServiceSettings.ExecutionSettings.DNSCleanupService.IsActive == false)
            {
                _consoleLogger.LogInformation("DNS Cleanup Service is not active. Exiting...");
                await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                return;
            }
#pragma warning restore CS8602
            this._consoleLogger.LogInformation("Starting DNS Cleanup Service");


            ////////////////////////////////////////////////////////////////////////////////////////
            /// Most data goes to cosmos so we have history of what we've done. 
#pragma warning disable CS8604
            IEventLogger eventLogger = new EventLogWriter(
                this._tokenProvider.Credential,
                this.ServiceSettings.EventLogSettings);
#pragma warning restore CS8604
            eventLogger.Service = "SvcDNSCleanup";

            eventLogger.LogInfo("Starting execution");


            while (!stoppingToken.IsCancellationRequested)
            {
                this._consoleLogger.LogInformation("Searching DNS Records");

                DNSEnvironment? nonProdEnvironment = this.ServiceSettings.DNSSettings.Environments.Where(x => x.Environments.Contains("Dogfood")).FirstOrDefault();
                if (nonProdEnvironment == null)
                {
                    eventLogger.LogError("Unable to locate Dogfood DNS Zone");
                }

                // Get DNS Records
                DNSRecords? nonProdDnsRecords = SvcUtilsCommon.GetDNSFromCache(
                    this._tokenProvider,
                    nonProdEnvironment,
                    eventLogger);
                Dictionary<string, List<ArmResource>> dnsResourceList = ParseDNSRecords(nonProdDnsRecords);
                List<ArmResource> recordsToDelete = new List<ArmResource>();


                if (this.ServiceSettings.ExecutionSettings.DNSCleanupService.ResolveCnameOption)
                {
                    // Try and do the resolution
                    this._consoleLogger.LogInformation("Validate CNAME Dns Resolution");
                    recordsToDelete.AddRange(this.GetDanglingCNAMERecords(nonProdDnsRecords, eventLogger));
                }

                ///////////////////////////////////////////////////////////////////////////////////////////
                /// Filter out A name records in the form NAME.[privatelink|internal|xxx] where Name is 
                /// not found in CNAME records. This is a sign that there are invalid and abandoned A
                /// records in the DNS Zone
                if (this.ServiceSettings.ExecutionSettings.DNSCleanupService.UnmatchedARecordsOption)
                {
                    this._consoleLogger.LogInformation("Filter unparented A records with additional path");
                    recordsToDelete.AddRange(this.GetAbandonedInstanceARecords(nonProdDnsRecords, eventLogger));
                }

                ///////////////////////////////////////////////////////////////////////////////////////////
                /// Filter out all the itNNNN instance records in BOTH A and CNAME tables to delete. These
                /// tend to be the ones that get abandoned wholesale. This *may* also capture things from above
                /// which may cause issues, but the delete shouldn't be a problem, we cover with a try/catch.
                if (this.ServiceSettings.ExecutionSettings.DNSCleanupService.FilterITInstancesOption)
                {
                    this._consoleLogger.LogInformation("Filter DNS Records for it instances");
                    foreach (KeyValuePair<string, List<ArmResource>> kvp in dnsResourceList)
                    {
                        var match = Regex.Match(kvp.Key, @"^it[\d][\d]+");
                        if (match.Success)
                        {
                            recordsToDelete.AddRange(kvp.Value);
                        }
                    }
                }

                // Now make sure we are only asking for a SINGLE delete per resource
                recordsToDelete = recordsToDelete.Distinct().ToList();
                this._consoleLogger.LogInformation($"Delete {recordsToDelete.Count} DNS Records");


                //////////////////////////////////////////////////////////////////////////////////
                // Now if we are to cleanup, this is where it happens. 
                if (this.ServiceSettings.ExecutionSettings.DNSCleanupService.ExecuteCleanup)
                {
                    int aRecordsDeleted = 0;
                    int cnameRecordsDeleted = 0;
                    foreach (var record in recordsToDelete)
                    {
                        try
                        {
                            if (record is DnsARecordResource)
                            {
                                aRecordsDeleted += 1;
                                eventLogger.LogInfo($"Deleting A record {(record as DnsARecordResource).Data.Name}");
                                (record as DnsARecordResource).Delete(Azure.WaitUntil.Started);
                            }
                            else if (record is DnsCnameRecordResource)
                            {
                                cnameRecordsDeleted += 1;
                                eventLogger.LogInfo($"Deleting CNAME record {(record as DnsCnameRecordResource).Data.Name}");
                                (record as DnsCnameRecordResource).Delete(Azure.WaitUntil.Started);
                            }
                        }
                        catch(Exception ex)
                        {
                            eventLogger.LogException($"Delete record exception", ex);
                        }

                        if ((aRecordsDeleted + cnameRecordsDeleted) % 10 == 0)
                        {
                            this._consoleLogger.LogInformation($"Deleted {(aRecordsDeleted + cnameRecordsDeleted)} of {recordsToDelete.Count} DNS Records");
                        }
                    }

                    eventLogger.LogInfo($"Deleted {cnameRecordsDeleted} CNAME records and {aRecordsDeleted} a records");
                }

                eventLogger.Subscription = string.Empty;
                eventLogger.LogInfo("ADME Cleanup Service has completed successfully.");
                eventLogger.Dispose();

                // If we are not running continuously, one off jobs, then kill the service.
                if (this.ServiceSettings.ExecutionSettings.DNSCleanupService.RunContinuous == false)
                {
                    await this.RunningState.StopBackgroundService(this, stoppingToken, this._applicationLifetime);
                    return;
                }
                else
                {
                    await Task.Delay(
                        this.ServiceSettings.ExecutionSettings.DNSCleanupService.GetTimeoutMilliseconds(),
                        stoppingToken
                        );

                }
            }
        }

        /// <summary>
        /// For each CNAME record from DNS, see if the name resolves. If not, it will throw a socket 
        /// exception. If so, look for all of the A records with the same name and collect all of the 
        /// associated records to delete. 
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        private List<ArmResource> GetDanglingCNAMERecords(DNSRecords records, IEventLogger eventLogger)
        {
            List<ArmResource> returnResources = new List<ArmResource>();

            List<string> unfoundInstances = new List<string>();
            foreach (DnsCnameRecordResource cnameRecord in records.CnameRecords)
            {
                try
                {
                    var response = System.Net.Dns.GetHostEntry(cnameRecord.Data.Cname);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 1101)
                    {
                        unfoundInstances.Add(ParseDNSName(cnameRecord.Data.Name));
                        returnResources.Add(cnameRecord);
                    }
                }
                catch(Exception ex)
                {
                    // We don't know but do NOT add it to the list
                    eventLogger.LogException("Unknown exception resolving DNS", ex);
                }
            }

            // Now go through the A records and see if there are alias records to clear as well
            if (unfoundInstances.Count > 0)
            {
                foreach (DnsARecordResource aRecord in records.ARecords)
                {
                    string name = ParseDNSName(aRecord.Data.Name);
                    if (unfoundInstances.Contains(name))
                    {
                        returnResources.Add(aRecord);
                    }
                }
            }

            eventLogger.LogInfo($"{returnResources.Count} CNAME records unresolvable");

            return returnResources;
        }

        /// <summary>
        /// For each ARecord that has a "." in it, if the parsed out name is not also 
        /// pointing to a CNAME record, then this was left dangling. Get a list of them
        /// to clean up as well. 
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        private List<ArmResource> GetAbandonedInstanceARecords(DNSRecords records, IEventLogger eventLogger)
        {
            List<ArmResource> returnResources = new List<ArmResource>();

            List<string> cNameRecords = new List<string>();
            foreach (DnsCnameRecordResource cnameRecord in records.CnameRecords)
            {
                cNameRecords.Add(ParseDNSName(cnameRecord.Data.Name));
            }

            foreach (DnsARecordResource aRecord in records.ARecords)
            {
                string name = ParseDNSName(aRecord.Data.Name);
                if(!cNameRecords.Contains(name) && aRecord.Data.Name.Contains("."))
                {
                    returnResources.Add(aRecord);
                }
            }

            eventLogger.LogInfo($"{returnResources.Count} unpaired A records meeting pattern");

            return returnResources;
        }

        /// <summary>
        /// Collect all of the A and CNAME records and parse out the names
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        private Dictionary<string, List<ArmResource>> ParseDNSRecords(DNSRecords records)
        {
            Dictionary<string, List<ArmResource>> returnResources = new Dictionary<string, List<ArmResource>>();

            foreach(DnsARecordResource aRecord in records.ARecords)
            {
                string name = ParseDNSName(aRecord.Data.Name);
                if(returnResources.ContainsKey(name.ToLower()) == false)
                {
                    returnResources.Add(name.ToLower(), new List<ArmResource>());
                }
                returnResources[name.ToLower()].Add(aRecord);
            }

            foreach(DnsCnameRecordResource cnameRecord in records.CnameRecords)
            {
                string name = ParseDNSName(cnameRecord.Data.Name);
                if (returnResources.ContainsKey(name.ToLower()) == false)
                {
                    returnResources.Add(name.ToLower(), new List<ArmResource>());
                }
                returnResources[name.ToLower()].Add(cnameRecord);
            }

            return returnResources;
        }

        private string ParseDNSName(string fqdn)
        {
            string returnVal = fqdn;
            if(fqdn.Contains('.'))
            {
                returnVal = fqdn.Split(".")[0];
            }

            if(returnVal.EndsWith("bkp"))
            {
                returnVal = returnVal.Split("bkp")[0];
            }

            return returnVal;
        }
    }
}