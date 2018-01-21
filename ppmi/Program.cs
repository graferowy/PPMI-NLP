using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ppmi
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            string[] files = { @"text.txt" };
            int[,] coocurrenceMatrix;
            double[,] ppmiMatrix;
            double[,] cosineSimilarityMatrix;
            int windowSize = 4;            
            List<string> dictionary = new List<string>();

            timer.Start();
            dictionary = GenerateDictionary(files);
            timer.Stop();
            Console.WriteLine("Dictionary Generated in {0}s. Dictionary length: {1}", timer.Elapsed, dictionary.Count);

            timer.Restart();
            coocurrenceMatrix = GenerateCoocurrenceMatrix(dictionary, files, windowSize);
            timer.Stop();
            Console.WriteLine("Coocurrence Matrix Generated in {0}s.", timer.Elapsed);

            timer.Restart();
            ppmiMatrix = CalculatePpmiMatrix(coocurrenceMatrix);
            timer.Stop();
            Console.WriteLine("PPMI Matrix Generated in {0}s.", timer.Elapsed);

            timer.Restart();
            cosineSimilarityMatrix = CalculateCosineSimilarityMatrix(ppmiMatrix);
            timer.Stop();
            Console.WriteLine("Cosine Similarity Matrix Generated in {0}s.", timer.Elapsed);
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

        static double[,] CalculatePpmiMatrix(int[,] coocMatrix)
        {
            double[,] tempPpmiMatrix = new double[coocMatrix.GetLength(0), coocMatrix.GetLength(1)];
            int denominator = GetDenominator(coocMatrix);
            for (int i = 0; i < coocMatrix.GetLength(0); i++)
                for (int j = i; j < coocMatrix.GetLength(1); j++)
                {
                    tempPpmiMatrix[i, j] = CalculatePpmi(coocMatrix, i, j, denominator);
                    tempPpmiMatrix[j, i] = tempPpmiMatrix[i, j];
                }
            return tempPpmiMatrix;
        }

        static double[,] CalculateCosineSimilarityMatrix(double[,] ppmiMatrix)
        {
            double[,] tempSimilarityMatrix = new double[ppmiMatrix.GetLength(0), ppmiMatrix.GetLength(1)];

            for (int i = 0; i < ppmiMatrix.GetLength(0); i++)
                for (int j = i; j < ppmiMatrix.GetLength(1); j++)
                {
                    tempSimilarityMatrix[i, j] = CalculateCosineSimilarity(ppmiMatrix, i, j);
                    tempSimilarityMatrix[j, i] = tempSimilarityMatrix[i, j];
                }
            return tempSimilarityMatrix;
        }

        static int GetDenominator(int[,] matrix)
        {
            int result = 0;
            for (int i = 0; i < matrix.GetLength(0); i++)
                for (int j = 0; j < matrix.GetLength(1); j++)
                    result += matrix[i, j];
            return result;
        }

        static double CalculatePpmi(int[,] coocMatrix, int i, int j, int denominator)
        {
            double p, p1, p2, p3;
            double p1numerator = 0, p2numerator = 0, p3numerator = 0;

            p1numerator = coocMatrix[i, j];
            for (int x = 0; x < coocMatrix.GetLength(0); x++)
            {
                p2numerator += coocMatrix[i, x];
                p3numerator += coocMatrix[x, j];
            }
            p1 = p1numerator / denominator;
            p2 = p2numerator / denominator;
            p3 = p3numerator / denominator;

            p = Math.Log((p1 / (p2 * p3)), 2);
            return (p < 0 || Double.IsNaN(p)) ? 0 : p;
        }

        static double CalculateCosineSimilarity(double[,] ppmiMatrix, int i, int j)
        {
            double sumOfProducts = 0;
            double sumOfSquaredFirstRow = 0;
            double sumOfSquaredSecondRow = 0;
            double cosineSimilarity = 0;

            for (int x = 0; x < ppmiMatrix.GetLength(0); x++)
            {
                sumOfProducts += ppmiMatrix[i, x] * ppmiMatrix[j, x];
                sumOfSquaredFirstRow += ppmiMatrix[i, x] * ppmiMatrix[i, x];
                sumOfSquaredSecondRow += ppmiMatrix[j, x] * ppmiMatrix[j, x];
            }

            cosineSimilarity = sumOfProducts / (Math.Sqrt(sumOfSquaredFirstRow) * Math.Sqrt(sumOfSquaredSecondRow));

            return cosineSimilarity;
        }
    }
}
