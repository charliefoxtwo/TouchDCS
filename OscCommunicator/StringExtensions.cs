using OscCore.Address;

namespace OscCommunicator
{
    public static class StringExtensions
    {
        public static bool IsValidOscAddress(this string str)
        {
            return OscAddress.IsValidAddressLiteral(str);
        }
    }
}
