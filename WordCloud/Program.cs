using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace WordCloud
{
    class Program
    {
        Dictionary<string, int> Frequencies = new Dictionary<string, int>();
        private static List<int> _topOccurencesQuantity;
        private static List<string> _topOccurencesWords;
        ulong TotalWordCount = 0;
        string Root = @"C:\Users\rober\RiderProjects\WordCloud\WordCloud\data";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            new Program().WordFrequencyAsync().Wait();
            WordCloudGenerator();
        }

        public static void WordCloudGenerator()
        {
            var wordCloud = new WordCloudSharp.WordCloud(800, 500, allowVerical: true, fontname: "YouYuan");
            var image = wordCloud.Draw(_topOccurencesWords, _topOccurencesQuantity);
            Encoder myEncoder = Encoder.Quality;
            EncoderParameters myEncoderParameters;
            myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 75L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            image.Save(@"C:\Users\rober\RiderProjects\WordCloud\WordCloud\Result-WordCloud.jpg");
        }
        

        public IEnumerable<string> GetFilePaths(string root)
        {
            foreach (string file in Directory.EnumerateFiles(root))
            {
                yield return file;
            }
        }

        public IEnumerable<string> GetWordsFromFile(string path)
        {
            var foundTextStart = false;
            bool foundTextEnd;
            foreach (string line in File.ReadLines(path))
            {
                if (foundTextStart == false)
                {
                    foundTextStart = BookDisclaimerFilter.isBeginningOfDisclaimer(line);
                    continue;
                }

                foundTextEnd = BookDisclaimerFilter.isEndingOfDisclaimer(line);
                if (foundTextEnd)
                    break;
                
                var words = line
                    .Trim(new[] {'\n', '\r'})
                    .Split(new[] {' ', '.', ',', '!', '?', '"', '\'', '{', '}', ']', '[', '(', ')', '<', '>', ';', ':'},
                        StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words.Select(word => word.ToLower())
                    .Where(p => StopWordFilter.isNotStopWord(p) && p.Length > 1))
                {
                    yield return word;
                }
            }
        }

        public Dictionary<string, int> BuildWordFrequency(IEnumerable<string> words)
        {
            var frequencies = new Dictionary<string, int>();

            foreach (var word in words)
            {
                if (frequencies.ContainsKey(word))
                {
                    frequencies[word] += 1;
                }
                else
                {
                    frequencies[word] = 1;
                }
            }

            return frequencies;
        }

        public void UpdateGlobalFrequenciesFromFileFrequencies(Dictionary<string, int> fileFrequencies)
        {
            lock (Frequencies)
            {
                foreach (var wordFrequency in fileFrequencies)
                {
                    var word = wordFrequency.Key;
                    if (Frequencies.ContainsKey(word))
                    {
                        Frequencies[word] += wordFrequency.Value;
                    }
                    else
                    {
                        Frequencies[word] = wordFrequency.Value;
                    }

                    TotalWordCount += (ulong)wordFrequency.Value;
                }
            }
        }

        public void GetWordFrequencies(string filePath)
        {
            var fileWords = GetWordsFromFile(filePath);
            var fileWordFrequencies = BuildWordFrequency(fileWords);
            // Console.WriteLine("Done for " + filePath);
            UpdateGlobalFrequenciesFromFileFrequencies(fileWordFrequencies);
        }
        
        public async Task WordFrequencyAsync()
        {
            var executionBlock = new ActionBlock<string>
            (
                GetWordFrequencies,
                new ExecutionDataflowBlockOptions {
                    MaxDegreeOfParallelism = 8,
                    BoundedCapacity = 8
                }
            );

            var timer = Stopwatch.StartNew();
            Console.WriteLine("Starting");

            foreach (var filePath in GetFilePaths(Root))
            {
                await executionBlock.SendAsync(filePath);
            }

            executionBlock.Complete();
            await executionBlock.Completion;

            timer.Stop();
            Console.WriteLine("Completed in " + timer.Elapsed.ToString());
            WriteTopOccurences();
        }
        
        public void WriteTopOccurences()
        {
            Console.WriteLine("Total word count: " + TotalWordCount);
            Console.WriteLine("Unique word count: " + Frequencies.Count);
            var filePath = @"C:\Users\rober\RiderProjects\WordCloud\WordCloud\word-analysis.csv";
            
            File.WriteAllLines(filePath, Frequencies.OrderByDescending(x => x.Value).Take(50)
                .Select(x =>  x.Value + ", " + x.Key));
            
            _topOccurencesQuantity = Frequencies.OrderByDescending(key => key.Value).Take(50)
                .Select(p => p.Value).ToList();
            _topOccurencesWords = Frequencies.OrderByDescending(key => key.Value).Take(50)
                .Select(p => p.Key).ToList();
        }
    }
}