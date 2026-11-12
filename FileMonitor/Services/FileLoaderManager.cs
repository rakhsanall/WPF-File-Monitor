
using FileMonitor.Interfaces;
using FileMonitor.Loaders;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace FileMonitor.Services
{
    public class FileLoaderManager
    {
        private readonly List<IFileLoader> _loaders = new();

        private class LoaderSettings
        {
            public List<string> EnabledLoaders { get; set; } = new();
        }

        public FileLoaderManager()
        {
            LoadEnabledLoaders();
        }

        private void LoadEnabledLoaders()
        {
            string settingsPath = Path.Combine(
                AppContext.BaseDirectory,
                "loaderSettings.json");

            if (!File.Exists(settingsPath))
            {
                System.Diagnostics.Debug.WriteLine(
                    "loaderSettings.json not found.");
                return;
            }

            string json = File.ReadAllText(settingsPath);

            LoaderSettings? settings =
                JsonSerializer.Deserialize<LoaderSettings>(json);

            if (settings == null)
                return;

            var availableLoaders = new List<IFileLoader>
            {
                new CsvLoader(),
                new XmlLoader(),
                new TxtLoader()
            };

            LoadPlugins(availableLoaders);

            foreach (IFileLoader loader in availableLoaders)
            {
                if (settings.EnabledLoaders.Any(
                    name => name.Equals(
                        loader.Format,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    _loaders.Add(loader);

                    System.Diagnostics.Debug.WriteLine(
                        $"Enabled loader: {loader.Format}");
                }
            }
        }

        private void LoadPlugins(List<IFileLoader> availableLoaders)
        {
            string pluginFolder = Path.Combine(
                AppContext.BaseDirectory,
                "Plugins");

            if (!Directory.Exists(pluginFolder))
                Directory.CreateDirectory(pluginFolder);

            foreach (string dllPath in Directory.GetFiles(
                pluginFolder, "*.dll"))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(dllPath);

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.IsClass &&
                            !type.IsAbstract &&
                            typeof(IFileLoader).IsAssignableFrom(type))
                        {
                            if (Activator.CreateInstance(type)
                                is IFileLoader loader)
                            {
                                availableLoaders.Add(loader);

                                System.Diagnostics.Debug.WriteLine(
                                    $"Plugin discovered: {loader.Format}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Plugin error: {dllPath} | {ex.Message}");
                }
            }
        }

        public IFileLoader? GetLoader(string filePath)
        {
            return _loaders.FirstOrDefault(
                loader => loader.CanLoad(filePath));
        }
    }
}
