namespace DcsBiosCommunicator.DataParsers
{
    public abstract class DataParser<T>
    {
        public int Address { get; }

        public string BiosCode { get; }

        public T CurrentValue { get; protected set; } = default!;

        protected DataParser(in int address, in string biosCode)
        {
            Address = address;
            BiosCode = biosCode;
        }

        public abstract void AddData(in int address, in int data);
    }
}