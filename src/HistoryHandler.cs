public class HistoryHandler
{
    public static void ListHistory(List<string> inputHistory)
    {
        int index = 0;
        Console.WriteLine(); //to skip a line, for test purposes
        foreach (string input in inputHistory)
        {
            Console.WriteLine($"{index} {input}");
            index++;
        }
    }
}