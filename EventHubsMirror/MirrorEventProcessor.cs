using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventHubsMirror
{
    public class MirrorEventProcessor : IEventProcessor
    {
        readonly ILogger logger;
        readonly ITopicClient sender;

        public MirrorEventProcessor()
        {
            logger = Program.LoggerFactory.CreateLogger("Processor");
            sender = new TopicClient(new ServiceBusConnectionStringBuilder(Program.Configuration.GetConnectionString("ServiceBusDestination")));
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            logger.LogInformation($"Close:{context.PartitionId}:{reason}");
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            logger.LogInformation($"Open:{context.PartitionId}");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            logger.LogError(error, $"Error:{context.PartitionId}:{error.Message}");
            Program.Close();
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            logger.LogTrace($"Process:{context.PartitionId}:Received {messages.Count()} messages");
            
            var busMessages = new List<Message>(messages.Count());
            var lastOffset = "";
            foreach (var m in messages)
            {
                logger.LogTrace($"Process:{context.PartitionId}:Decode offset={m.SystemProperties.Offset}");
                var data = Encoding.UTF8.GetString(m.Body.Array, m.Body.Offset, m.Body.Count);
                var message = new Message(Encoding.UTF8.GetBytes(data));
                busMessages.Add(message);
                lastOffset = m.SystemProperties.Offset;
            }

            foreach (var group in busMessages.GroupBy(m => m.PartitionKey)) {
                logger.LogDebug($"Process:{context.PartitionId}:Send group with partition key \"{group.Key}\"");
                await sender.SendAsync(group.ToArray());
            }

            logger.LogDebug($"Process:{context.PartitionId}:Checkpointing offset={lastOffset}");
            await context.CheckpointAsync();
            logger.LogDebug($"Process:{context.PartitionId}:Processed {messages.Count()} messages");
        }
    }
}