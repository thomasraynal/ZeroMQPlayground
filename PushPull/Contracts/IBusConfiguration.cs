namespace ZeroMQPlayground.PushPull
{
    public interface IBusConfiguration
    {
        bool IsPeerDirectory { get; set; }
        string DirectoryEndpoint { get; set; }
        string Endpoint { get; set; }
        string PeerName { get; set; }
    }
}