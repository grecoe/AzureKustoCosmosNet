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
    using SubscriptionCleanupUtils.Models;

    public class Program
    {
        public static void Main(string[] args)
        {
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

            // Each service has it's onw flags on whether they should run or not, just launch 
            // them all, but we'll use our own running state so when all background services 
            // finish the application will terminate
            BackgroundServiceRunningState appModel = new BackgroundServiceRunningState();
            appModel.RegisterBackgroundService<SvcADMECleanup>();
            appModel.RegisterBackgroundService<SvcExpirationCheck>();
            appModel.RegisterBackgroundService<SvcLiveView>();
            appModel.RegisterBackgroundService<SvcDNSCleanup>();

            builder.Services.AddHostedService<SvcADMECleanup>();
            builder.Services.AddHostedService<SvcExpirationCheck>();
            builder.Services.AddHostedService<SvcLiveView>();
            builder.Services.AddHostedService<SvcDNSCleanup>();

            builder.Services.AddSingleton<SubscriptionCleanupUtils.Domain.Interface.ITokenProvider, TokenProvider>();
            builder.Services.AddSingleton<AutoMapper.IMapper>(mapper);
            builder.Services.AddSingleton<BackgroundServiceRunningState>(appModel);

            var host = builder.Build();
            host.Run();
        }
    }
}