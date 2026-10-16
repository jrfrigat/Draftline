using Draftline.App.Services;
using Microsoft.JSInterop;

namespace Draftline.App.Tests;

public sealed class AutosaveFailureTests
{
    [Fact]
    public async Task A_failed_JS_write_is_reported_and_keeps_the_document_dirty()
    {
        var session = new DocumentSession();
        using var save = new AutosaveService(session, new ProjectCatalog(new FailingStorage()), TimeSpan.FromHours(1));
        session.Rename("Несохранённый");
        Assert.False(await save.FlushAsync(TestContext.Current.CancellationToken));
        Assert.Equal(AutosaveState.Failed, save.State);
        Assert.True(session.IsDirty);
        Assert.Contains("Не сохранилось", save.Message);
    }

    private sealed class FailingStorage : IBrowserStorage
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default) => throw new JSException("Запись запрещена");
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> KeysAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
    }
}
