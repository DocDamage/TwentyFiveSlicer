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
}
