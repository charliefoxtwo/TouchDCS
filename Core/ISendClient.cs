using System.Threading.Tasks;

namespace Core
{
    public interface ISendClient
    {
        Task Send(string address, object data);
        string DeviceIpAddress { get; }
    }
}