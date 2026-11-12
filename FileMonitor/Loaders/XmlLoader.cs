using FileMonitor.Interfaces;
using FileMonitor.Models;

using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace FileMonitor.Loaders
{
    public class XmlLoader : IFileLoader
    {
        public string Format => "XML";

        public bool CanLoad(string filePath)
        {
            return Path.GetExtension(filePath)
                .Equals(".xml", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<FileData>> LoadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.");

            var result = new List<FileData>();

            using XmlReader reader = XmlReader.Create(
                filePath,
                new XmlReaderSettings
                {
                    Async = true
                });

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == System.Xml.XmlNodeType.Element
                    && reader.Name == "value")
                {
                    var data = new FileData
                    {
                        Date = DateOnly.Parse(
                            reader.GetAttribute("date")!,
                            CultureInfo.InvariantCulture),

                        Open = decimal.Parse(
                            reader.GetAttribute("open")!,
                            CultureInfo.InvariantCulture),

                        High = decimal.Parse(
                            reader.GetAttribute("high")!,
                            CultureInfo.InvariantCulture),

                        Low = decimal.Parse(
                            reader.GetAttribute("low")!,
                            CultureInfo.InvariantCulture),

                        Close = decimal.Parse(
                            reader.GetAttribute("close")!,
                            CultureInfo.InvariantCulture),

                        Volume = long.Parse(
                            reader.GetAttribute("volume")!,
                            CultureInfo.InvariantCulture)
                    };

                    result.Add(data);
                }
            }

            return result;
        }
    }
}
