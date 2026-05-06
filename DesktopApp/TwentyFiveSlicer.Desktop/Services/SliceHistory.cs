using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class SliceHistory
{
    private readonly Stack<SliceEditorState> _undo = new();
    private readonly Stack<SliceEditorState> _redo = new();
    private SliceEditorState _current;

    public SliceHistory(SliceEditorState initialState)
    {
        _current = initialState.Snapshot();
    }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Push(SliceEditorState state)
    {
        if (StateEquals(_current, state))
        {
            return;
        }

        _undo.Push(_current.Snapshot());
        _current = state.Snapshot();
        _redo.Clear();
    }

    public SliceEditorState Undo()
    {
        if (!CanUndo)
        {
            return _current.Snapshot();
        }

        _redo.Push(_current.Snapshot());
        _current = _undo.Pop();
        return _current.Snapshot();
    }

    public SliceEditorState Redo()
    {
        if (!CanRedo)
        {
            return _current.Snapshot();
        }

        _undo.Push(_current.Snapshot());
        _current = _redo.Pop();
        return _current.Snapshot();
    }

    public void Reset(SliceEditorState state)
    {
        _undo.Clear();
        _redo.Clear();
        _current = state.Snapshot();
    }

    private static bool StateEquals(SliceEditorState left, SliceEditorState right)
    {
        return Math.Abs(left.TargetWidth - right.TargetWidth) < 0.01d &&
            Math.Abs(left.TargetHeight - right.TargetHeight) < 0.01d &&
            left.VerticalBorders.SequenceEqual(right.VerticalBorders) &&
            left.HorizontalBorders.SequenceEqual(right.HorizontalBorders) &&
            SegmentModesEqual(left.XSegments, right.XSegments) &&
            SegmentModesEqual(left.YSegments, right.YSegments) &&
            CellOverridesEqual(left.CellOverrides, right.CellOverrides);
    }

    private static bool SegmentModesEqual(IReadOnlyList<SliceSegmentDefinition>? left, IReadOnlyList<SliceSegmentDefinition>? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return left.Select(segment => segment.Mode).SequenceEqual(right.Select(segment => segment.Mode));
    }

    private static bool CellOverridesEqual(IReadOnlyList<SliceCellOverrideDefinition>? left, IReadOnlyList<SliceCellOverrideDefinition>? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            SliceCellOverrideDefinition leftOverride = left[index];
            SliceCellOverrideDefinition rightOverride = right[index];
            if (leftOverride.Column != rightOverride.Column || leftOverride.Row != rightOverride.Row)
            {
                return false;
            }

            if (!RectOverridesEqual(leftOverride.SourceRectPercent, rightOverride.SourceRectPercent) ||
                !RectOverridesEqual(leftOverride.DestinationRectPercent, rightOverride.DestinationRectPercent))
            {
                return false;
            }
        }

        return true;
    }

    private static bool RectOverridesEqual(SliceCellRectOverride? left, SliceCellRectOverride? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return Math.Abs(left.XPercent - right.XPercent) < 0.001d &&
            Math.Abs(left.YPercent - right.YPercent) < 0.001d &&
            Math.Abs(left.WidthPercent - right.WidthPercent) < 0.001d &&
            Math.Abs(left.HeightPercent - right.HeightPercent) < 0.001d;
    }
}
