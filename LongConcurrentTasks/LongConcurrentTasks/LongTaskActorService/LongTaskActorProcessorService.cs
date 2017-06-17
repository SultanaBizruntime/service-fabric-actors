using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using LongTaskActorService.Interfaces;
using LongTaskActorService.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace LongTaskActorService
{
    [ActorService(Name = "LongTaskActorProcessorService")]
    [StatePersistence(StatePersistence.Persisted)]
    internal class LongTaskActorProcessorService : Actor, ILongTaskActorProcessorService, IRemindable
    {
        private const string RedisKey = "Test";
        private const string RedisConnectionString = "127.0.0.1:6379";
        private IConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _redisDb;

        public LongTaskActorProcessorService(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);
            _redisDb = _connectionMultiplexer.GetDatabase();
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
        }

        protected override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
            _connectionMultiplexer.Close();
        }

        public async Task ProcessAsync(string supervisorId, Message<int[]> message, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.TryAddStateAsync(message.Id.ToString(), message, cancellationToken);
                await StateManager.TryAddStateAsync(Id.ToString(), cancellationToken, cancellationToken);

                await RegisterReminderAsync(message.Id.ToString(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(-1));
            }
            catch (Exception exception)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(exception.ToString());
            }
        }

        public async Task<string> HelloFromActorAsync()
        {
            await _redisDb.SetAddAsync(RedisKey, "Hello");
            return "Hello";
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            try
            {
                GetReminder(reminderName);
                var reminder = GetReminder(reminderName);

                if (reminder != null)
                {
                    await UnregisterReminderAsync(reminder);
                }
                
                var message = await StateManager.TryGetStateAsync<Message<int[]>>(reminderName);

                if (message.HasValue)
                {
                    foreach (var value in message.Value.Payload)
                    {
                        await _redisDb.SetAddAsync(RedisKey, value);
                    }
                }
                else
                {
                    ActorEventSource.Current.ActorMessage(this, "Received message has no value");
                }
            }
            catch (Exception exception)
            {
                ActorEventSource.Current.ActorHostInitializationFailed($"Error occured in ProcessorActor=[{Id}]: {exception}");
            }
        }
    }
}
