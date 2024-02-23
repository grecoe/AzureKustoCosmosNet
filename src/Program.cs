//
// Copyright (c) 2024 Microsoft 
//
namespace SubscriptionCleanupUtils
{
    using SubscriptionCleanupUtils.Services;
    using AutoMapper;
    using AutoMapper.Data;
    using System.Data;
    using SubscriptionCleanupUtils.Domain;
    using SubscriptionCleanupUtils.Models.Kusto;

    public class Program
    {
        public static void Main(string[] args)
        {
            //CosmosTest test = new CosmosTest();

            // Kusto data mappers for ADME Cleanup task. If you add more of queries/results
            // you will need to update this configuration to map the new classes. 
            AutoMapper.IConfigurationProvider configuration = new MapperConfiguration(cfg =>
            {
                // https://github.com/AutoMapper/AutoMapper.Data
                cfg.AddDataReaderMapping();
                cfg.CreateMap<IDataRecord, ServiceSubscriptionsDTO>();
                cfg.CreateMap<IDataRecord, ADMEResourcesDTO>();
            });
            AutoMapper.IMapper mapper = new Mapper(configuration);//configuration.CreateMapper();


            // Actual build
            var builder = Host.CreateApplicationBuilder(args);

            // ADME problem instance cleanup
            builder.Services.AddHostedService<SvcADMECleanup>();

            /// WARNING WARNING - This isn't fully baked because
            /// - It will not protect anything except managed groups or ones with delete=false tag
            ///   meaning , for example Azure Default group, will be wiped if tags are not kept in place. 
            ///   Even if allowed to run, it will ONLY tag untagged groups with the expiration tag and 
            ///   report what would have happened because the delete logic is commented out.
            /// - It is not complete. If an ADME instance is a target for expiration, there is more work
            ///   to do like updating Cosmos and removing DNS records. 
            // builder.Services.AddHostedService<SvcExpirationCheck>();
            /// WARNING WARNING

            builder.Services.AddSingleton<SubscriptionCleanupUtils.Domain.Interface.ITokenProvider, TokenProvider>();
            builder.Services.AddSingleton<AutoMapper.IMapper>(mapper);

            var host = builder.Build();
            host.Run();
        }
    }
}