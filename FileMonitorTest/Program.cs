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
        // Infinite loop keeps prompting the user until they enter a valid directory.
        while (true)
        {

            Console.WriteLine("Enter file directory to be monitored:");
            Console.Write(">> ");

            // Read user input and stores directoryPath as a string.
            var directoryPath = Console.ReadLine();

            // If the directory exists:
            if (Directory.Exists(directoryPath))
            {
                Console.Clear();

                // Function call to the file monitoring function with the valid directory.
                RunFileMonitor(directoryPath);
            }

            // If the directory does not exist:
            else
            {
                Console.WriteLine("ERROR: Directory does not exist. Enter valid directory.");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    // Dictionary that maps file paths (string) to CancellationTokenSource objects.
    // Helps implement debouncing, ensuring that only the last action read within
    // a time period is processed.
    private static readonly Dictionary<string, CancellationTokenSource> debounceTokens = new();

    // Handles the setup of the file system watcher to monitor changes in the given directory.
    public static void RunFileMonitor(string directoryPath)
    {
        // Creates a FileSystemWatcher object to monitor the given directory.
        // using var -> Ensures automatic cleanup of the watcher when the function exits.
        using var watcher = new FileSystemWatcher(directoryPath);

        watcher.NotifyFilter = NotifyFilters.FileName;  // Configure watcher to monitor file name and file creation changes.
        watcher.Created += OnChanged;                   // Triggers/Subscribes OnChanged() when new file is created in directory.
        watcher.Error += OnError;                       // Triggers/Subscribes OnError() to handle any errors from FileSystemWatcher.
        watcher.Filter = "*txt";                        // Filter to monitor only .txt files.
        watcher.EnableRaisingEvents = true;             // Starts listening for file system changes in directory.

        // Display a monitoring status update to console.
        Console.WriteLine("[{0}] Monitoring directory: {1}", DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"), directoryPath);

        // Wait for user input to keep application running indefinitely.
        Console.ReadLine();
        Console.Clear();
    }

    // Triggered when a new .txt file is created.
    // object source = reference to the FileSystemWatcher.
    // fileEventArgs = contains path that file will be written into and its name.
    private static void OnChanged(object source, FileSystemEventArgs fileEventArgs)
    {
        // Debounce() prevents redundant execution by introducing a delay of 4000 ms (4 sec).
        // If another action occurs within the 4-sec window on the same file, the previous
        // action is canceled, and the timer restarts.
        // fileEventArgs.FullPath = The absolute path of the newly created file. 
        //                          Acts as a unique key to track debouncing for files.
        // () => {...} = A lambda/anonymous function containing the logic to execute after
        //               The debounce delay
        Debounce(fileEventArgs.FullPath, 4000, () =>
        {
            // Display a monitoring status update to the console when a new file is detected
            // after the debounce delay.
            Console.WriteLine("[{0}] File '{1}' created in directory.", DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"), fileEventArgs.Name);
            ProcessFile(fileEventArgs.FullPath);
        });
    }

    // When an error occurs in the FileSystemWatcher, calls PrintExecption() to display details.
    private static void OnError(object source, ErrorEventArgs errorArgs) =>
            // Print information to console about any errors that occur during
            // file system monitoring.
            PrintException(errorArgs.GetException());

    // Read in the file, count instances of words, and store results in JSON file.
    private static void ProcessFile(string filePath)
    {
        Dictionary<string, int> wordList = new Dictionary<string, int>();   // Stores word frequencies.
        FileInfo fileInfo = new(filePath);                                  // Retrieves file details.    
        int lineCount = 0;                                                  // Track number of lines in the file.

        try
        {
            // Open the .txt file for reading.
            using (StreamReader reader = new StreamReader(filePath))
            {
                // Iterate through each line in the file.
                while (!reader.EndOfStream)
                {
                    // Read in entire line and store within 'line' variable.
                    string line = reader.ReadLine();

                    // Split line into individual words using a list of delimiters.
                    // Each word in split line is stored inside 'words' array.
                    string[] words = line.Split(new char[] { ' ', '\t', '\n', '.', ',', '!', '?', ';', ':', '"', '(', ')', '[', ']', '{', '}', '-', '+', '&' }, StringSplitOptions.RemoveEmptyEntries);

                    lineCount++;

                    // Iterate over each word in the 'words' array.
                    foreach (string word in words)
                    {
                        // Convert each word to lowercase for case-insensitive counting.
                        string lowerWord = word.ToLower();

                        // Check to see if lowerWord is already in 'wordList' dictionary.
                        if (wordList.ContainsKey(lowerWord))
                        {
                            // Increment count if word is in dictionary.
                            wordList[lowerWord]++;
                        }

                        // Otherwise add word with a count of 1.
                        else
                        {
                            wordList[lowerWord] = 1;
                        }
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

            // Display a monitoring status update to console
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

    // Ensure that the last event is executed after a delay.
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
                // If interrupted, the previous delay is canceled and a new delay timer starts.
                await Task.Delay(milliseconds, token);

                // Checks if the task has not been cancelled after timeframe.
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