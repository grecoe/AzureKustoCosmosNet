//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models.AppSettings
{
    /*
        "ExpirationCheckService": {
      "DaysToExpiration": 4,
      "ExecuteCleanup" : false,
      "TimeoutHours": 96
    },
    "ADMECleanup": {
    "ExecuteCleanup" :  true,
      "TimeoutHours": 24
    }
    */

    internal class BaseServiceSettings
    {
        public bool ExecuteCleanup { get; set; } = false;
        public int TimeoutHours { get; set; } = 24;

        public int GetTimeoutMilliseconds()
        {
            return this.TimeoutHours* 60 * 1000;
        }
    }

    internal class ExpirationService : BaseServiceSettings
    {
        public int DaysToExpiration { get; set; } = 4;
    }

    /// <summary>
    /// Timing information for different processes
    /// </summary>
    internal class ExecutionSettings
    {
        public const string SECTION = "ExecutionSettings";

        public BaseServiceSettings ADMECleanupService { get; set; } = new BaseServiceSettings();
        public ExpirationService ExpirationService { get; set; } = new ExpirationService();
    }
}
