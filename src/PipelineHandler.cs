using System.IO.Pipelines;
using System.Diagnostics;

public class PipelineHandler
{
    public static async Task RunPipeline2(
        string leftCmd, List<string> leftArgs,
        string rightCmd, List<string> rightArgs)
    {
        var leftPsi = new ProcessStartInfo
        {
            FileName = leftCmd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in leftArgs) leftPsi.ArgumentList.Add(a);

        var rightPsi = new ProcessStartInfo
        {
            FileName = rightCmd,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false
        };
        foreach (var a in rightArgs) rightPsi.ArgumentList.Add(a);

        using var left = Process.Start(leftPsi);
        using var right = Process.Start(rightPsi);
        if (left == null || right == null) return;

        var pump = Task.Run(async () =>
        {
            try
            {
                await left.StandardOutput.BaseStream.CopyToAsync(right.StandardInput.BaseStream);
            }
            catch (IOException)
            {
                
            }
            finally
            {
                try { right.StandardInput.Close(); } catch { }
            }
        });
        

        //wait for the right side to finish (pipeline result)
        await right.WaitForExitAsync();

        //make sure pump finishes; if head exited, pump will stop anyway
        await pump;

        //wait for left too
        await left.WaitForExitAsync();

    }
    
}