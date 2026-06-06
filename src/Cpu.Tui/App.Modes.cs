using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;

namespace Cpu.Tui;

public partial class App
{
    private ScreenSize GetRequestedScreenSize() => _screenMode switch
    {
        ScreenMode.Rows24Cols40 => new ScreenSize(24, 40),
        ScreenMode.Rows25Cols40 => new ScreenSize(25, 40),
        _ => new ScreenSize(25, 80)
    };

    private void CycleScreenMode()
    {
        _screenMode = _screenMode switch
        {
            ScreenMode.Rows24Cols40 => ScreenMode.Rows25Cols40,
            ScreenMode.Rows25Cols40 => ScreenMode.Rows25Cols80,
            _ => ScreenMode.Rows24Cols40
        };
        SetStatus($"{GetRequestedScreenSize().Rows}x{GetRequestedScreenSize().Cols}");
    }

    private void StartDemo(int index)
    {
        _screen.Reset(); _pia.Reset(); _piaAdapter.Reset();
        string text = PiaDemos.GetText(index);
        _demoBuffer = text.Split('\n');
        _demoLine = 0; _demoChar = 0; _demoRunning = true;
        SetStatus(PiaDemos.Names[index]);
    }

    private void TickDemo()
    {
        if (!_demoRunning) return;
        while (_demoLine < _demoBuffer.Length)
        {
            if (_demoChar < _demoBuffer[_demoLine].Length)
            {
                byte ch = (byte)_demoBuffer[_demoLine][_demoChar];
                WritePia(ch); _demoChar++;
            }
            else
            {
                WritePia(0x0D); WritePia(0x0A);
                _demoLine++; _demoChar = 0;
            }
        }
        _demoRunning = false; _dirty = true;
    }

    private void WritePia(byte data)
    {
        ushort addr = _pia.BaseAddress;
        _pia.Write((ushort)(addr + PiaDevice.PRA), data);
        _pia.Write((ushort)(addr + PiaDevice.PRB), 0x08);
        _pia.Write((ushort)(addr + PiaDevice.PRB), 0x00);
    }

    private void StartImageMode()
    {
        _imageMode = true; _showHelp = false; _demoMenu = false;
        _demoRunning = false; _echoMode = false;
        _loadedImage = null; _loadedImagePath = null;
        _imageIndex = 0;
        SetStatus("Image");
    }

    private PixelBuffer GetCurrentImage()
    {
        string path = _imagePaths[_imageIndex];
        if (_loadedImage != null && _loadedImagePath == path)
            return _loadedImage;
        _loadedImage = JpegImageLoader.Load(path);
        _loadedImagePath = path;
        return _loadedImage;
    }

    private void SeedScreen()
    {
        _screen.FillRect(0, 0, ScreenBuffer.Width, ScreenBuffer.Height, ' ', ConsoleColor.Gray, ConsoleColor.Black);
    }

    private void EnterCanvasMode()
    {
        _canvasMode = true; _canvasDemoIndex = 0;
        SeedCanvasDemo(0);
        SetStatus(_canvasDemos[0]);
    }
}
