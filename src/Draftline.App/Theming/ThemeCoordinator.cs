using Draftline.App.Model;
using Flare.Abstractions;
using Flare.Abstractions.Tokens;
using Microsoft.JSInterop;

namespace Draftline.App.Theming;
/// <summary>The editor uses Flare's root theme and persistence; drawing Auto mode follows the OS independently.</summary>
public sealed class ThemeCoordinator : IAsyncDisposable
{
    public IThemeService Editor { get; }
    public bool SystemDark { get; private set; }

    public event Action? Changed;
    private readonly IJSRuntime _js;
    private Task? _initialization;
    private IJSObjectReference? _module;
    private DotNetObjectReference<ThemeCoordinator>? _self;
    private readonly Dictionary<string, DrawingTheme?> _drawings = new();
    public ThemeCoordinator(IThemeService editor, IJSRuntime js)
    {
        Editor = editor;
        _js = js;
        Editor.OnThemeChanged += RootChanged;
    }

    private Task RootChanged()
    {
        _drawings.Clear();
        Changed?.Invoke();
        return Task.CompletedTask;
    }

    public Task InitializeAsync() => _initialization ??= InitializeCore();
    private async Task InitializeCore()
    {
        _module = await _js.InvokeAsync<IJSObjectReference>("import", "./js/draftline-theme.js");
        _self = DotNetObjectReference.Create(this);
        SystemDark = await _module.InvokeAsync<bool>("attach", _self);
        Changed?.Invoke();
    }

    [JSInvokable]
    public void SystemChanged(bool dark)
    {
        SystemDark = dark;
        Changed?.Invoke();
    }

    public ThemeMode ResolveMode(string mode) => mode == "dark" || mode == "auto" && SystemDark ? ThemeMode.Dark : ThemeMode.Light;
    public DrawingTheme? Drawing(MockupThemeSelection selection)
    {
        if (selection.ThemeId == "wireframe")
            return null;
        var key = selection.ThemeId + "/" + selection.PaletteId + "/" + ResolveMode(selection.Mode);
        if (!_drawings.TryGetValue(key, out var drawing))
            _drawings[key] = drawing = MockupThemeResolver.Resolve(Editor, selection, SystemDark);
        return drawing;
    }

    public static ThemeMode EditorMode(string? mode) => mode switch
    {
        "dark" => ThemeMode.Dark,
        "light" => ThemeMode.Light,
        _ => ThemeMode.Auto
    };
    public async ValueTask DisposeAsync()
    {
        Editor.OnThemeChanged -= RootChanged;
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("detach");
            await _module.DisposeAsync();
        }

        _self?.Dispose();
    }
}
