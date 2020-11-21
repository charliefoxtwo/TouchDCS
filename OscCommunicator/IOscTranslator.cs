namespace OscCommunicator
{
    public interface IOscTranslator
    {
        public void FromOsc<T>(string ipAddress, string address, T data);
    }
}