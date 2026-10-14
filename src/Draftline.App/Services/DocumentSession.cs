using Draftline.App.Model;

namespace Draftline.App.Services;

/// <summary>
/// The editor's state: which wireframe is open, what is selected on it, and how it got here.
///
/// Every change goes through <see cref="Edit"/>. That single door is what keeps three consumers
/// honest - the canvas draws what this class holds, the inspector writes through it, and the autosave
/// listens to it - because there is no second path by which the document could change. A change that
/// did not go through here would be a change nobody could undo and nobody would save.
/// </summary>
public sealed class DocumentSession
{
    private readonly DocumentHistory _history;
    private readonly List<string> _selection = new();
    private DraftDocument? _gestureBefore;
    private string? _gestureLabel;
    private bool _gestureChanged;

    /// <summary>A session holding a new, empty wireframe.</summary>
    public DocumentSession()
        : this(new DocumentHistory())
    {
    }

    /// <summary>A session over a history of its own - what lets the undo depth be chosen.</summary>
    public DocumentSession(DocumentHistory history)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        Document = DraftDocument.CreateNew("Без имени");
    }

    /// <summary>The open wireframe. Never null: a session always has something to draw.</summary>
    public DraftDocument Document { get; private set; }

    /// <summary>Identities of the selected blocks, in the order they were selected.</summary>
    public IReadOnlyList<string> Selection => _selection;

    /// <summary>The single selected block, or null when nothing - or more than one thing - is selected.</summary>
    public WireframeElement? Selected => _selection.Count == 1 ? Document.Find(_selection[0]) : null;

    /// <summary>Raised after every change of the document or the selection.</summary>
    public event Action? Changed;

    /// <summary>True while the document holds changes the storage has not written yet.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>What the last change was. The shell shows it beside the save state.</summary>
    public string Status { get; private set; } = "Новый макет";

    /// <summary>Whether there is a change to undo.</summary>
    public bool CanUndo => _history.CanUndo;

    /// <summary>Whether there is an undone change to redo.</summary>
    public bool CanRedo => _history.CanRedo;

    /// <summary>
    /// Opens a document, forgetting the history and the selection of the one before it: undo across
    /// two documents would restore blocks into a wireframe they never belonged to.
    /// </summary>
    public void Open(DraftDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Document = document;
        _history.Clear();
        _selection.Clear();
        IsDirty = false;
        Status = "Открыт макет: " + document.Name;

        Changed?.Invoke();
    }

    /// <summary>
    /// Changes the document: remembers the state before, applies the change, marks the document dirty
    /// and tells everyone. The label is what the person reads in the status line and what an undo
    /// says afterwards.
    /// </summary>
    public void Edit(string label, Action<DraftDocument> change)
    {
        ArgumentNullException.ThrowIfNull(change);

        var before = Document.Clone();
        change(Document);
        Document.Touch();
        _history.Record(before, label);

        IsDirty = true;
        Status = label;

        Changed?.Invoke();
    }

    /// <summary>Adds a block at a point, snaps it to the grid, selects it and returns it.</summary>
    public WireframeElement Add(WireframeKind kind, double x, double y)
    {
        var element = ElementDefaults.Create(kind);
        var bounds = CanvasGeometry.Clamp(
            new Rect(
                CanvasGeometry.Snap(x, Document.GridSize),
                CanvasGeometry.Snap(y, Document.GridSize),
                element.Width,
                element.Height),
            Document.CanvasWidth,
            Document.CanvasHeight);

        element.X = bounds.X;
        element.Y = bounds.Y;
        element.Width = bounds.Width;
        element.Height = bounds.Height;

        Edit("Добавлен блок: " + ElementDefaults.NameOf(kind), document =>
        {
            element.ZIndex = document.NextZIndex();
            document.Elements.Add(element);
        });

        Select(element.Id);
        return element;
    }

    /// <summary>Selects one block; an identity that is not in the document clears the selection.</summary>
    public void Select(string? id) =>
        SetSelection(id is null || Document.Find(id) is null ? Array.Empty<string>() : new[] { id });

    /// <summary>Replaces the selection.</summary>
    public void SetSelection(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var next = ids.Where(id => Document.Find(id) is not null).ToList();
        if (next.SequenceEqual(_selection))
        {
            return;
        }

        _selection.Clear();
        _selection.AddRange(next);

        Changed?.Invoke();
    }

    /// <summary>Deselects everything - what a click on the empty sheet does.</summary>
    public void ClearSelection() => SetSelection(Array.Empty<string>());

    /// <summary>Deletes the selected blocks. False when there was nothing to delete.</summary>
    public bool RemoveSelected()
    {
        var ids = _selection.Where(id => Document.Find(id) is not null).ToList();
        if (ids.Count == 0)
        {
            return false;
        }

        Edit(
            ids.Count == 1 ? "Блок удалён" : $"Удалено блоков: {ids.Count}",
            document => document.Elements.RemoveAll(element => ids.Contains(element.Id)));

        ClearSelection();
        return true;
    }

    /// <summary>Copies the selected block a step away, selects the copy and returns it.</summary>
    public WireframeElement? DuplicateSelected()
    {
        if (Selected is not { } source)
        {
            return null;
        }

        var copy = source.Clone(keepId: false);
        var offset = Document.GridSize > 1 ? Document.GridSize : 8;
        var bounds = CanvasGeometry.Clamp(
            source.Bounds.With(x: source.X + offset, y: source.Y + offset),
            Document.CanvasWidth,
            Document.CanvasHeight);

        copy.X = bounds.X;
        copy.Y = bounds.Y;

        Edit("Блок скопирован", document =>
        {
            copy.ZIndex = document.NextZIndex();
            document.Elements.Add(copy);
        });

        Select(copy.Id);
        return copy;
    }

    /// <summary>Puts the selected block in front of the others.</summary>
    public void BringSelectedToFront() =>
        RearrangeSelected("Блок поверх остальных", (document, element) => document.BringToFront(element));

    /// <summary>Puts the selected block behind the others.</summary>
    public void SendSelectedToBack() =>
        RearrangeSelected("Блок позади остальных", (document, element) => document.SendToBack(element));

    /// <summary>Steps back one change. False when there is nothing to undo.</summary>
    public bool Undo()
    {
        var entry = _history.Undo(Document);
        if (entry is null)
        {
            return false;
        }

        Document = entry.Document;
        PruneSelection();

        // Undoing is a change like any other: it leaves the document different from what the storage
        // last wrote, so the document is dirty again and the status says what was undone.
        IsDirty = true;
        Status = entry.Status;

        Changed?.Invoke();
        return true;
    }

    /// <summary>Steps forward again into an undone change. False when there is nothing to redo.</summary>
    public bool Redo()
    {
        var entry = _history.Redo(Document);
        if (entry is null)
        {
            return false;
        }

        Document = entry.Document;
        PruneSelection();
        IsDirty = true;
        Status = entry.Status;

        Changed?.Invoke();
        return true;
    }

    /// <summary>Renames the open wireframe.</summary>
    public void Rename(string name)
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? "Без имени" : name.Trim();
        if (trimmed == Document.Name)
        {
            return;
        }

        Edit("Макет переименован: " + trimmed, document => document.Name = trimmed);
    }

    /// <summary>The storage has written the document: it is no longer dirty, and the status says so.</summary>
    public void MarkSaved(string status)
    {
        IsDirty = false;
        Status = status;

        Changed?.Invoke();
    }

    private void RearrangeSelected(string label, Action<DraftDocument, WireframeElement> rearrange)
    {
        if (Selected is not { } selected)
        {
            return;
        }

        Edit(label, document => rearrange(document, selected));
    }

    /// <summary>
    /// Starts a change that will be drawn many times but recorded once - a drag, a resize. The
    /// document is snapshotted here, so everything <see cref="Preview"/> does between this call and
    /// <see cref="EndGesture"/> becomes a single step in the history. Without that, dragging a block
    /// across the sheet would fill the history with one step per pixel of movement.
    /// </summary>
    public void BeginGesture(string label)
    {
        _gestureBefore = Document.Clone();
        _gestureLabel = label;
        _gestureChanged = false;

        Status = label;
        Changed?.Invoke();
    }

    /// <summary>Changes the document while a gesture is running: no history step, one repaint.</summary>
    public void Preview(Action<DraftDocument> change)
    {
        ArgumentNullException.ThrowIfNull(change);

        change(Document);
        Document.Touch();

        _gestureChanged = true;
        IsDirty = true;

        Changed?.Invoke();
    }

    /// <summary>
    /// Ends the gesture. What it did becomes one undoable step - unless nothing actually moved, in
    /// which case a press and release with no drag leaves no trace in the history at all.
    /// </summary>
    public bool EndGesture()
    {
        var before = _gestureBefore;
        var label = _gestureLabel ?? "Изменение";
        var changed = _gestureChanged;

        _gestureBefore = null;
        _gestureLabel = null;
        _gestureChanged = false;

        if (before is null || !changed)
        {
            return false;
        }

        _history.Record(before, label);
        Status = label;

        Changed?.Invoke();
        return true;
    }

    /// <summary>True while a gesture is being drawn.</summary>
    public bool IsGestureRunning => _gestureBefore is not null;

    private void PruneSelection() => _selection.RemoveAll(id => Document.Find(id) is null);
}
