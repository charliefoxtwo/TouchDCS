using System;

namespace DcsBiosCommunicator.DataParsers
{
    public class IntegerParser : DataParser<int>
    {
        public int Mask { get; }
        public int Shift { get; }

        public override bool DataReady => true;

        public IntegerParser(in int mask, in string biosCode) : base(default, biosCode)
        {
            Mask = mask;
            Shift = (int) Math.Log2(mask);
        }

        public IntegerParser(in int mask, in int shift, in string biosCode) : base(default, biosCode)
        {
            Mask = mask;
            Shift = shift;
        }

        public override bool TryGetValue(in int address, in int data, out int value)
        {
            value = (data & Mask) >> Shift;
            return true;
        }
    }
}