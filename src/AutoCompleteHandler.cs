
public class AutoCompletionHandler : IAutoCompleteHandler
{
    public char[] Separators { get; set; } = new[] { ' ', '\t' };

    private readonly string[] _builtins = { "echo", "exit", "pwd", "cd", "type" };

    private static bool IsExecutable(string fullPath)
    {
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

    private string FindLongestCommonPrefix(string[] matches)
    {
        if (matches.Length == 0) return "";
        if (matches.Length == 1) return matches[0];

        var prefix = matches[0];
        for (int i = 1; i < matches.Length; i++)
        {
            int j = 0;
            while (j < prefix.Length && j < matches[i].Length && prefix[j] == matches[i][j])
            {
                j++;
            }
            prefix = prefix.Substring(0, j);
            if (prefix.Length == 0)
                break;
        }
        return prefix;
    }

    private bool _pressedTabOnce;
    private static readonly string _logFile = Environment.GetEnvironmentVariable("DBG_LOG") ?? "/tmp/shell-dbg.log";

    private static void Dbg(string msg)
    {
        try { File.AppendAllText(_logFile, msg + "\n"); } catch { }
    }

    public string[] GetSuggestions(string text, int index)
    {
        // Workaround: in some test/CI consoles, ReadLine passes index=0 even when cursor is at end.
        int cursor = (index == 0 && text.Length > 0) ? text.Length : Math.Clamp(index, 0, text.Length);
        Dbg($"text=\"{text}\" index={index} cursor={cursor}");

        // Extract the current token (last word) from the FULL line.
        int start = text.LastIndexOfAny(Separators, Math.Max(0, cursor - 1));
        start = (start == -1) ? 0 : start + 1;
        string currentWord = text.Substring(start, cursor - start);
        Dbg($"start={start} currentWord=\"{currentWord}\"");

        var matches = _builtins.Where(x => x.StartsWith(currentWord, StringComparison.Ordinal))
            .Concat(GetExecutablesFromPath(currentWord))
            .Distinct()
            .ToArray();

        Array.Sort(matches, StringComparer.Ordinal);
        Dbg($"matches ({matches.Length}): [{string.Join(", ", matches)}]");

        if (matches.Length == 0)
        {
            Dbg("-> no matches, bell");
            Console.Write("\x07");
            _pressedTabOnce = false;
            return Array.Empty<string>();
        }

        if (matches.Length == 1)
        {
            string completion = matches[0] + " ";
            Dbg($"-> single match, returning \"{completion}\"");
            _pressedTabOnce = false;
            return new[] { completion };
        }

        string lcp = FindLongestCommonPrefix(matches);
        Dbg($"lcp=\"{lcp}\" lcp.Length={lcp.Length} currentWord.Length={currentWord.Length}");

        if (lcp.Length > currentWord.Length)
        {
            Dbg($"-> LCP extends, returning \"{lcp}\"");
            _pressedTabOnce = false;
            return new[] { lcp };
        }

        if (!_pressedTabOnce)
        {
            Dbg("-> first tab, bell");
            Console.Write("\x07");
            _pressedTabOnce = true;
            return Array.Empty<string>();
        }

        Dbg("-> second tab, printing all matches");
        Console.WriteLine();
        Console.WriteLine(string.Join("  ", matches));
        Console.Write("$ " + text);
        Console.Write("\u001b[0K");
        _pressedTabOnce = false;
        return Array.Empty<string>();
    }

}
