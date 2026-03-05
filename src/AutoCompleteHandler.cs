using ReadLine;

public class AutoCompletionHandler : IAutoCompleteHandler
{
    public char[] Separators { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-./"
        .ToCharArray();

    private readonly string[] _builtins = { "echo", "exit", "pwd", "cd", "type" };

    private static readonly string[] WindowsExeExtensions = { ".exe", ".cmd", ".bat", ".com" };

    private static bool IsExecutable(string fullPath)
    {
        if (OperatingSystem.IsWindows())
        {
            var ext = Path.GetExtension(fullPath);
            return WindowsExeExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var mode = File.GetUnixFileMode(fullPath);
            return (mode & UnixFileMode.UserExecute) != 0;
        }
        catch
        {
            return false;
        }
    }

    private IEnumerable<string> GetExecutablesFromPath(string prefix)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (paths == null)
            yield break;

        var seen = new HashSet<string>();

        foreach (var path in paths)
        {
            if (!Directory.Exists(path))
                continue;

            string[] files;
            try
            {
                files = Directory.GetFiles(path);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith(prefix) && IsExecutable(file) && seen.Add(fileName))
                {
                    yield return fileName;
                }
            }
        }
    }
    
    
    
    

    private static string? LongestCommonPrefix(string[] items)
    {
        if (items.Length == 0) return null;
        var prefix = items[0];
        foreach (var item in items.Skip(1))
        {
            int i = 0;
            while (i < prefix.Length && i < item.Length && prefix[i] == item[i]) i++;
            prefix = prefix[..i];
        }
        return prefix.Length > 0 ? prefix : null;
    }

    private bool _pressedTabOnce;
    private string _lastText = string.Empty;
    public string[] GetSuggestions(string text, int index)
    {
        if (text != _lastText)
        {
            _pressedTabOnce = false;
            _lastText = text;
        }
        var builtinMatches = _builtins.Where(x => x.StartsWith(text));
        var executableMatches = GetExecutablesFromPath(text);

        var matches = builtinMatches.Concat(executableMatches).Distinct().ToArray();

        if (matches.Length == 0)
        {
            Console.Write("\x07");
            return Array.Empty<string>();
        }

        if (matches.Length == 1)
        {
            // Single match: complete immediately
            _pressedTabOnce = false;
            return new[] { matches[0].Substring(index) + " " };
        }
        
        //LCP situation, the code is really dumb right now, due to some ReadLine library quirks
        var lcpMatch = LongestCommonPrefix(matches);
        
        if (matches.Length > 1 && lcpMatch != null && lcpMatch.Length > text.Length)
        {
            _pressedTabOnce = false;
            _lastText = lcpMatch;
            return new[] { lcpMatch.Substring(index) };
        }
        
        // Multiple matches: ring bell
        Console.Write("\x07");

        if (!_pressedTabOnce)
        {
            // First TAB: set flag and don't complete yet
            _pressedTabOnce = true;
            return Array.Empty<string>();
        }
        
        else
        {
            // Second TAB: show all options
            Array.Sort(matches, StringComparer.Ordinal);
            Console.WriteLine();
            Console.WriteLine(string.Join("  ", matches));
            Console.Write("$ " + text);
            _pressedTabOnce = false;
            return Array.Empty<string>();
        }
    }
}
