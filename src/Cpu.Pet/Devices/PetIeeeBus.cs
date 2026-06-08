namespace Cpu.Pet.Devices;

public sealed class PetIeeeBus
{
    private enum BusState { Idle, Command, DataOut, DataIn }

    private readonly List<IIeeeDevice> _devices = [];
    private BusState _state = BusState.Idle;
    private int _listenerAddr = -1;
    private int _talkerAddr = -1;
    private byte _listenerSec;
    private byte _talkerSec;
    private IIeeeDevice? _listenerDevice;
    private IIeeeDevice? _talkerDevice;
    private byte _lastDio;
    private byte _cachedInput;
    private bool _hasCachedInput;

    public byte LastDio => _lastDio;
    public bool DAV { get; private set; }

    public byte GetCurrentDio()
    {
        if (_hasCachedInput)
            return _cachedInput;
        return 0xFF;
    }
    public bool NRFD { get; private set; }
    public bool NDAC { get; private set; }

    public void AttachDevice(IIeeeDevice device) => _devices.Add(device);

    public void OnATNWrite(bool atn)
    {
        if (atn)
        {
            switch (_state)
            {
                case BusState.DataOut:
                    _listenerDevice?.Close();
                    break;
                case BusState.DataIn:
                    _talkerDevice?.Close();
                    break;
            }
            _state = BusState.Command;
        }
        else
        {
            if (_listenerDevice != null)
            {
                _state = BusState.DataOut;
                _listenerDevice.OpenForRead(_listenerSec);
            }
            else if (_talkerDevice != null)
            {
                _state = BusState.DataIn;
                _talkerDevice.OpenForWrite(_talkerSec);
            }
            else
            {
                _state = BusState.Idle;
            }
        }
    }

    public void OnDioWrite(byte data)
    {
        _lastDio = data;

        switch (_state)
        {
            case BusState.Command:
                ProcessCommandByte(data);
                break;
            case BusState.DataOut:
                _listenerDevice?.Write(data);
                AcceptHandshake();
                break;
        }
    }

    public byte OnDioRead()
    {
        if (_hasCachedInput)
        {
            _hasCachedInput = false;
            _lastDio = _cachedInput;
            ProvideHandshake();
            return _cachedInput;
        }
        _lastDio = 0xFF;
        return 0xFF;
    }

    public byte GetViaPortBInput()
    {
        byte result = 0;
        if (NDAC) result |= 0x01;
        if (NRFD) result |= 0x40;
        if (DAV) result |= 0x80;
        return result;
    }

    public void CompleteHandshake()
    {
        DAV = true;
        NRFD = false;
        NDAC = false;
    }

    public void SignalDAV()
    {
        DAV = true;
    }

    public void Tick()
    {
        if (!_hasCachedInput && _state == BusState.DataIn && _talkerDevice is { } dev)
        {
            if (dev.TryRead(out byte data))
            {
                _cachedInput = data;
                _hasCachedInput = true;
                DAV = true;
                NRFD = false;
                NDAC = true;
            }
        }
    }

    private void ProcessCommandByte(byte cmd)
    {
        if (cmd >= 0x20 && cmd <= 0x3E)
        {
            _listenerAddr = cmd & 0x1F;
            _listenerDevice = FindDevice(_listenerAddr);
            CommandHandshake();
        }
        else if (cmd == 0x3F)
        {
            _listenerDevice?.Close();
            _listenerAddr = -1;
            _listenerDevice = null;
            CommandHandshake();
        }
        else if (cmd >= 0x40 && cmd <= 0x5E)
        {
            _talkerAddr = cmd & 0x1F;
            _talkerDevice = FindDevice(_talkerAddr);
            CommandHandshake();
        }
        else if (cmd == 0x5F)
        {
            _talkerDevice?.Close();
            _talkerAddr = -1;
            _talkerDevice = null;
            CommandHandshake();
        }
        else if (cmd >= 0x60 && cmd <= 0x7F)
        {
            byte sec = (byte)(cmd & 0x1F);
            if (_listenerDevice != null)
                _listenerSec = sec;
            if (_talkerDevice != null)
                _talkerSec = sec;
            CommandHandshake();
        }
        else if (cmd >= 0xE0 && cmd <= 0xFF)
        {
            CommandHandshake();
        }
        else
        {
            CommandHandshake();
        }
    }

    private void CommandHandshake()
    {
        DAV = true;
        NRFD = true;
        NDAC = true;
    }

    private void AcceptHandshake()
    {
        DAV = true;
        NRFD = true;
        NDAC = true;
    }

    private void ProvideHandshake()
    {
        DAV = true;
        NRFD = false;
        NDAC = true;
    }

    private IIeeeDevice? FindDevice(int primaryAddr)
    {
        for (int i = 0; i < _devices.Count; i++)
        {
            if (_devices[i].PrimaryAddress == primaryAddr)
                return _devices[i];
        }
        return null;
    }

    public void Reset()
    {
        _listenerDevice?.Close();
        _talkerDevice?.Close();
        _state = BusState.Idle;
        _listenerAddr = -1;
        _talkerAddr = -1;
        _listenerDevice = null;
        _talkerDevice = null;
        _lastDio = 0;
        DAV = true;
        NRFD = false;
        NDAC = false;
        _listenerSec = 0;
        _talkerSec = 0;
        _cachedInput = 0;
        _hasCachedInput = false;
    }
}
