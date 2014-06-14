using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Index.Logic;
namespace Index.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteHelp();
            Boolean exit = false;
            //Созаем парсер
            var textParser = new SimpleByWordParser();
            //Создаем индексатор на основе простого парсера
            var indexer = new Indexer(textParser);
            while (!exit)
            {
                var res = Console.ReadLine() ?? "";
                exit = res.ToLowerInvariant().Equals("q");
                try
                {
                    if (!exit)
                    {
                        if (res.StartsWith("addc"))
                        {
                            var directoryPath = res.Remove(0, 4).Trim();
                            indexer.AddDirectory(directoryPath);
                        }
                        else if (res.StartsWith("add"))
                        {
                            var filePath = res.Remove(0, 3).Trim();
                            indexer.AddFile(filePath);
                        }
                        else if (res.StartsWith("?"))
                        {
                            var quest = res.Remove(0, 1).Trim();
                            var result = indexer.Find(quest);
                            if (result.Count == 0)
                            {
                                Console.WriteLine("В коллекции нет файлов!");
                            }
                            else
                            {
                                foreach (var finddFiles in result)
                                {
                                    Console.WriteLine(finddFiles);
                                }
                            }
                        }
                        else if (res.StartsWith("files"))
                        {
                            var files = indexer.GetAddedFiles();
                            foreach (var file in files)
                            {
                                Console.WriteLine(file);
                            }
                        }
                        else if (res.StartsWith("cats"))
                        {
                            var cats = indexer.GetAddedCatalogs();
                            foreach (var cat in cats)
                            {
                                Console.WriteLine(cat);
                            }
                        }
                        else if (res.StartsWith("help"))
                        {
                            WriteHelp();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void WriteHelp()
        {
            Console.WriteLine("add [путь к файлу] - добавить файл");
            Console.WriteLine("addc [путь к каталогу] - добавить каталог");
            Console.WriteLine("files - выведет список добавленных файлов (с учетом файлов внутри каталогов)");
            Console.WriteLine("cats - выведет список добавленных каталогов");
            Console.WriteLine("? [слово] - показать в каких файлах встречается это слово");
            Console.WriteLine("q - выход");
            Console.WriteLine("help - выведет подсказку");
        }
    }
}
