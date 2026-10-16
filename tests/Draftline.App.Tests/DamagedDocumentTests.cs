using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class DamagedDocumentTests
{
    [Theory]
    [InlineData("{\"elements\":[null]}")]
    [InlineData("{\"schemaVersion\":9,\"layers\":[null]}")]
    [InlineData("{\"elements\":[{\"options\":[null],\"groupPath\":null,\"tabContents\":null,\"rows\":[null,[null]],\"choiceItems\":[null,{\"children\":[null]}]}]}")]
    [InlineData("{\"schemaVersion\":9,\"layers\":[{\"alternates\":[null,{\"elements\":[null]}]}],\"components\":[null,{\"elements\":[null]}]}")]
    public void Null_entries_are_repaired_and_the_result_can_be_cloned_and_saved(string json)
    {
        var result = DraftlineJson.Read(json);
        Assert.True(result.Succeeded, result.Error);
        Assert.True(DraftlineJson.Read(DraftlineJson.Serialize(result.Document!.Clone())).Succeeded);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"elements\":42}")]
    [InlineData("{\"schemaVersion\":999}")]
    public void Invalid_files_return_a_failure(string json) => Assert.False(DraftlineJson.Read(json).Succeeded);

    [Fact]
    public void Huge_sheets_are_limited()
    {
        var document = DraftlineJson.Read("{\"canvasWidth\":1e308,\"canvasHeight\":1e308}").Document!;
        Assert.Equal(DraftLayer.MaximumCanvasSize, document.StartLayer.CanvasWidth);
        Assert.Equal(DraftLayer.MaximumCanvasSize, document.StartLayer.CanvasHeight);
    }
}
