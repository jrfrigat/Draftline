using System.Text;
using System.Text.Json;
using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class LargeImportTests
{
    [Fact]
    public async Task Formula_import_reports_bytes_and_screen_batches_and_survives_storage_roundtrip()
    {
        var json = File.ReadAllText(FormulaFile());
        var steps = new List<DocumentImportProgress>();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await DraftlineJson.ReadAsync(stream, stream.Length, step => { steps.Add(step); return Task.CompletedTask; });
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(260, result.Document!.Layers.Count);
        Assert.Equal(10299, result.Document.TotalElements);
        Assert.Contains(steps, step => step.Stage == "Чтение файла" && step.Percent is > 0 and < 100);
        Assert.Contains(steps, step => step.Stage == "Чтение файла" && step.Completed == stream.Length);
        Assert.Contains(steps, step => step.Stage == "Подготовка экранов" && step.Completed == 260);
        var stored = DraftlineJson.SerializeForStorage(result.Document);
        using var parsed = JsonDocument.Parse(stored);
        Assert.False(parsed.RootElement.TryGetProperty("allElements", out _));
        Assert.False(parsed.RootElement.TryGetProperty("screenElementLists", out _));
        Assert.False(parsed.RootElement.TryGetProperty("storedElements", out _));
        Assert.False(parsed.RootElement.TryGetProperty("startLayer", out _));
        Assert.True(Encoding.UTF8.GetByteCount(stored) < 9_000_000);
        Assert.Equal(stored, DraftlineJson.SerializeForStorage(DraftlineJson.Read(stored).Document!));
    }

    [Fact]
    public void Legacy_computed_projections_are_ignored_and_real_content_is_preserved()
    {
        var result = DraftlineJson.Read("{\"schemaVersion\":9,\"allElements\":42,\"storedElements\":[null],\"startLayer\":{},\"elements\":[{\"text\":\"Кнопка\",\"kind\":3}]}");
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("Кнопка", result.Document!.StartLayer.Elements.Single().Text);
        Assert.DoesNotContain("allElements", DraftlineJson.Serialize(result.Document));
    }

    [Fact]
    public async Task Owned_import_does_not_clone_and_keeps_existing_project_on_collision()
    {
        var storage = new InMemoryBrowserStorage();
        var catalog = new ProjectCatalog(storage);
        var original = DraftDocument.CreateNew("Оригинал");
        await catalog.SaveAsync(original);
        var incoming = original.Clone();
        var owned = await catalog.PrepareOwnedImportAsync(incoming);
        Assert.Same(incoming, owned);
        Assert.NotEqual(original.Id, owned.Id);
        await catalog.CommitImportAsync(owned);
        Assert.Equal(owned.Id, await catalog.LastOpenIdAsync());
        Assert.Equal(2, (await catalog.ListAsync()).Count);
        Assert.Equal("Оригинал", (await catalog.LoadAsync(original.Id)).Document!.Name);
        Assert.Equal("Оригинал (копия)", (await catalog.LoadAsync(owned.Id)).Document!.Name);
    }

    [Fact]
    public async Task Failed_import_rolls_back_document_index_and_last_open_selection()
    {
        var storage = new FailLastWrite();
        var catalog = new ProjectCatalog(storage);
        var original = DraftDocument.CreateNew("Текущий");
        await catalog.SaveAsync(original);
        await catalog.RememberLastAsync(original.Id);
        storage.Fail = true;
        var incoming = DraftDocument.CreateNew("Импорт");
        await Assert.ThrowsAsync<BrowserStorageException>(() => catalog.CommitImportAsync(incoming));
        Assert.Null(await storage.GetAsync(ProjectCatalog.DocumentKeyPrefix + incoming.Id));
        Assert.Equal(original.Id, await catalog.LastOpenIdAsync());
        Assert.Equal(original.Id, (await catalog.ListAsync()).Single().Id);
    }

    [Fact]
    public async Task Cancelled_read_propagates_cancellation_instead_of_opening_partial_data()
    {
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"schemaVersion\":9}"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DraftlineJson.ReadAsync(stream, stream.Length, _ => Task.CompletedTask, cancelled.Token));
    }

    private static string FormulaFile()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var file = Path.Combine(folder.FullName, "artifacts/order-formula-template/order-formula-template.wireframe.json");
            if (File.Exists(file)) return file;
        }
        throw new FileNotFoundException("Formula import regression fixture is missing.");
    }

    private sealed class FailLastWrite : IBrowserStorage
    {
        private readonly InMemoryBrowserStorage _inner = new();
        public bool Fail;
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => _inner.GetAsync(key, cancellationToken);
        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            if (Fail && key == ProjectCatalog.LastOpenKey) throw new BrowserStorageException("quota");
            return _inner.SetAsync(key, value, cancellationToken);
        }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => _inner.RemoveAsync(key, cancellationToken);
        public Task<IReadOnlyList<string>> KeysAsync(CancellationToken cancellationToken = default) => _inner.KeysAsync(cancellationToken);
    }
}
