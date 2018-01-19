using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ppmi
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = { @"text.txt" };
            int windowSize = 4;            
            List<string> dictionary = new List<string>();
            dictionary = GenerateDictionary(files);
            Console.WriteLine("Dictionary Generated... Dictionary length: {0}", dictionary.Count);
            int[,] coocurrenceMatrix = GenerateCoocurrenceMatrix(dictionary, files, windowSize);            
            Console.WriteLine("Coocurrence Matrix Generated...");
        }

        static List<string> GenerateDictionary(string[] files)
        {
            Regex rgx = new Regex("[0-9$*+,:=?/\\[\\]@#|'<>.^\"*()%!-]");
            List<string> tempDictionary = new List<string>();
            foreach (var file in files)
                if(File.Exists(file))
                    using (StreamReader sr = new StreamReader(file))
                        while(sr.Peek() >= 0)
                            foreach (var word in sr.ReadLine().Split(' '))
                            {
                                string currentWord = rgx.Replace(word, "").ToLower();
                                if (!tempDictionary.Contains(currentWord) && currentWord.Length > 0)
                                    tempDictionary.Add(currentWord);
                            }
            return tempDictionary;
        }

        static int[,] GenerateCoocurrenceMatrix(List<string> dictionary, string[] files, int windowSize)
        {
            int[,] tempMatrix = new int[dictionary.Count, dictionary.Count];
            Queue<string> windowWords = new Queue<string>();
            Regex rgx = new Regex("[0-9$*+,:=?/\\[\\]@#|'<>.^\"*()%!-]");
            foreach (var file in files)
                if (File.Exists(file))
                    using (StreamReader sr = new StreamReader(file))
                        while (sr.Peek() >= 0)
                            foreach (var word in sr.ReadLine().Split(' '))
                            {
                                string currentWord = rgx.Replace(word, "").ToLower();
                                if(dictionary.Contains(currentWord)) { 
                                    foreach (var previousWord in windowWords)
                                    {
                                        tempMatrix[dictionary.IndexOf(currentWord), dictionary.IndexOf(previousWord)] += 1;
                                        tempMatrix[dictionary.IndexOf(previousWord), dictionary.IndexOf(currentWord)] += 1;
                                    }
                                    windowWords.Enqueue(currentWord);
                                    if (windowWords.Count > windowSize)
                                        windowWords.Dequeue();
                                }
                            }
            return tempMatrix;
        }
    }
}
