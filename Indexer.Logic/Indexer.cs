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

        private ITextParser _textParser;
        private Dictionary<String, List<String>> _reversIndex;
        private List<String> _registeredFiles;
        private List<String> _registeredFolders; 

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="textParser">заданный разборщик слов в файле</param>
        public Indexer(ITextParser textParser)
        {
            _reversIndex = new Dictionary<string, List<string>>();
            _textParser = textParser;
            _registeredFiles = new List<string>();
            _registeredFolders = new List<string>();
            _syncMutex = new Mutex();
            _updateSyncMutex = new Mutex();
        }

        private void CreateDirectoryWatcher(String directoryPath)
        {
            var watcher = CreateWatcher();
            watcher.Path = directoryPath;
            watcher.EnableRaisingEvents = true;
        }

        private void CreateFileWatcher(String filePath)
        {
            var watcher = CreateWatcher();
            watcher.Path = Path.GetDirectoryName(filePath);
            watcher.Filter = Path.GetFileName(filePath);
            watcher.EnableRaisingEvents = true;
        }

        private FileSystemWatcher CreateWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.NotifyFilter = NotifyFilters.LastWrite
           | NotifyFilters.FileName | NotifyFilters.DirectoryName; ;

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
                Debug.WriteLine("Changed "+path);
            };
            watcher.Created += (s, e) =>
            {
                var path = e.FullPath;
                var isFile = Path.GetFileName(path) != null;
                if (isFile)
                    AddFileToIndex(path);
                else
                {
                    var files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        AddFileToIndex(file);
                    }
                }
                Debug.WriteLine("Created " + path);
            };
            watcher.Deleted += (s, e) =>
            {
                var path = e.FullPath;
                var isFile = Path.GetFileName(path) != null;
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
            return watcher;
        }

        /// <summary>
        /// Добавляет файл в индекс
        /// </summary>
        /// <param name="pathToFile">путь к файлу</param>
        public void AddFile(String pathToFile)
        {
            if (!File.Exists(pathToFile))
                throw new ArgumentException("Файла не существует!");
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
            CreateDirectoryWatcher(pathToDirectory);
            _registeredFolders.Add(pathToDirectory);
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
            _syncMutex.WaitOne();
            _updateSyncMutex.WaitOne();
            List<String> findedFiles;
            _reversIndex.TryGetValue(searchString, out findedFiles);
            if (findedFiles == null)
            {
                findedFiles = new List<String>();
            }
            _updateSyncMutex.ReleaseMutex();
            _syncMutex.ReleaseMutex();
            return findedFiles;
        }

        private void RemoveFileFromIndex(String filePath)
        {
            _syncMutex.WaitOne();
            _registeredFiles.Remove(filePath);
            foreach (var index in _reversIndex)
            {
                index.Value.Remove(filePath);
            }
            _syncMutex.ReleaseMutex();
        }

        private void UpdateFileInIndex(String filePath)
        {
            _updateSyncMutex.WaitOne();
            RemoveFileFromIndex(filePath);
            AddFileToIndex(filePath);
            _updateSyncMutex.ReleaseMutex();
        }

        private void AddFileToIndex(String filePath)
        {
            _syncMutex.WaitOne();
            _registeredFiles.Add(filePath);

            var words = _textParser.GetWordList(filePath);
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

        public bool ContainsAnyFile()
        {
            return _registeredFiles.Count > 0;
        }

        public List<String> GetAddedFiles()
        {
            return _registeredFiles.ToList();
        }

        public List<String> GetAddedCatalogs()
        {
            return _registeredFolders.ToList();
        } 
    }
}
