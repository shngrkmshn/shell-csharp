using System.Diagnostics;
//to add: linux or windows checks, and compatibility for both
class Program
{
    static bool IsExecutable(string fullPath)
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
    static string? FindExecutableInPath(string fileName)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (paths == null)
            return null;
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath) && IsExecutable(fullPath))
                return fullPath;
        }
        return null;
    }

    static void RunProgram(string fullPath, List<string> arguments, string? redirectFile, string? redirectStdError, bool append,  string commandName)
    {
        var executableDir = Path.GetDirectoryName(fullPath)!;
        var psi = new ProcessStartInfo
        {
            FileName = commandName,
            WorkingDirectory = executableDir,
            UseShellExecute = false,
            RedirectStandardOutput = redirectFile != null,
            RedirectStandardError = redirectStdError != null
        };
        
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);
        
        using var process = Process.Start(psi);
        if (process == null) return;

        if (redirectFile != null)
        {
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var dir = Path.GetDirectoryName(redirectFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) 
                Directory.CreateDirectory(dir);
            if (append)
                File.AppendAllText(redirectFile, output);
            else
                File.WriteAllText(redirectFile, output);
        }

        if (redirectStdError != null)
        {
            var output = process.StandardError.ReadToEnd();
            process.WaitForExit();
            var dir = Path.GetDirectoryName(redirectStdError);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) 
                Directory.CreateDirectory(dir);
            if (append)
                File.AppendAllText(redirectStdError, output);
            else
                File.WriteAllText(redirectStdError, output);
        }
        else
            process.WaitForExit();
    }
    
    static void WriteOutput(string content, string? redirectFile, bool append)
    {
        if (redirectFile != null)
        {
            var dir = Path.GetDirectoryName(redirectFile);
            if (!string.IsNullOrEmpty(dir)) 
                Directory.CreateDirectory(dir);
            if (append)
                File.AppendAllText(redirectFile, content + Environment.NewLine);
            else
                File.WriteAllText(redirectFile, content + Environment.NewLine);
        }
        else
        {
            Console.WriteLine(content);
        }
    }
    
    static void PrepareRedirectionFile(string? path)
    {
        if (path == null) return;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Create or truncate immediately
        if (!File.Exists(path))
        {
            using var _ = File.Create(path); // create if missing
        }
    }

    
    
    
    static readonly AutoCompletionHandler _completionHandler = new AutoCompletionHandler();

    static string ReadInput(string prompt)
    {
        Console.Write(prompt);
        Console.Out.Flush();
        var buffer = new System.Text.StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Remove(buffer.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            else if (key.Key == ConsoleKey.Tab)
            {
                string text = buffer.ToString();
                string[] completions = _completionHandler.GetSuggestions(text, 0);
                if (completions.Length > 0)
                {
                    string completion = completions[0];
                    // Find where the current word starts (same logic as ReadLine library)
                    int wordStart = text.LastIndexOfAny(_completionHandler.Separators);
                    wordStart = (wordStart == -1) ? 0 : wordStart + 1;
                    // Replace the current word with the completion
                    string newText = text.Substring(0, wordStart) + completion;
                    buffer.Clear();
                    buffer.Append(newText);
                    // Redraw using carriage return (no cursor position tracking needed)
                    Console.Write($"\r{prompt}{newText}\x1b[0K");
                    Console.Out.Flush();
                }
            }
            else if (key.KeyChar != '\0')
            {
                buffer.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }
    }

    static void Main()
    {
        while (true)
        {
            var append = false;

            var consoleInput = ReadInput("$ ")?.Trim();
            if (consoleInput == null) continue;
            var tokenizedInput = TokenizationHandler.Tokenize(consoleInput);
            if (tokenizedInput == null)
            {
                Console.WriteLine($"{consoleInput}: input not found");
                continue;
            }

          
            var redirectionIndex = tokenizedInput.FindIndex(t => t is ">" or "1>");
            var errorRedirectionIndex = tokenizedInput.FindIndex(t => t is "2>");
            var appendRedirectionIndex = tokenizedInput.FindIndex(t => t is ">>" or "1>>");
            var appendErrorRedirectionIndex = tokenizedInput.FindIndex(t => t is "2>>");
            string? redirectFile = null;
            string? errorRedirectionFile = null;
            //redirect needed or not
            if (redirectionIndex != -1)
            {
                if (redirectionIndex + 1 >= tokenizedInput.Count)
                {
                    Console.WriteLine("syntax error: expected filename after >");
                    continue;
                }
                redirectFile = tokenizedInput[redirectionIndex + 1];
                tokenizedInput = tokenizedInput.Take(redirectionIndex).ToList(); //DANGEROUS 
            }

            if (errorRedirectionIndex != -1)
            {
                if (errorRedirectionIndex + 1 >= tokenizedInput.Count)
                {
                    Console.WriteLine("syntax error: expected filename after 2>");
                    continue;
                }
                errorRedirectionFile =  tokenizedInput[errorRedirectionIndex + 1];
                tokenizedInput = tokenizedInput.Take(errorRedirectionIndex).ToList();
                PrepareRedirectionFile(errorRedirectionFile); 
                
            }

            if (appendErrorRedirectionIndex != -1)
            {
                if (appendErrorRedirectionIndex + 1 >= tokenizedInput.Count)
                {
                    Console.WriteLine("syntax error: expected filename after 2>>");
                    continue;
                }
                append = true;
                errorRedirectionFile = tokenizedInput[appendErrorRedirectionIndex + 1];
                tokenizedInput = tokenizedInput.Take(appendErrorRedirectionIndex).ToList();
                PrepareRedirectionFile(errorRedirectionFile);
            }

            if (appendRedirectionIndex != -1)
            {
                if (appendRedirectionIndex + 1 >= tokenizedInput.Count)
                {
                    Console.WriteLine("syntax error: expected filename after >>");
                    continue;
                }
                append = true;
                redirectFile = tokenizedInput[appendRedirectionIndex + 1];
                tokenizedInput = tokenizedInput.Take(appendRedirectionIndex).ToList();
                PrepareRedirectionFile(redirectFile);
            }
            
            var command = tokenizedInput[0];
            var arguments = tokenizedInput.Skip(1).ToList();
            var message = string.Join(" ", arguments);
            
            switch (command)
            {
                case "type":
                    if (tokenizedInput.Count < 2) { Console.WriteLine("type: missing argument"); break; }
                    switch (tokenizedInput[1])
                    {
                        case "exit" or "quit" or "type" or "echo" or "pwd":
                            Console.WriteLine($"{tokenizedInput[1]} is a shell builtin");
                            break;
                        default: //assumes we are checking for paths, for now
                            var fullPath = FindExecutableInPath(tokenizedInput[1]);
                            if (fullPath != null)
                            {
                                Console.WriteLine($"{tokenizedInput[1]} is {fullPath}");
                            }
                            else
                            {
                                WriteOutput($"{tokenizedInput[1]}: not found", errorRedirectionFile, append);
                            }
                            break;
                    }
                    break;
                case "exit":
                    Console.WriteLine("exit");
                    return;
                case "echo":
                    WriteOutput(message, redirectFile, append);
                    break;
                case "pwd":
                    WriteOutput(Directory.GetCurrentDirectory(), redirectFile, append);
                    break;
                case "cd":
                    if (tokenizedInput.Count < 2)
                    {
                        WriteOutput("cd: missing argument", errorRedirectionFile, append); 
                        break;
                    }

                    if (tokenizedInput[1] == "~")
                    {
                        var homePath = Environment.GetEnvironmentVariable("HOME");//this might be different in windows, check when adding windows
                        if (homePath != null)
                            Directory.SetCurrentDirectory(homePath);
                        break;
                    }
                    
                    if (!Directory.Exists(tokenizedInput[1]))
                    {
                        WriteOutput($"cd: {tokenizedInput[1]}: No such file or directory", errorRedirectionFile, append);
                        break;
                    }
                    Directory.SetCurrentDirectory(tokenizedInput[1]);
                    break;
                
                default: //now we assume the command is a program
                    var executable = FindExecutableInPath(command);
                    if (executable == null)
                    {
                        WriteOutput($"{command}: command not found", errorRedirectionFile, append);
                        break;
                    }
                    //giving the full path executable gave a test log error (it works, however console output is the path instead of executable name, so I am writing just the name for now, should be full executable normally 
                    RunProgram(executable, arguments, redirectFile, errorRedirectionFile, append, command); //maybe a better way to skip first token?
                    break;
            }
        }
    }
}

