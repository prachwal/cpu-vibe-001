using Cpu.Pet.Devices;
using Cpu.Pet.Devices.CbmDos;
using Cpu.Pet.System;
using Cpu.Board.Core;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class IeeeStationCliTests
{
    private static string D => Path.Combine(
        AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    /* ================================================================
     * 1. IeeeTraceMiddleware — wrapper z analizą błędów
     * ================================================================ */
    public sealed record CallRecord(
        string Method, string? Args, string StackTrace,
        DateTime Timestamp, bool IsPetSide
    );

    public sealed class IeeeTraceMiddleware : IIeeeDevice
    {
        private readonly IIeeeDevice _inner;
        public readonly List<CallRecord> Calls = [];
        public bool FailOnError { get; set; } = true;
        public string? LastFault { get; private set; }

        public int PrimaryAddress => _inner.PrimaryAddress;
        public bool DataAvailable
        {
            get { bool r = _inner.DataAvailable; Record("DataAvailable", r.ToString(), true); return r; }
        }

        public IeeeTraceMiddleware(IIeeeDevice inner) => _inner = inner;

        private void Record(string method, string? args, bool isPetSide)
        {
            Calls.Add(new CallRecord(method, args,
                Environment.StackTrace, DateTime.UtcNow, isPetSide));
        }

        private static bool IsPetFrame(string f) =>
            f.Contains("PetIeeeBus") || f.Contains("PetMachine") ||
            f.Contains("OnDioWrite") || f.Contains("OnDioRead") ||
            f.Contains("Cpu.Pet.System") || f.Contains("Mos6502");

        public string AnalyzeFault(string context)
        {
            var st = Environment.StackTrace;
            var frames = st.Split('\n').Select(f => f.Trim()).ToList();
            int pf = frames.Count(IsPetFrame);
            int df = frames.Count(f => f.Contains("IeeeTraceMiddleware") || f.Contains("CbmDos"));
            string fault = pf > df ? "PET (KERNAL/CPU)" : "STATION (DOS engine)";
            var last = Calls.Count > 0 ? Calls[^1] : null;
            LastFault = $"""
                FAULT: {context}
                SOURCE: {fault} ({pf} PET frames vs {df} DRV frames)
                LAST:   {last?.Method}({last?.Args}) @ {last?.Timestamp:HH:mm:ss.fff}
                STACK:
                {string.Join("\n  ", frames.Take(8))}
                """;
            return LastFault;
        }

        public string DumpConversation(int tail = 20)
        {
            var start = Math.Max(0, Calls.Count - tail);
            return string.Join("\n", Calls.Skip(start).Select(c =>
                $"  [{(c.IsPetSide ? 'P' : 'S')}] {c.Method,-15} {c.Args}"));
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
                    AnalyzeFault($"TryRead FAIL at pos={_inner.DataAvailable}"));
            return ok;
        }
    }

    /* ================================================================
     * 2. Testy CLI — bezpośrednio przez CbmDosEngine
     * ================================================================ */

    [Fact]
    public void Cli_ListFiles()
    {
        var img = D64Image.Load(Path.Combine(D, "games-1.d64"));
        var dir = img.ReadDirectory();

        global::System.Console.Error.WriteLine($"\n=== GAMES-1.D64: {dir.Count} files ===");
        foreach (var e in dir.OrderBy(e => e.Filename))
            global::System.Console.Error.WriteLine(
                $"  [{e.Type,-3} {(e.IsClosed ? 'C' : '*')}] {e.Filename,-20} {e.SizeInSectors,4} sect");
    }

    [Fact]
    public void Cli_DownloadAndVerify()
    {
        var img = D64Image.Load(Path.Combine(D, "games-1.d64"));
        var target = img.ReadDirectory().First(e => e.Type == FileType.Prg && e.Filename.Contains("HELLO"));
        byte[] raw = img.ReadFile(target);
        ushort la = (ushort)(raw[0] | (raw[1] << 8));

        global::System.Console.Error.WriteLine(
            $"\nHELLO: ${la:X4}-${la + raw.Length - 2:X4} ({raw.Length} B)");
        raw.Length.Should().BeGreaterThan(2);
        raw[0].Should().Be(0x01);
    }

    [Fact]
    public void Cli_FullDownloadAll()
    {
        var img = D64Image.Load(Path.Combine(D, "games-1.d64"));
        var prgs = img.ReadDirectory().Where(e => e.Type == FileType.Prg && e.IsClosed).ToList();
        int err = 0;
        foreach (var p in prgs.OrderBy(e => e.Filename))
            try
            {
                var d = img.ReadFile(p);
                global::System.Console.Error.WriteLine($"  OK  {p.Filename,-20} ${d.Length} B");
            }
            catch { err++; }
        global::System.Console.Error.WriteLine($"  Errors: {err}/{prgs.Count}");
        err.Should().Be(0);
    }

    /* ================================================================
     * 3. Test komunikacji PET ↔ STACJA przez magistralę IEEE-488
     * ================================================================ */

    [Fact]
    public void Comm_FullLoadSequence()
    {
        // Tworzymy drive z wrapperem
        var drive = new PetIeeeDiskDrive(8);
        drive.Engine.AttachImage(D64Image.Load(Path.Combine(D, "games-1.d64")));
        var trace = new IeeeTraceMiddleware(drive);
        trace.FailOnError = false;

        var bus = new PetIeeeBus();
        bus.AttachDevice(trace);

        // Sekwencja: LISTEN 8 + SEC 0 → filename "$" → UNLISTEN → TALK 8 + SEC 0 → read
        bus.OnATNWrite(true);                     // ATN ON
        bus.OnDioWrite(0x28);                     // LISTEN 8
        bus.OnDioWrite(0x60);                     // SECONDARY 0
        bus.OnATNWrite(false);                    // ATN OFF → DataOut
        bus.OnDioWrite(0x24);                     // "$"
        bus.OnATNWrite(true);                     // ATN ON
        bus.OnDioWrite(0x3F);                     // UNLISTEN
        bus.OnATNWrite(false);                    // ATN OFF → Idle

        // TALK 8 + SEC 0 → Read directory
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);                     // TALK 8
        bus.OnDioWrite(0x60);                     // SEC 0
        bus.OnATNWrite(false);                    // ATN OFF → DataIn

        // Odczytaj listing katalogu
        var output = new global::System.Collections.Generic.List<byte>();
        while (bus.GetCurrentDio() != 0xFF || trace.DataAvailable)
        {
            bus.Tick();
            byte b = bus.OnDioRead();
            if (b == 0xFF) break;
            output.Add(b);
        }

        // Sprawdź: pierwsze 2 bajty to load address BASICu ($0401)
        global::System.Console.Error.WriteLine(
            $"\n=== Full LOAD sequence ===\n{trace.DumpConversation(30)}");
        global::System.Console.Error.WriteLine($"\nOutput: {output.Count} bytes, load=${(output.Count > 1 ? $"{output[1]:X2}{output[0]:X2}" : "?")}");

        output.Count.Should().BeGreaterThan(20, "directory listing should have >20 bytes");
        output[0].Should().Be(0x01, "BASIC load address lo");
        output[1].Should().Be(0x04, "BASIC load address hi");
    }

    [Fact]
    public void Comm_LoadHello()
    {
        var drive = new PetIeeeDiskDrive(8);
        drive.Engine.AttachImage(D64Image.Load(Path.Combine(D, "games-1.d64")));
        var trace = new IeeeTraceMiddleware(drive) { FailOnError = false };
        var bus = new PetIeeeBus();
        bus.AttachDevice(trace);

        byte[] fname = [0x48, 0x45, 0x4C, 0x4C, 0x4F];

        // LISTEN 8 + SEC 0 + filename
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        foreach (byte b in fname) bus.OnDioWrite(b);
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        // TALK 8 + SEC 0 → read file
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        var data = new global::System.Collections.Generic.List<byte>();
        for (int i = 0; i < 5000; i++)
        {
            bus.Tick();
            byte b = bus.OnDioRead();
            if (b == 0xFF && !trace.DataAvailable) break;
            if (b != 0xFF) data.Add(b);
        }

        global::System.Console.Error.WriteLine(
            $"\n=== HELLO load ===\n{trace.DumpConversation(30)}");
        global::System.Console.Error.WriteLine($"\nLoaded {data.Count} bytes");

        data.Count.Should().BeGreaterThan(20);
        ushort la = (ushort)(data[0] | (data[1] << 8));
        la.Should().Be(0x0401);
    }

    [Fact]
    public void Comm_ErrorAnalysis()
    {
        // Test który celowo wywołuje błąd i sprawdza analizę winnej strony
        var drive = new PetIeeeDiskDrive(8);
        var trace = new IeeeTraceMiddleware(drive) { FailOnError = false };
        var bus = new PetIeeeBus();
        bus.AttachDevice(trace);

        // Próbuje TALK bez wcześniejszego filename → brak danych
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);  // TALK 8
        bus.OnDioWrite(0x60);  // SEC 0
        bus.OnATNWrite(false); // DataIn

        bus.Tick();
        byte b = bus.OnDioRead(); // brak danych → zwróci $FF

        string analysis = trace.AnalyzeFault("No data after TALK (expected: file not opened)");
        global::System.Console.Error.WriteLine($"\n=== Error analysis ===\n{analysis}");

        b.Should().Be(0xFF);
        trace.LastFault.Should().NotBeNull();
        trace.LastFault.Should().Contain("SOURCE:");
    }
}
