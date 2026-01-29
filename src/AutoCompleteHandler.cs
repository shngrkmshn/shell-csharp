
public class AutoCompletionHandler : IAutoCompleteHandler
{
    public char[] Separators { get; set; } = "abcdefghijklmnopqrstuvwxyz".ToArray();

    private readonly string[] builtins = { "echo", "exit", "pwd", "cd", "type" };

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

    public string[] GetSuggestions(string text, int index)
    {
        var builtinMatches = builtins.Where(x => x.StartsWith(text));
        var executableMatches = GetExecutablesFromPath(text);

        var matches = builtinMatches.Concat(executableMatches).Distinct().ToArray();

        if (matches.Length == 0)
        {
            Console.Write("\x07");
            return Array.Empty<string>();
        }

        return matches.Select(b => b.Substring(text.Length) + " ").ToArray();
    }
}
