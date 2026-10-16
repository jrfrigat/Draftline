using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class CompactStorageTests
{
    [Fact]
    public async Task Storage_is_compact_and_exports_stay_readable()
    {
        var storage = new InMemoryBrowserStorage(); var catalog = new ProjectCatalog(storage);
        var document = DraftDocument.CreateNew("Переносимый макет");
        await catalog.SaveAsync(document, TestContext.Current.CancellationToken);
        var stored = (await storage.GetAsync(ProjectCatalog.DocumentKeyPrefix + document.Id, TestContext.Current.CancellationToken))!;
        Assert.DoesNotContain("\n", stored);
        Assert.Contains("\n", DraftlineJson.Serialize(document));
        Assert.True(DraftlineJson.Read(stored).Succeeded);
    }
}
