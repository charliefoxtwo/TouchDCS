using System;
using System.Text;

namespace DcsBiosCommunicator.DataParsers
{
    public sealed class StringParser : DataParser<string>
    {
        public override bool DataReady => !_dirty;

        private bool _dirty;
        public int Length { get; }
        private readonly char[] _buffer;

        public StringParser(in int address, in int length, in string biosCode) : base(address, biosCode)
        {
            Length = length;
            _buffer = new char[length];
        }

        private void SetCharacter(int index, char ch)
        {
            _dirty = true;
            _buffer[index] = ch;
        }

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

            if ((done || address == 0xfffe) && _dirty)
            {
                _dirty = false;
                value = new string(_buffer);
                return true;
            }

            return false;
        }
    }
}