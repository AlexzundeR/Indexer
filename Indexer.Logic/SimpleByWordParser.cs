using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Index.Logic
{
    /// <summary>
    /// Парсер, который ищет с помощью регулярки слова в файле, если он является текстовым
    /// </summary>
    public sealed class SimpleByWordParser : ITextParser
    {
        private readonly Regex _byWordParseRegex;
        public SimpleByWordParser()
        {
            _byWordParseRegex = new Regex(@"\w+");
        }
        /// <summary>
        /// Интерфейсный метод, который вытаскивает из файла текстовое содержание и из текта вытягивает слова
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public List<string> GetWordList(string filePath)
        {
            //Получаем текст
            var text = GetText(filePath);
            //Пробегаемся регуляркой и ищем слова
            var matchCollection = _byWordParseRegex.Matches(text);
            var words = new List<String>();
            foreach (Match match in matchCollection)
            {
                words.Add(match.Value);
            }
            return words;
        }

        /// <summary>
        /// Парсер потенциально может обрабатывать любые файлы (даже URL),
        /// Этот конкретный парсер может кушать только txt файлы
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string GetText(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            if (ext != null && ext.Equals(".txt"))
                return File.ReadAllText(filePath, Encoding.UTF8);
            else
                return "";
        }

    }
}
