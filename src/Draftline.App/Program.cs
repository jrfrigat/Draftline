using Draftline.App;
using Draftline.App.Services;
using Draftline.App.Theming;
using Flare.Abstractions.Tokens;
using Flare.Components.IDE;
using Flare.Extensions;
using Flare.Theme.FluentUI2;
using Flare.Theme.MaterialDesign3Expressive;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
// The root theme is local editor chrome; document appearance is isolated by MockupThemeScope.
// Keep these initial defaults in sync with the bootstrap in index.html. Flare persists subsequent
// editor choices itself; the drawing's theme travels with the document instead.
builder.Services.AddFlare(options =>
{
    options.DefaultTheme = DraftlineTheme.Build();
    options.DefaultPalette = Fluent2Palettes.Blue;
    options.DefaultMode = ThemeMode.Auto;
});
// The IDE shell's companion service: the layout drags its region borders through it. Registered beside
// the design system because the shell is part of it, not of the editor.
builder.Services.AddFlareIde();
builder.Services.AddFlareTheme(new FluentUI2Theme());
builder.Services.AddFlareTheme(new MaterialDesign3ExpressiveTheme());
builder.Services.AddScoped<ThemeCoordinator>();
// The editor's state, one per browser session: the open wireframe, what is selected on it, and the
// history behind it. Everything that draws or changes a wireframe reads this same object.
builder.Services.AddScoped<DocumentSession>();
// What the palette is carrying while a block is dragged from it onto the sheet.
builder.Services.AddScoped<PaletteDrag>();
// Where the wireframes are kept. The browser's own storage when it has one, the page's memory when it
// has not - a private window should cost the person the writing, not the editor. Registered as both
// its own type and the interface, because the shell has to be able to ask which of the two it got.
builder.Services.AddSingleton(sp => new FallbackBrowserStorage(new IndexedDbBrowserStorage(sp.GetRequiredService<IJSRuntime>())));
builder.Services.AddSingleton<IBrowserStorage>(sp => sp.GetRequiredService<FallbackBrowserStorage>());
builder.Services.AddSingleton<ProjectCatalog>();
// Writing the open wireframe back after every change: one per session, because it listens to the
// session it belongs to.
builder.Services.AddScoped<AutosaveService>();
// Handing a wireframe to the person as a file, and taking one back.
builder.Services.AddScoped<DocumentFileService>();
await builder.Build().RunAsync();
