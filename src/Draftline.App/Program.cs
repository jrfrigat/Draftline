using Draftline.App;
using Draftline.App.Services;
using Flare.Abstractions.Tokens;
using Flare.Extensions;
using Flare.Theme.FluentUI2;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// One design system, one palette, no theme switch: Draftline is a wireframing tool, so the chrome
// stays quiet on purpose and the canvas owns the attention. Fluent UI 2 is the tighter preset - more
// of the screen goes to the sheet - and the neutral blue palette keeps the one accent the low-fi look
// is allowed. The mode follows the machine until the visitor decides otherwise: the same defaults the
// bootstrap script in index.html paints the first frame with, because two of those disagreeing is a
// flash of the wrong colour.
builder.Services.AddFlare(options =>
{
    options.DefaultTheme = new FluentUI2Theme();
    options.DefaultPalette = Fluent2Palettes.Blue;
    options.DefaultMode = ThemeMode.Auto;
});

// The editor's state, one per browser session: the open wireframe, what is selected on it, and the
// history behind it. Everything that draws or changes a wireframe reads this same object.
builder.Services.AddScoped<DocumentSession>();

// What the palette is carrying while a block is dragged from it onto the sheet.
builder.Services.AddScoped<PaletteDrag>();

// Where the wireframes are kept. The browser's own storage when it has one, the page's memory when it
// has not - a private window should cost the person the writing, not the editor. Registered as both
// its own type and the interface, because the shell has to be able to ask which of the two it got.
builder.Services.AddSingleton(sp => new FallbackBrowserStorage(
    new LocalStorageBrowserStorage(sp.GetRequiredService<IJSRuntime>())));
builder.Services.AddSingleton<IBrowserStorage>(sp => sp.GetRequiredService<FallbackBrowserStorage>());
builder.Services.AddSingleton<ProjectCatalog>();

// Writing the open wireframe back after every change: one per session, because it listens to the
// session it belongs to.
builder.Services.AddScoped<AutosaveService>();

// Handing a wireframe to the person as a file, and taking one back.
builder.Services.AddScoped<DocumentFileService>();

await builder.Build().RunAsync();
