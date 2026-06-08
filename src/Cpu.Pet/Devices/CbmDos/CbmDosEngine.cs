namespace Cpu.Pet.Devices.CbmDos;

public sealed class CbmDosEngine
{
    private D64Image? _image;
    private List<byte> _commandBuffer = [];
    private List<byte> _fileOutput = [];
    private int _filePosition;
    private List<byte> _errorOutput = [];
    private int _errorPosition;
    private byte _currentSecAddr = 0xFF;
    private bool _waitingForFilename;

    public bool DataAvailable =>
        _currentSecAddr == 15
            ? _errorPosition < _errorOutput.Count
            : _filePosition < _fileOutput.Count;

    public void AttachImage(D64Image image)
    {
        _image = image;
        SetError("73,CBM DOS V2.6 4040,00,00");
        _fileOutput.Clear();
        _filePosition = 0;
    }

    public void OpenChannel(byte secAddr)
    {
        _currentSecAddr = secAddr;
        _commandBuffer.Clear();

        switch (secAddr)
        {
            case 15:
                _waitingForFilename = false;
                break;

            case 0:
                _waitingForFilename = true;
                break;

            default:
                SetError("65,NO CHANNEL,00,00");
                break;
        }
    }

    public void CloseChannel()
    {
        if (_waitingForFilename && _commandBuffer.Count > 0)
        {
            ProcessFilename();
            _waitingForFilename = false;
            return;
        }

        if (_commandBuffer.Count > 0 && !_waitingForFilename)
        {
            ProcessCommand();
            return;
        }

        _waitingForFilename = false;
    }

    public void ReceiveByte(byte data)
    {
        _commandBuffer.Add(data);
    }

    public bool TryGetByte(out byte data)
    {
        if (_currentSecAddr == 15)
        {
            if (_errorPosition < _errorOutput.Count)
            {
                data = _errorOutput[_errorPosition++];
                return true;
            }
        }
        else
        {
            if (_filePosition < _fileOutput.Count)
            {
                data = _fileOutput[_filePosition++];
                return true;
            }
        }
        data = 0;
        return false;
    }

    public void Tick()
    {
    }

    private void ProcessCommand()
    {
        string cmd = PetsciiToString(_commandBuffer);
        if (string.IsNullOrEmpty(cmd))
        {
            SetError("00, OK,00,00");
            return;
        }

        char c = char.ToUpperInvariant(cmd[0]);
        switch (c)
        {
            case 'I':
                SetError("00, OK,00,00");
                break;

            case 'V':
                SetError("00, OK,00,00");
                break;

            case 'S':
                SetError("00, OK,00,00");
                break;

            case 'R':
                SetError("00, OK,00,00");
                break;

            case 'U':
                SetError("00, OK,00,00");
                break;

            default:
                SetError("30,SYNTAX ERROR,00,00");
                break;
        }
    }

    private void ProcessFilename()
    {
        if (_commandBuffer.Count == 1 && _commandBuffer[0] == 0x24)
        {
            GenerateDirectoryListing();
            return;
        }

        if (_image == null)
        {
            SetError("74,DRIVE NOT READY,00,00");
            return;
        }

        var dir = _image.ReadDirectory();
        var entry = FindFileByBytes(dir, _commandBuffer);

        if (entry.FilenameBytes is null)
        {
            SetError("62,FILE NOT FOUND,00,00");
            return;
        }

        byte[] fileData = _image.ReadFile(entry);
        _fileOutput.Clear();
        _fileOutput.AddRange(fileData);
        _filePosition = 0;
        SetError("00, OK,00,00");
    }

    private static DirEntry FindFileByBytes(List<DirEntry> dir, List<byte> nameBytes)
    {
        foreach (var entry in dir)
        {
            if (entry.Type != FileType.Prg || entry.FilenameBytes is null)
                continue;

            if (entry.SizeInSectors == 0)
                continue;

            if (BytesMatch(nameBytes, entry.FilenameBytes))
                return entry;
        }
        return default;
    }

