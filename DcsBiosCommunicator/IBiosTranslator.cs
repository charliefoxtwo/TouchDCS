namespace DcsBiosCommunicator
{
    public interface IBiosTranslator
    {
        void FromBios<T>(string biosCode, T data);
    }
}