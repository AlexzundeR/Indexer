using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Index.Logic
{
    public sealed class Indexer
    {
        private Mutex _syncMutex;
        private Mutex _updateSyncMutex;


        private ITextParser _textParser;    //Парсер файлов, ему передаются фалы на обработку для получения списка ключевых слов.
        private Dictionary<String, List<String>> _reversIndex;      //Обратный индекс. На каждое ключевое слово хранит список файлов, в котором оно содержится
        private List<String> _registeredFiles;      //Список зарегестрированных для мониторинга и индексации файлов
        private List<String> _registeredDirectories;    //Список зарегестрированных для мониторинга каталогов

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="textParser">заданный разборщик слов в файле</param>
        public Indexer(ITextParser textParser)
        {
            _reversIndex = new Dictionary<string, List<string>>();
            _textParser = textParser;
            _registeredFiles = new List<string>();
            _registeredDirectories = new List<string>();
            _syncMutex = new Mutex();
            _updateSyncMutex = new Mutex();
        }
        /// <summary>
        /// Есть ли на данный момент зарегестрированные фалы
        /// </summary>
        /// <returns></returns>
        public bool ContainsAnyFile()
        {
            return _registeredFiles.Count > 0;
        }
        /// <summary>
        /// Возвращает список всех зарегестрированных файлов
        /// </summary>
        /// <returns></returns>
        public List<String> GetAddedFiles()
        {
            return _registeredFiles.ToList();
        }
        /// <summary>
        /// Возвращает список зарегестрированных каталогов
        /// </summary>
        /// <returns></returns>
        public List<String> GetAddedCatalogs()
        {
            return _registeredDirectories.ToList();
        }

        /// <summary>
        /// Добавляет файл в индекс
        /// </summary>
        /// <param name="pathToFile">путь к файлу</param>
        public void AddFile(String pathToFile)
        {
            if (!File.Exists(pathToFile))
                throw new ArgumentException("Файла не существует!");
            //Если файл существует, добавляем его в индекс и начинаем за ним следить
            CreateFileWatcher(pathToFile);
            AddFileToIndex(pathToFile);
        }

        /// <summary>
        /// Добавляет каталог в индекс
        /// </summary>
        /// <param name="pathToDirectory">путь к каталогу</param>
        public void AddDirectory(String pathToDirectory)
        {
            if (!Directory.Exists(pathToDirectory))
                throw new ArgumentException("Каталог не существует!");
            //Если каталог существует, то начинаем следить за изменениями в каталоге
            CreateDirectoryWatcher(pathToDirectory);
            //Регистрируем
            _registeredDirectories.Add(pathToDirectory);

            //И каждый файл внутри добавляем в индекс со слежкой
            var files = Directory.GetFiles(pathToDirectory);
            foreach (var file in files)
            {
                AddFile(file);
            }
        }

        /// <summary>
        /// Ищет заданную строку в индексе
        /// </summary>
        /// <param name="searchString">Строка поиска</param>
        public List<String> Find(String searchString)
        {
            //Пытаемся получить из обратного индекса список файлов по запросу
            List<String> findedFiles;
            _reversIndex.TryGetValue(searchString, out findedFiles);
            if (findedFiles == null)
            {
                //Если ничего не нашли, вернем пустой список
                findedFiles = new List<String>();
            }
            return findedFiles;
        }
        /// <summary>
        /// Добавляем файл в индекс
        /// </summary>
        /// <param name="filePath"></param>
        private void AddFileToIndex(String filePath)
        {
            //Ожидаем выполнения других операций
            _syncMutex.WaitOne();
            //Регистрируем файл
            _registeredFiles.Add(filePath);
            //От парсера получаем список ключевых слов
            var words = _textParser.GetWordList(filePath);
            //Заполняем индекс
            foreach (var word in words)
            {
                List<String> files;
                if (_reversIndex.TryGetValue(word, out files))
                {
                    files.Add(filePath);
                }
                else
                {
                    _reversIndex.Add(word, new List<string>() { filePath });
                }
            }
            _syncMutex.ReleaseMutex();
        }
        /// <summary>
        /// Переиндексируем файл в индексе
        /// </summary>
        /// <param name="filePath"></param>
        private void UpdateFileInIndex(String filePath)
        {
            //Ожидаем выполнения операции обновления
            _updateSyncMutex.WaitOne();
            //Удаляем старые данные
            RemoveFileFromIndex(filePath);
            //Добавляем новые
            AddFileToIndex(filePath);
            _updateSyncMutex.ReleaseMutex();
        }
        /// <summary>
        /// Удаляем файл из индекса
        /// </summary>
        /// <param name="filePath"></param>
        private void RemoveFileFromIndex(String filePath)
        {
            //Ожидаем выполнения других операций
            _syncMutex.WaitOne();
            //Удаляем из зарегестрированных
            _registeredFiles.Remove(filePath);
            //Удаляем упоминание о файле из индекса
            foreach (var index in _reversIndex)
            {
                index.Value.Remove(filePath);
            }
            //Осаобождаем мьютекс
            _syncMutex.ReleaseMutex();
        }
        /// <summary>
        /// Переименовывает файл в индексе, без переиндексации
        /// </summary>
        /// <param name="newPath"></param>
        /// <param name="oldPath"></param>
        private void RenameFileInIndex(string newPath, string oldPath)
        {
            CreateFileWatcher(newPath);
            _syncMutex.WaitOne();
            //Удаляем старые записи, добавляем новые
            _registeredFiles.Remove(oldPath);
            _registeredFiles.Add(newPath);
            //У каждого ключевого слова
            foreach (var index in _reversIndex)
            {
                if (index.Value.Contains(oldPath))
                {
                    //Старое упомниаение меняем на новое
                    index.Value.Remove(oldPath);
                    index.Value.Add(newPath);
                }
            }
            _syncMutex.ReleaseMutex();
        }

        private void CreateDirectoryWatcher(String directoryPath)
        {
            //Следим за папкой без фильтрации
            var watcher = CreateWatcher(true);
            watcher.Path = directoryPath;
            watcher.EnableRaisingEvents = true;
        }

        private void CreateFileWatcher(String filePath)
        {
            //Следим за файлом с фильтром по его имени
            var watcher = CreateWatcher(false);
            watcher.Path = Path.GetDirectoryName(filePath);
            watcher.Filter = Path.GetFileName(filePath);
            watcher.EnableRaisingEvents = true;
        }
        /// <summary>
        /// Создает следильщика с дефолтными натсройками.
        /// При изменении файла он переиндексируется, при изменении каталога, переиндексируются все вложенные файлы
        /// </summary>
        /// <returns></returns>
        private FileSystemWatcher CreateWatcher(bool forDirectory)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            //Будем следить за записью в файл, за переименованием файла или каталога
            watcher.NotifyFilter = NotifyFilters.LastWrite
           | NotifyFilters.FileName | NotifyFilters.DirectoryName; ;

            

            if (forDirectory)
            {
                watcher.Created += (s, e) =>
                {
                    var path = e.FullPath;
                    var isFile = Path.GetFileName(path) != null;
                    if (isFile)
                        AddFile(path);
                    else
                    {
                        var files = Directory.GetFiles(path);
                        foreach (var file in files)
                        {
                            AddFile(file);
                        }
                    }
                    Debug.WriteLine("Created " + path);
                };

                watcher.Renamed += (s, e) =>
                {
                    var oldPath = e.OldFullPath;
                    var path = e.FullPath;
                    var isFile = Path.GetFileName(path) != null;
                    if (isFile)
                        RenameFileInIndex(path, oldPath);
                    else
                    {
                        var files = Directory.GetFiles(path);
                        foreach (var file in files)
                        {
                            RenameFileInIndex(file, oldPath + "\\" + Path.GetFileName(file));
                        }
                    }
                    Debug.WriteLine("Renamed " + path);
                };
            }
            if (!forDirectory)
            {
                watcher.Deleted += (s, e) =>
                {
                    var path = e.FullPath;
                    var isFile = Path.GetFileName(path) != null;
                    ((FileSystemWatcher)s).EnableRaisingEvents = false;
                    if (isFile)
                        RemoveFileFromIndex(path);
                    else
                    {
                        var files = Directory.GetFiles(path);
                        foreach (var file in files)
                        {
                            RemoveFileFromIndex(file);
                        }
                    }
                    Debug.WriteLine("Deleted " + path);
                };
                
                watcher.Changed += (s, e) =>
                {
                    var path = e.FullPath;
                    var isFile = Path.GetFileName(path) != null;
                    if (isFile)
                        UpdateFileInIndex(path);
                    else
                    {
                        var files = Directory.GetFiles(path);
                        foreach (var file in files)
                        {
                            UpdateFileInIndex(file);
                        }
                    }
                    Debug.WriteLine("Changed " + path);
                };
            }
            
            return watcher;
        }


    }
}
