using FileMonitor.Interfaces;
using FileMonitor.Models;
using System.Text.Json;

namespace JsonPlugin
{
    public class JsonLoader : IFileLoader
    {
        public string Format => "JSON";

        public bool CanLoad(string filePath)
        {
            return Path.GetExtension(filePath)
                .Equals(".json", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<FileData>> LoadAsync(string filePath)
        {
            await using FileStream stream = File.OpenRead(filePath);

            List<FileData>? data =
                await JsonSerializer.DeserializeAsync<List<FileData>>(
                    stream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return data ?? new List<FileData>();
        }
    }
}