
using FileMonitor.Interfaces;
using FileMonitor.Models;
using FileMonitor.Services;
using System.Collections.Concurrent;
using System.IO;
using System.Windows;

namespace FileMonitor
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _monitoringCancellation;
        private bool _isMonitoring = false;

        private readonly FileLoaderManager _loaderManager;

        private readonly ConcurrentDictionary<string, byte> _processedFiles =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, byte> _processingFiles =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly SemaphoreSlim _semaphore = new(3);

        public MainWindow()
        {
            InitializeComponent();

            _loaderManager = new FileLoaderManager();
        }

        private async Task<bool> IsFileReadyAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            const int maxAttempts = 5;
            const int delayMilliseconds = 1000;

            long previousSize = -1;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileInfo = new FileInfo(filePath);
                    long currentSize = fileInfo.Length;

                 
                    if (currentSize != previousSize)
                    {
                        previousSize = currentSize;
                        await Task.Delay(delayMilliseconds, cancellationToken);
                        continue;
                    }

                    using FileStream stream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.None);

                    return true;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }

                await Task.Delay(delayMilliseconds, cancellationToken);
            }

            return false;
        }


        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog =
                   new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select a folder to monitor";

                if (dialog.ShowDialog() ==
                    System.Windows.Forms.DialogResult.OK)
                {
                    FolderPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private async void LoadFilesButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                string folderPath = FolderPathTextBox.Text;

                if (string.IsNullOrWhiteSpace(folderPath) ||
                    !Directory.Exists(folderPath))
                {
                    System.Windows.MessageBox.Show("Please select a valid folder.");
                    return;
                }

                int records = await ProcessNewFilesAsync(folderPath);

                System.Windows.MessageBox.Show(
                    $"Successfully processed {records} records.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.Message,
                    "File Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void StartMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FolderPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(folderPath) ||
                !Directory.Exists(folderPath))
            {
                System.Windows.MessageBox.Show("Please select a valid folder.");
                return;
            }

            if (!int.TryParse(
                IntervalTextBox.Text, out int intervalSeconds) ||
                intervalSeconds <= 0)
            {
                System.Windows.MessageBox.Show(
                    "Please enter a valid positive interval.");
                return;
            }

            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _monitoringCancellation = new CancellationTokenSource();

            StartMonitoringButton.IsEnabled = false;
            StopMonitoringButton.IsEnabled = true;

            try
            {
                await MonitorFolderAsync(
                    folderPath,
                    intervalSeconds,
                    _monitoringCancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Monitoring Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isMonitoring = false;

                _monitoringCancellation?.Dispose();
                _monitoringCancellation = null;

                StartMonitoringButton.IsEnabled = true;
                StopMonitoringButton.IsEnabled = false;
            }
        }

        private void StopMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            _monitoringCancellation?.Cancel();
        }

        private async Task MonitorFolderAsync(
            string folderPath,
            int intervalSeconds,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ProcessNewFilesAsync(
                    folderPath, cancellationToken);

                await Task.Delay(
                    TimeSpan.FromSeconds(intervalSeconds),
                    cancellationToken);
            }
        }

        private async Task<int> ProcessNewFilesAsync(
            string folderPath,
            CancellationToken cancellationToken = default)
        {
            List<Task<int>> loadingTasks = new();

            foreach (string file in Directory.GetFiles(folderPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_processedFiles.ContainsKey(file))
                    continue;

                IFileLoader? loader = _loaderManager.GetLoader(file);

                if (loader == null)
                    continue;

                loadingTasks.Add(
                    ProcessSingleFileAsync(
                        loader, file, cancellationToken));
            }

            int[] results = await Task.WhenAll(loadingTasks);

            return results.Sum();
        }

        private async Task<int> ProcessSingleFileAsync(
            IFileLoader loader,
            string file,
            CancellationToken cancellationToken)
        {
            if (_processedFiles.ContainsKey(file))
                return 0;

            if (!_processingFiles.TryAdd(file, 0))
                return 0;

            bool semaphoreAcquired = false;

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                semaphoreAcquired = true;

                bool isReady = await IsFileReadyAsync(
     file,
     cancellationToken);

                if (!isReady)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"File is not ready: {Path.GetFileName(file)}");

                    return 0;
                }

                List<FileData> data =
                    await loader.LoadAsync(file);

                cancellationToken.ThrowIfCancellationRequested();

                await Dispatcher.InvokeAsync(() =>
                {
                    List<FileData> currentData =
                        FilesDataGrid.ItemsSource as List<FileData>
                        ?? new List<FileData>();

                    currentData.AddRange(data);

                    FilesDataGrid.ItemsSource = null;
                    FilesDataGrid.ItemsSource = currentData;
                });

                _processedFiles.TryAdd(file, 0);

                System.Diagnostics.Debug.WriteLine(
                    $"File: {Path.GetFileName(file)} | " +
                    $"Format: {loader.Format} | " +
                    $"Records: {data.Count}");

                return data.Count;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error processing {Path.GetFileName(file)}: " +
                    ex.Message);

                return 0;
            }
            finally
            {
                if (semaphoreAcquired)
                    _semaphore.Release();

                _processingFiles.TryRemove(file, out _);
            }
        }
    }
}
