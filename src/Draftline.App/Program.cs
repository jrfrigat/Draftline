using Draftline.App;
using Flare.Abstractions.Tokens;
using Flare.Extensions;
using Flare.Theme.MaterialDesign3.Tokens;
using Flare.Theme.MaterialDesign3Expressive;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// One design system, one palette, no theme switch: Draftline is a wireframing tool, so the chrome
// stays quiet on purpose and the canvas owns the attention. The mode follows the machine until the
// visitor decides otherwise - the same default the bootstrap script in index.html paints the first
// frame with, because two of those disagreeing is a flash of the wrong colour.
builder.Services.AddFlare(options =>
{
    options.DefaultTheme = new MaterialDesign3ExpressiveTheme();
    options.DefaultPalette = Md3Palettes.Violet;
    options.DefaultMode = ThemeMode.Auto;
});

await builder.Build().RunAsync();
