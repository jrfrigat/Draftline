namespace Draftline.App.Services;
/// <summary>
/// The browser's own storage, seen by the app: a small key-value box that outlives a reload.
///
/// Everything above this interface - the project list, the autosave - works in strings and knows
/// nothing about JavaScript, localStorage or the quota behind them, which is what makes the storage
/// layer checkable without a browser and replaceable without touching the editor.
/// </summary>
public interface IBrowserStorage
{
    /// <summary>The value under a key, or null when there is none.</summary>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Writes a value. Throws <see cref = "BrowserStorageException"/> when the browser refuses.</summary>
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    /// <summary>Removes a key. Removing a key that is not there is not an error.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Every key the app's storage holds.</summary>
    Task<IReadOnlyList<string>> KeysAsync(CancellationToken cancellationToken = default);

    /// <summary>Commit related entries together. Nontransactional adapters restore prior values on failure.</summary>
    async Task SetManyAsync(IReadOnlyDictionary<string, string> entries, CancellationToken cancellationToken = default)
    {
        var before = new Dictionary<string, string?>();
        foreach (var key in entries.Keys) before[key] = await GetAsync(key, cancellationToken);
        var written = new List<string>();
        try
        {
            foreach (var entry in entries)
            {
                await SetAsync(entry.Key, entry.Value, cancellationToken);
                written.Add(entry.Key);
            }
        }
        catch
        {
            foreach (var key in written.AsEnumerable().Reverse())
                if (before[key] is { } value) await SetAsync(key, value, CancellationToken.None);
                else await RemoveAsync(key, CancellationToken.None);
            throw;
        }
    }
}

/// <summary>Storage refused a read or a write: the quota, a private window, or a browser that forbids it.</summary>
public sealed class BrowserStorageException : Exception
{
    /// <summary>A refusal with the sentence the person sees.</summary>
    public BrowserStorageException(string message) : base(message)
    {
    }

    /// <summary>A refusal that wraps what actually happened.</summary>
    public BrowserStorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Storage that lasts as long as the page does.
///
/// It is the fallback for a browser that has no persistent storage - a private window or a forbidden
/// storage area - so the editor keeps working with everything but the writing, and says so, instead
/// of refusing to open. It is also what the checks run against, because the catalog and the autosave
/// are the parts worth checking and neither of them should need a browser to be exercised.
/// </summary>
public sealed class InMemoryBrowserStorage : IBrowserStorage
{
    private readonly Dictionary<string, string> _entries = new(StringComparer.Ordinal);
    /// <summary>True when this storage forgets everything with the page - the reason it exists.</summary>
    public bool IsVolatile => true;

    /// <inheritdoc/>
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(_entries.TryGetValue(key, out var value) ? value : null);
    /// <inheritdoc/>
    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _entries[key] = value;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _entries.Remove(key);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> KeysAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(_entries.Keys.ToList());
}