    private static bool BytesMatch(List<byte> a, byte[] b)
    {
        if (a.Count == 0) return false;

        int len = a.Count;
        if (len > b.Length) return false;

        for (int i = 0; i < len; i++)
        {
            byte ba = a[i];
            byte bb = b[i];

            if (ba == bb) continue;

            if (ba >= (byte)'a' && ba <= (byte)'z' && bb == ba - 32) continue;
            if (bb >= (byte)'a' && bb <= (byte)'z' && ba == bb - 32) continue;

            return false;
        }
        return true;
    }

    private void GenerateDirectoryListing()
    {
        if (_image == null)
        {
            SetError("74,DRIVE NOT READY,00,00");
            return;
        }

        var dir = _image.ReadDirectory();
        _fileOutput.Clear();

        ushort addr = 0x0401;
        int totalBlocks = 0;

        _fileOutput.Add((byte)(addr & 0xFF));
        _fileOutput.Add((byte)(addr >> 8));

        foreach (var entry in dir)
        {
            if (entry.Type == FileType.Del)
                continue;

            totalBlocks += entry.SizeInSectors;
            int lineLen = 1 + 16 + 1 + 5;
            ushort nextLine = (ushort)(addr + 4 + lineLen);

            _fileOutput.Add((byte)(nextLine & 0xFF));
            _fileOutput.Add((byte)(nextLine >> 8));
            _fileOutput.Add((byte)(addr & 0xFF));
            _fileOutput.Add((byte)(addr >> 8));

            _fileOutput.Add(0x20);
            for (int i = 0; i < 16; i++)
            {
                byte b = entry.FilenameBytes[i];
                _fileOutput.Add(b == 0xA0 ? (byte)0x20 : b);
            }
            _fileOutput.Add(0x20);

            string sizeStr = entry.SizeInSectors.ToString();
            foreach (char ch in sizeStr)
                _fileOutput.Add((byte)ch);

            _fileOutput.Add(0x00);
            addr = nextLine;
        }

        string blocksFree = $"BLOCKS FREE. {664 - totalBlocks}";
        int blocksLen = 1 + 16 + 1 + blocksFree.Length;
        ushort lastLine = (ushort)(addr + 4 + blocksLen + 2);

        _fileOutput.Add((byte)(lastLine & 0xFF));
        _fileOutput.Add((byte)(lastLine >> 8));
        _fileOutput.Add((byte)(addr & 0xFF));
        _fileOutput.Add((byte)(addr >> 8));

        _fileOutput.Add(0x20);
        for (int i = 0; i < 16; i++)
        {
            _fileOutput.Add(i < blocksFree.Length ? (byte)blocksFree[i] : (byte)0x20);
        }
        _fileOutput.Add(0x20);
        foreach (char ch in blocksFree)
            _fileOutput.Add((byte)ch);
        _fileOutput.Add(0x00);
        _fileOutput.Add(0x00);
        _fileOutput.Add(0x00);

        _filePosition = 0;
        SetError("00, OK,00,00");
    }

    private void SetError(string message)
    {
        _errorOutput.Clear();
        _errorPosition = 0;
        foreach (char ch in message)
            _errorOutput.Add((byte)ch);
        _errorOutput.Add(0x0D);
        _errorOutput.Add(0x00);
        _errorPosition = 0;
    }

    private static string PetsciiToString(List<byte> bytes)
    {
        var chars = new char[bytes.Count];
        for (int i = 0; i < bytes.Count; i++)
        {
            byte b = bytes[i];
            if (b >= 0x20 && b < 0x7F)
                chars[i] = (char)b;
            else if (b == 0x0D || b == 0x0A)
                chars[i] = ' ';
            else
                chars[i] = (char)b;
        }
        return new string(chars).TrimEnd('\0', ' ');
    }

    public void Reset()
    {
        _commandBuffer.Clear();
        _fileOutput.Clear();
        _filePosition = 0;
        _errorOutput.Clear();
        _errorPosition = 0;
        _currentSecAddr = 0xFF;
        _waitingForFilename = false;
        SetError("73,CBM DOS V2.6 4040,00,00");
    }
}
