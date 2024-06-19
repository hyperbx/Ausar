using System.Reflection;

namespace Ausar.Helpers
{
    internal class AssemblyHelper
    {
        public static string GetInformationalVersion()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
                .Split('+')[0];
        }

        public static string GetAssemblyName()
        {
            return Assembly.GetEntryAssembly().GetName().Name;
        }
    }
}
