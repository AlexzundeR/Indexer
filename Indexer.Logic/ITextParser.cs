using System;
using System.Collections.Generic;
using System.IO;

namespace Index.Logic
{
    /// <summary>
    /// Сущность, которая парсит входящий текст на слова.
    /// </summary>
    public interface ITextParser
    {
        /// <summary>
        /// Парсит текст на слова
        /// </summary>
        /// <param name="text">Путь к файлу с текстом</param>
        /// <returns>Список слов</returns>
        List<String> GetWordList(String text);
    }
}
