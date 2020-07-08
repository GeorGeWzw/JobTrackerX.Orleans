﻿using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JobTrackerX.Entities
{
    public class ServiceBusWrapper
    {
        private static readonly Random Rand = new Random();

        public ServiceBusWrapper(IOptions<JobTrackerConfig> options)
        {
            var idGeneratorConfig = options.Value.IdGeneratorConfig;
            var actionHandlerConfig = options.Value.ActionHandlerConfig;
            ScaleSize = idGeneratorConfig.ScaleSize;
            CrashDistance = idGeneratorConfig.CrashDistance;
            IdQueueReceiver = new MessageReceiver(idGeneratorConfig.ConnStr, idGeneratorConfig.IdQueueEntityPath,
                ReceiveMode.ReceiveAndDelete);
            IdQueueSender = new MessageSender(idGeneratorConfig.ConnStr, idGeneratorConfig.IdQueueEntityPath);
            ManagementClient = new ManagementClient(idGeneratorConfig.ConnStr);
            ActionQueues =
                actionHandlerConfig.ActionQueues
                .Select(name => new QueueClient(actionHandlerConfig.ConnStr, name)).ToList();
            StateCheckQueues =
                actionHandlerConfig.StateCheckQueues
                .Select(name => new QueueClient(actionHandlerConfig.ConnStr, name)).ToList();
        }

        public IQueueClient GetRandomActionQueueClient()
        {
            int index = Rand.Next(ActionQueues.Count);
            return ActionQueues[index];
        }
        public IQueueClient GetRandomStateCheckQueueClient()
        {
            int index = Rand.Next(StateCheckQueues.Count);
            return StateCheckQueues[index];
        }

        public List<QueueClient> ActionQueues { get; set; } = new List<QueueClient>();
        public List<QueueClient> StateCheckQueues { get; set; } = new List<QueueClient>();
        public int ScaleSize { get; }
        public int CrashDistance { get; }
        public IMessageReceiver IdQueueReceiver { get; }
        public IMessageSender IdQueueSender { get; }
        public ManagementClient ManagementClient { get; }
    }

    public class StorageAccountWrapper
    {
        public CloudStorageAccount Account { get; set; }
        public Microsoft.Azure.Cosmos.Table.CloudStorageAccount TableAccount { get; set; }
    }

    public class IndexStorageAccountWrapper : StorageAccountWrapper
    {
    }

    public class LogStorageAccountWrapper : StorageAccountWrapper
    {
    }
}