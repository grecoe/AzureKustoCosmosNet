namespace SubscriptionCleanupUtils.Domain.Interface
{
    internal interface IEventLogger : IDisposable
    {
        public string Subscription { get;set; }
        public string Service { get; set; }

        public void LogInfo(string message, object? payload = null);
        public void LogWarning(string message, object? payload = null);
        public void LogError( string message, object? payload = null);
        public void LogException(string message, Exception ex);
        public void LogEvent(string level, string subscription, string service, string message, object? payload = null);
    }
}
