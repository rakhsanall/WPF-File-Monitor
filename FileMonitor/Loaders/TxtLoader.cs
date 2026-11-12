using FileMonitor.Interfaces;
using FileMonitor.Models;
using System.Globalization;
using System.IO;


namespace FileMonitor.Loaders
{
    public class TxtLoader : IFileLoader
    {
        public string Format => "TXT";

        public bool CanLoad(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            return Path.GetExtension(filePath)
                .Equals(".txt", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<FileData>> LoadAsync(string filePath)
        {
            var result = new List<FileData>();

            using StreamReader reader = new StreamReader(filePath);

            await reader.ReadLineAsync();

            string? line;

            int lineNumber = 1;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] columns = line.Split(';');

                if (columns.Length != 6)
                {
                    throw new FormatException(
                        $"Invalid TXT format at line {lineNumber}");
                }

                var data = new FileData
                {
                    Date = DateOnly.Parse(columns[0], CultureInfo.InvariantCulture),
                    Open = decimal.Parse(columns[1], CultureInfo.InvariantCulture),
                    High = decimal.Parse(columns[2], CultureInfo.InvariantCulture),
                    Low = decimal.Parse(columns[3], CultureInfo.InvariantCulture),
                    Close = decimal.Parse(columns[4], CultureInfo.InvariantCulture),
                    Volume = long.Parse(columns[5], CultureInfo.InvariantCulture)
                };

                result.Add(data);
            }

            return result;
        }
    }
}
