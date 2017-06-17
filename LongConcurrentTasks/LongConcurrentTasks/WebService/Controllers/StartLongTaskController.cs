using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LongTaskActorService.Interfaces;
using LongTaskActorService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using StackExchange.Redis;

namespace WebService.Controllers
{
    [Route("api/longtask")]
    public class StartLongTaskController : Controller
    {
        [HttpGet("start")]
        public async Task<IActionResult> StartLongTask()
        {
            IEnumerable<int> aLotOfValuesToAddToRedis = Enumerable.Range(0, 1000000);
            ILongTaskActorSupervisorService actorProcessorService = ActorProxy.Create<ILongTaskActorSupervisorService>(ActorId.CreateRandom(),
                new Uri("fabric:/LongConcurrentTasks/LongTaskActorSupervisorService"));

            try
            {
                await actorProcessorService.StartProcessingAsync(new Message<int[]>
                {
                    Id = Guid.NewGuid(),
                    Payload = aLotOfValuesToAddToRedis.ToArray()
                });
            }
            catch (Exception exception)
            {
                return new ObjectResult(exception);
            }

            return Accepted();
        }

        [HttpGet("start/normalParallel")]
        public IActionResult StartLongTaskWithoutActor()
        {
            IEnumerable<int> aLotOfValuesToAddToRedis = Enumerable.Range(0, 1000000);
            IDatabase redisDb = ConnectionMultiplexer.Connect("localhost:6379").GetDatabase();

            Parallel.ForEach(aLotOfValuesToAddToRedis, itemToAdd => redisDb.SetAdd("Test", itemToAdd));

            return Ok();
        }
    }
}
