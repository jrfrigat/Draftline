using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class ImportTests
{
    [Fact]
    public async Task A_collision_imports_as_a_copy_and_preserves_the_existing_project()
    {
        var catalog = new ProjectCatalog(new InMemoryBrowserStorage());
        var current = DraftDocument.CreateNew("Новая работа");
        await catalog.SaveAsync(current, TestContext.Current.CancellationToken);
        var older = current.Clone(); older.Name = "Старая работа";
        var imported = await catalog.PrepareImportAsync(older);
        await catalog.SaveAsync(imported, TestContext.Current.CancellationToken);
        Assert.NotEqual(current.Id, imported.Id);
        Assert.EndsWith(" (копия)", imported.Name);
        Assert.Equal("Новая работа", (await catalog.LoadAsync(current.Id, TestContext.Current.CancellationToken)).Document!.Name);
        Assert.Equal(current.Id, older.Id);
        Assert.Equal(2, (await catalog.ListAsync(TestContext.Current.CancellationToken)).Count);
    }

    [Fact]
    public async Task A_new_import_keeps_its_identity()
    {
        var catalog = new ProjectCatalog(new InMemoryBrowserStorage());
        var source = DraftDocument.CreateNew("Новый");
        Assert.Equal(source.Id, (await catalog.PrepareImportAsync(source)).Id);
    }
}
