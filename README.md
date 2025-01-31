###### [↩️Return to Portfolio](https://github.com/EricDelgado993/Portfolio)
# File Handler and Monitor

## Overview
This program monitors a specified directory for newly created `.txt` files, processes them by analyzing word frequencies, and generates a `.json` report containing file metadata and word statistics.

---

## 📂 **Project Files**
- [Program.cs](https://github.com/EricDelgado993/File-Monitor/blob/main/FileMonitorTest/Program.cs)

---

## Features
- **Real-time Directory Monitoring**: Uses `FileSystemWatcher` to detect newly created `.txt` files.
- **Debouncing Mechanism**: Prevents redundant processing by applying a 4-second delay to file events.
- **Word Frequency Analysis**: Reads and processes text files to determine word occurrences.
- **Automatic JSON Report Generation**: Outputs a structured JSON report with file metadata and word statistics.
- **Error Handling**: Captures and logs errors to prevent application crashes.

## Key Components
- **`RunFileMonitor(directoryPath)`**: Monitors the specified directory for new `.txt` files.
- **`OnChanged(object, FileSystemEventArgs)`**: Handles file creation events and triggers processing.
- **`ProcessFile(string filePath)`**: Reads and analyzes the contents of the text file, generating a JSON report.
- **`Debounce(string, int, Action)`**: Implements a delay mechanism to avoid multiple processing triggers.
- **`PrintException(Exception?)`**: Logs exceptions that occur during execution.

## Usage
1. Run the program and enter a directory path to monitor.
2. The program will continuously watch for new `.txt` files.
3. When a file is detected, it will be processed, and a `.json` report will be created.
4. Press `Enter` to exit monitoring mode.

## Dependencies
- **.NET Core** (C#)
- **System.IO** for file operations
- **System.Text.Json** for JSON serialization

## Example JSON Output
```json
{
  "Timestamp": "01-30-2025 02:45:12 PM",
  "FileName": "example.txt",
  "SizeInBytes": 1024,
  "DateLastModified": "01-30-2025 02:40:00 PM",
  "LineCount": 20,
  "TopTenWords": {
    "example": 15,
    "text": 10,
    "file": 8
  },
  "WordFrequencies": {
    "example": 15,
    "text": 10,
    "file": 8,
    "handler": 5
  }
}
