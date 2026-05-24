using Microsoft.JSInterop;

namespace LoanSuperMarket.Blazor.Services;

/// <summary>
/// Manages dark/light theme state and persists preference to localStorage.
/// </summary>
public sealed class ThemeService
{
    private readonly IJSRuntime _js;
    private bool _isDark;

    public event Action? OnThemeChanged;

    public bool IsDark => _isDark;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", "theme");
            _isDark = stored == "dark";
            await ApplyThemeAsync();
        }
        catch { }
    }

    public async Task ToggleAsync()
    {
        _isDark = !_isDark;
        await _js.InvokeVoidAsync("localStorage.setItem", "theme", _isDark ? "dark" : "light");
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    private async Task ApplyThemeAsync()
    {
        if (_isDark)
        {
            await _js.InvokeVoidAsync("document.documentElement.classList.add", "dark");
        }
        else
        {
            await _js.InvokeVoidAsync("document.documentElement.classList.remove", "dark");
        }
    }
}
