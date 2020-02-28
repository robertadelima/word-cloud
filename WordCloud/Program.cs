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
        ulong TotalWordCount = 0;
        string Root = @"C:\Users\rober\RiderProjects\BooksAnalysis\BooksAnalysis\10k-livros";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // testingWordCloudGenerator();
            new Program().WordFrequencyAsync().Wait();
        }

        public static void testingWordCloudGenerator()
        {
            Image newImage = Image.FromFile(@"C:\Users\rober\RiderProjects\WordCloud\WordCloud\cloud.png");
            var wordCloud = new WordCloudSharp.WordCloud(200, 100, allowVerical: true, fontname: "YouYuan");
            //WordCloudSharp.WordCloud wordCloud = new WordCloudSharp.WordCloud(100, 50, false, null, -1F, 1, newImage, false, null);
            var xPalavras = new List<string>();
            xPalavras.Add("hello");
            xPalavras.Add("hi");
            xPalavras.Add("carpool");
            xPalavras.Add("california");
            xPalavras.Add("health");
            xPalavras.Add("book");
            xPalavras.Add("journalism");
            xPalavras.Add("notebook");
            xPalavras.Add("university");
            xPalavras.Add("venezia");
            var xFrequencia = new List<Int32>();
            xFrequencia.Add(408000);
            xFrequencia.Add(72000);
            xFrequencia.Add(10000);
            xFrequencia.Add(200);
            xFrequencia.Add(105000);
            xFrequencia.Add(205000);
            xFrequencia.Add(2200);
            xFrequencia.Add(7500);
            xFrequencia.Add(11600);
            xFrequencia.Add(14000);

            var image = wordCloud.Draw(xPalavras, xFrequencia);
            // Save the bitmap as a JPEG file with quality level 75.
            Bitmap myBitmap;
            ImageCodecInfo myImageCodecInfo;
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

        public void PrintFrequenciesSorted()
        {
            Console.WriteLine("Total word count: " + TotalWordCount);
            Console.WriteLine("Unique word count: " + Frequencies.Count);
            foreach (var freq in Frequencies.OrderByDescending(key => key.Value).Take(10))
            {
                Console.WriteLine(freq.Key + ": " + freq.Value);
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
            PrintFrequenciesSorted();
        }
    }
}