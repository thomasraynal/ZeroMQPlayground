using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.InProc
{
    public class Work : IWork
    {
        public Guid ProducerId { get; set; }
    }

    [TestFixture]
    public class TestInProc
    {
        [Test]
        public async Task TestE2E()
        {
            var bag = new ConcurrentBag<Work>();
            var cancel = new CancellationTokenSource();
            var router = new Router<Work>(cancel.Token);
           
            Func<Work> unitOfWork = () =>
            {
                Task.Delay(250).Wait();
                return new Work();
            };

            Action<Work> unitOfWorkHandle = (work) =>
            {
                bag.Add(work);
            };

            
            for(var i=0; i < 100; i++)
            {
                router.Produce(unitOfWork, unitOfWorkHandle);
            }

            await Task.Delay(3000);

        }
    }
}
