using System.Threading.Tasks;

namespace DcsBiosCommunicator
{
    public interface IBiosSendClient
    {
        Task Send(string address, string data);
    }
}