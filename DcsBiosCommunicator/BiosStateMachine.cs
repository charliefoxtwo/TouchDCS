using System;
using System.Collections.Generic;

namespace DcsBiosCommunicator
{
    public class BiosStateMachine
    {
        private enum State
        {
            AddressLow,
            AddressHigh,
            CountLow,
            CountHigh,
            DataLow,
            DataHigh,
            WaitForSync,
        }

        private readonly Dictionary<State, Action<byte>> _actions;

        private State _state = State.WaitForSync;
        private int _syncByteCount;
        private int _address;
        private int _count;
        private int _data;

        public delegate void DataWriteDelegate(int address, int data);
        public delegate void FrameSyncDelegate();

        public event DataWriteDelegate? OnDataWrite;
        public event FrameSyncDelegate? OnFrameSync;

        public BiosStateMachine()
        {
            _actions = new Dictionary<State, Action<byte>>
            {
                [State.AddressLow] = AddressLow,
                [State.AddressHigh] = AddressHigh,
                [State.CountLow] = CountLow,
                [State.CountHigh] = CountHigh,
                [State.DataLow] = DataLow,
                [State.DataHigh] = DataHigh,
                [State.WaitForSync] = WaitForSync,
            };
        }

        public void ProcessBytes(IEnumerable<byte> data)
        {
            foreach (var b in data)
            {
                ProcessByte(b);
            }
        }

        public void ProcessByte(byte data)
        {
            _actions[_state].Invoke(data);

            if (data == 0x55)
            {
                _syncByteCount += 1;
            }
            else
            {
                _syncByteCount = 0;
            }

            WaitForSync(data);
        }

        private void AddressLow(byte data)
        {
            _address = data;
            _state = State.AddressHigh;
        }

        private void AddressHigh(byte data)
        {
            _address += data << 8;
            _state = _address == 0x5555 ? State.WaitForSync : State.CountLow;
        }

        private void CountLow(byte data)
        {
            _count = data;
            _state = State.CountHigh;
        }

        private void CountHigh(byte data)
        {
            _count += data << 8;
            _state = State.DataLow;
        }

        private void DataLow(byte data)
        {
            _data = data;
            _count -= 1;
            _state = State.DataHigh;
        }

        private void DataHigh(byte data)
        {
            _data += data << 8;
            _count -= 1;

            OnDataWrite?.Invoke(_address, _data);

            _address += 2;

            _state = _count > 0 ? State.DataLow : State.AddressLow;
        }

        private void WaitForSync(byte _)
        {
            if (_syncByteCount == 4)
            {
                _state = State.AddressLow;
                _syncByteCount = 0;
                OnFrameSync?.Invoke();
            }
        }
    }
}