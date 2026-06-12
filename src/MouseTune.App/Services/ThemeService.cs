using System.Windows;
using Microsoft.Win32;
using MouseTune.Models;

namespace MouseTune.Services;

public sealed class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    public void Apply(AppTheme theme)
    {
        var effectiveTheme = theme == AppTheme.System ? GetSystemTheme() : theme;
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"Themes/{effectiveTheme}.xaml", UriKind.Relative)
        };

        var resources = Application.Current.Resources.MergedDictionaries;
        var existing = resources
            .FirstOrDefault(item => item.Source is not null
                && item.Source.OriginalString.StartsWith("Themes/", StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            resources.Remove(existing);
        }

        resources.Add(dictionary);
    }

    private static AppTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            var value = key?.GetValue(AppsUseLightThemeValue);
            return value is int intValue && intValue == 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch (System.Security.SecurityException)
        {
            return AppTheme.Light;
        }
        catch (UnauthorizedAccessException)
        {
            return AppTheme.Light;
        }
    }
}
