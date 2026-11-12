using FileMonitor.Loaders;
using Xunit;

namespace FileMonitor.Tests
{
    public class LoaderTests
    {
     

        [Fact]
        public async Task CsvLoader_ShouldReadFileCorrectly()
        {
            string filePath = Path.GetTempFileName();

            await File.WriteAllTextAsync(filePath,
                "Date,Open,High,Low,Close,Volume\n" +
                "2026-10-01,100.5,105.2,99.8,103.4,15000");

            var loader = new CsvLoader();

            var result = await loader.LoadAsync(filePath);

            Assert.Single(result);
            Assert.Equal(100.5m, result[0].Open);
            Assert.Equal(103.4m, result[0].Close);
            Assert.Equal(15000, result[0].Volume);

            File.Delete(filePath);
        }

        [Fact]
        public async Task TxtLoader_ShouldReadFileCorrectly()
        {
            string filePath = Path.GetTempFileName();

            await File.WriteAllTextAsync(filePath,
                "Date;Open;High;Low;Close;Volume\n" +
                "2026-10-01;100.5;105.2;99.8;103.4;15000");

            var loader = new TxtLoader();

            var result = await loader.LoadAsync(filePath);

            Assert.Single(result);
            Assert.Equal(100.5m, result[0].Open);
            Assert.Equal(15000, result[0].Volume);

            File.Delete(filePath);
        }

        [Fact]
        public async Task XmlLoader_ShouldReadFileCorrectly()
        {
            string filePath = Path.GetTempFileName();

            await File.WriteAllTextAsync(filePath,
                """
                <values>
                    <value date="2026-10-01"
                           open="100.5"
                           high="105.2"
                           low="99.8"
                           close="103.4"
                           volume="15000" />
                </values>
                """);

            var loader = new XmlLoader();

            var result = await loader.LoadAsync(filePath);

            Assert.Single(result);
            Assert.Equal(100.5m, result[0].Open);
            Assert.Equal(103.4m, result[0].Close);

            File.Delete(filePath);
        }

        [Fact]
        public async Task JsonLoader_ShouldReadFileCorrectly()
        {
            string filePath = Path.GetTempFileName();

            await File.WriteAllTextAsync(filePath,
                """
        [
          {
            "date": "2026-10-01",
            "open": 100.5,
            "high": 105.2,
            "low": 99.8,
            "close": 103.4,
            "volume": 15000
          }
        ]
        """);

            var loader = new JsonPlugin.JsonLoader();

            var result = await loader.LoadAsync(filePath);

            Assert.Single(result);
            Assert.Equal(100.5m, result[0].Open);
            Assert.Equal(103.4m, result[0].Close);
            Assert.Equal(15000, result[0].Volume);

            File.Delete(filePath);
        }

    }
}
