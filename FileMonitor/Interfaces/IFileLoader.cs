using FileMonitor.Models;

namespace FileMonitor.Interfaces
{

  
        public interface IFileLoader
        {
            string Format { get; }

            bool CanLoad(string filePath);

            Task<List<FileData>> LoadAsync(string filePath);
        }
    }


