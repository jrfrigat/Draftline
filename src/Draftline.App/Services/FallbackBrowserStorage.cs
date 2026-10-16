namespace Draftline.App.Services;
/// <summary>
/// The storage the app actually uses: the browser's own, or the memory of the page when the browser
/// has none to give.
///
/// A private window, a storage area the settings forbid, a machine whose quota is already spent - none
/// of them should cost the person the editor. This class asks once which of the two answers and
/// remembers it, so the question is asked on the first read rather than on every write.
///
/// <see cref = "IsVolatile"/> is what the shell says out loud. An editor that loses an afternoon because
/// a tab never mentioned it could not save is worse than one that cannot save and says so.
/// </summary>
public sealed class FallbackBrowserStorage : IBrowserStorage
{
    private readonly IBrowserStorage _persistent;
    private readonly InMemoryBrowserStorage _volatileStorage = new();
    private bool? _hasPersistentStorage;
    /// <summary>The fallback over the storage that may or may not be there.</summary>
    public FallbackBrowserStorage(IBrowserStorage persistent) => _persistent = persistent;
    /// <summary>True once the browser has said it has nowhere to keep anything.</summary>
    public bool IsVolatile => _hasPersistentStorage == false;

    /// <summary>Asks the question now rather than at the first read - what the shell does on startup.</summary>
    public async Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
    {
        await CurrentAsync(cancellationToken).ConfigureAwait(false);
        return _hasPersistentStorage == true;
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var storage = await CurrentAsync(cancellationToken).ConfigureAwait(false);
        return await storage.GetAsync(key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var storage = await CurrentAsync(cancellationToken).ConfigureAwait(false);
        await storage.SetAsync(key, value, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetManyAsync(IReadOnlyDictionary<string, string> entries, CancellationToken cancellationToken = default)
    {
        var storage = await CurrentAsync(cancellationToken).ConfigureAwait(false);
        await storage.SetManyAsync(entries, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var storage = await CurrentAsync(cancellationToken).ConfigureAwait(false);
        await storage.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> KeysAsync(CancellationToken cancellationToken = default)
    {
        var storage = await CurrentAsync(cancellationToken).ConfigureAwait(false);
        return await storage.KeysAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Which storage answers from now on, asked once and remembered.</summary>
    private async Task<IBrowserStorage> CurrentAsync(CancellationToken cancellationToken)
    {
        if (_hasPersistentStorage is null)
        {
            _hasPersistentStorage = _persistent is IndexedDbBrowserStorage localStorage ? await localStorage.ProbeAsync(cancellationToken).ConfigureAwait(false) : true;
        }

        return _hasPersistentStorage.Value ? _persistent : _volatileStorage;
    }
}
