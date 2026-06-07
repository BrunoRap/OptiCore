using System;
using System.Collections.Generic;
using System.Windows;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class LocalizationService
    {
        private static readonly Dictionary<string, string> Languages = new()
        {
            ["en"] = "English",
            ["pt"] = "Português (BR)",
            ["es"] = "Español",
            ["fr"] = "Français",
            ["de"] = "Deutsch"
        };

        public static IEnumerable<KeyValuePair<string, string>> AvailableLanguages => Languages;

        public static void ApplyLanguage(string langCode)
        {
            try
            {
                var uri = new Uri($"src/Resources/Strings.{langCode}.xaml", UriKind.Relative);
                var dict = new ResourceDictionary { Source = uri };
                var existing = FindLanguageDict();
                if (existing != null)
                    Application.Current.Resources.MergedDictionaries.Remove(existing);
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch { }
        }

        private static ResourceDictionary? FindLanguageDict()
        {
            foreach (var d in Application.Current.Resources.MergedDictionaries)
                if (d.Source?.ToString().Contains("Strings.") == true)
                    return d;
            return null;
        }

        public static string Get(string key, string fallback = "")
        {
            try
            {
                return Application.Current.Resources[key]?.ToString() ?? fallback;
            }
            catch { return fallback; }
        }
    }
}
