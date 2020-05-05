using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SOR4Explorer
{
    class Settings
    {
        public static Settings Instance => instance ?? new Settings();
        private static Settings instance;

        public static string InstallationPath
        {
            get => Instance.variables.InstallationPath;
            set => Instance.variables.InstallationPath = value;
        }
        public static string LocalDataPath => Instance.localDataPath;

        public static void SetFileSize(string name, long size)
        {
            Instance.variables.SetFileSize(name, size);
        }

        public static long GetFileSize(string name)
        {
            return Instance.variables.GetFileSize(name);
        }

        public static bool FileExists(string name)
        {
            return File.Exists(FileName(name));
        }

        public static string FileName(string name)
        {
            return Path.Combine(Instance.localDataPath, name);
        }

        #region Implementation
        
        [Serializable]
        class Variables : INotifyPropertyChanged
        {
            private string installationPath;
            public string InstallationPath
            {
                get => installationPath;
                set
                {
                    if (installationPath != value)
                    {
                        installationPath = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InstallationPath"));
                    }
                }
            }

            public Dictionary<string, long> FileSizes { get; set; } = new Dictionary<string, long>();

            public long GetFileSize(string name) => FileSizes.TryGetValue(name, out long size) ? size : 0;
            public void SetFileSize(string name, long size)
            {
                FileSizes[name] = size;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileSize"));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private readonly string localDataPath;
        private Variables variables;

        private Settings()
        {
            instance = this;
            localDataPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "SOR4 Explorer");
            Directory.CreateDirectory(localDataPath);

            var settingsFile = Path.Combine(localDataPath, SettingsFileName);
            if (File.Exists(settingsFile))
            {
                var text = File.ReadAllText(settingsFile);
                variables = JsonSerializer.Deserialize<Variables>(text);
            }
            else
            {
                variables = new Variables();
            }
            variables.PropertyChanged += (sender, ev) =>
            {
                var text = JsonSerializer.Serialize(variables, new JsonSerializerOptions() { 
                    WriteIndented = true 
                });
                File.WriteAllText(settingsFile, text);
            };
        }

        private const string SettingsFileName = "settings.json";

        #endregion
    }
}
