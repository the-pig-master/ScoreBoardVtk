using System.Windows;
using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Wpf.Localization;

public sealed class LocalizationManager
{
    private const string EnglishDictionaryPath = "Resources/Strings.en.xaml";
    private const string RussianDictionaryPath = "Resources/Strings.ru.xaml";

    private LocalizationManager()
    {
    }

    public static LocalizationManager Instance { get; } = new();

    public event EventHandler? LanguageChanged;

    public UiLanguage CurrentLanguage { get; private set; } = UiLanguage.English;

    public void ApplyLanguage(UiLanguage language)
    {
        CurrentLanguage = language;

        if (Application.Current is null)
        {
            LanguageChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var localizationDictionary = new ResourceDictionary
        {
            Source = new Uri(GetDictionaryPath(language), UriKind.Relative),
        };

        for (var index = dictionaries.Count - 1; index >= 0; index--)
        {
            if (IsLocalizationDictionary(dictionaries[index]))
            {
                dictionaries.RemoveAt(index);
            }
        }

        dictionaries.Add(localizationDictionary);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key)
    {
        if (Application.Current?.TryFindResource(key) is string value)
        {
            return value;
        }

        return key;
    }

    private static string GetDictionaryPath(UiLanguage language)
    {
        return language == UiLanguage.Russian
            ? RussianDictionaryPath
            : EnglishDictionaryPath;
    }

    private static bool IsLocalizationDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;

        return string.Equals(source, EnglishDictionaryPath, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(source, RussianDictionaryPath, StringComparison.OrdinalIgnoreCase);
    }
}
