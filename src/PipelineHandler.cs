using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;

//TODO: use the try catch clauses to actually handle errors, they are placeholders currently

public static class PipelineHandler
{
    static List<string> _builtins = new()
    {
        "cd", "exit", "echo", "pwd", "type", "history"
    };
    
    private static async Task WriteLinesToFileAsync(IEnumerable<string> lines, string path, bool append)
    {
        await using var writer = new StreamWriter(path, append);
        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line);
        }
    }
    public static bool TryParsePipeline(
        List<string> tokenizedInput,
        out List<List<string>> pipeline,
        out string? error)
    {
        pipeline = new List<List<string>>();
        error = null;
        
        List<string> current = new List<string>();

        for (int i = 0; i < tokenizedInput.Count; i++)
        {
            string tok = tokenizedInput[i];
            if (tok == "|")
            {
                if (current.Count == 0 || i == tokenizedInput.Count - 1)
                {
                    error = "syntax error near unexpected token `|`";
                    pipeline.Clear();
                    return false;
                }

                pipeline.Add(current);
                current = new List<string>();
                continue;
            }
            current.Add(tok);
        }
        if (current.Count == 0)
        {
            error = "syntax error near unexpected token `|`";
            pipeline.Clear();
            return false;
        }
        pipeline.Add(current);
        return true;
    }
    
    
    
    
    // pipeline[i] is the token list of command i (no "|" tokens inside)
    public static async Task RunPipelineN(List<List<string>> pipeline, List<string> inputHistory)
    {
        int n = pipeline.Count;
        if (n == 0) return;

        // N-1 pipes connect stage i -> stage i+1
        Pipe[] pipes = new Pipe[Math.Max(0, n - 1)];
        for (int i = 0; i < pipes.Length; i++)
            pipes[i] = new Pipe();

        Task[] tasks = new Task[n];

        for (int i = 0; i < n; i++)
        {
            // Decide stage input/output
            Stream input = (i == 0)
                ? Stream.Null //bugs happen when this is not like this, no idea why
                : pipes[i - 1].Reader.AsStream();

            Stream output = (i == n - 1)
                ? Console.OpenStandardOutput()
                : pipes[i].Writer.AsStream();

            bool closeInput = (i != 0);       // only close pipe streams
            bool closeOutput = (i != n - 1);  // only close pipe streams

            // Capture locals for task
            tasks[i] = RunStageAsync(pipeline[i], input, output, closeInput, closeOutput, inputHistory);
        }

        await Task.WhenAll(tasks);
    }

    private static async Task RunStageAsync(
        List<string> tokens,
        Stream input,
        Stream output,
        bool closeInput,
        bool closeOutput,
        List<string> inputHistory)
    {
        try
        {
            string cmd = tokens[0];

            if (IsBuiltin(cmd))
            {
                await RunBuiltinAsync(tokens, input, output, inputHistory);
            }
            else
            {
                await RunExternalAsync(tokens, input, output);
            }
        }
        finally
        {
            //closing internal pipe writer signals, unless they are stdin or stdout
            if (closeOutput)
            {
                await output.FlushAsync();
                output.Dispose();
            }
            if (closeInput)
            {
               input.Dispose();
            }
        }
    }

    private static async Task RunExternalAsync(List<string> tokens, Stream input, Stream output)
    {
        var psi = new ProcessStartInfo
        {
            FileName = tokens[0],
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        for (int i = 1; i < tokens.Count; i++)
            psi.ArgumentList.Add(tokens[i]);

        using Process p = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: {tokens[0]}");

        // Bridge streams concurrently
        Task taskIn = Task.Run(async () =>
        {
            await input.CopyToAsync(p.StandardInput.BaseStream);
            p.StandardInput.Close();
        });

        Task taskOut = p.StandardOutput.BaseStream.CopyToAsync(output);

        Task taskErr = p.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError());

        await Task.WhenAll(taskIn, taskOut, taskErr);
        await p.WaitForExitAsync();
    }
    
    private static bool IsBuiltin(string cmd)
    {
        return _builtins.Contains(cmd);
    }
    public static Task WriteLineToStreamAsync(string content, Stream stream)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content + Environment.NewLine);
        return stream.WriteAsync(bytes, 0, bytes.Length);
    }
    private static async Task RunBuiltinAsync(List<string> tokens, Stream input, Stream output, List<string> inputHistory)
    {
        if (tokens.Count == 0) return;

        string command = tokens[0];

        List<string> arguments = new List<string>();
        for (int i = 1; i < tokens.Count; i++)
            arguments.Add(tokens[i]);

        string message = "";
        if (tokens.Count > 1)
        {
            message = tokens[1];
            for (int i = 2; i < tokens.Count; i++)
                message += " " + tokens[i];
        }

        Stream stderr = Console.OpenStandardError();

        switch (command)
        {
            case "type":
                if (tokens.Count < 2)
                {
                    await WriteLineToStreamAsync("type: missing argument", stderr);
                    break;
                }

                switch (tokens[1])
                {
                    case "exit" or "quit" or "type" or "echo" or "pwd" or "history":
                        await WriteLineToStreamAsync($"{tokens[1]} is a shell builtin", output);
                        break;

                    default:
                        var fullPath = Program.FindExecutableInPath(tokens[1]);
                        if (fullPath != null)
                            await WriteLineToStreamAsync($"{tokens[1]} is {fullPath}", output);
                        else
                            await WriteLineToStreamAsync($"{tokens[1]}: not found", stderr);
                        break;
                }
                break;

            case "exit":
                await WriteLineToStreamAsync("exit", output);
                return;

            case "echo":
                await WriteLineToStreamAsync(message, output);
                break;

            case "pwd":
                await WriteLineToStreamAsync(Directory.GetCurrentDirectory(), output);
                break;

            case "cd":
                await WriteLineToStreamAsync("cd: not supported in pipelines", stderr);
                break;
            
            case "history":
                if (tokens.Count == 1)
                {
                    await HistoryHandler.ListHistoryAsync(inputHistory, output);
                }
                else if (tokens.Count == 2 && int.TryParse(tokens[1], out int historyCount))
                {
                    await HistoryHandler.ListLastNHistoryAsync(inputHistory, historyCount, output);
                }
                
                else if (tokens.Count == 3 && tokens[1] is "-r")
                {
                    var historyFile = tokens[2];
                    if (!File.Exists(historyFile))
                    {
                        await WriteLineToStreamAsync("history: missing or wrong argument", stderr);
                        break;
                    }

                    var fileHistoryText = await HistoryHandler.ReadHistoryFileAsync(historyFile);
                    inputHistory.AddRange(fileHistoryText);
                }
                else if (tokens.Count == 3 && tokens[1] == "-w")
                {
                    
                    var historyFile = Program.FindExecutableInPath(tokens[2]);
                    if (historyFile != null) await WriteLinesToFileAsync(inputHistory, historyFile, append: false);
                }
                else
                {
                    await WriteLineToStreamAsync("history: missing or wrong argument", stderr);
                }
                break;

            default:
                await WriteLineToStreamAsync($"{command}: builtin not implemented", stderr);
                break;
        }
    }
}
