using System;
using System.Text;

namespace DcsBiosCommunicator.DataParsers
{
    public sealed class StringParser : DataParser<string>
    {
        public override bool DataReady => !_voidBuffer;

        public int Length { get; }
        private readonly char[] _buffer;

        private string? _currentValue;
        public string CurrentValue
        {
            get
            {
                if (_currentValue is null || _voidBuffer)
                {
                    _voidBuffer = false;
                    _currentValue = string.Concat(_buffer);
                }

                return _currentValue;
            }
        }
        private bool _voidBuffer = true;

        public StringParser(in int address, in int length, in string biosCode) : base(address, biosCode)
        {
            Length = length;
            _buffer = new char[length];
        }

        private void SetCharacter(int index, char ch)
        {
            _voidBuffer = true;
            _buffer[index] = ch;
        }

        /// <summary>
        /// Builds upon the existing string, and provides the completed string if available.
        /// </summary>
        /// <remarks>
        /// The output of this command really isn't all that accurate for strings. It will return true if the provided
        /// address is at the end of the address range for the string, but otherwise will return false. 
        /// </remarks>
        /// <param name="address">BIOS Address the data came from</param>
        /// <param name="data">Data received at the provided BIOS address</param>
        /// <param name="value">The value of the string, if completed</param>
        /// <returns></returns>
        public override bool TryGetValue(in int address, in int data, out string value)
        {
            value = string.Empty;
            var done = false;
            if (Address <= address && address < Address + Length)
            {
                var dataBytes = Encoding.UTF8.GetString(BitConverter.GetBytes(data));
                SetCharacter(address - Address, dataBytes[0]);
                if (address - Address + 1 == Length) done = true;
                if (Address + Length > address + 1)
                {
                    if (address - Address + 2 == Length) done = true;
                    SetCharacter(address - Address + 1, dataBytes[1]);
                }
            }

            if (done)
            {
                value = CurrentValue;
                return true;
            }

            return false;
        }
    }
}