using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;

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
            new Program().WordFrequencyAsync().Wait();
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

                foreach (var word in words.Select(word => word.ToLower()))
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