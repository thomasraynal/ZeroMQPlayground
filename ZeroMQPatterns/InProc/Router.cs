using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.InProc
{
    public class Router<T> where T : IWork
    {
        private List<Worker<T>> _workers;
        private readonly CancellationToken _token;

        public Router(CancellationToken token)
        {
            _workers = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => new Worker<T>(token)).ToList();
        }

        public void Produce(Func<T> produce, Action<T> onFinished)
        {
            var next = _workers.OrderByDescending(worker => worker.LastAwoken).First();

            next.Enqueue(new ProducerAction<T>()
            {
                OnFinished = onFinished,
                Produce = produce
            });
        }
    }
}
