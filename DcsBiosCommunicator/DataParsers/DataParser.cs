namespace DcsBiosCommunicator.DataParsers
{
    public abstract class DataParser<T>
    {
        public int Address { get; }
        public string BiosCode { get; set; }

        public T? LastValue { get; set; }

        public abstract bool DataReady { get; }

        private int _lastSync = 0;

        // TODO: allow global configuration?
        private const int ForceSyncCutoff = 1;

        protected DataParser(in int address, in string biosCode)
        {
            Address = address;
            BiosCode = biosCode;
        }

        public bool NeedsSync(T value)
        {
            if (value?.Equals(LastValue) == true && _lastSync++ < ForceSyncCutoff) return false;

            _lastSync = 0;
            return true;
        }

        public abstract bool TryGetValue(in int address, in int data, out T value);
    }
}