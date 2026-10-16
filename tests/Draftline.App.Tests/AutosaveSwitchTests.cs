using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class AutosaveSwitchTests
{
    [Fact]
    public async Task Flush_writes_the_previous_project_before_a_switch()
    {
        var session = new DocumentSession();
        var catalog = new ProjectCatalog(new InMemoryBrowserStorage());
        using var save = new AutosaveService(session, catalog, TimeSpan.FromHours(1));
        var id = session.Document.Id;
        session.Rename("Последняя правка");
        Assert.True(await save.FlushAsync(TestContext.Current.CancellationToken));
        session.Open(DraftDocument.CreateNew("Следующий"));
        Assert.Equal("Последняя правка", (await catalog.LoadAsync(id, TestContext.Current.CancellationToken)).Document!.Name);
        Assert.False(session.IsDirty);
    }

    [Fact]
    public async Task A_write_in_flight_does_not_mark_newer_edits_saved()
    {
        var storage = new DelayedStorage();
        var session = new DocumentSession();
        using var save = new AutosaveService(session, new ProjectCatalog(storage), TimeSpan.FromHours(1));
        session.Rename("До записи");
        var pending = save.FlushAsync(TestContext.Current.CancellationToken);
        await storage.Started.Task;
        session.Rename("Во время записи");
        storage.Release.SetResult();
        Assert.True(await pending);
        Assert.True(session.IsDirty);
        Assert.Equal(AutosaveState.Pending, save.State);
        Assert.Equal("До записи", DraftlineJson.Read((await storage.Inner.GetAsync(ProjectCatalog.DocumentKeyPrefix + session.Document.Id, TestContext.Current.CancellationToken))!).Document!.Name);
    }

    private sealed class DelayedStorage : IBrowserStorage
    {
        public InMemoryBrowserStorage Inner { get; } = new();
        public TaskCompletionSource Started { get; } = new();
        public TaskCompletionSource Release { get; } = new();
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Inner.GetAsync(key, cancellationToken);
        public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            if (key.StartsWith(ProjectCatalog.DocumentKeyPrefix)) { Started.TrySetResult(); await Release.Task; }
            await Inner.SetAsync(key, value, cancellationToken);
        }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Inner.RemoveAsync(key, cancellationToken);
        public Task<IReadOnlyList<string>> KeysAsync(CancellationToken cancellationToken = default) => Inner.KeysAsync(cancellationToken);
    }
}
