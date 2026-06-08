namespace Cpu.Pet.Devices.CbmDos;

public readonly struct DirEntry
{
    public FileType Type { get; }
    public bool IsClosed { get; }
    public bool IsLocked { get; }
    public byte StartTrack { get; }
    public byte StartSector { get; }
    public byte[] FilenameBytes { get; }
    public int SizeInSectors { get; }

    public string Filename =>
        global::System.Text.Encoding.ASCII.GetString(FilenameBytes).TrimEnd('\xA0', '\x00', ' ');

    public DirEntry(
        FileType type, bool isClosed, bool isLocked,
        byte startTrack, byte startSector,
        byte[] filenameBytes, int sizeInSectors)
    {
        Type = type;
        IsClosed = isClosed;
        IsLocked = isLocked;
        StartTrack = startTrack;
        StartSector = startSector;
        FilenameBytes = filenameBytes;
        SizeInSectors = sizeInSectors;
    }
}
