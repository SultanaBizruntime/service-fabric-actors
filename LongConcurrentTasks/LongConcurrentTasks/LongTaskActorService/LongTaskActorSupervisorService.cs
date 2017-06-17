using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LongTaskActorService.Interfaces;
using LongTaskActorService.Models;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace LongTaskActorService
{
    [ActorService(Name = "LongTaskActorSupervisorService")]
    [StatePersistence(StatePersistence.Persisted)]
    public class LongTaskActorSupervisorService : Actor, ILongTaskActorSupervisorService
    {
        private const int ActorsLimit = 20;
        private readonly Uri _processorActorUri = new Uri("fabric:/LongConcurrentTasks/LongTaskActorProcessorService");

        public LongTaskActorSupervisorService(ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
        }

        public async Task StartProcessingAsync(Message<int[]> message)
        {
            try
            {
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(message.Id.ToString());

                if (result.HasValue)
                {
                    ActorEventSource.Current.Message($"SupervisorActor=[{Id}] is already processing MessageId=[{message.Id}].");
                    return;
                }

                var cancellationTokenSource = new CancellationTokenSource();
                await StateManager.TryAddStateAsync(message.Id.ToString(), cancellationTokenSource, cancellationTokenSource.Token);

                foreach (var chunkedCollection in message.Payload.Chunk(ActorsLimit))
                {
                    var processingActorProxy = ActorProxy.Create<ILongTaskActorProcessorService>(ActorId.CreateRandom(), _processorActorUri);
                    await processingActorProxy.ProcessAsync(Id.ToString(), new Message<int[]>
                    {
                        Id = Guid.NewGuid(),
                        Payload = chunkedCollection.ToArray()
                    }, cancellationTokenSource.Token);
                }
            }
            catch (Exception exception)
            {
                ActorEventSource.Current.Message($"Error occured in SupervisorActor=[{Id}]: {exception}");
            }
        }
    }
}
