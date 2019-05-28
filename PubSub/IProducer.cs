namespace ZeroMQPlayground.PubSub
{
    public interface IProducer
    {
        void Start();
        void Stop();
    }


    public interface IProducer<TEvent> : IProducer
    {
        TEvent Next();
    }

}