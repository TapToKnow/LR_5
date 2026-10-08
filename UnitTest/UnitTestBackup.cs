using LR__1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace UnitTest
{
    // Модульные тесты резервного копирования коллекции (ЛР №5)
    [TestClass]
    public class UnitTestBackup
    {
        private string folder;

        [TestInitialize]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "MusicCollectionBackups_" + Guid.NewGuid().ToString("N"));
        }

        [TestCleanup]
        public void TearDown()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private static MusicCollection CreateCollection()
        {
            var collection = new MusicCollection();
            collection.AddTrack(new MusicTrack("Ария", "Штиль", "Рок", "1997"));
            collection.AddTrack(new MusicTrack("Кипелов", "Вавилон", "Метал", "2004"));
            return collection;
        }

        [TestMethod]
        public void CreateBackup_CreatesFolderAndJsonFile()
        {
            string path = CreateCollection().CreateBackup(folder);

            Assert.IsTrue(Directory.Exists(folder));
            Assert.IsTrue(File.Exists(path));
            StringAssert.StartsWith(Path.GetFileName(path), "backup_");
            Assert.AreEqual(".json", Path.GetExtension(path));
        }

        [TestMethod]
        public void CreateBackup_Twice_CreatesTwoDifferentFiles()
        {
            var collection = CreateCollection();

            string first = collection.CreateBackup(folder);
            string second = collection.CreateBackup(folder);

            Assert.AreNotEqual(first, second);
            Assert.AreEqual(2, MusicCollection.GetBackups(folder).Count);
        }

        [TestMethod]
        public void RestoreLatestBackup_RestoresCollectionAfterChanges()
        {
            var collection = CreateCollection();
            collection.CreateBackup(folder);
            collection.AddTrack(new MusicTrack("Singer", "Song", "Style", "2020"));
            collection.RemoveTrack(new MusicTrack("Ария", "Штиль", "Рок", "1997"));

            int restored = collection.RestoreLatestBackup(folder);

            Assert.AreEqual(2, restored);
            Assert.AreEqual(2, collection.Tracks.Count);
            Assert.AreEqual("Ария", collection.Tracks[0].Artist);
            Assert.AreEqual("Кипелов", collection.Tracks[1].Artist);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void RestoreLatestBackup_NoBackups_ThrowsFileNotFoundException()
        {
            CreateCollection().RestoreLatestBackup(folder);
        }

        [TestMethod]
        public void GetBackups_FolderDoesNotExist_ReturnsEmptyList()
        {
            Assert.AreEqual(0, MusicCollection.GetBackups(folder).Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateBackup_EmptyFolder_ThrowsArgumentException()
        {
            CreateCollection().CreateBackup(" ");
        }
    }
}
