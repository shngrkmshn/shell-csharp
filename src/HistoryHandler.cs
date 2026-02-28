public class HistoryHandler
{
    //pipelines made it so that the code has to work async whenever possible. Preplanning would have been great.
    public static async Task ListHistoryAsync(List<string> inputHistory, Stream output)
    {
        int index = 0;

        // optional: only do this in interactive mode, not in pipeline/tests
        // await WriteLineToStreamAsync("", output);

        foreach (string input in inputHistory)
        {
            await PipelineHandler.WriteLineToStreamAsync($"{index} {input}", output);
            index++;
        }
    }
}