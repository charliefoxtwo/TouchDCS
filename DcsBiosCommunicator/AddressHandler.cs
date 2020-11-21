using System.Collections.Generic;
using System.Linq;
using DcsBiosCommunicator.DataParsers;

namespace DcsBiosCommunicator
{
    public class IntegerHandler
    {
        public int Address { get; }

        public IList<IntegerParser> MaskShifts { get; }

        public IntegerHandler(in int address, IEnumerable<IntegerParser> maskShifts)
        {
            Address = address;
            MaskShifts = maskShifts.ToList();
        }
    }
}