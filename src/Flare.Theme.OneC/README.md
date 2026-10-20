# Flare.Theme.OneC

Unofficial theme inspired by the 1C Taxi interface: yellow accents, neutral surfaces, compact controls and small corner radii. This is an independent Flare package, not an official 1C product or a pixel-perfect reproduction.

```csharp
using Flare.Extensions;
using Flare.Theme.OneC;

builder.Services.AddFlare(options =>
{
    options.DefaultTheme = new OneCTheme();
    options.DefaultPalette = OneCPalettes.Yellow;
});
// Or register it as an additional theme:
builder.Services.AddFlareTheme(new OneCTheme());
```

Theme ID: `onec`. Palette ID: `onec-yellow`. Light and dark modes are included. Use Flare's `InputVariant.Outlined` for outlined inputs.

The package targets .NET 8 and uses the public Flare 0.48 API. It intentionally inherits the complete Fluent UI 2 token baseline and component assets, overriding its typography, geometry and colors. The NuGet dependency brings these assets with it. It has no dependency on Draftline, application services, JavaScript or remote fonts. The source directory can be moved into the Flare repository independently; keep the package references or replace them with Flare project references there.

Build the package with `dotnet pack src/Flare.Theme.OneC -c Release`. The package is locally generated; publishing is a separate step.
