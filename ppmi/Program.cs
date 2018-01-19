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
            List<string> dictionary = new List<string>();
            dictionary = GenerateDictionary(files);
            Console.WriteLine("Dictionary length: {0}", dictionary.Count);
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
    }
}
