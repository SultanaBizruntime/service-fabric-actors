using System.Threading;
using System.Threading.Tasks;
using LongTaskActorService.Models;
using Microsoft.ServiceFabric.Actors;

namespace LongTaskActorService.Interfaces
{
    public interface ILongTaskActorProcessorService : IActor
    {
        Task ProcessAsync(string supervisorId, Message<int[]> message, CancellationToken cancellationToken);
        Task<string> HelloFromActorAsync();
    }
}
