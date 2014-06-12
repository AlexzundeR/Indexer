using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Index.Logic
{
    public sealed class SimpleByWordParser : ITextParser
    {
        private readonly Regex _byWordParseRegex;
        public SimpleByWordParser()
        {
            _byWordParseRegex = new Regex(@"\w+");
        }

        public List<string> GetWordList(string filePath)
        {
            var text = GetText(filePath);

            var matchCollection = _byWordParseRegex.Matches(text);
            var words = new List<String>();
            foreach (Match match in matchCollection)
            {
                words.Add(match.Value);
            }
            return words;
        }

        private string GetText(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            if (ext!=null && ext.Equals(".txt"))
            return File.ReadAllText(filePath);
            else
                throw new ArgumentException("Невозможно обработать файл! Могу обрабатывать только txt файлы");
        }

    }
}
