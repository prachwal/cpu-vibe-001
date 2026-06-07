using Cpu.Tui.Graphics;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class JpegImageLoaderTests
{
    [Fact]
    public void IsFfmpegAvailable_ReturnsWithoutThrowing()
    {
        _ = JpegImageLoader.IsFfmpegAvailable();
    }

    [Fact]
    public void ProbeSize_MissingFile_ThrowsFileNotFound()
    {
        Action act = () => JpegImageLoader.ProbeSize("/nonexistent/sample.jpg");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFound()
    {
        Action act = () => JpegImageLoader.Load("/nonexistent/sample.jpg");
        act.Should().Throw<FileNotFoundException>();
    }
}
