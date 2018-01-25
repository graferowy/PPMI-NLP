using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

namespace ppmi
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            Random rand = new Random();

            //Just for testing
            //string[] files = { @"text.txt" };

            string[] filesToSelect = { };
                        
            string[] directories = Directory.GetDirectories(Environment.CurrentDirectory + "\\extracted\\").ToArray();

            string[] files = new string[5];

            int fileIndex = 0;

            for(int i = 0; i < 5; i++)
            {
                filesToSelect = Directory.GetFiles(directories[i]).ToArray();

                files[fileIndex] = filesToSelect[rand.Next(0, filesToSelect.Count())];
                fileIndex++;
            }

            int[,] coocurrenceMatrix;
            double[,] ppmiMatrix;

            int windowSize = 4;

            Dictionary<string, string> toeflSuggestions = new Dictionary<string, string>();

            //List<string> elsTestSet = new List<string>();

            DictionaryWithCounts dictionary = new DictionaryWithCounts();
            
            timer.Start();
            dictionary = GenerateDictionary(files);
            dictionary.DictionaryCounts = new List<int>(new int[dictionary.DictionaryWords.Count()]);
            timer.Stop();
            Console.WriteLine("Dictionary Generated in {0}s. Dictionary length: {1}", timer.Elapsed, dictionary.DictionaryCounts.Count);

            timer.Restart();
            coocurrenceMatrix = GenerateCoocurrenceMatrix(dictionary, files, windowSize);
            timer.Stop();
            Console.WriteLine("Coocurrence Matrix Generated in {0}s.", timer.Elapsed);

            timer.Restart();
            ppmiMatrix = CalculatePpmiMatrix(coocurrenceMatrix, dictionary.DictionaryCounts);
            timer.Stop();
            Console.WriteLine("PPMI Matrix Generated in {0}s.", timer.Elapsed);

            /*
             * Bardzo długo to trwa (nie da się za bardzo zoptymalizować),
             * zresztą nie ma sensu, jeśli potrzebujemy te wartości tylko dla słowa x
             * i 4 jego ewentualnych synonimów
            timer.Restart();
            cosineSimilarityMatrix = CalculateCosineSimilarityMatrix(ppmiMatrix);
            timer.Stop();
            Console.WriteLine("Cosine Similarity Matrix Generated in {0}s.", timer.Elapsed);
            */

            timer.Restart();
            TestToeflExamples(dictionary, ppmiMatrix);
            timer.Stop();
            Console.WriteLine("Results for TOEFL examples Generated in {0}s.", timer.Elapsed);
        }

        static DictionaryWithCounts GenerateDictionary(string[] files)
        {
            Regex rgx = new Regex("[0-9$*+,:=?/\\[\\]@#|'<>.^\"*()%!-]");
            DictionaryWithCounts tempDictionary = new DictionaryWithCounts();
            foreach (var file in files)
            {
                if (File.Exists(file))
                    using (StreamReader sr = new StreamReader(file))
                        while (sr.Peek() >= 0)
                            foreach (var word in sr.ReadLine().Split(' '))
                            {
                                string currentWord = rgx.Replace(word, "").ToLower();

                                if (!tempDictionary.DictionaryWords.Contains(currentWord) && currentWord.Length > 0)
                                {
                                    tempDictionary.DictionaryWords.Add(currentWord);
                                }
                            }
                Console.WriteLine(file);
            }
                
            return tempDictionary;
        }

        static int[,] GenerateCoocurrenceMatrix(DictionaryWithCounts d, string[] files, int windowSize)
        {
            int[,] tempMatrix = new int[d.DictionaryWords.Count, d.DictionaryWords.Count];

            Queue<string> windowWords = new Queue<string>();
            Regex rgx = new Regex("[0-9$*+,:=?/\\[\\]@#|'<>.^\"*()%!-]");
            foreach (var file in files)
                if (File.Exists(file))
                    using (StreamReader sr = new StreamReader(file))
                        while (sr.Peek() >= 0)
                            foreach (var word in sr.ReadLine().Split(' '))
                            {
                                string currentWord = rgx.Replace(word, "").ToLower();
                                    if(d.DictionaryWords.Contains(currentWord)) {
                                        foreach (var previousWord in windowWords)
                                        {
                                            tempMatrix[d.DictionaryWords.IndexOf(currentWord), d.DictionaryWords.IndexOf(previousWord)] += 1;
                                            tempMatrix[d.DictionaryWords.IndexOf(previousWord), d.DictionaryWords.IndexOf(currentWord)] += 1;

                                            d.DictionaryCounts[d.DictionaryWords.IndexOf(currentWord)]++;
                                            d.DictionaryCounts[d.DictionaryWords.IndexOf(previousWord)]++;

                                        }
                                    windowWords.Enqueue(currentWord);
                                    if (windowWords.Count > windowSize)
                                        windowWords.Dequeue();
                                }
                            }
            return tempMatrix;
        }

        static double[,] CalculatePpmiMatrix(int[,] coocMatrix, List<int> dictionaryCounts)
        {
            double[,] tempPpmiMatrix = new double[coocMatrix.GetLength(0), coocMatrix.GetLength(1)];
            int denominator = GetDenominator(coocMatrix);
            int iSum = 0;
            int jSum = 0;

            for (int i = 0; i < coocMatrix.GetLength(0); i++)
            {
                iSum = dictionaryCounts[i];

                for (int j = 0; j < coocMatrix.GetLength(1); j++)
                {
                    jSum = dictionaryCounts[j];
                    tempPpmiMatrix[i, j] = CalculatePpmi(coocMatrix[i,j], iSum, jSum, denominator);
                }
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

        static double CalculatePpmi(int x, int i, int j, int denominator)
        {
            double p, p1, p2, p3;

            p1 = Convert.ToDouble(x) / denominator;
            p2 = Convert.ToDouble(i) / denominator;
            p3 = Convert.ToDouble(j) / denominator;

            p = Math.Log((p1 / (p2 * p3)), 2);
            return (p < 0 || Double.IsNaN(p)) ? 0 : p;
        }

        static double CalculateCosineSimilarity(double[,] ppmiMatrix, int i, int j)
        {
            /* Bezsensowne nazwy zmiennych, ale nie mam pojęcia jak to lepiej nazwać nazwać
             * W skrócie: sumOfProducts to suma iloczynów wszystkich dwóch wektorów,
             * sumOfSquaredFirstRow to suma kwadratów wszystkich elementów pierwszego wektora (i)
             * sumOfSquaredSecondRow to suma kwadratów wszystkich elementów drugiego wektora (j)
             * Przemyślę to i zmienię ;)
             */
            double sumOfProducts = 0;
            double sumOfSquaredFirstRow = 0;
            double sumOfSquaredSecondRow = 0;
            double cosineSimilarity = 0;

            for (int x = 0; x < ppmiMatrix.GetLength(0); x++)
            {
                sumOfProducts += ppmiMatrix[i, x] * ppmiMatrix[j, x];
                sumOfSquaredFirstRow += Math.Pow(ppmiMatrix[i, x], 2);
                sumOfSquaredSecondRow += Math.Pow(ppmiMatrix[j, x], 2);
            }

            cosineSimilarity = sumOfProducts / (Math.Sqrt(sumOfSquaredFirstRow) * Math.Sqrt(sumOfSquaredSecondRow));

            return cosineSimilarity;
        }

        static void ShowEslResults(List<string> words, List<string> suggestions, List<string> correctAnswers, Dictionary<string, string> results)
        {
            int correctAnswersCounter = 0;



            Dictionary<string, string> QASet = BuildQASet(words, correctAnswers, suggestions);

            for (int i = 0; i < 80; i++)
            {
                Console.WriteLine("SLOWO: " + words[i] + ", TYP: " + results[words[i]] + ", POPRAWNIE: " + QASet[words[i]]);
                if (results[words[i]] == QASet[words[i]])
                {
                    Console.WriteLine("DOBRZE");
                    correctAnswersCounter++;
                }
                else
                {
                    Console.WriteLine("ŹLE");
                }

            }
            Console.WriteLine("Poprawnych odpowiedzi: " + correctAnswersCounter + "/80.");
        }

        static Dictionary<string, string> BuildQASet(List<string> words, List <string> correctAnswers, List<string> suggestions)
        {
            int addition = 0, extraAddition = 0;
            string correctAnswer = "";

            Dictionary<string, string> WordWithAnswerLetter = new Dictionary<string, string>();
            for (int i = 0; i < 80; i++)
            {
                correctAnswer = correctAnswers[i];
                switch (correctAnswer)
                {
                    case "a":
                        extraAddition = 0;
                        break;
                    case "b":
                        extraAddition = 1;
                        break;
                    case "c":
                        extraAddition = 2;
                        break;
                    case "d":
                        extraAddition = 3;
                        break;
                    default:
                        extraAddition = 0;
                        break;
                }

                WordWithAnswerLetter.Add(words[i], suggestions[addition + extraAddition]);
                addition += 4;
            }

            return WordWithAnswerLetter;
        }

        //TODO
        static double TestEslExamples()
        {
            throw new NotImplementedException();
        }

        static void TestToeflExamples(DictionaryWithCounts dictionary, double[,] ppmiMatrix)
        {
            List<string> toeflTestSetWord = new List<string>();
            List<string> toeflTestSetSynonymSuggestion = new List<string>();
            List<string> toeflTestSetSynonymCorrect = new List<string>();
            Dictionary<string, string> Results = new Dictionary<string, string>();

            string temp = "";

            //Wczytanie z pliku z danymi
            if (File.Exists("toeflTest.set"))
                using (StreamReader sr = new StreamReader("toeflTest.set"))
                    while (sr.Peek() >= 0)
                    {
                        for(int i=0; i<80; i++)
                        {
                            if (toeflTestSetWord.Count < 9)
                                toeflTestSetWord.Add(sr.ReadLine().Remove(0, 3));
                            else
                                toeflTestSetWord.Add(sr.ReadLine().Remove(0, 4));
                            for (int j=0; j<4; j++)
                            {
                                toeflTestSetSynonymSuggestion.Add(sr.ReadLine().Remove(0,3));
                            }
                            sr.ReadLine();
                        }

                        for(int i=0; i<80; i++)
                        {
                            temp = sr.ReadLine();
                            toeflTestSetSynonymCorrect.Add(temp.Substring(temp.Length - 1,1));
                            sr.ReadLine();
                        }
                    }

            int startCounterForCurrentWord = 0;
            int endCounterForCurrentWord = 4;
            double newCosineSimilarity = 0.0;

            //Szukanie najtrafniejszego synonimu
            foreach (var word in toeflTestSetWord)
            {
                string tempSynonym = "";
                double tempCosineSimilarity = 0.0;
                for(int i = startCounterForCurrentWord; i < endCounterForCurrentWord; i++)
                {
                    if(dictionary.DictionaryWords.Contains(word) && dictionary.DictionaryWords.Contains(toeflTestSetSynonymSuggestion[i]))
                    {
                        newCosineSimilarity = CalculateCosineSimilarity(ppmiMatrix, dictionary.DictionaryWords.IndexOf(word), dictionary.DictionaryWords.IndexOf(toeflTestSetSynonymSuggestion[i]));
                        if(newCosineSimilarity > tempCosineSimilarity)
                        {
                            tempSynonym = dictionary.DictionaryWords[dictionary.DictionaryWords.IndexOf(toeflTestSetSynonymSuggestion[i])];
                            tempCosineSimilarity = ppmiMatrix[dictionary.DictionaryWords.IndexOf(word), dictionary.DictionaryWords.IndexOf(toeflTestSetSynonymSuggestion[i])];
                        }
                    }
                }
                startCounterForCurrentWord += 4;
                endCounterForCurrentWord += 4;
                Results.Add(word, tempSynonym);
            }

            ShowEslResults(toeflTestSetWord, toeflTestSetSynonymSuggestion, toeflTestSetSynonymCorrect, Results);
        }
    }
}
