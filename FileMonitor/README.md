# File Monitor
A WPF desktop application that monitors a selected directory, detects new files, and loads their data into a DataGrid.

## Features
- Configurable directory monitoring.
- Configurable monitoring interval.
- Supports CSV, XML, TXT, and JSON file formats.
- Separate file loaders for each format.
- Plugin-based architecture for adding external file loaders.
- Configurable enabled file formats through `loaderSettings.json`.
- Asynchronous file processing.
- Limited parallel processing to improve responsiveness.
- Prevents duplicate processing of the same file during application execution.
- Checks whether a file is ready before reading it.

## Technologies
- C#
- .NET 8
- WPF
- XML Reader
- System.Text.Json
- xUnit

## Configuration

The `loaderSettings.json` file controls which file formats are enabled.

Remove a format from `EnabledLoaders` to disable its loader.

## Plugin Installation

The application supports external loaders through DLL plugins.

To install a plugin:

1. Build the plugin class library.
2. Copy its DLL into the application's `Plugins` directory.
3. Add the plugin format to `loaderSettings.json`.
4. Restart the application.

Plugins must implement the `IFileLoader` interface.

## Running the Application

1. Open the solution in Visual Studio.
2. Build the solution.
3. Run the `FileMonitor` project.
4. Select a directory.
5. Set the monitoring interval in seconds.
6. Click **Load Files** to process existing files, or **Start Monitoring** to detect new files automatically.

## Running Tests

Open Test Explorer in Visual Studio and select **Run All Tests**.
The test project includes tests for CSV, XML, TXT, and JSON loaders.