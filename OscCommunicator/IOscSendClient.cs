namespace OscCommunicator
{
    public interface IOscSendClient
    {
        bool Send(string address, object data);
        string DeviceIpAddress { get; }
    }
}