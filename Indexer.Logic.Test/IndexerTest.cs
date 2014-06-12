using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Index.Logic.Test
{
    [TestFixture]
    public class IndexerTest
    {
        [Test]
        public void SimpleByWordParserTest()
        {
            SimpleByWordParser parser = new SimpleByWordParser();
            var words = parser.GetWordList("Abc aaa d.");

            Assert.AreEqual("Abc", words[0]);
            Assert.AreEqual("aaa", words[1]);
            Assert.AreEqual("d", words[2]);
        }
        [Test]
        public void MainTest()
        {
            SimpleByWordParser parser = new SimpleByWordParser();
            Indexer indexer = new Indexer(parser);

            var testFilesDirectory = @"D:\MyProjects\Tests";
            var testFileName = @"text.txt";
            CreateTestFile(testFilesDirectory,testFileName);
            indexer.AddFile(testFilesDirectory+"\\"+testFileName);


            for (int i = 0; i < 2; i++)
            {
                var newDirectory = testFilesDirectory + "\\" + i + "Test";
                Directory.CreateDirectory(newDirectory);
                for (int j = 0; j < 5; j++)
                {
                    CreateTestFile(newDirectory, j+testFileName);    
                }
                
                indexer.AddDirectory(newDirectory);
            }

            var findedFiles = indexer.Find("AAA");
            Assert.IsTrue(findedFiles.Count==11);

        }
        [Test]
        public void WatcherTest()
        {
            SimpleByWordParser parser = new SimpleByWordParser();
            Indexer indexer = new Indexer(parser);

            var testFilesDirectory = @"D:\MyProjects\Tests";
            var testFileName = @"text.txt";
            CreateTestFile(testFilesDirectory, testFileName);
            indexer.AddFile(testFilesDirectory + "\\" + testFileName);

            var findedFiles = indexer.Find("AAA");
            Assert.AreEqual(1,findedFiles.Count);

            File.Delete(testFilesDirectory+"\\"+testFileName);
            Thread.Sleep(1000);
            findedFiles = indexer.Find("AAA");
            Assert.AreEqual(0, findedFiles.Count);

            indexer.AddDirectory(testFilesDirectory);
            using (var a = File.CreateText(testFilesDirectory + "\\1" + testFileName))
            {
                a.Write("WWW");
            }
            Thread.Sleep(1000);
            findedFiles = indexer.Find("WWW");
            Assert.AreEqual(1, findedFiles.Count);


            var nextFolder = testFilesDirectory + "\\" + "tst";
            Directory.CreateDirectory(nextFolder);
            using (var a = File.CreateText(nextFolder + "\\" + testFileName))
            {
                a.Write("GGG");
            }
            indexer.AddFile(nextFolder + "\\" + testFileName);
            using (var a = File.CreateText(nextFolder + "\\" + testFileName))
            {
                a.Write("ZZZ");
            }
            Thread.Sleep(2000);
            findedFiles = indexer.Find("ZZZ");
            Assert.AreEqual(1, findedFiles.Count);
        }

        private void CreateTestFile(String dirName, String fileName)
        {
            Directory.CreateDirectory(dirName);
            using (var file = File.CreateText(dirName + "\\" + fileName))
            {
                file.Write("AAA BBB CCC DDD");
            }
        }
    }
}
