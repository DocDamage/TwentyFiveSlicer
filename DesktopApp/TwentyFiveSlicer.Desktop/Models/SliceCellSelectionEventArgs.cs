namespace TwentyFiveSlicer.Desktop.Models;

public sealed class SliceCellSelectionEventArgs : EventArgs
{
    public SliceCellSelectionEventArgs(int column, int row)
    {
        Column = column;
        Row = row;
    }

    public int Column { get; }

    public int Row { get; }
}