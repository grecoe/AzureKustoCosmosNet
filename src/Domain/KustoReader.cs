//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils.Domain
{
    using AutoMapper;
    using Azure.Core;
    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;
    using System.Data;

    internal class KustoReader
    {
        private readonly string _cluster;
        private readonly string _database;
        private readonly TokenCredential _credential;
        private readonly KustoConnectionStringBuilder _kcsb;
        private readonly IMapper _mapper;

        public KustoReader(
            TokenCredential tokenCredential, 
            IMapper mapper,
            string cluster, 
            string database
            )
        {
            _mapper = mapper;
            _cluster = cluster;
            _database = database;
            _credential = tokenCredential;

            _kcsb = new KustoConnectionStringBuilder(_cluster, _database)
                .WithAadAzureTokenCredentialsAuthentication(_credential);
        }

        public List<T> ReadData<T>(string queryString)
        {
            List<T> returnList = new List<T>();

            using (var queryProvider = KustoClientFactory.CreateCslQueryProvider(_kcsb))
            {
                var clientRequestProperties = new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() };
                using (var reader = queryProvider.ExecuteQuery(queryString, clientRequestProperties))
                {
                    returnList = this._mapper.Map<IDataReader, List<T>>(reader);
                }
            }

            return returnList;
        }
    }
}

