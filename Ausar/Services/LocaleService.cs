using System.Windows;

namespace Ausar.Services
{
    public class LocaleService
    {
        public static string Localise(string in_key)
        {
            var resource = Application.Current.TryFindResource(in_key);

            if (resource is string out_str)
                return out_str.Replace("\\n", "\n");

            return in_key;
        }

        public static string Localise(string in_key, params object[] in_args)
        {
            return string.Format(Localise(in_key), in_args);
        }
    }
}
