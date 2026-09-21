using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Simultria.RustDeskCompanion;
using Xunit;

namespace RustDeskHop.Tests;

public sealed class AppBrandingTests
{
    [Fact]
    public void SlimmerRingKeepsItsApprovedContourAndOriginalOuterArcs()
    {
        var master = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "BrandingMaster.svg"));
        XNamespace svg = "http://www.w3.org/2000/svg";
        var ring = master.Descendants(svg + "g").Single(element => (string?)element.Attribute("id") == "rustdesk-ring");
        var path = Assert.Single(ring.Elements(svg + "path"));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((string)path.Attribute("d")!)));
        // Intentional 18% inner-contour expansion; the original outer arcs and
        // outside rounded endings remain unchanged (absolute SVG coordinates).
        Assert.Equal("AEF6FABCC73078F4FEF09EB62748DF28F76B4933A62949D89B9AB1FD89AB826D", hash);
        foreach (var arc in new[] {
            "A1.154 1.154 0 0 0 73.264 921.605",
            "A13.005 13.005 0 0 0 89.174 919.693",
            "A12.97 12.97 0 0 0 91.13 903.806",
            "A1.154 1.154 0 0 0 89.318 903.552",
            "A12.969 12.969 0 0 0 68.831 917.132",
            "A1.154 1.154 0 0 0 70.643 917.386",
            "A1.152 1.152 0 0 0 86.698 899.332",
            "A13.009 13.009 0 0 0 70.805 901.305" })
            Assert.Contains(arc, (string)path.Attribute("d")!);
        Assert.Equal("translate(62 62) scale(34.61538461538461) translate(-66.993 -897.484)", (string?)ring.Attribute("transform"));
        Assert.Equal("url(#b)", (string?)path.Attribute("fill"));
        var gradient = master.Descendants(svg + "linearGradient").Single(element => (string?)element.Attribute("id") == "b");
        Assert.Equal("matrix(26.00048 0 0 25.99935 66.993 897.485)", (string?)gradient.Attribute("gradientTransform"));
    }

    [Fact]
    public void RenderedTopBandIsFifteenToTwentyPercentThinnerWithoutShrinkingOutside()
    {
        var image = AppBranding.Logo;
        var first = -1;
        var last = -1;
        for (var y = 0; y < 100; y++)
        {
            var pixel = image.GetPixel(128, y);
            if (pixel.R < 60 && pixel.B > 160)
            {
                if (first < 0) first = y;
                last = y;
            }
            else if (first >= 0) break;
        }
        Assert.Equal(16, first); // The previous icon's outside boundary.
        // The previous 256px frame had a 46px solid band here; allow pixel rounding.
        Assert.InRange(last - first + 1, 37, 39);
    }

    [Fact]
    public void LargerForegroundBunnyUsesOneOutlineForBothColorAndWhiteSeparation()
    {
        var master = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "BrandingMaster.svg"));
        XNamespace svg = "http://www.w3.org/2000/svg", xlink = "http://www.w3.org/1999/xlink";
        var bunny = master.Descendants(svg + "g").Single(element => (string?)element.Attribute("id") == "foreground-bunny");
        var outline = master.Descendants(svg + "path").Single(element => (string?)element.Attribute("id") == "bunny");
        Assert.Equal("39FF2740DA6ADA7BF1DC87EF8D900777627C52BB5AA4010EF5A7906848C89F23",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((string)outline.Attribute("d")!))));
        Assert.Equal("translate(512 540) scale(1.064) translate(-625 -605)", (string?)bunny.Attribute("transform"));
        var layers = bunny.Elements(svg + "use").ToArray();
        Assert.Equal(2, layers.Length);
        Assert.All(layers, layer => Assert.Equal("#bunny", (string?)layer.Attribute(xlink + "href")));
        Assert.Equal("#fff", (string?)layers[0].Attribute("stroke"));
        Assert.Equal("40", (string?)layers[0].Attribute("stroke-width"));
        Assert.Equal("url(#bunny-blue)", (string?)layers[1].Attribute("fill"));
    }

    [Fact]
    public void IconResourceContainsAllWindowsSizes()
    {
        using var stream = typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.IconResourceName);
        Assert.NotNull(stream);
        using var reader = new BinaryReader(stream);
        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());
        var count = reader.ReadUInt16();
        var sizes = new List<int>();
        for (var index = 0; index < count; index++)
        {
            var width = reader.ReadByte();
            var height = reader.ReadByte();
            Assert.Equal(width, height);
            sizes.Add(width == 0 ? 256 : width);
            reader.ReadBytes(14);
        }
        Assert.Equal(new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 }, sizes);
    }

    [Fact]
    public void WindowsIconFramesFillTheirCanvasWithBalancedPadding()
    {
        using var stream = typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.IconResourceName)!;
        using var reader = new BinaryReader(stream);
        reader.ReadUInt16(); reader.ReadUInt16();
        var count = reader.ReadUInt16();
        var frames = new List<(int Size, int Length, int Offset)>();
        for (var index = 0; index < count; index++)
        {
            var size = reader.ReadByte();
            reader.ReadBytes(7);
            frames.Add((size == 0 ? 256 : size, reader.ReadInt32(), reader.ReadInt32()));
        }
        foreach (var frame in frames)
        {
            stream.Position = frame.Offset;
            using var png = new MemoryStream(reader.ReadBytes(frame.Length));
            using var image = new Bitmap(png);
            var left = image.Width; var top = image.Height; var right = -1; var bottom = -1;
            for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
            {
                if (image.GetPixel(x, y).A < 128) continue;
                left = Math.Min(left, x); top = Math.Min(top, y);
                right = Math.Max(right, x); bottom = Math.Max(bottom, y);
            }
            Assert.Equal(frame.Size, image.Width);
            Assert.Equal(frame.Size, image.Height);
            // Fractional edge coverage rounds to whole pixels at tiny taskbar sizes.
            Assert.True(right - left + 1 >= Math.Floor(frame.Size * .95), $"The {frame.Size}px frame has excessive horizontal padding.");
            Assert.True(bottom - top + 1 >= Math.Floor(frame.Size * .98), $"The {frame.Size}px frame has excessive vertical padding.");
            Assert.InRange(Math.Abs(left - (image.Width - 1 - right)), 0, 1);
            Assert.InRange(Math.Abs(top - (image.Height - 1 - bottom)), 0, 1);
            AssertWhiteBackground(image.GetPixel(0, 0));
        }
    }

    [Fact]
    public void PreviewArtworkIsExactlyTheLargestGeneratedWindowsFrame()
    {
        using var stream = typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.IconResourceName)!;
        using var reader = new BinaryReader(stream);
        reader.ReadBytes(4);
        var count = reader.ReadUInt16();
        stream.Position = 6 + 16 * (count - 1) + 8;
        var length = reader.ReadInt32();
        var offset = reader.ReadInt32();
        stream.Position = offset;
        var iconPng = reader.ReadBytes(length);
        using var logo = typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.LogoResourceName)!;
        using var png = new MemoryStream();
        logo.CopyTo(png);
        Assert.Equal(iconPng, png.ToArray());
        // The approved composite keeps its white background, not a cutout.
        var mark = AppBranding.Logo.GetPixel(128, 32);
        Assert.True(mark.A > 240 && mark.B > 180 && mark.R < 60);
        AssertWhiteBackground(AppBranding.Logo.GetPixel(128, 64));
    }

    [Fact]
    public void EmbeddedArtworkLoadsWithoutExternalFiles()
    {
        Assert.NotEqual(IntPtr.Zero, AppBranding.Icon.Handle);
        var logo = AppBranding.Logo;
        Assert.Equal(logo.Width, logo.Height);
        Assert.True(logo.Width >= 256);
        AssertWhiteBackground(logo.GetPixel(0, 0));
        AssertWhiteBackground(logo.GetPixel(logo.Width - 1, logo.Height - 1));
        Assert.True(logo.GetPixel(logo.Width / 2, logo.Height / 2).A > 0);
    }

    [Fact]
    public void EveryApplicationWindowUsesTheSharedBranding()
    {
        var formTypes = typeof(AppBranding).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(Form).IsAssignableFrom(type)).ToArray();
        Assert.NotEmpty(formTypes);
        Assert.All(formTypes, type => Assert.True(typeof(BrandedForm).IsAssignableFrom(type), type.Name));

        RunOnStaThread(() =>
        {
            var profiles = new[] { new ServerProfile { Name = "Public", ServerAddress = "public" } };
            using var main = new MainForm();
            using var targets = new TargetEditorForm(profiles, null);
            using var networks = new ProfilesForm(profiles);
            using var login = new PublicSignInForm("rustdesk.exe");
            foreach (var form in new Form[] { main, targets, networks, login })
            {
                Assert.Same(AppBranding.Icon, form.Icon);
                Assert.True(form.ShowIcon);
            }
            Assert.Empty(main.Controls.Find("ApplicationLogo", true));
            // Windows draws the shared icon in the real caption, not a second bitmap control.
            Assert.Empty(main.Controls.Find("TitleBarIcon", true));
        });
    }

    private static void AssertWhiteBackground(Color pixel)
    {
        Assert.Equal(255, pixel.A);
        Assert.True(pixel.R >= 248 && pixel.G >= 248 && pixel.B >= 248);
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception error) { failure = error; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Form construction timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
