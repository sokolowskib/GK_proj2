using Bezier3D.Core.Rendering;

namespace Bezier3D.Core.Tests;

public class FrameBufferTests
{
    private static readonly uint Bg = FrameBuffer.Rgba(255, 255, 255);
    private static readonly uint Red = FrameBuffer.Rgba(255, 0, 0);
    private static readonly uint Blue = FrameBuffer.Rgba(0, 0, 255);

    private static FrameBuffer Cleared()
    {
        var f = new FrameBuffer(4, 3);
        f.Clear(Bg);
        return f;
    }

    [Fact]
    public void CloserPixelWinsRegardlessOfOrder()
    {
        var f = Cleared();
        f.SetPixel(2, 1, 20, Blue); // dalej
        f.SetPixel(2, 1, 50, Red);  // bliżej → nadpisuje
        f.SetPixel(2, 1, 30, Blue); // dalej niż czerwony → nie nadpisuje

        Assert.Equal(Red, f.Color[1 * 4 + 2]);
        Assert.Equal(50, f.Depth[1 * 4 + 2]);
    }

    [Fact]
    public void OutsideIsIgnored()
    {
        var f = Cleared();
        f.SetPixel(-1, 0, 10, Red);
        f.SetPixel(4, 0, 10, Red);
        f.SetPixel(0, 3, 10, Red);
        Assert.All(f.Color, c => Assert.Equal(Bg, c));
    }

    [Fact]
    public void RgbaByteOrderIsRGBA()
    {
        var bytes = BitConverter.GetBytes(FrameBuffer.Rgba(1, 2, 3)); // little-endian jak w pamięci tekstury
        Assert.Equal([1, 2, 3, 255], bytes);
    }
}
