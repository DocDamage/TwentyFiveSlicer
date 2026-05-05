using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace TwentyFiveSlicer.Desktop.Services;

public static class SvgIconRenderer
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    public static DrawingGroup LoadResource(string resourcePath, Brush foreground)
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "TwentyFiveSlicer.Desktop";
        var resourceUri = new Uri($"/{assemblyName};component/{resourcePath}", UriKind.Relative);
        System.Windows.Resources.StreamResourceInfo? resource = Application.GetResourceStream(resourceUri);

        if (resource is null)
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, resourcePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(filePath))
            {
                return CreateFallbackDrawing(foreground);
            }

            using FileStream fileStream = File.OpenRead(filePath);
            return Load(fileStream, foreground);
        }

        using Stream stream = resource.Stream;
        return Load(stream, foreground);
    }

    public static DrawingGroup LoadFile(string filePath, Brush foreground)
    {
        using FileStream stream = File.OpenRead(filePath);
        return Load(stream, foreground);
    }

    public static DrawingGroup Load(Stream stream, Brush foreground)
    {
        XDocument document = XDocument.Load(stream);
        XElement root = document.Root ?? throw new InvalidDataException("SVG document has no root element.");
        Rect viewBox = ReadViewBox(root);
        var group = new DrawingGroup
        {
            ClipGeometry = new RectangleGeometry(viewBox)
        };

        using (DrawingContext context = group.Open())
        {
            RenderChildren(context, root, foreground, root);
        }

        group.Transform = new TranslateTransform(-viewBox.X, -viewBox.Y);
        group.Freeze();
        return group;
    }

    private static void RenderChildren(DrawingContext context, XElement element, Brush foreground, XElement root)
    {
        foreach (XElement child in element.Elements())
        {
            string localName = child.Name.LocalName;
            if (localName is "title" or "desc")
            {
                continue;
            }

            if (localName is "g" or "svg")
            {
                RenderChildren(context, child, foreground, root);
                continue;
            }

            Geometry? geometry = localName switch
            {
                "path" => CreatePathGeometry(child),
                "line" => CreateLineGeometry(child),
                "polyline" => CreatePolylineGeometry(child, close: false),
                "polygon" => CreatePolylineGeometry(child, close: true),
                "circle" => CreateCircleGeometry(child),
                "rect" => CreateRectGeometry(child),
                _ => null
            };

            if (geometry is null)
            {
                continue;
            }

            Brush? fill = ReadBrush(child, root, "fill", foreground, defaultToForeground: false);
            Pen? pen = CreatePen(child, root, foreground);
            if (fill is null && pen is null)
            {
                fill = foreground;
            }

            context.DrawGeometry(fill, pen, geometry);
        }
    }

    private static Geometry? CreatePathGeometry(XElement element)
    {
        string? data = ReadString(element, "d");
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        return Geometry.Parse(data);
    }

    private static Geometry? CreateLineGeometry(XElement element)
    {
        return new LineGeometry(
            new Point(ReadDouble(element, "x1"), ReadDouble(element, "y1")),
            new Point(ReadDouble(element, "x2"), ReadDouble(element, "y2")));
    }

    private static Geometry? CreatePolylineGeometry(XElement element, bool close)
    {
        string? pointsValue = ReadString(element, "points");
        if (string.IsNullOrWhiteSpace(pointsValue))
        {
            return null;
        }

        double[] values = pointsValue
            .Replace(',', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => double.Parse(value, CultureInfo.InvariantCulture))
            .ToArray();

        if (values.Length < 4)
        {
            return null;
        }

        var figure = new PathFigure
        {
            StartPoint = new Point(values[0], values[1]),
            IsClosed = close
        };

        for (int index = 2; index + 1 < values.Length; index += 2)
        {
            figure.Segments.Add(new LineSegment(new Point(values[index], values[index + 1]), true));
        }

        return new PathGeometry([figure]);
    }

    private static Geometry CreateCircleGeometry(XElement element)
    {
        return new EllipseGeometry(
            new Point(ReadDouble(element, "cx"), ReadDouble(element, "cy")),
            ReadDouble(element, "r"),
            ReadDouble(element, "r"));
    }

    private static Geometry CreateRectGeometry(XElement element)
    {
        return new RectangleGeometry(new Rect(
            ReadDouble(element, "x"),
            ReadDouble(element, "y"),
            ReadDouble(element, "width"),
            ReadDouble(element, "height")));
    }

    private static Pen? CreatePen(XElement element, XElement root, Brush foreground)
    {
        Brush? stroke = ReadBrush(element, root, "stroke", foreground, defaultToForeground: false);
        if (stroke is null)
        {
            return null;
        }

        double strokeWidth = ReadDouble(element, root, "stroke-width", defaultValue: 1d);
        var pen = new Pen(stroke, strokeWidth)
        {
            StartLineCap = ReadLineCap(element, root),
            EndLineCap = ReadLineCap(element, root),
            LineJoin = ReadLineJoin(element, root)
        };
        pen.Freeze();
        return pen;
    }

    private static Brush? ReadBrush(XElement element, XElement root, string attributeName, Brush foreground, bool defaultToForeground)
    {
        string? value = ReadString(element, attributeName) ?? ReadString(root, attributeName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultToForeground ? foreground : null;
        }

        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(value, "currentColor", StringComparison.OrdinalIgnoreCase))
        {
            return foreground;
        }

        try
        {
            var converter = new BrushConverter();
            return converter.ConvertFromString(value) as Brush ?? foreground;
        }
        catch (NotSupportedException)
        {
            return foreground;
        }
        catch (FormatException)
        {
            return foreground;
        }
    }

    private static PenLineCap ReadLineCap(XElement element, XElement root)
    {
        return (ReadString(element, "stroke-linecap") ?? ReadString(root, "stroke-linecap")) switch
        {
            "round" => PenLineCap.Round,
            "square" => PenLineCap.Square,
            _ => PenLineCap.Flat
        };
    }

    private static PenLineJoin ReadLineJoin(XElement element, XElement root)
    {
        return (ReadString(element, "stroke-linejoin") ?? ReadString(root, "stroke-linejoin")) switch
        {
            "round" => PenLineJoin.Round,
            "bevel" => PenLineJoin.Bevel,
            _ => PenLineJoin.Miter
        };
    }

    private static Rect ReadViewBox(XElement root)
    {
        string? viewBox = ReadString(root, "viewBox");
        if (!string.IsNullOrWhiteSpace(viewBox))
        {
            double[] values = viewBox
                .Replace(',', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => double.Parse(value, CultureInfo.InvariantCulture))
                .ToArray();

            if (values.Length == 4)
            {
                return new Rect(values[0], values[1], values[2], values[3]);
            }
        }

        return new Rect(0d, 0d, ReadDouble(root, "width", 24d), ReadDouble(root, "height", 24d));
    }

    private static string? ReadString(XElement element, string attributeName)
    {
        return element.Attribute(attributeName)?.Value ??
            element.Attribute(SvgNamespace + attributeName)?.Value;
    }

    private static double ReadDouble(XElement element, string attributeName, double defaultValue = 0d)
    {
        string? value = ReadString(element, attributeName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        value = value.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            ? value[..^2]
            : value;
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : defaultValue;
    }

    private static double ReadDouble(XElement element, XElement root, string attributeName, double defaultValue)
    {
        string? value = ReadString(element, attributeName) ?? ReadString(root, attributeName);
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                ? result
                : defaultValue;
    }

    private static DrawingGroup CreateFallbackDrawing(Brush foreground)
    {
        var group = new DrawingGroup();
        using DrawingContext context = group.Open();
        var pen = new Pen(foreground, 2d);
        context.DrawRectangle(null, pen, new Rect(4d, 4d, 16d, 16d));
        context.DrawLine(pen, new Point(7d, 7d), new Point(17d, 17d));
        context.DrawLine(pen, new Point(17d, 7d), new Point(7d, 17d));
        group.Freeze();
        return group;
    }
}
