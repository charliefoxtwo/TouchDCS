using System;

namespace DcsBiosCommunicator
{
    public sealed class MaskShift
    {
        public int Mask { get; }
        public int Shift { get; }
        public Action<int> Action { get; }

        public int LastValue { get; set; }

        public MaskShift(int mask, Action<int> action)
        {
            Mask = mask;
            Shift = (int) Math.Log2(mask);
            Action = action;
        }

        public MaskShift(int mask, int shift, Action<int> action)
        {
            Mask = mask;
            Shift = shift;
            Action = action;
        }

        public static int GetValue(in int data, in int mask, in int shift)
        {
            return (data & mask) >> shift;
        }
    }
}