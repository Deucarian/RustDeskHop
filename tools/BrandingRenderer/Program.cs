using System.Drawing.Imaging;
using Svg;

if (args.Length != 2) throw new ArgumentException("Usage: BrandingRenderer <master.svg> <output.png>");
var document = SvgDocument.Open<SvgDocument>(Path.GetFullPath(args[0]));
if (document.ViewBox.Width != document.ViewBox.Height || document.ViewBox.Width <= 0)
    throw new InvalidDataException("The icon master must have a square viewBox.");
using var bitmap = document.Draw(1024, 1024);
var destination = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
bitmap.Save(destination, ImageFormat.Png);
Console.WriteLine($"Rendered single SVG master to {destination}");
