using System.Collections.Generic;
using System.Threading.Tasks;
using LongTaskActorService.Models;
using Microsoft.ServiceFabric.Actors;

namespace LongTaskActorService.Interfaces
{
    public interface ILongTaskActorSupervisorService : IActor
    {
        Task StartProcessingAsync(Message<int[]> values);
    }
}
