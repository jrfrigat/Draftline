using System.Text;
using System.Text.Json;
using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class LargeImportTests
{
    [Fact]
    public async Task Large_import_reports_bytes_and_screen_batches_and_survives_storage_roundtrip()
    {
        var token = TestContext.Current.CancellationToken;
        var json = DraftlineJson.Serialize(LargeDocument());
        var steps = new List<DocumentImportProgress>();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await DraftlineJson.ReadAsync(stream, stream.Length, step => { steps.Add(step); return Task.CompletedTask; }, token);
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(260, result.Document!.Layers.Count);
        Assert.Equal(260 * 40, result.Document.TotalElements);
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
        var token = TestContext.Current.CancellationToken;
        var storage = new InMemoryBrowserStorage();
        var catalog = new ProjectCatalog(storage);
        var original = DraftDocument.CreateNew("Оригинал");
        await catalog.SaveAsync(original, token);
        var incoming = original.Clone();
        var owned = await catalog.PrepareOwnedImportAsync(incoming);
        Assert.Same(incoming, owned);
        Assert.NotEqual(original.Id, owned.Id);
        await catalog.CommitImportAsync(owned, token);
        Assert.Equal(owned.Id, await catalog.LastOpenIdAsync(token));
        Assert.Equal(2, (await catalog.ListAsync(token)).Count);
        Assert.Equal("Оригинал", (await catalog.LoadAsync(original.Id, token)).Document!.Name);
        Assert.Equal("Оригинал (копия)", (await catalog.LoadAsync(owned.Id, token)).Document!.Name);
    }

    [Fact]
    public async Task Failed_import_rolls_back_document_index_and_last_open_selection()
    {
        var token = TestContext.Current.CancellationToken;
        var storage = new FailLastWrite();
        var catalog = new ProjectCatalog(storage);
        var original = DraftDocument.CreateNew("Текущий");
        await catalog.SaveAsync(original, token);
        await catalog.RememberLastAsync(original.Id, token);
        storage.Fail = true;
        var incoming = DraftDocument.CreateNew("Импорт");
        await Assert.ThrowsAsync<BrowserStorageException>(() => catalog.CommitImportAsync(incoming, token));
        Assert.Null(await storage.GetAsync(ProjectCatalog.DocumentKeyPrefix + incoming.Id, token));
        Assert.Equal(original.Id, await catalog.LastOpenIdAsync(token));
        Assert.Equal(original.Id, (await catalog.ListAsync(token)).Single().Id);
    }

    [Fact]
    public async Task Cancelled_read_propagates_cancellation_instead_of_opening_partial_data()
    {
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"schemaVersion\":9}"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DraftlineJson.ReadAsync(stream, stream.Length, _ => Task.CompletedTask, cancelled.Token));
    }

    // The same shape as the order-formula mockup that ran out of memory: 260 screens of formula blocks
    // whose buttons open the next screen. Built here, so the regression needs no file outside the repo.
    private static DraftDocument LargeDocument()
    {
        var document = DraftDocument.CreateNew("Большой макет");
        var layers = new List<DraftLayer> { document.Layers[0] };
        for (var index = 1; index < 260; index++) layers.Add(document.AddLayer("Экран " + (index + 1)));
        for (var index = 0; index < layers.Count; index++)
        {
            for (var block = 0; block < 40; block++)
            {
                var element = ElementDefaults.Create(block % 4 == 0 ? WireframeKind.Button : WireframeKind.Text);
                element.X = block % 5 * 160;
                element.Y = block / 5 * 48;
                element.Text = $"Параметр {index}.{block} · ОКРУГЛВВЕРХ(СУММА(1 301 * 35 %) / 2)";
                if (element.Kind == WireframeKind.Button)
                {
                    element.Transition = TransitionKind.Navigate;
                    element.TargetLayerId = layers[(index + 1) % layers.Count].Id;
                }
                layers[index].Elements.Add(element);
            }
        }
        return document;
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
