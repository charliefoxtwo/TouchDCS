using System;
using System.Text;

namespace DcsBiosCommunicator.DataParsers
{
    public sealed class StringParser : DataParser<string>
    {
        public bool DataReady { get; private set; }

        public int Length { get; }
        private readonly char[] _buffer;

        public StringParser(in int address, in int length, in string biosCode) : base(address, biosCode)
        {
            Length = length;
            _buffer = new char[length];
        }

        private void SetCharacter(int index, char ch)
        {
            DataReady = false;
            _buffer[index] = ch;
        }

        /// <summary>
        /// Builds upon the existing string data
        /// </summary>
        /// <param name="address">BIOS Address the data came from</param>
        /// <param name="data">Data received at the provided BIOS address</param>
        /// <returns></returns>
        public override void AddData(in int address, in int data)
        {
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
                DataReady = true;
            }

            CurrentValue = string.Concat(_buffer);
        }
    }
}