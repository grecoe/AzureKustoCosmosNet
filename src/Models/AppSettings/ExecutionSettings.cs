//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Models.AppSettings
{
    internal class BaseServiceSettings
    {
        /// <summary>
        /// Services check this flag, if set, it does not run the service
        /// </summary>
        public bool IsActive { get; set; } = false;
        /// <summary>
        /// For tasks that perform side effect moves with deletes, this flag 
        /// represents the -WhatIf option. If set to false, side effects of deletions
        /// should not occur.
        /// </summary>
        public bool ExecuteCleanup { get; set; } = false;
        /// <summary>
        /// If running continuous, the number of hours to sleep between each run.
        /// </summary>
        public int TimeoutHours { get; set; } = 24;
        /// <summary>
        /// Flag to indicate if the service should continue to run, or one and done. 
        /// </summary>
        public bool RunContinuous { get; set; } = false;

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
        public BaseServiceSettings LiveViewService { get; set; } = new BaseServiceSettings();
        public ExpirationService ExpirationService { get; set; } = new ExpirationService();
    }
}
