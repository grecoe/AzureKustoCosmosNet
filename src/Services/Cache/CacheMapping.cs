namespace SubscriptionCleanupUtils.Services.Cache
{
    internal class CacheMapping
    {
        public string MethodName { get; set; } = string.Empty;
        public List<Type> RequiredTypes { get; set; } = new List<Type>();
    }
}
