namespace Cpu.Pet.Devices;

public sealed class PetIeeeBus : IDisposable
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
    private bool _lastAtn;
    private int _eventIndex;
    private StreamWriter? _logFile;

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

    public record TraceEntry(string Type, string Text);
    private readonly List<TraceEntry> _traceLog = [];
    private const int MaxTraceEntries = 500;

    public void AttachDevice(IIeeeDevice device) => _devices.Add(device);

    public IReadOnlyList<TraceEntry> GetTraceLog() => _traceLog;

    public void ClearTraceLog() => _traceLog.Clear();

    public void SetLogFile(string path)
    {
        _logFile?.Dispose();
        _logFile = new StreamWriter(path) { AutoFlush = true };
        _logFile.WriteLine("IEEE-488 trace started");
    }

    private void FileLog(string type, string text)
    {
        if (_logFile == null) return;
        int n = ++_eventIndex;
        _logFile.WriteLine($"[{n,5}] {type,4} | {text}");
    }

    private void Trace(string type, string text)
    {
        _traceLog.Add(new TraceEntry(type, text));
        if (_traceLog.Count > MaxTraceEntries)
            _traceLog.RemoveRange(0, _traceLog.Count - MaxTraceEntries);
        FileLog(type, text);
    }

    public void OnATNWrite(bool atn)
    {
        if (atn == _lastAtn) return;
        _lastAtn = atn;
        Trace("ATN", atn ? "ON" : "OFF");

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

    private bool _pendingCommand;

    public void MarkPendingCommand()
    {
        _pendingCommand = true;
    }

    public void OnDioWrite(byte data)
    {
        _lastDio = data;

        if (_pendingCommand)
        {
            _pendingCommand = false;
            Trace("CMD", $"DIO ${data:X2} → cmd");
            ProcessCommandByte(data);
            AcceptHandshake();
            return;
        }

        switch (_state)
        {
            case BusState.Command:
                Trace("CMD", $"DIO ${data:X2} → cmd");
                ProcessCommandByte(data);
                break;
            case BusState.DataOut:
                Trace("TX", $"DIO ${data:X2} → data to device");
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
            Trace("RX", $"DIO → ${_lastDio:X2}");
            return _cachedInput;
        }
        _lastDio = 0xFF;
        return 0xFF;
    }

    public byte GetViaPortBInput()
    {
        byte result = 0;
        if (!NDAC) result |= 0x01;
        if (!NRFD) result |= 0x40;
        if (!DAV) result |= 0x80;
        return result;
    }

    public void CompleteHandshake()
    {
        NRFD = true;
        NDAC = true;
    }

    public void SetDAVState(bool asserted)
    {
        DAV = asserted;
    }

    public void AcceptHandshake()
    {
        NRFD = false;
        NDAC = false;
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
                NDAC = false;
            }
        }
    }

    private void ProcessCommandByte(byte cmd)
    {
        if (cmd >= 0x20 && cmd <= 0x3E)
        {
            int dev = cmd & 0x1F;
            _listenerAddr = dev;
            _listenerDevice = FindDevice(_listenerAddr);
            Trace("CMD", $"LISTEN dev={dev} found={_listenerDevice != null}");
            CommandHandshake();
        }
        else if (cmd == 0x3F)
        {
            _listenerDevice?.Close();
            _listenerAddr = -1;
            _listenerDevice = null;
            Trace("CMD", "UNLISTEN");
            CommandHandshake();
        }
        else if (cmd >= 0x40 && cmd <= 0x5E)
        {
            int dev = cmd & 0x1F;
            _talkerAddr = dev;
            _talkerDevice = FindDevice(_talkerAddr);
            Trace("CMD", $"TALK dev={dev} found={_talkerDevice != null}");
            CommandHandshake();
        }
        else if (cmd == 0x5F)
        {
            _talkerDevice?.Close();
            _talkerAddr = -1;
            _talkerDevice = null;
            Trace("CMD", "UNTALK");
            CommandHandshake();
        }
        else if (cmd >= 0x60 && cmd <= 0x7F)
        {
            byte sec = (byte)(cmd & 0x1F);
            if (_listenerDevice != null)
                _listenerSec = sec;
            if (_talkerDevice != null)
                _talkerSec = sec;
            Trace("CMD", $"SECONDARY sec={sec}");
            CommandHandshake();
        }
        else if (cmd >= 0xE0 && cmd <= 0xFF)
        {
            Trace("CMD", $"CLOSE sec={cmd & 0x1F}");
            CommandHandshake();
        }
        else
        {
            Trace("CMD", $"UNKNOWN ${cmd:X2}");
            CommandHandshake();
        }
    }

    private void CommandHandshake()
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

    public void Dispose() => _logFile?.Dispose();

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
        DAV = false;
        NRFD = true;
        NDAC = true;
        _listenerSec = 0;
        _talkerSec = 0;
        _cachedInput = 0;
        _hasCachedInput = false;
    }
}
