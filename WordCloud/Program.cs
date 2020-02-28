using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace WordCloud
{
    class Program
    {
        Dictionary<string, int> Frequencies = new Dictionary<string, int>();
        private static List<int> topOccurencesQuantity;
        private static List<string> topOccurencesWords;
        ulong TotalWordCount = 0;
        string Root = @"C:\Users\rober\RiderProjects\BooksAnalysis\BooksAnalysis\10k-livros";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            new Program().WordFrequencyAsync().Wait();
            testingWordCloudGenerator();
        }

        public static void testingWordCloudGenerator()
        {
            Image newImage = Image.FromFile(@"C:\Users\rober\RiderProjects\WordCloud\WordCloud\cloud.png");
            var wordCloud = new WordCloudSharp.WordCloud(800, 500, allowVerical: true, fontname: "YouYuan");
            var image = wordCloud.Draw(topOccurencesWords, topOccurencesQuantity);
            // Save the bitmap as a JPEG file with quality level 75.
            Encoder myEncoder = Encoder.Quality;
            EncoderParameters myEncoderParameters;
            myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 75L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            // myImageCodecInfo = GetEncoderInfo("image/jpeg");
            image.Save(@"C:\Users\rober\RiderProjects\WordCloud\WordCloud\Shapes075.jpg");
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
            foreach (string line in File.ReadLines(path))
            {
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

        public void Bla(string filePath)
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
                Bla,
                new ExecutionDataflowBlockOptions {
                    MaxDegreeOfParallelism = 4,
                    BoundedCapacity = 4
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
            //PrintFrequenciesSorted();
            WriteTopOccurences();
        }
        
        public void WriteTopOccurences()
        {
            Console.WriteLine("Total word count: " + TotalWordCount);
            Console.WriteLine("Unique word count: " + Frequencies.Count);
            var filePath = Root + @"\word-analysis.csv";
            File.WriteAllLines(filePath, Frequencies.Select(x =>  x.Value + ", " + x.Key));
            topOccurencesQuantity = Frequencies.OrderByDescending(key => key.Value).Take(50)
                .Select(p => p.Value).ToList();
            topOccurencesWords = Frequencies.OrderByDescending(key => key.Value).Take(50)
                .Select(p => p.Key).ToList();
        }
        
        public void PrintFrequenciesSorted()
        {
            Console.WriteLine("Total word count: " + TotalWordCount);
            Console.WriteLine("Unique word count: " + Frequencies.Count);
            foreach (var freq in Frequencies.OrderByDescending(key => key.Value).Take(10))
            {
                Console.WriteLine(freq.Key + ": " + freq.Value);
            }
        }
    }
}