using System;

namespace DcsBiosCommunicator.DataParsers
{
    public class IntegerParser : DataParser<int>
    {
        private readonly int _mask;
        private readonly int _shift;

        public IntegerParser(in int mask, in string biosCode) : base(default, biosCode)
        {
            _mask = mask;
            _shift = (int) Math.Log2(mask);
        }

        public IntegerParser(in int mask, in int shift, in string biosCode) : base(default, biosCode)
        {
            _mask = mask;
            _shift = shift;
        }

        public override void AddData(in int address, in int data)
        {
            CurrentValue = (data & _mask) >> _shift;
        }
    }
}