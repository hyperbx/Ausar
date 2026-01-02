using HandyControl.Tools;
using System.Text;
using System.Windows;

namespace Ausar
{
    public class Languages : List<Language>
    {
        public static int Count { get; set; }
    }

    public class Language
    {
        public string FileName { get; set; }

        public string Name { get; set; }

        public int Lines { get; set; }

        public override string ToString()
        {
            return Lines != Languages.Count
                ? $"{Name} ({(float)Lines / Languages.Count * 100:N0}%)"
                : Name;
        }

        public static void Load(string in_cultureCode)
        {
            var langDict = new ResourceDictionary()
            {
                Source = new($"Resources/Languages/{in_cultureCode}.xaml", UriKind.Relative)
            };

            while (Application.Current.Resources.MergedDictionaries.Count > 5)
                Application.Current.Resources.MergedDictionaries.RemoveAt(5);

            try
            {
                // Set HandyControl language.
                ConfigHelper.Instance.SetLang(in_cultureCode);
            }
            catch
            {
                // Fall back on English (United Kingdom) if the selected culture is invalid.
                ConfigHelper.Instance.SetLang("en-GB");
            }

            if (in_cultureCode == "en-GB")
                return;

            Application.Current.Resources.MergedDictionaries.Add(langDict);
        }

        public static void LoadResources()
        {
            object? resource = Application.Current.TryFindResource("Languages");

            if (resource is Languages langs)
                App.SupportedLanguages = langs;

            App.CurrentLanguage = GetClosestLanguage(App.Settings.Language);

            if (App.CurrentLanguage != null)
                Load(App.CurrentLanguage.FileName);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static void UpdateResources()
        {
            Load(App.Settings.Language = App.CurrentLanguage.FileName);
        }

        public static Language? GetClosestLanguage(string in_cultureCode)
        {
            var entry = App.SupportedLanguages?.FirstOrDefault(t => t.FileName == in_cultureCode);

            if (entry != null)
                return entry;

            var language = in_cultureCode.Split('-')[0];
            entry = App.SupportedLanguages?.FirstOrDefault(t => t.FileName.Split('-')[0] == language);

            return entry;
        }
    }
}