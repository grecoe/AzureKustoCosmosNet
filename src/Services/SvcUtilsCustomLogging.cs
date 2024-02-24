//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Services
{
    using SubscriptionCleanupUtils.Models;

    internal class SvcUtilsCustomLogging
    {

        public static void ReportGroupExpirationTagging<T>(ILogger<T> logger, GroupExpirationResult expirationResult)
            where T : class
        {
            List<string> expired = expirationResult.ExpiredGroups.Select(x => x.Name).ToList();
            Dictionary<string, List<string>> subLayout = new Dictionary<string, List<string>>()
            {
                { "Recently Tagged" , expirationResult.TaggedGroups},
                { "Unable To Tag" , expirationResult.TagFailureGroups},
                { "Expired But Protected" , expirationResult.ExpiredButProtectedGroups},
                { "Previous Delete Attempts" , expirationResult.PreviousDeleteAttemptGroups},
                { "Expired" , expired},
            };

            string formatted = Newtonsoft.Json.JsonConvert.SerializeObject(subLayout, Newtonsoft.Json.Formatting.Indented);

            logger.LogInformation("Group Expiration Results");
            logger.LogInformation(formatted);
        }


    }
}
