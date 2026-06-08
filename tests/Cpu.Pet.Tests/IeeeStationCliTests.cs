using Cpu.Pet.Devices;
using Cpu.Pet.Devices.CbmDos;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class IeeeStationCliTests
{
    private static string D => Path.Combine(
        AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    public sealed record CallRecord(
        string Method,
        string? Args,
        string StackTrace,
        DateTime Timestamp,
        bool IsPetSide
    );

    public sealed class IeeeTraceMiddleware : IIeeeDevice
    {
        private readonly IIeeeDevice _inner;
        public readonly List<CallRecord> Calls = [];
        public bool FailOnError { get; set; } = true;

        public int PrimaryAddress => _inner.PrimaryAddress;
        public bool DataAvailable
        {
            get
            {
                bool r = _inner.DataAvailable;
                Record("DataAvailable", r.ToString(), true);
                return r;
            }
        }

        public IeeeTraceMiddleware(IIeeeDevice inner) => _inner = inner;

        private void Record(string method, string? args, bool isPetSide)
        {
            Calls.Add(new CallRecord(method, args,
                Environment.StackTrace, DateTime.UtcNow, isPetSide));
        }

        private static bool IsPetFrame(string frame) =>
            frame.Contains("PetIeeeBus") || frame.Contains("PetMachine") ||
            frame.Contains("OnDioWrite") || frame.Contains("OnDioRead") ||
            frame.Contains("Cpu.Pet.System") || frame.Contains("Mos6502");

        public string AnalyzeFault(string context)
        {
            var st = Environment.StackTrace;
            var frames = st.Split('\n').Select(f => f.Trim()).ToList();
            int petFrames = frames.Count(IsPetFrame);
            int drvFrames = frames.Count(f => f.Contains("IeeeTraceMiddleware") || f.Contains("CbmDos"));
            string fault = petFrames > drvFrames ? "PET (KERNAL/CPU)" : "STATION (DOS engine)";
            var last = Calls.Count > 0 ? Calls[^1] : null;
            return $"""
                FAULT: {context}
                SOURCE: {fault} ({petFrames} PET frames vs {drvFrames} DRV frames)
                Last call: {last?.Method}({last?.Args}) at {last?.Timestamp:HH:mm:ss.fff}
                Stack:
                {string.Join("\n  ", frames.Take(8))}
                """;
        }

        public void OpenForRead(byte a)
        {
            Record("OpenForRead", $"sec={a}", false);
            _inner.OpenForRead(a);
            if (FailOnError && !_inner.DataAvailable)
                throw new InvalidOperationException(AnalyzeFault("OpenForRead: no data"));
        }

        public void OpenForWrite(byte a)
        {
            Record("OpenForWrite", $"sec={a}", false);
            _inner.OpenForWrite(a);
        }

        public void Close()
        {
            Record("Close", null, false);
            _inner.Close();
        }

        public void Write(byte data)
        {
            Record("Write", $"${data:X2}", false);
            _inner.Write(data);
        }

        public bool TryRead(out byte data)
        {
            bool ok = _inner.TryRead(out data);
            Record("TryRead", ok ? $"${data:X2}" : "FAIL", true);
            if (!ok && FailOnError)
                throw new InvalidOperationException(
                    AnalyzeFault($"TryRead returned false (pos={_inner.DataAvailable})"));
            return ok;
        }
    }

    [Fact]
    public void StationCli_ListFiles()
    {
        var img = D64Image.Load(Path.Combine(D, "games-1.d64"));
        var dir = img.ReadDirectory();

        global::System.Console.Error.WriteLine($"\n=== GAMES-1.D64: {dir.Count} files ===\n");

        dir.Sort((a, b) => string.Compare(a.Filename, b.Filename, StringComparison.Ordinal));

        foreach (var e in dir)
        {
            string pad = e.Filename.PadRight(20);
            string fn = e.FilenameBytes is not null
                ? BitConverter.ToString(e.FilenameBytes).Replace("-", " ")
                : "?";
            global::System.Console.Error.WriteLine(
                $"  [{e.Type,-3} {(e.IsClosed ? 'C' : '*')}] {pad} {e.SizeInSectors,4} sect  @ T{e.StartTrack}/S{e.StartSector}  hex={fn}");
        }
    }

    [Fact]
    public void StationCli_DownloadAndVerify()
    {
        var img = D64Image.Load(Path.Combine(D, "games-1.d64"));

        var prgs = img.ReadDirectory().Where(e => e.Type == FileType.Prg && e.IsClosed).ToList();
        prgs.Should().NotBeEmpty("at least one PRG file on disk");

        var target = prgs.First(e => e.Filename.Contains("HELLO"));
        global::System.Console.Error.WriteLine($"\n=== Download: \"{target.Filename}\" ({target.SizeInSectors} sectors) ===\n");

        byte[] raw = img.ReadFile(target);

        ushort loadAddr = (ushort)(raw[0] | (raw[1] << 8));
        int prgLen = raw.Length;
        ushort endAddr = (ushort)(loadAddr + prgLen - 2);

        global::System.Console.Error.WriteLine($"  Load address: ${loadAddr:X4}");
        global::System.Console.Error.WriteLine($"  PRG length:   {prgLen} bytes");
        global::System.Console.Error.WriteLine($"  End address:  ${endAddr:X4}");
        global::System.Console.Error.WriteLine($"  First 64 bytes hex:");
        for (int i = 0; i < Math.Min(64, raw.Length); i++)
            global::System.Console.Error.Write(i % 16 == 0 ? $"\n    ${loadAddr + i:X4}: " : $"{raw[i]:X2} ");
        global::System.Console.Error.WriteLine();

        raw.Length.Should().BeGreaterThan(2, "PRG must have load address + data");
        raw[0].Should().Be(0x01, "BASIC load address low = $01");
    }

    [Fact]
    public void StationCli_ListUtils()
    {
        var img = D64Image.Load(Path.Combine(D, "utils.d64"));
        var dir = img.ReadDirectory();

        global::System.Console.Error.WriteLine($"\n=== UTILS.D64: {dir.Count} files ===\n");

        int totalSectors = 0;
        foreach (var e in dir.OrderBy(e => e.Filename))
        {
            totalSectors += e.SizeInSectors;
            global::System.Console.Error.WriteLine(
                $"  [{e.Type,-3} {(e.IsClosed ? 'C' : '*')}] {e.Filename,-22} {e.SizeInSectors,4} sect");
        }
        global::System.Console.Error.WriteLine($"\n  TOTAL: {dir.Count} files, {totalSectors} sectors");
    }

    [Fact]
    public void StationCli_FullDownloadAllPrgs()
    {
        var img = D64Image.Load(Path.Combine(D, "games-1.d64"));
        var prgs = img.ReadDirectory().Where(e => e.Type == FileType.Prg && e.IsClosed).ToList();

        global::System.Console.Error.WriteLine($"\n=== Full download: {prgs.Count} PRGs ===\n");

        int errors = 0;
        foreach (var prg in prgs.OrderBy(e => e.Filename))
        {
            try
            {
                byte[] data = img.ReadFile(prg);
                ushort la = (ushort)(data[0] | (data[1] << 8));
                global::System.Console.Error.WriteLine(
                    $"  OK  {prg.Filename,-20} ${la:X4}-${la + data.Length - 2:X4} ({data.Length} bytes)");
            }
            catch (Exception ex)
            {
                global::System.Console.Error.WriteLine($"  ERR {prg.Filename,-20} {ex.Message}");
                errors++;
            }
        }

        global::System.Console.Error.WriteLine($"\n  Errors: {errors}/{prgs.Count}");
        errors.Should().Be(0);
    }
}
