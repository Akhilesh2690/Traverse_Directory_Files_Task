public class FileSystemVisitor
{
    private readonly string _startFolder;
    private readonly Func<string, bool> _filterFn;

    public event EventHandler StartSearch;

    public event EventHandler FinishSearch;

    public event EventHandler<FileSystemEventArgs> FileFound;

    public event EventHandler<FileSystemEventArgs> DirectoryFound;

    public event EventHandler<FileSystemEventArgs> FilteredFileFound;

    public event EventHandler<FileSystemEventArgs> FilteredDirectoryFound;

    public FileSystemVisitor(string startFolder, Func<string, bool> filterFn = null)
    {
        _startFolder = startFolder ?? throw new ArgumentNullException(nameof(startFolder));
        _filterFn = filterFn ?? (path => true);
    }

    private IEnumerable<string> Traverse(string currentFolder)
    {
        StartSearch?.Invoke(this, EventArgs.Empty);

        foreach (var directory in Directory.GetDirectories(currentFolder))
        {
            var directoryEventArgs = new FileSystemEventArgs(directory);
            DirectoryFound?.Invoke(this, directoryEventArgs);

            if (directoryEventArgs.Abort) break;

            if (directoryEventArgs.Exclude) continue;

            if (_filterFn(directory))
            {
                var filteredDirectoryEventArgs = new FileSystemEventArgs(directory);
                FilteredDirectoryFound?.Invoke(this, filteredDirectoryEventArgs);

                if (filteredDirectoryEventArgs.Abort) break;
                if (filteredDirectoryEventArgs.Exclude) continue;
                yield return directory;
            }

            foreach (var subItem in Traverse(directory))
            {
                yield return subItem;
            }
        }

        foreach (var file in Directory.GetFiles(currentFolder))
        {
            var fileEventArgs = new FileSystemEventArgs(file);
            FileFound?.Invoke(this, fileEventArgs);
            if (fileEventArgs.Abort) break;
            if (fileEventArgs.Exclude) continue;

            if (_filterFn(file))
            {
                var filteredFileEventArgs = new FileSystemEventArgs(file);
                FilteredFileFound?.Invoke(this, filteredFileEventArgs);

                if (filteredFileEventArgs.Abort) break;
                if (filteredFileEventArgs.Exclude) continue;
                yield return file;
            }
        }

        FinishSearch?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<string> GetFileSystemItems()
    {
        return Traverse(_startFolder);
    }
}

public class FileSystemEventArgs : EventArgs
{
    public string Path { get; }
    public bool Abort { get; set; }
    public bool Exclude { get; set; }

    public FileSystemEventArgs(string path)
    {
        Path = path;
    }
}

internal class Program
{
    private static void Main()
    {
        Console.WriteLine("Enter the directory path in format(E:\\DirectoryName):");
        string directoryPath = Console.ReadLine();

        if (string.IsNullOrEmpty(directoryPath))
        {
            Console.WriteLine("The directory path cannot be empty.");
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"The directory '{directoryPath}' does not exist or is not a valid directory.");
            return;
        }

        var visitor = new FileSystemVisitor(directoryPath, path => path.EndsWith(".txt"));

        visitor.StartSearch += (sender, e) => Console.WriteLine("Search Started.");
        visitor.FinishSearch += (sender, e) => Console.WriteLine("Search Finished.");
        visitor.FileFound += (sender, e) => Console.WriteLine($"File Found: {e.Path}");
        visitor.DirectoryFound += (sender, e) => Console.WriteLine($"Directory Found: {e.Path}");
        visitor.FilteredFileFound += (sender, e) => Console.WriteLine($"Filtered File Found: {e.Path}");
        visitor.FilteredDirectoryFound += (sender, e) => Console.WriteLine($"Filtered Directory Found: {e.Path}");

        visitor.FileFound += (sender, e) =>
        {
            if (e.Path.Contains("text4.txt"))
            {
                e.Exclude = true;
                Console.WriteLine($"Excluding file: {e.Path}");
            }
        };

        visitor.DirectoryFound += (sender, e) =>
        {
            if (e.Path.Contains("TestAbortFolder"))
            {
                e.Exclude = true;
                Console.WriteLine($"Excluding directory: {e.Path}");
            }
        };

        foreach (var item in visitor.GetFileSystemItems())
        {
            Console.WriteLine(item);
        }
    }
}