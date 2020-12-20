namespace OscCommunicator
{
    public interface IOscSendClient
    {
        void Send(string address, object data);
        string DeviceIpAddress { get; }
    }
}