using Cpu.Tui;

namespace Cpu.Tui.Devices.Pia;

/// <summary>
/// Demo sequences that send data through PIA to the terminal adapter.
/// Each demo fills the screen with predefined content.
/// </summary>
public static class PiaDemos
{
    private static readonly string[] LoremIpsum =
    [
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
        "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
        "Ut enim ad minim veniam, quis nostrud exercitation ullamco.",
        "Duis aute irure dolor in reprehenderit in voluptate velit esse.",
        "Cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat.",
        "Cupidatat non proident, sunt in culpa qui officia deserunt.",
        "Mollit anim id est laborum. Sed ut perspiciatis unde omnis.",
        "Iste natus error sit voluptatem accusantium doloremque laudantium.",
        "Totam rem aperiam, eaque ipsa quae ab illo inventore veritatis.",
        "Et quasi architecto beatae vitae dicta sunt explicabo."
    ];

    private static readonly string[] BoxDrawing =
    [
        "+----------------------------------------------+",
        "|  PIA Terminal Demo - Box Drawing             |",
        "+----------------------------------------------+",
        "|                                              |",
        "|  +----------+  +----------+  +----------+    |",
        "|  |  Port A  |  |  Port B  |  |   CRA    |    |",
        "|  |   Data   |  |   Ctrl   |  |   Flag   |    |",
        "|  +----------+  +----------+  +----------+    |",
        "|                                              |",
        "|  DDR: 0x0F  PRA: 0x41  CRA: 0x04             |",
        "|  DDR: 0xF0  PRB: 0x20  CRB: 0x01             |",
        "|                                              |",
        "+----------------------------------------------+"
    ];

    private static readonly string[] ColorBars =
    [
        "\x1B[40m  BLACK   \x1B[41m  RED     \x1B[42m  GREEN   \x1B[43m  YELLOW  \x1B[0m",
        "\x1B[44m  BLUE    \x1B[45m  MAGENTA \x1B[46m  CYAN    \x1B[47m  WHITE   \x1B[0m",
        "\x1B[31m RED TEXT \x1B[32m GRN TEXT \x1B[33m YEL TEXT \x1B[34m BLU TEXT \x1B[0m",
        "\x1B[35m MAG TEXT \x1B[36m CYN TEXT \x1B[37m WHT TEXT \x1B[91m LRED     \x1B[0m",
        "\x1B[92m LGREEN   \x1B[93m LYELLOW  \x1B[94m LBLUE    \x1B[95m LMAGENTA \x1B[0m",
        "\x1B[96m LCYAN    \x1B[97m LWHITE   \x1B[0m                         ",
        "",
        "Color test complete. All 16 console colors displayed."
    ];

    private static readonly string[] CursorDemo =
    [
        "\x1B[2J\x1B[H",
        "Cursor Movement Demo",
        "====================",
        "",
        "Home position: (0,3)",
        "\x1B[3;1H>>> This line was placed at row 3 using CSI H",
        "",
        "Move right:  >>>>>>>>",
        "\x1B[5;1H\x1B[8CColumn 8, Row 5",
        "",
        "Move down:",
        "\x1B[7;1HRow 7 - placed with CSI",
        "\x1B[9;1HRow 9 - placed with CSI",
        "\x1B[11HRow 11 - placed with CSI",
        "\x1B[13HRow 13 - End of cursor demo"
    ];

    private static readonly string[] ScrollDemo =
    [
        "Line 01 - Scroll demo starting",
        "Line 02 - Each line pushes up",
        "Line 03 - Watch the screen",
        "Line 04 - Content scrolls",
        "Line 05 - Automatically",
        "Line 06 - When screen fills",
        "Line 07 - New lines appear",
        "Line 08 - At the bottom",
        "Line 09 - Old lines disappear",
        "Line 10 - From the top",
        "Line 11 - This is line 11",
        "Line 12 - This is line 12",
        "Line 13 - This is line 13",
        "Line 14 - This is line 14",
        "Line 15 - This is line 15",
        "Line 16 - This is line 16",
        "Line 17 - This is line 17",
        "Line 18 - This is line 18",
        "Line 19 - This is line 19",
        "Line 20 - This is line 20",
        "Line 21 - This is line 21",
        "Line 22 - This is line 22",
        "Line 23 - This is line 23",
        "Line 24 - This is line 24",
        "Line 25 - This is line 25 - SCROLL!",
        "Line 26 - Scrolled!",
        "Line 27 - Scrolled!",
        "Line 28 - Scrolled!",
        "Line 29 - Scrolled!",
        "Line 30 - End of scroll demo"
    ];

    public static readonly string[] Names =
    [
        "Lorem Ipsum",
        "Box Drawing",
        "Color Bars",
        "Cursor Movement",
        "Scrolling"
    ];

    private static string[] GetLines(int index) => index switch
    {
        0 => LoremIpsum,
        1 => BoxDrawing,
        2 => ColorBars,
        3 => CursorDemo,
        4 => ScrollDemo,
        _ => LoremIpsum
    };

    /// <summary>
    /// Get demo text as a single string with newlines.
    /// </summary>
    public static string GetText(int index)
    {
        return string.Join("\n", GetLines(index));
    }

    /// <summary>
    /// Run the selected demo by sending data through PIA Port A with strobe.
    /// </summary>
    public static void Run(PiaDevice pia, int demoIndex)
    {
        foreach (string line in GetLines(demoIndex))
        {
            SendString(pia, line);
            SendByte(pia, 0x0D); // CR
            SendByte(pia, 0x0A); // LF
        }
    }

    private static void SendString(PiaDevice pia, string text)
    {
        foreach (char c in text)
            SendByte(pia, (byte)c);
    }

    private static void SendByte(PiaDevice pia, byte data)
    {
        pia.Write((ushort)(pia.BaseAddress + PiaDevice.PRA), data);
        pia.Write((ushort)(pia.BaseAddress + PiaDevice.PRB), 0x08); // strobe high
        pia.Write((ushort)(pia.BaseAddress + PiaDevice.PRB), 0x00); // strobe low
    }
}
