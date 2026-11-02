using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Linq;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class AppBrandingTests
    {
        #region Test Methods
        [Fact]
        public void SingleMasterEmbedsTheExactApprovedBunnyRatherThanRedrawingIt()
        {
            XDocument document = LoadMaster();
            XNamespace svg = "http://www.w3.org/2000/svg";
            XNamespace xlink = "http://www.w3.org/1999/xlink";
            Assert.Equal("0 0 1014 1014", (string?)document.Root!.Attribute("viewBox"));
            XElement image = Assert.Single(document.Descendants(svg + "image"));
            Assert.Equal("approvedBunny", (string?)image.Attribute("id"));
            Assert.Equal("url(#bunnyClip)", (string?)image.Attribute("clip-path"));
            string uri = (string)image.Attribute(xlink + "href")!;
            const string PREFIX = "data:image/png;base64,";
            Assert.StartsWith(PREFIX, uri);
            byte[] originalBytes = Convert.FromBase64String(uri[PREFIX.Length..]);

            // Exact choice-A source, embedded once. Its bunny clip excludes the old
            // ring; the new ring is editable geometry, not another raster master.
            Assert.Equal("B4AAFD2C037A37C279283FDA94BA92511FE92CA29B2E68A47F99CB1C82DC1378",
                         Convert.ToHexString(SHA256.HashData(originalBytes))
                        );
            using MemoryStream stream = new MemoryStream(originalBytes);
            using Bitmap master = new Bitmap(stream);
            Assert.Equal(1014, master.Width);
            Assert.Equal(1014, master.Height);
            Assert.Equal(0, master.GetPixel(0, 0).A);
            AssertWhiteBackground(master.GetPixel(507, 0));
        }

        [Fact]
        public void RingUsesOneHalfRotatedExactly180DegreesOnTheRustDeskDiagonal()
        {
            XDocument document = LoadMaster();
            XNamespace svg = "http://www.w3.org/2000/svg";
            XNamespace xlink = "http://www.w3.org/1999/xlink";
            XElement ring = document.Descendants(svg + "g").Single(e => (string?)e.Attribute("id") == "ring");
            Assert.Equal("translate(532 505) rotate(-45)", (string?)ring.Attribute("transform"));
            XElement[] halves = ring.Elements(svg + "use").ToArray();
            Assert.Equal(2, halves.Length);
            Assert.All(halves, half => Assert.Equal("#ringHalf", (string?)half.Attribute(xlink + "href")));
            Assert.Null(halves[0].Attribute("transform"));
            Assert.Equal("rotate(180)", (string?)halves[1].Attribute("transform"));
            XElement shape = document.Descendants(svg + "path").Single(e => (string?)e.Attribute("id") == "ringHalf");
            string path = (string)shape.Attribute("d")!;
            Assert.Contains("A 414,414", path);
            Assert.Contains("A 338,338", path); // 76 units, the approved thin band.
        }

        [Fact]
        public void RenderedOpeningsAreClearOnBothEndsOfTheDiagonal()
        {
            Bitmap image = AppBranding.Logo;
            foreach (double angle in new[]
                     {
                         -45.0,
                         135.0
                     })
            {
                foreach (double radius in new[]
                         {
                             352.0,
                             376.0,
                             400.0
                         })
                {
                    AssertWhiteBackground(SampleRing(image, angle, radius));
                    foreach (double side in new[]
                             {
                                 -10.0,
                                 10.0
                             })
                    {
                        Color pixel = SampleRing(image, angle + side, radius);
                        Assert.True(pixel.B > 170 && pixel.R < 60,
                                    $"Missing ring at {angle + side} degrees / {radius} units."
                                   );
                    }
                }
            }
        }

        [Fact]
        public void RenderedTopBandRetainsTheDistinctiveEarlierThinRing()
        {
            Bitmap image = AppBranding.Logo;
            int first = -1;
            int last = -1;
            for (int y = 0; y < 100; y++)
            {
                Color pixel = image.GetPixel(128, y);
                if (pixel.R < 60 && pixel.B > 160)
                {
                    if (first < 0)
                        first = y;
                    last = y;
                }
                else if (first >= 0)
                    break;
            }

            Assert.InRange(first, 23, 25);
            Assert.InRange(last - first + 1, 18, 21);
        }

        [Fact]
        public void IconResourceContainsAllWindowsSizes()
        {
            using Stream? stream =
                typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.ICON_RESOURCE_NAME);
            Assert.NotNull(stream);
            using BinaryReader reader = new BinaryReader(stream);
            Assert.Equal(0, reader.ReadUInt16());
            Assert.Equal(1, reader.ReadUInt16());
            ushort count = reader.ReadUInt16();
            List<int> sizes = new List<int>();
            for (int index = 0; index < count; index++)
            {
                byte width = reader.ReadByte();
                byte height = reader.ReadByte();
                Assert.Equal(width, height);
                sizes.Add(width == 0 ? 256 : width);
                reader.ReadBytes(14);
            }

            Assert.Equal(new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 }, sizes);
        }

        [Fact]
        public void WindowsIconFramesFillTheirCanvasWithBalancedPadding()
        {
            using Stream stream =
                typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.ICON_RESOURCE_NAME)!;
            using BinaryReader reader = new BinaryReader(stream);
            reader.ReadUInt16();
            reader.ReadUInt16();
            ushort count = reader.ReadUInt16();
            List<(int Size, int Length, int Offset)> frames = new List<(int Size, int Length, int Offset)>();
            for (int index = 0; index < count; index++)
            {
                byte size = reader.ReadByte();
                reader.ReadBytes(7);
                frames.Add((size == 0 ? 256 : size, reader.ReadInt32(), reader.ReadInt32()));
            }

            foreach ((int Size, int Length, int Offset) frame in frames)
            {
                stream.Position = frame.Offset;
                using MemoryStream png = new MemoryStream(reader.ReadBytes(frame.Length));
                using Bitmap image = new Bitmap(png);
                int left = image.Width;
                int top = image.Height;
                int right = -1;
                int bottom = -1;
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        if (image.GetPixel(x, y).A < 128)
                            continue;

                        left = Math.Min(left, x);
                        top = Math.Min(top, y);
                        right = Math.Max(right, x);
                        bottom = Math.Max(bottom, y);
                    }
                }

                Assert.Equal(frame.Size, image.Width);
                Assert.Equal(frame.Size, image.Height);

                // Fractional edge coverage rounds to whole pixels at tiny taskbar sizes.
                Assert.True(right - left + 1 >= Math.Floor(frame.Size * .99),
                            $"The {frame.Size}px frame has excessive horizontal padding."
                           );
                Assert.True(bottom - top + 1 >= Math.Floor(frame.Size * .98),
                            $"The {frame.Size}px frame has excessive vertical padding."
                           );
                Assert.InRange(Math.Abs(left - (image.Width - 1 - right)), 0, 1);
                Assert.InRange(Math.Abs(top - (image.Height - 1 - bottom)), 0, 1);

                // The authentic tile radius has fractional corner coverage in the
                // 16px frame. Preserve that antialiasing, not an opaque square.
                int cornerCoverage = frame.Size == 16 ? 24 : 0;
                Assert.InRange(image.GetPixel(0, 0).A, 0, cornerCoverage);
                Assert.InRange(image.GetPixel(frame.Size - 1, 0).A, 0, cornerCoverage);
                Assert.InRange(image.GetPixel(0, frame.Size - 1).A, 0, cornerCoverage);
                Assert.InRange(image.GetPixel(frame.Size - 1, frame.Size - 1).A, 0, cornerCoverage);
                AssertWhiteBackground(image.GetPixel(frame.Size / 2, 0));
            }
        }

        [Fact]
        public void PreviewArtworkIsExactlyTheLargestGeneratedWindowsFrame()
        {
            using Stream stream =
                typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.ICON_RESOURCE_NAME)!;
            using BinaryReader reader = new BinaryReader(stream);
            reader.ReadBytes(4);
            ushort count = reader.ReadUInt16();
            stream.Position = 6 + 16 * (count - 1) + 8;
            int length = reader.ReadInt32();
            int offset = reader.ReadInt32();
            stream.Position = offset;
            byte[] iconPng = reader.ReadBytes(length);
            using Stream logo = typeof(AppBranding).Assembly.GetManifestResourceStream(AppBranding.LOGO_RESOURCE_NAME)!;
            using MemoryStream png = new MemoryStream();
            logo.CopyTo(png);
            Assert.Equal(iconPng, png.ToArray());

            // The approved composite keeps its white background, not a cutout.
            Color mark = AppBranding.Logo.GetPixel(128, 32);
            Assert.True(mark.A > 240 && mark.B > 180 && mark.R < 60);
            AssertWhiteBackground(AppBranding.Logo.GetPixel(128, 54));
        }

        [Fact]
        public void EmbeddedArtworkLoadsWithoutExternalFiles()
        {
            Assert.NotEqual(IntPtr.Zero, AppBranding.Icon.Handle);
            Bitmap logo = AppBranding.Logo;
            Assert.Equal(logo.Width, logo.Height);
            Assert.True(logo.Width >= 256);
            Assert.Equal(0, logo.GetPixel(0, 0).A);
            Assert.Equal(0, logo.GetPixel(logo.Width - 1, logo.Height - 1).A);
            AssertWhiteBackground(logo.GetPixel(logo.Width / 2, 0));
            Assert.True(logo.GetPixel(logo.Width / 2, logo.Height / 2).A > 0);
        }

        [Fact]
        public void EveryApplicationWindowUsesTheSharedBranding()
        {
            Type[] formTypes = typeof(AppBranding)
                .Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(Form).IsAssignableFrom(type))
                .ToArray();
            Assert.NotEmpty(formTypes);
            Assert.All(formTypes, type => Assert.True(typeof(BrandedForm).IsAssignableFrom(type), type.Name));
            RunOnStaThread(() =>
                           {
                               ServerProfile[] profiles = new[]
                               {
                                   new ServerProfile
                                   {
                                       Name = "Public",
                                       ServerAddress = "public"
                                   }
                               };
                               using MainForm main = TestApplication.CreateMainForm();
                               using ProfilesForm networks = new ProfilesForm(profiles);
                               using PublicSignInForm login = TestApplication.CreateSignInForm("rustdesk.exe");
                               foreach (Form? form in new Form[]
                                        {
                                            main,
                                            networks,
                                            login
                                        })
                               {
                                   Assert.Same(AppBranding.Icon, form.Icon);
                                   Assert.True(form.ShowIcon);
                               }

                               Assert.Empty(main.Controls.Find("ApplicationLogo", true));

                               // Windows draws the shared icon in the real caption, not a second bitmap control.
                               Assert.Empty(main.Controls.Find("TitleBarIcon", true));
                           }
                          );
        }
        #endregion

        #region Methods
        private static XDocument LoadMaster() =>
            XDocument.Load(Path.Combine(AppContext.BaseDirectory, "BrandingMaster.svg"));

        private static Color SampleRing(Bitmap image, double angle, double radius)
        {
            double radians = angle * Math.PI / 180;
            return image.GetPixel((int)Math.Round((532 + Math.Cos(radians) * radius) * image.Width / 1014),
                                  (int)Math.Round((505 + Math.Sin(radians) * radius) * image.Height / 1014)
                                 );
        }

        private static void AssertWhiteBackground(Color pixel)
        {
            Assert.Equal(255, pixel.A);
            Assert.True(pixel.R >= 248 && pixel.G >= 248 && pixel.B >= 248);
        }

        private static void RunOnStaThread(Action action)
        {
            Exception? failure = null;
            Thread thread = new Thread(() =>
                                       {
                                           try
                                           {
                                               action();
                                           }
                                           catch (Exception error)
                                           {
                                               failure = error;
                                           }
                                       }
                                      )
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Form construction timed out.");
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
        #endregion
    }
}