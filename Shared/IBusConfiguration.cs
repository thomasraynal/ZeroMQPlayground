namespace ZeroMQPlayground.Shared
{
    public interface IBusConfiguration
    {
        string Endpoint { get; set; }
        string PeerName { get; set; }
    }
}