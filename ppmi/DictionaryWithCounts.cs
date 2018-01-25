using System;
using System.Collections.Generic;
using System.Text;

namespace ppmi
{
    class DictionaryWithCounts
    {
        public List<string> DictionaryWords { get; set; }
        public List<int> DictionaryCounts { get; set; }

        public DictionaryWithCounts()
        {
            DictionaryWords = new List<string>();
            DictionaryCounts = new List<int>();
        }
    }
}
