using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOR4Explorer
{
    static class Settings
    {
        public static string InstallationPath
        {
            get => GetString(InstallationPathKey);
            set => SetString(InstallationPathKey, value);
        }

        #region Implementation

        private const string RegistryKey = @"SOFTWARE\SOR4 Explorer";
        private const string InstallationPathKey = "installationPath";

        private static string GetString(string subkey)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var value = key.GetValue(subkey) as string;
                key.Close();
                return value ?? "";
            }
            return "";
        }

        private static void SetString(string subkey, string value)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            if (value == null)
                key.DeleteValue(subkey);
            else
                key.SetValue(subkey, value);
            key.Close();
        }

        #endregion
    }
}
