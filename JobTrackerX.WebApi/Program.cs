﻿using JobTrackerX.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Hosting;
using JobTrackerX.Grains;
using JobTrackerX.Grains.InMem;

namespace JobTrackerX.WebApi
{
    public static class Program
    {
        private static readonly string SettingFileName = $"appsettings.{Constants.EnvName}.json";

        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(SettingFileName, false)
            .AddEnvironmentVariables()
            .Build();

        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            try
            {
                ThreadPool.GetMaxThreads(out _, out var completionThreads);
                ThreadPool.SetMinThreads(500, completionThreads);
                await CreateWebHostBuilder(args).Build().RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateWebHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config => config.AddJsonFile(SettingFileName))
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .UseOrleans((context, options) =>
                {
                    var jobTrackerConfig =
                       context.Configuration.GetSection(nameof(JobTrackerConfig)).Get<JobTrackerConfig>();
                    var siloConfig = jobTrackerConfig.SiloConfig;
                    var tableStorageOption = new Action<AzureTableStorageOptions>(tableStorageOptions =>
                    {
                        tableStorageOptions.ConnectionString = siloConfig.JobEntityPersistConfig.ConnStr;
                        if (string.IsNullOrEmpty(siloConfig.JobEntityPersistConfig.TableName)) return;
                        tableStorageOptions.TableName = siloConfig.JobEntityPersistConfig.TableName;
                        tableStorageOptions.UseJson = true;
                    });

                    var blobStorageOption = new Action<AzureBlobStorageOptions>(blobStorageOptions =>
                    {
                        blobStorageOptions.ConnectionString = siloConfig.ReadOnlyJobIndexPersistConfig.ConnStr;
                        if (string.IsNullOrEmpty(siloConfig.ReadOnlyJobIndexPersistConfig.ContainerName)) return;
                        blobStorageOptions.ContainerName = siloConfig.ReadOnlyJobIndexPersistConfig.ContainerName;
                        blobStorageOptions.UseJson = true;
                    });

                    options.UseLocalhostClustering(serviceId: siloConfig.ServiceId, clusterId: siloConfig.ClusterId)
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(JobGrain).Assembly).WithReferences().WithCodeGeneration())
                        .AddIncomingGrainCallFilter<BufferFilter>()
                        .AddAzureTableGrainStorage(Constants.JobEntityStoreName, tableStorageOption)
                        .AddAzureTableGrainStorage(Constants.JobRefStoreName, tableStorageOption)
                        .AddAzureTableGrainStorage(Constants.JobIdStoreName, tableStorageOption)
                        .AddAzureTableGrainStorage(Constants.JobIdOffsetStoreName, tableStorageOption)
                        .AddAzureBlobGrainStorage(Constants.ReadOnlyJobIndexStoreName, blobStorageOption)
                        .AddAzureBlobGrainStorage(Constants.AttachmentStoreName, blobStorageOption)
                        .AddAzureBlobGrainStorage(Constants.AppendStoreName, blobStorageOption)
                       .Configure<GrainCollectionOptions>(grainCollectionOptions =>
                       {
                           grainCollectionOptions.CollectionAge = siloConfig.GrainCollectionAge ?? TimeSpan.FromMinutes(10);
                           grainCollectionOptions.ClassSpecificCollectionAge[typeof(AggregateJobIndexGrain).FullName ?? throw new
                                                                  InvalidOperationException()] = TimeSpan.FromMinutes(5);
                           grainCollectionOptions.ClassSpecificCollectionAge[typeof(RollingJobIndexGrain).FullName ?? throw new
                                                                  InvalidOperationException()] = TimeSpan.FromMinutes(5);
                       });

                    if (jobTrackerConfig.CommonConfig.UseDashboard)
                    {
                        options.UseDashboard(x => x.HostSelf = false);
                    }
                })
                .UseSerilog();
        }
    }
}