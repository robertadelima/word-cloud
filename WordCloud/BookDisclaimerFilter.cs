namespace WordCloud
{
    public class BookDisclaimerFilter
    {
        public static bool isBeginningOfDisclaimer(string line)
        {
            return line.StartsWith("*** START");
        }
        
        public static bool isEndingOfDisclaimer(string line)
        {
            return line.Contains("*** END");
        }
    }
}