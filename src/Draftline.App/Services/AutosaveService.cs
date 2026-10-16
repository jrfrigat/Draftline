namespace Draftline.App.Services;
/// <summary>What the autosave is doing, as the shell shows it.</summary>
public enum AutosaveState
{
    /// <summary>Nothing has changed since the last write.</summary>
    Idle,
    /// <summary>Something changed and the writing is waiting for quiet.</summary>
    Pending,
    /// <summary>The write is happening.</summary>
    Saving,
    /// <summary>The last change is in the browser's storage.</summary>
    Saved,
    /// <summary>The write was refused - and the reason is in the message.</summary>
    Failed
}

/// <summary>
/// Writes the open wireframe to the browser's storage after every change, once the changes stop.
///
/// It listens to <see cref = "DocumentSession.Changed"/> rather than being called from the canvas or
/// the inspector: those two change the document in a dozen places, and a save that has to be
/// remembered in a dozen places is a save that will be forgotten in one of them.
///
/// The delay exists because a drag is hundreds of changes: writing after each one would spend the
/// browser's time on documents nobody has finished making, and writing only at the end would lose
/// everything between two gestures. Waiting for quiet is the middle that costs nothing.
/// </summary>
public sealed class AutosaveService : IDisposable
{
    /// <summary>How long the writing waits for the changes to stop.</summary>
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(800);
    private readonly DocumentSession _session;
    private readonly ProjectCatalog _catalog;
    private readonly TimeSpan _delay;
    private CancellationTokenSource? _pending;
    private bool _disposed;
    private readonly SemaphoreSlim _writes = new(1, 1);
    /// <summary>Autosave for a session, writing through a catalog.</summary>
    public AutosaveService(DocumentSession session, ProjectCatalog catalog, TimeSpan? delay = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _delay = delay ?? DefaultDelay;
        _session.Changed += OnSessionChanged;
    }

    /// <summary>Where the saving is now.</summary>
    public AutosaveState State { get; private set; } = AutosaveState.Idle;
    /// <summary>What to tell the person about it.</summary>
    public string Message { get; private set; } = "Изменений нет";

    /// <summary>Raised whenever the state or the message changed.</summary>
    public event Action? Changed;
    /// <summary>Writes right now, whatever the delay was waiting for.</summary>
    public async Task<bool> FlushAsync(CancellationToken cancellationToken = default)
    {
        CancelPending();
        await _writes.WaitAsync(cancellationToken);
        try
        {
            var documentId = _session.Document.Id;
            do
            {
                if (!await WriteAsync(cancellationToken)) return false;
                if (_session.Document.Id != documentId) return false;
            }
            while (_session.IsDirty);
            return true;
        }
        finally
        {
            _writes.Release();
        }
    }

    private async Task<bool> WriteAsync(CancellationToken cancellationToken)
    {
        if (_session.IsGestureRunning)
            return false;
        if (!_session.IsDirty)
            return true;
        var version = _session.EditVersion;
        var document = _session.Document.Clone();
        State = AutosaveState.Saving;
        Message = "Сохраняется…";
        Changed?.Invoke();
        try
        {
            await _catalog.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await _catalog.RememberLastAsync(document.Id, cancellationToken).ConfigureAwait(false);
            State = AutosaveState.Saved;
            Message = "Сохранено в браузере в " + DateTimeOffset.Now.ToString("HH:mm:ss");
            // Telling the session is what stops the loop: a document that was just written is not
            // dirty any more, and the write itself must not look like another change.
            if (_session.Document.Id == document.Id && _session.EditVersion == version)
                _session.MarkSaved(Message);
            else
            {
                State = AutosaveState.Pending;
                Message = "Есть несохранённые изменения";
                Schedule();
            }

            Changed?.Invoke();
            return true;
        }
        catch (OperationCanceledException)when (cancellationToken.IsCancellationRequested)
        {
            State = AutosaveState.Pending;
            Message = "Есть несохранённые изменения";
            Changed?.Invoke();
            return false;
        }
        catch (Exception exception)
        {
            State = AutosaveState.Failed;
            Message = "Не сохранилось: " + exception.Message;
            Changed?.Invoke();
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.Changed -= OnSessionChanged;
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }

    private void OnSessionChanged()
    {
        // The session raises this for selection changes too, and for the write the autosave itself
        // just made. Only a dirty document is worth writing.
        if (_disposed || !_session.IsDirty)
        {
            return;
        }

        State = AutosaveState.Pending;
        Message = "Сохраняется…";
        Changed?.Invoke();
        Schedule();
    }

    private void Schedule()
    {
        CancelPending();
        var source = new CancellationTokenSource();
        _pending = source;
        _ = RunAsync(source.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
            {
                await _writes.WaitAsync(cancellationToken);
                try
                {
                    await WriteAsync(cancellationToken);
                }
                finally
                {
                    _writes.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
        // Another change arrived first: its own write will carry this one with it.
        }
    }

    private void CancelPending()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }
}
