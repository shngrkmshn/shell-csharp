public class HistoryHandler
{
    private static string FormatHistoryLine(int zeroBasedIndex, string commandText)
    {
        int displayIndex = zeroBasedIndex + 1;
        return $"{displayIndex,5}  {commandText}";
    }

    //pipelines made it so that the code has to work async whenever possible. Preplanning would have been great.
    public static async Task ListHistoryAsync(List<string> inputHistory, Stream output)
    {
        int index = 0;

        foreach (string input in inputHistory)
        {
            await PipelineHandler.WriteLineToStreamAsync(FormatHistoryLine(index, input), output);
            index++;
        }
    }

    public static async Task ListLastNHistoryAsync(List<string> inputHistory, int previousCount, Stream output)
    {
        if (previousCount < 0)
            return;

        int start = Math.Max(0, inputHistory.Count - previousCount);
        for (int index = start; index < inputHistory.Count; index++)
        {
            await PipelineHandler.WriteLineToStreamAsync(FormatHistoryLine(index, inputHistory[index]), output);
        }
    }
}
