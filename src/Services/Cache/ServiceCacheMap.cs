namespace SubscriptionCleanupUtils.Services.Cache
{
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Models;
    using AutoMapper;
    using SubscriptionCleanupUtils.Models.AppSettings;

    internal class ServiceCacheMap
    {
        public static Dictionary<Type, CacheMapping> FullMap = new Dictionary<Type, CacheMapping>()
        {
            {
                typeof(SubscriptionResults),
                new CacheMapping() {
                    MethodName = "GetSubscriptionCache",
                    RequiredTypes = new List<Type>() {
                        typeof(ITokenProvider),
                        typeof(IEventLogger),
                        typeof(IMapper),
                        typeof(ServiceSettings),
                        typeof(List<string>)
                    }
                }
            },
            {
                typeof(DNSRecords),
                new CacheMapping() {
                    MethodName = "GetDNSCache",
                    RequiredTypes = new List<Type>() {
                        typeof(ITokenProvider),
                        typeof(IEventLogger),
                        typeof(DNSEnvironment)
                    }
                }
            }
        };
    }
}
