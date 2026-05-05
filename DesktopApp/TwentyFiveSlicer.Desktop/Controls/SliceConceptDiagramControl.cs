using System.Windows;
using System.Windows.Media;

namespace TwentyFiveSlicer.Desktop.Controls;

public sealed class SliceConceptDiagramControl : FrameworkElement
{
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(220d, 140d);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        Rect bounds = new(0d, 0d, ActualWidth, ActualHeight);
        if (bounds.Width <= 0d || bounds.Height <= 0d)
        {
            return;
        }

        Pen borderPen = new(new SolidColorBrush(Color.FromRgb(54, 71, 91)), 1.5d);
        Pen guidePen = new(new SolidColorBrush(Color.FromRgb(0, 184, 148)), 1d);
        Brush fixedBrush = new SolidColorBrush(Color.FromRgb(242, 201, 76));
        Brush stretchBrush = new SolidColorBrush(Color.FromRgb(86, 204, 242));

        Rect art = new(12d, 12d, Math.Max(20d, bounds.Width - 24d), Math.Max(20d, bounds.Height - 24d));
        drawingContext.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(18, 26, 36)), borderPen, art, 6d, 6d);

        double[] xs =
        [
            art.Left,
            art.Left + art.Width * 0.18d,
            art.Left + art.Width * 0.36d,
            art.Left + art.Width * 0.64d,
            art.Left + art.Width * 0.82d,
            art.Right
        ];
        double[] ys =
        [
            art.Top,
            art.Top + art.Height * 0.18d,
            art.Top + art.Height * 0.36d,
            art.Top + art.Height * 0.64d,
            art.Top + art.Height * 0.82d,
            art.Bottom
        ];

        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                Rect cell = new(xs[column], ys[row], xs[column + 1] - xs[column], ys[row + 1] - ys[row]);
                bool stretchRegion = row is 1 or 3 || column is 1 or 3;
                drawingContext.DrawRectangle(stretchRegion ? stretchBrush : fixedBrush, null, cell);
            }
        }

        for (int index = 1; index < 5; index++)
        {
            drawingContext.DrawLine(guidePen, new Point(xs[index], art.Top), new Point(xs[index], art.Bottom));
            drawingContext.DrawLine(guidePen, new Point(art.Left, ys[index]), new Point(art.Right, ys[index]));
        }

        drawingContext.DrawRoundedRectangle(null, borderPen, art, 6d, 6d);
    }
}
