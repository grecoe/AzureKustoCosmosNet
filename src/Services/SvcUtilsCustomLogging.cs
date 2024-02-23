//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Services
{
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Models;

    internal class SvcUtilsCustomLogging
    {
        public static void ReportSubscriptions<T>(ILogger<T> logger, SubscriptionResults results)
            where T : class
        {
            List<string> ValidSubs = results.Subscriptions.Select(x => x.ServiceSubscriptionsDTO.SubscriptionName).ToList();
            List<string> Unreachable = results.UnreachableSubscriptions.Select(x => x.SubscriptionName).ToList();
            Dictionary<string, List<string>> subLayout = new Dictionary<string, List<string>>()
            {
                { "Scannable" , ValidSubs},
                { "Unreachable" , Unreachable}
            };

            string formatted = Newtonsoft.Json.JsonConvert.SerializeObject(subLayout, Newtonsoft.Json.Formatting.Indented);

            logger.LogInformation("Subscription Results");
            logger.LogInformation(formatted);
        }

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

        public static void ReportProblematicADMEResources<T>(
            ILogger<T> logger,
            List<ADMEResourceCollection> invalidColletion,
            List<ADMEResource> abandoned
            )
            where T : class
        {
            List<string> invalidInstances = invalidColletion.Select(x => x.Parent.Resource.Name).ToList();
            List<string> abandonedInstance = abandoned.Select(x => x.Resource.Name).ToList();
            Dictionary<string, List<string>> subLayout = new Dictionary<string, List<string>>()
            {
                { "Invalid Instances" , invalidInstances},
                { "Abandoned Instances" , abandonedInstance}
            };

            string formatted = Newtonsoft.Json.JsonConvert.SerializeObject(subLayout, Newtonsoft.Json.Formatting.Indented);

            logger.LogInformation("Problematic ADME Instances");
            logger.LogInformation(formatted);
        }

        public static void ReportCleanupResults<T>(
            ILogger<T> logger,
            ADMEResourceCleanupResults cleanupData
            )
            where T : class
        {
            List<string> delete = cleanupData.DeleteList.Select(x => x.Resource.Name).ToList();
            List<string> investigate = cleanupData.InvestigationList.Keys.Select(x => x.InstanceName).ToList();
            Dictionary<string, List<string>> subLayout = new Dictionary<string, List<string>>()
            {
                { "Delete Groups" , delete},
                { "Investigate Instances" , investigate}
            };

            string formatted = Newtonsoft.Json.JsonConvert.SerializeObject(subLayout, Newtonsoft.Json.Formatting.Indented);

            logger.LogInformation("Instances to Delete");
            logger.LogInformation(formatted);
        }

    }
}
