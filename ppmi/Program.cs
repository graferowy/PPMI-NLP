using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ppmi
{
    class Program
    {
        static void Main(string[] args)
        {
            //Kluczowe parametry
            int windowSize = 4;
            int numberOfCorporaFiles = 25;

            Stopwatch timer = new Stopwatch();

            Random rand = new Random();

            SparseMatrix coocurrenceMatrix;
            SparseMatrix ppmiMatrix;

            DictionaryWithCounts dictionary = new DictionaryWithCounts();

            string[] files = { };

            try
            {
                string[] filesToSelect = { };
                string[] directories = Directory.GetDirectories(Environment.CurrentDirectory + "\\extracted\\").ToArray();
                files = new string[numberOfCorporaFiles];
                string tempFileString = "";

                int fileIndex = 0;
                for (int i = 0; i < numberOfCorporaFiles; i++)
                {
                    filesToSelect = Directory.GetFiles(directories[rand.Next(0, directories.Count())]).ToArray();

                    do
                    {
                        tempFileString = filesToSelect[rand.Next(0, filesToSelect.Count())];
                    }
                    while (Array.IndexOf(files, tempFileString) > -1);

                    files[fileIndex] = tempFileString;

                    fileIndex++;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            
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

            timer.Restart();
            TestToeflExamples(dictionary, ppmiMatrix);
            timer.Stop();
            Console.WriteLine("Results for TOEFL examples Generated in {0}s.", timer.Elapsed);

            timer.Restart();
            TestEslExamples(dictionary, ppmiMatrix);
            timer.Stop();
            Console.WriteLine("Results for ESL examples Generated in {0}s.", timer.Elapsed);
        }

        static DictionaryWithCounts GenerateDictionary(string[] files)
        {
            int wordCounter = 0;
            Regex rgx = new Regex("[0-9$*+,:=?/\\[\\]@#|'<>.^\"*()%!-]");
            DictionaryWithCounts tempDictionary = new DictionaryWithCounts();
            try
            {
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
                    wordCounter++;
                    Console.WriteLine(file + " " + wordCounter);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return tempDictionary;
        }
        
        static SparseMatrix GenerateCoocurrenceMatrix(DictionaryWithCounts d, string[] files, int windowSize)
        {
            SparseMatrix tempMatrix = SparseMatrix.Create(d.DictionaryWords.Count, d.DictionaryWords.Count, 0);
            Queue<string> windowWords = new Queue<string>();
            Regex rgx = new Regex("[0-9$*+,:=?/\\[\\]@#|'<>.^\"*()%!-]");
            var currentWordIndex = 0;
            var previousWordIndex = 0;
            string currentWord = "";
            try
            {
                foreach (var file in files)
                {
                    if (File.Exists(file))
                        using (StreamReader sr = new StreamReader(file))
                            while (sr.Peek() >= 0)
                                foreach (var word in sr.ReadLine().Split(' '))
                                {
                                    currentWord = rgx.Replace(word, "").ToLower();
                                    if (d.DictionaryWords.Contains(currentWord))
                                    {
                                        foreach (var previousWord in windowWords)
                                        {
                                            currentWordIndex = d.DictionaryWords.IndexOf(currentWord);
                                            previousWordIndex = d.DictionaryWords.IndexOf(previousWord);
                                            tempMatrix[currentWordIndex, previousWordIndex] += 1;
                                            tempMatrix[currentWordIndex, previousWordIndex] += 1;

                                            d.DictionaryCounts[currentWordIndex]++;
                                            d.DictionaryCounts[previousWordIndex]++;
                                        }
                                        windowWords.Enqueue(currentWord);
                                        if (windowWords.Count > windowSize)
                                            windowWords.Dequeue();
                                    }
                                }
                    Console.WriteLine(file);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return tempMatrix;
        }

        static SparseMatrix CalculatePpmiMatrix(SparseMatrix coocMatrix, List<int> dictionaryCounts)
        {
            SparseMatrix A = SparseMatrix.Create(coocMatrix.ColumnCount, coocMatrix.ColumnCount, 0);

            double denominator = GetDenominator(coocMatrix);
            int iSum = 0;
            int jSum = 0;

            for (int i = 0; i < coocMatrix.ColumnCount; i++)
            {
                iSum = dictionaryCounts[i];

                for (int j = 0; j < coocMatrix.ColumnCount; j++)
                {
                    jSum = dictionaryCounts[j];
                    if(coocMatrix[i, j] != 0)
                    {
                        A[i, j] = CalculatePpmi(coocMatrix[i, j], iSum, jSum, denominator);
                    }
                }
            }
            return A;
        }

        static double GetDenominator(SparseMatrix matrix)
        {
            double result = 0;
            for (int i = 0; i < matrix.ColumnCount; i++)
                for (int j = 0; j < matrix.ColumnCount; j++)
                    result += matrix[i,j];
            return result;
        }

        static double CalculatePpmi(double x, int i, int j, double denominator)
        {
            double p, p1, p2, p3;

            p1 = x / denominator;
            p2 = Convert.ToDouble(i) / denominator;
            p3 = Convert.ToDouble(j) / denominator;

            p = Math.Log((p1 / (p2 * p3)), 2);
            return (p < 0 || Double.IsNaN(p)) ? 0 : p;
        }

        static double CalculateCosineSimilarity(SparseMatrix ppmiMatrix, int i, int j)
        {
            double sumOfProducts = 0;
            double sumOfSquaredFirstRow = 0;
            double sumOfSquaredSecondRow = 0;
            double cosineSimilarity = 0;

            for (int x = 0; x < ppmiMatrix.ColumnCount; x++)
            {
                sumOfProducts += ppmiMatrix[i,x] * ppmiMatrix[j,x];
                sumOfSquaredFirstRow += Math.Pow(ppmiMatrix[i,x], 2);
                sumOfSquaredSecondRow += Math.Pow(ppmiMatrix[j,x], 2);
            }

            cosineSimilarity = sumOfProducts / (Math.Sqrt(sumOfSquaredFirstRow) * Math.Sqrt(sumOfSquaredSecondRow));

            return cosineSimilarity;
        }

        static void ShowResults(List<string> words, List<string> suggestions, List<string> correctAnswers, int noAnswerCounter, Dictionary<string, string> results)
        {
            int correctAnswersCounter = 0;

            Dictionary<string, string> QASet = BuildQASet(words, correctAnswers, suggestions);

            for (int i = 0; i < words.Count; i++)
            {
                if (results[words[i]] == QASet[words[i]])
                {
                    Console.WriteLine("SLOWO: " + words[i] + ", TYP: " + results[words[i]] + ", POPRAWNIE: " + QASet[words[i]] + " +");
                    correctAnswersCounter++;
                }
                else
                {
                    Console.WriteLine("SLOWO: " + words[i] + ", TYP: " + results[words[i]] + ", POPRAWNIE: " + QASet[words[i]] + " -");
                }
            }
            Console.WriteLine("Poprawnych odpowiedzi: " + correctAnswersCounter + "/" + words.Count + ".");
            Console.WriteLine("W bazie nie znaleziono odpowiedzi na: " + noAnswerCounter + " zapytań");
        }

        static Dictionary<string, string> BuildQASet(List<string> words, List <string> correctAnswers, List<string> suggestions)
        {
            int addition = 0, extraAddition = 0;
            string correctAnswer = "";

            Dictionary<string, string> WordWithAnswerLetter = new Dictionary<string, string>();
            for (int i = 0; i < words.Count; i++)
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

        static void TestEslExamples(DictionaryWithCounts dictionary, SparseMatrix ppmiMatrix)
        {
            List<string> eslTestSetWord = new List<string>();
            List<string> eslTestSetSynonymSuggestion = new List<string>();
            List<string> eslTestSetSynonymCorrect = new List<string>();
            Dictionary<string, string> Results = new Dictionary<string, string>();

            string temp = "";
            string newWord = "";

            int firstCharOccurance;

            int nextCharOccurance;

            try
            {
                //Wczytanie z pliku z danymi
                if (File.Exists("eslTest.set"))
                    using (StreamReader sr = new StreamReader("eslTest.set"))
                        while (sr.Peek() >= 0)
                        {
                            temp = sr.ReadLine();

                            eslTestSetWord.Add(temp.Split('[', ']')[1]);

                            for (int i = 0; i < 3; i++)
                            {
                                firstCharOccurance = temp.IndexOf("|");
                                temp = temp.Substring(firstCharOccurance + 1);
                                nextCharOccurance = temp.IndexOf("|");
                                newWord = temp.Substring(0, nextCharOccurance);
                                eslTestSetSynonymSuggestion.Add(newWord);
                                temp = temp.Remove(0, newWord.Length);
                            }

                            firstCharOccurance = temp.IndexOf("|");
                            temp = temp.Substring(firstCharOccurance + 1);
                            nextCharOccurance = temp.IndexOf("\":");
                            newWord = temp.Substring(0, nextCharOccurance);
                            eslTestSetSynonymSuggestion.Add(newWord);
                            temp = temp.Remove(0, newWord.Length + 2);
                            eslTestSetSynonymCorrect.Add(temp);
                        }
            }
            
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            int startCounterForCurrentWord = 0;
            int endCounterForCurrentWord = 4;
            double newCosineSimilarity = 0.0;
            int noAnswerCounter = 0;

            //Szukanie najtrafniejszego synonimu
            foreach (var word in eslTestSetWord)
            {
                string tempSynonym = "";
                double tempCosineSimilarity = 0.0;

                for (int i = startCounterForCurrentWord; i < endCounterForCurrentWord; i++)
                {
                    if (dictionary.DictionaryWords.Contains(word) && dictionary.DictionaryWords.Contains(eslTestSetSynonymSuggestion[i]))
                    {
                        newCosineSimilarity = CalculateCosineSimilarity(ppmiMatrix, dictionary.DictionaryWords.IndexOf(word), dictionary.DictionaryWords.IndexOf(eslTestSetSynonymSuggestion[i]));
                        if (newCosineSimilarity > tempCosineSimilarity)
                        {
                            tempSynonym = dictionary.DictionaryWords[dictionary.DictionaryWords.IndexOf(eslTestSetSynonymSuggestion[i])];
                            tempCosineSimilarity = newCosineSimilarity;
                        }
                    }
                }
                if (tempSynonym == "")
                {
                    noAnswerCounter++;
                }

                startCounterForCurrentWord += 4;
                endCounterForCurrentWord += 4;
                Results.Add(word, tempSynonym);
            }
            ShowResults(eslTestSetWord, eslTestSetSynonymSuggestion, eslTestSetSynonymCorrect, noAnswerCounter, Results);
        }

        static void TestToeflExamples(DictionaryWithCounts dictionary, SparseMatrix ppmiMatrix)
        {
            List<string> toeflTestSetWord = new List<string>();
            List<string> toeflTestSetSynonymSuggestion = new List<string>();
            List<string> toeflTestSetSynonymCorrect = new List<string>();
            Dictionary<string, string> Results = new Dictionary<string, string>();

            string temp = "";

            try
            {
                //Wczytanie z pliku z danymi
                if (File.Exists("toeflTest.set"))
                    using (StreamReader sr = new StreamReader("toeflTest.set"))
                        while (sr.Peek() >= 0)
                        {
                            for (int i = 0; i < 80; i++)
                            {
                                if (toeflTestSetWord.Count < 9)
                                    toeflTestSetWord.Add(sr.ReadLine().Remove(0, 3));
                                else
                                    toeflTestSetWord.Add(sr.ReadLine().Remove(0, 4));
                                for (int j = 0; j < 4; j++)
                                {
                                    toeflTestSetSynonymSuggestion.Add(sr.ReadLine().Remove(0, 3));
                                }
                                sr.ReadLine();
                            }

                            for (int i = 0; i < 80; i++)
                            {
                                temp = sr.ReadLine();
                                toeflTestSetSynonymCorrect.Add(temp.Substring(temp.Length - 1, 1));
                                sr.ReadLine();
                            }
                        }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            int startCounterForCurrentWord = 0;
            int endCounterForCurrentWord = 4;
            double newCosineSimilarity = 0.0;
            int noAnswerCounter = 0;

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
                            tempCosineSimilarity = newCosineSimilarity;
                        }
                    }
                }
                if (tempSynonym == "")
                {
                    noAnswerCounter++;
                }

                startCounterForCurrentWord += 4;
                endCounterForCurrentWord += 4;
                Results.Add(word, tempSynonym);
            }
            ShowResults(toeflTestSetWord, toeflTestSetSynonymSuggestion, toeflTestSetSynonymCorrect, noAnswerCounter, Results);
        }
    }
}
