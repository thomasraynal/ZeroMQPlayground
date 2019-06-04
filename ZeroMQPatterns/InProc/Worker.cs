using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQPlayground.ZeroMQPatterns.InProc
{
    public class Worker<T> where T : IWork
    {
        private readonly Thread _thread;
        private CancellationToken _cancel;
        private Guid _id;
        private Queue<ProducerAction<T>> _producers;
        private object _locker = new object();
        private readonly ManualResetEventSlim _reset = new ManualResetEventSlim(false);

        public Worker(CancellationToken cancel)
        {
            LastAwoken = DateTime.MaxValue.Ticks;

            _thread = new Thread(Consume);
            _cancel = cancel;
            _id = Guid.NewGuid();
            _producers = new Queue<ProducerAction<T>>();
            _thread.Start();
        }

        public void Enqueue(ProducerAction<T> producer)
        {
            _producers.Enqueue(producer);
            LastAwoken = DateTime.Now.Ticks;
            _reset.Set();
        }

        public long LastAwoken { get; private set; }

        public void Consume()
        {
            while (!_cancel.IsCancellationRequested)
            {
                _reset.Wait();

                if (_producers.TryDequeue(out var producer))
                {
                    var result = producer.Produce();
                    result.ProducerId = _id;
                    producer.OnFinished(result);
                }
            }
        }
    }
}
