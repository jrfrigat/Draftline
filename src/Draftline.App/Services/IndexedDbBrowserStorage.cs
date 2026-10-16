using Microsoft.JSInterop;

namespace Draftline.App.Services;
/// <summary>
/// The app's storage as the browser keeps it: IndexedDB, reached through the small JavaScript
/// module in wwwroot/js/draftline-storage.js.
///
/// The module answers rather than throws where it can - a browser that forbids storage or a write
/// that does not fit becomes a message this class turns into <see cref = "BrowserStorageException"/>,
/// which is the one thing the layer above has to handle. When IndexedDB is not there at all, the
/// caller can fall back to <see cref = "InMemoryBrowserStorage"/> and the editor still opens.
/// </summary>
public sealed class IndexedDbBrowserStorage : IBrowserStorage, IAsyncDisposable
{
    private const string ModulePath = "./js/draftline-storage.js";
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    /// <summary>Storage over the given JavaScript runtime.</summary>
    public IndexedDbBrowserStorage(IJSRuntime js) => _js = js;
    /// <summary>
    /// True when the browser has no usable IndexedDB and the app is running on memory instead -
    /// what the shell tells the person once, so nobody wonders why a reload lost the work.
    /// </summary>
    public bool IsVolatile { get; private set; }

    /// <summary>Asks the browser whether it has somewhere to write, and falls back when it has not.</summary>
    public async Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        var available = await module.InvokeAsync<bool>("isAvailable", cancellationToken).ConfigureAwait(false);
        IsVolatile = !available;
        return available;
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        return await module.InvokeAsync<string?>("get", cancellationToken, key).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        var error = await module.InvokeAsync<string?>("set", cancellationToken, key, value).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(error))
        {
            throw new BrowserStorageException(error);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync("remove", cancellationToken, key).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> KeysAsync(CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        var keys = await module.InvokeAsync<string[]>("keys", cancellationToken).ConfigureAwait(false);
        return keys;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync().ConfigureAwait(false);
            _module = null;
        }
    }

    private async Task<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken) => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath).ConfigureAwait(false);
}
