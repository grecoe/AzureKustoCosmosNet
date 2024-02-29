//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using AutoMapper;
    using Kusto.Cloud.Platform.Utils;
    using SubscriptionCleanupUtils.Domain.Interface;
    using SubscriptionCleanupUtils.Models.AppSettings;
    using SubscriptionCleanupUtils.Models.Kusto;

    internal class SubscriptionResults
    {
        public List<AzureSubscription> Subscriptions = new List<AzureSubscription>();
        public List<ServiceSubscriptionsDTO> UnreachableSubscriptions = new List<ServiceSubscriptionsDTO>();
    }

    internal class SubscriptionFactory
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly IMapper _mapper;
        private readonly string _serviceTreeConnection;
        private readonly string _serviceTreeDatabase;
        private readonly ServiceTreeSettings _serviceTreeServiceIdentity;

        public SubscriptionFactory(
            ITokenProvider tokenProvider, 
            IMapper mapper,
            string serviceTreeKustoConnection,
            string serviceTreeKustoDatabase,
            ServiceTreeSettings serviceTreeServiceIdentity)
        {
            _tokenProvider = tokenProvider;
            _mapper = mapper;
            _serviceTreeConnection = serviceTreeKustoConnection;
            _serviceTreeDatabase = serviceTreeKustoDatabase;
            _serviceTreeServiceIdentity = serviceTreeServiceIdentity;
        }

        /// <summary>
        /// Reads the Service Tree Kusto database to find all subscriptions related to 
        /// a specific service ID (identified in appsettings.json). 
        /// 
        /// Currently limited to only non-prod subs because prod requires that we run in the 
        /// AME tenant. 
        /// 
        /// Changing this to simply take a flag on WHICH sub type to return would e trivial, but
        /// would require another appsetting to indicate which ones. 
        /// 
        /// </summary>
        /// <param name="limitIdList">Optional list of string which are subscription id GUID 
        /// values. If this list exists, it will return ONLY the subs that were identified in it
        /// otherwise it returns all of them. 
        /// 
        /// Handy for debugging/testing so you can target a single sub.</param>
        /// <returns></returns>
        public SubscriptionResults GetNonProdSubscriptions(string[]? limitIdList = null)
        {
            SubscriptionResults returnResults = new SubscriptionResults();

            KustoReader reader = new KustoReader(
                _tokenProvider.Credential,
                this._mapper,
                this._serviceTreeConnection,
                this._serviceTreeDatabase);

            // Get all subscriptions for the service id
            string query = ServiceSubscriptionsDTO.GetServiceTreeQuery(this._serviceTreeServiceIdentity);
            List<ServiceSubscriptionsDTO> subscriptions = reader.ReadData<ServiceSubscriptionsDTO>(query);

            // Filter down to only active non-prod subs
            List<ServiceSubscriptionsDTO> nonProdActive = subscriptions
                .Where(sub => (sub.Status == "Active") && (sub.Environment != "Prod"))
                .ToList();

            // TODO: This limits it for dev purposes because loading them all takes too lon
            List<ServiceSubscriptionsDTO> filteredList = nonProdActive
                .Where( sub=> 
                    limitIdList == null
                    ||
                    limitIdList.Contains(sub.SubscriptionId) //== "b0844137-4c2f-4091-b7f1-bc64c8b60e9c")
                    )
                .ToList();
            // TODO: This limits it for dev purposes because loading them all takes too lon

            // Attempt to reach all of them, maybe possible, maybe not if identity does not have 
            // the appropriate settings. 
            foreach (ServiceSubscriptionsDTO dto in filteredList /* TODO: This is real ->nonProdActive */)
            {
#pragma warning disable CS0168 // Variable is declared but never used
                try
                {
                    AzureSubscription sub = new AzureSubscription(dto, _tokenProvider.GetAzureArmClient(dto.SubscriptionId));
                    if( sub != null)
                    {
                        returnResults.Subscriptions.Add(sub);
                    }
                }
                catch (Azure.RequestFailedException e)
                {
                    // e ignored because it means we can't reach the sub, don't have permission.
                    returnResults.UnreachableSubscriptions.Add(dto);
                }
#pragma warning restore CS0168 // Variable is declared but never used
            }

            return returnResults;
        }
    }
}
