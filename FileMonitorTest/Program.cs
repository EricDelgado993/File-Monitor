using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Enter file directory to be monitored:");
            Console.Write(">> ");

            var directoryPath = Console.ReadLine();

            if (Directory.Exists(directoryPath))
            {
                Console.Clear();
                RunFileMonitor(directoryPath);
            }

            else
            {
                Console.WriteLine("ERROR: Directory does not exist. Enter valid directory.");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    private static readonly Dictionary<string, CancellationTokenSource> debounceTokens = new();

    public static void RunFileMonitor(string directoryPath)
    {
        using var watcher = new FileSystemWatcher(directoryPath);

        watcher.NotifyFilter = NotifyFilters.FileName;  // Monitor changes to the fileName or file creation.
        watcher.Created += OnChanged;                   // Triggers OnChanged() when new file is created in directory.
        watcher.Error += OnError;                       // Triggers OnError() to handle any errors from FileSystemWatcher.
        watcher.Filter = "*txt";                        // Filter to monitor only .txt files.
        watcher.EnableRaisingEvents = true;             // Begin FileSystemWatcher to monitor directory.

        Console.WriteLine("[{0}] Monitoring directory: {1}", DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"), directoryPath);
        
        // Wait for user input to keep application running indefinitely.
        Console.ReadLine();
        Console.Clear();
    }

    private static void OnChanged(object source, FileSystemEventArgs fileEventArgs)
    {
        // Debounce() is invoked when a file change occurs with a 4-second delay.
        // If multiple changes occur within 4 seconds, only the last event will trigger an action
        // while all previous events are cancelled. Prevents multiple calls to process the same file.
        Debounce(fileEventArgs.FullPath, 4000, () =>
        {
            Console.WriteLine("[{0}] File '{1}' created in directory.", DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"), fileEventArgs.Name);
            ProcessFile(fileEventArgs.FullPath);
        });
    }

    private static void OnError(object source, ErrorEventArgs errorArgs) =>
            // Print information to console about any errors that occur during
            // file system monitoring.
            PrintException(errorArgs.GetException());

    private static void ProcessFile(string filePath)
    {
        Dictionary<string, int> wordList = new Dictionary<string, int>();   // Stores word frequencies.
        FileInfo fileInfo = new(filePath);                                  // Retrieves file details.    
        int lineCount = 0;

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                // Iterate through each line .txt file.
                while (!reader.EndOfStream)
                {
                    // Read in line and split into words using list of delimiters.
                    // Each word in split line is stored inside 'words' array.
                    string line = reader.ReadLine();
                    string[] words = line.Split(new char[] { ' ', '\t', '\n', '.', ',', '!', '?', ';', ':', '"', '(', ')', '[', ']', '{', '}', '-', '+', '&' }, StringSplitOptions.RemoveEmptyEntries);

                    lineCount++;

                    // Iterate over each word in the split array.
                    foreach (string word in words)
                    {
                        // Convert each word to lowercase for case-insensitive counting.
                        string lowerWord = word.ToLower();

                        // Check to see if word is already in 'wordList' dictionary.
                        // Increment count if word is in dictionary.
                        if (wordList.ContainsKey(lowerWord))
                            wordList[lowerWord]++;

                        // Otherwise add word with a count of 1.
                        else
                            wordList[lowerWord] = 1;
                    }
                }
            }

            // Anonymous object containing results of file analysis.
            var fileData = new
            {
                Timestamp = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"),
                FileName = fileInfo.Name,
                SizeInBytes = fileInfo.Length,
                DateLastModified = fileInfo.LastWriteTime.ToString("MM-dd-yyyy hh:mm:ss tt"),
                LineCount = lineCount, 
                TopTenWords = wordList
                .OrderByDescending(wordEntry => wordEntry.Value)                         // Orders wordList dictionary by value in descending order. Words with highest frequency will appear first.
                .Take(10)                                                                // Takes first 10 elements from sorted collection, returning an IEnumerable dictionary.
                .ToDictionary(wordEntry => wordEntry.Key, wordEntry => wordEntry.Value), // Collection is converted back into a dictionary of 10 top most frequent words.
                WordFrequencies = wordList
            };

            // Generate the path for the JSON report by changing the file extension from .txt .json.
            string jsonFilePath = Path.ChangeExtension(filePath, ".json");
            
            // Serializes the fileData object into JSON string with intended formatting.
            string jsonString = JsonSerializer.Serialize(fileData, new JsonSerializerOptions { WriteIndented = true });

            // Writes JSON string to the specified .json file.
            File.WriteAllText(jsonFilePath, jsonString);

            Console.WriteLine("[{0}] Data written to '{1}'.", DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"), Path.GetFileName(jsonFilePath));
        }

        // Catches any exceptions that occur during file reading or processing.
        catch (Exception exception)
        {
            Console.WriteLine($"Error processing file: {exception.Message}");
        }
    }

    private static void PrintException(Exception? exception)
    {
        // Recursive method that prints details of an exception.
        // Prints the inner exception, if it exists.
        if (exception != null)
        {
            Console.WriteLine($"Message: {exception.Message}");
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(exception.StackTrace);
            Console.WriteLine();
            PrintException(exception.InnerException);
        }
    }

    private static void Debounce(string key, int milliseconds, Action action)
    {
        // Checks if the file is modified again within the timeframe.
        if (debounceTokens.ContainsKey(key))
        {
            debounceTokens[key].Cancel();           // Cancels previous debouncing task for the file.
            debounceTokens[key].Dispose();          // Releases any resources associated with task that could affect subsequent events.
        }

        var cts = new CancellationTokenSource();    // Created each time a new event occurs.
        debounceTokens[key] = cts;                  // Assigns cancellation token to event within dictionary.
        var token = cts.Token;                      // Used to monitor cancellation requests.

        // An asynchronous task is created and run in the background.
        // Handles debouncing logic.
        Task.Run(async () =>
        {
            try
            {
                // Task is delayed for specified milliseconds.
                // Task will be interrupted if token is cancelled before delay finishes.
                await Task.Delay(milliseconds, token);

                // Checks if the task has not been cancelled.
                if (!token.IsCancellationRequested)
                {
                    // Invokes function.
                    action();
                }
            }

            // Invokes if task has been cancelled.
            catch (TaskCanceledException)
            {
                // Ignore. Action has been prevented from executing multiple times.
            }
        });
    }
}