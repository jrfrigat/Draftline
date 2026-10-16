using Draftline.App.Model;

namespace Draftline.App.Services;
/// <summary>
/// Undo and redo over whole-document snapshots.
///
/// Snapshots rather than commands: a wireframe is small - a few dozen blocks of a handful of numbers
/// each - so copying one costs nothing a person can feel, and a snapshot cannot drift out of step
/// with the document the way an undo command can when another code path changes the very field it
/// means to restore. The copies are made on the way in and on the way out, so neither the history
/// nor its caller can reach into the other's document.
/// </summary>
public sealed class DocumentHistory
{
    /// <summary>How many steps back the editor remembers.</summary>
    public const int DefaultCapacity = 50;
    private readonly List<Entry> _past = new();
    private readonly List<Entry> _future = new();
    /// <summary>A history that remembers the given number of steps.</summary>
    public DocumentHistory(int capacity = DefaultCapacity) => Capacity = Math.Max(1, capacity);
    /// <summary>How many steps back this history keeps.</summary>
    public int Capacity { get; }
    /// <summary>Whether there is a step to go back to.</summary>
    public bool CanUndo => _past.Count > 0;
    /// <summary>Whether there is a step to go forward to again.</summary>
    public bool CanRedo => _future.Count > 0;
    /// <summary>How many steps back are remembered right now.</summary>
    public int Depth => _past.Count;

    /// <summary>
    /// Remembers the state a change is about to replace. Call it with the document as it was before
    /// the change, together with the label the person will see - a change that is not recorded is a
    /// change that cannot be undone.
    /// </summary>
    public void Record(DraftDocument before, string label)
    {
        ArgumentNullException.ThrowIfNull(before);
        RecordSnapshot(before.Clone(), label);
    }

    /// <summary>Consumes a snapshot privately created by DocumentSession; callers retain no mutable reference.</summary>
    internal void RecordSnapshot(DraftDocument before, string label)
    {
        _past.Add(new Entry(before, label));
        if (_past.Count > Capacity)
        {
            _past.RemoveAt(0);
        }

        // Changing something after an undo abandons the future it was heading for.
        _future.Clear();
    }

    /// <summary>The state to return to, and the sentence for the status line; null when there is nothing to undo.</summary>
    public DocumentHistoryEntry? Undo(DraftDocument current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (_past.Count == 0)
        {
            return null;
        }

        var entry = _past[^1];
        _past.RemoveAt(_past.Count - 1);
        _future.Add(new Entry(current.Clone(), entry.Label));
        return new DocumentHistoryEntry(entry.Document.Clone(), "Отменено: " + entry.Label);
    }

    /// <summary>The state an undone change would bring back, or null when there is nothing to redo.</summary>
    public DocumentHistoryEntry? Redo(DraftDocument current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (_future.Count == 0)
        {
            return null;
        }

        var entry = _future[^1];
        _future.RemoveAt(_future.Count - 1);
        _past.Add(new Entry(current.Clone(), entry.Label));
        return new DocumentHistoryEntry(entry.Document.Clone(), "Повторено: " + entry.Label);
    }

    /// <summary>Forgets everything - what opening another document does.</summary>
    public void Clear()
    {
        _past.Clear();
        _future.Clear();
    }

    private sealed record Entry(DraftDocument Document, string Label);
}

/// <summary>A step the history produced: the state to show, and what to say about it.</summary>
/// <param name = "Document">The document as it was, or as it will be again.</param>
/// <param name = "Status">The sentence the status line shows.</param>
public sealed record DocumentHistoryEntry(DraftDocument Document, string Status);
