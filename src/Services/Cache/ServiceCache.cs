namespace SubscriptionCleanupUtils.Services.Cache
{
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Models;
    using System.Reflection;
    using AutoMapper;
    using SubscriptionCleanupUtils.Models.AppSettings;
    using Azure.ResourceManager.Dns;
    using Azure.ResourceManager.Resources;
    using Azure.ResourceManager;

    /// <summary>
    ///  Long running operations should be masked behind a cache so that recent calls, or calls
    ///  across services (which all use the same token provider) will not have to wait each time
    ///  as the data SHOULD not be changing fast enough to make a difference.
    /// </summary>
    internal class ServiceCache
    {
        #region Private cache objects
        /// <summary>
        /// Collecting DNS Records is long and arduous so we want to keep a cache on hand. Cache items timeout after 20
        /// minutes so it *should* allow all subs to be processed before having to reload the data.
        /// </summary>
        private static Dictionary<DNSEnvironment, DNSRecords> DNSRecordsCache = new Dictionary<DNSEnvironment, DNSRecords>();

        /// <summary>
        /// Collection of NonProd subs based on ServiceTreeId. The collection can take time so for each service that needs 
        /// access, the first one will take the hit, the others will breeze through it. 
        /// </summary>
        private static Dictionary<string, SubscriptionResults> SubscriptionResultsCache = new Dictionary<string, SubscriptionResults>();
        #endregion

        #region public objects that can be set by callers to pre-seed required inputs
        public static ITokenProvider? TokenProvider { get; set; }
        public static IMapper? Mapper { get; set; }
        public static IEventLogger? EventLogger { get; set; }
        public static ServiceSettings? ServiceSettings { get; set; }
        #endregion

        public static T? GetCachedValues<T>(params object[] inputs)
            where T : class
        {
            List<object> inputList = ServiceCache.PrepareInputs(inputs);

            // Now verify inputs for the calls 
            if (ServiceCacheMap.FullMap.ContainsKey(typeof(T)))
            {
                ServiceCache.ValidateInputs(ServiceCacheMap.FullMap[typeof(T)], inputList);
            }
            else
            {
                throw new ArgumentException($"Type is not a valid cacheable type - {typeof(T).Name}");
            }

            T? returnValue = null;

            CacheMapping usableMap = ServiceCacheMap.FullMap[typeof(T)];
            MethodInfo? method = typeof(ServiceCache).GetMethod(usableMap.MethodName);
            if (method != null)
            {
                returnValue = (T?)typeof(ServiceCache).InvokeMember(
                    usableMap.MethodName,
                    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
                    null,
                    null,
                    inputList.ToArray()
                    );
            }
            else
            {
                throw new ArgumentException($"Declard cache call {usableMap.MethodName} not present");
            }

            return returnValue;
        }

        #region Parameter Validation/Retrieval

        private static List<object> PrepareInputs(params object[] inputs)
        {
            List<object> inputList = new List<object>(inputs);

            // Set up globals generally used
            if (ServiceCache.GetParamByType<ITokenProvider>() == null && ServiceCache.TokenProvider != null)
            {
                inputList.Add(ServiceCache.TokenProvider);
            }
            if (ServiceCache.GetParamByType<ServiceSettings>() == null && ServiceCache.ServiceSettings != null)
            {
                inputList.Add(ServiceCache.ServiceSettings);
            }
            if (ServiceCache.GetParamByType<IMapper>() == null && ServiceCache.Mapper != null)
            {
                inputList.Add(ServiceCache.Mapper);
            }
            if (ServiceCache.GetParamByType<IEventLogger>() == null && ServiceCache.EventLogger != null)
            {
                inputList.Add(ServiceCache.EventLogger);
            }

            return inputList;
        }

        private static void ValidateInputs(CacheMapping requiredInputs, List<object> inputs)
        {
            foreach (Type t in requiredInputs.RequiredTypes)
            {
                var objects = inputs.Where(x => t.IsAssignableFrom(x.GetType())).ToList().FirstOrDefault();

                if (objects == null)
                {
                    throw new ArgumentException($"Missing required parameter of type {t.Name}");
                }
            }
        }

        private static T? GetParamByType<T>(params object[] inputs)
        {
            T? obj = (T?)inputs
                .Where(x => (x is T) == true)
                .ToList()
                .FirstOrDefault();

            return obj;
        }
        #endregion

        #region SubscriptionResults Cache
        /// <summary>
        /// Example of a cache call. 
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public static SubscriptionResults GetSubscriptionCache(params object[] inputs)
        {
            // Pull parameters
            ITokenProvider? tokenProvider = (ITokenProvider?)ServiceCache.GetParamByType<ITokenProvider>(inputs);
            IEventLogger? eventLogger = (IEventLogger?)ServiceCache.GetParamByType<IEventLogger>(inputs);
            IMapper? mapper = (IMapper?)ServiceCache.GetParamByType<IMapper>(inputs);
            ServiceSettings? settings = (ServiceSettings?)ServiceCache.GetParamByType<ServiceSettings>(inputs);

            List<string>? optional = (List<string>?)ServiceCache.GetParamByType<List<string>?>(inputs);

            // Now attempt to pull from cache
            SubscriptionResults? cachedResults = null;
            if (
                (ServiceCache.SubscriptionResultsCache.ContainsKey(settings.ServiceTreeSettings.ServiceId) == true)
                &&
                (DateTime.Now <= ServiceCache.SubscriptionResultsCache[settings.ServiceTreeSettings.ServiceId].Expires)
              )
            {
                cachedResults = ServiceCache.SubscriptionResultsCache[settings.ServiceTreeSettings.ServiceId];
            }

            if (cachedResults == null)
            {
                eventLogger.LogWarning($"Load Subscripitons for service {settings.ServiceTreeSettings.ServiceId}");

#pragma warning disable CS8602, CS8604
                SubscriptionFactory factory = new SubscriptionFactory(
                    tokenProvider,
                    mapper,
                    settings.KustoSettings.ServiceTreeEndpoint,
                    settings.KustoSettings.ServiceTreeDatabase,
                    settings.ServiceTreeSettings.ServiceId
                    );
#pragma warning restore CS8602, CS8604

                // Always get them all because we can have different filters. 
                cachedResults = factory.GetNonProdSubscriptions(null);

                if (ServiceCache.SubscriptionResultsCache.ContainsKey(settings.ServiceTreeSettings.ServiceId))
                {
                    ServiceCache.SubscriptionResultsCache[settings.ServiceTreeSettings.ServiceId] = cachedResults;
                }
                else
                {
                    ServiceCache.SubscriptionResultsCache.Add(settings.ServiceTreeSettings.ServiceId, cachedResults);
                }
            }

            // Now trim down to whatever was asked for
            SubscriptionResults returnResults = cachedResults;
            if (optional != null && optional.Count > 0)
            {
                // Have to build it up
                returnResults = new SubscriptionResults();

                returnResults.Subscriptions = cachedResults.Subscriptions
                    .Where(x => optional.Contains(x.ServiceSubscriptionsDTO.SubscriptionId))
                    .ToList();
                returnResults.UnreachableSubscriptions = cachedResults.UnreachableSubscriptions
                    .Where(x => optional.Contains(x.SubscriptionId))
                    .ToList();
            }

            return returnResults;
        }
        #endregion

        #region DNSRecords Cache
        /// <summary>
        /// Example of a cache call. 
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public static DNSRecords GetDNSCache(params object[] inputs)
        {
            ITokenProvider? tokenProvider = (ITokenProvider?)ServiceCache.GetParamByType<ITokenProvider>(inputs);
            IEventLogger? eventLogger = (IEventLogger?)ServiceCache.GetParamByType<IEventLogger>(inputs);
            DNSEnvironment? dnSEnvironment = (DNSEnvironment?)ServiceCache.GetParamByType<DNSEnvironment>(inputs);

            DNSRecords? returnValue = null;
            if (ServiceCache.DNSRecordsCache.ContainsKey(dnSEnvironment))
            {
                DateTime expires = ServiceCache.DNSRecordsCache[dnSEnvironment].Expires;
                if (expires < DateTime.Now)
                {
                    eventLogger.LogWarning("DNS Cache Expired for {dns}", dnSEnvironment.ZoneName);
                    ServiceCache.DNSRecordsCache.Remove(dnSEnvironment);
                }
                else
                {
                    returnValue = ServiceCache.DNSRecordsCache[dnSEnvironment];
                }
            }

            if (returnValue == null)
            {
                eventLogger.LogInfo($"Loading DNS data for {dnSEnvironment.ZoneName}");
                try
                {
                    DNSRecords updatedRecords = new DNSRecords();

                    ArmClient dnsArmClient = tokenProvider.GetAzureArmClient(dnSEnvironment.Subscription);
                    SubscriptionResource subscription = dnsArmClient.GetDefaultSubscription();
                    // first we need to get the resource group
                    ResourceGroupResource resourceGroup = subscription.GetResourceGroups().Get(dnSEnvironment.ResourceGroup);
                    // Now we get the DnsZone collection from the resource group
                    DnsZoneResource dnsZoneResource = resourceGroup.GetDnsZones().Get(dnSEnvironment.ZoneName);

                    updatedRecords.CnameRecords = dnsZoneResource.GetDnsCnameRecords().ToList();
                    updatedRecords.ARecords = dnsZoneResource.GetDnsARecords().ToList();

                    ServiceCache.DNSRecordsCache.Add(dnSEnvironment, updatedRecords);
                    returnValue = updatedRecords;
                }
                catch (Exception ex)
                {
                    eventLogger.LogException($"Exception on DNS {dnSEnvironment.ZoneName}", ex);
                }

            }

            return returnValue;
        }
        #endregion

    }
}
