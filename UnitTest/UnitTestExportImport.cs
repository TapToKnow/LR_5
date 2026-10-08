using LR__1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace UnitTest
{
    // Модульные тесты новой функции «Экспорт и импорт коллекции» (ЛР №5)
    [TestClass]
    public class UnitTestExportImport
    {
        private string folder;

        [TestInitialize]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "MusicCollectionTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private string FilePath(string name) => Path.Combine(folder, name);

        private static MusicCollection CreateCollection()
        {
            var collection = new MusicCollection();
            collection.AddTrack(new MusicTrack("Кипелов", "Вавилон", "Метал", "2004"));
            collection.AddTrack(new MusicTrack("Ария", "Штиль", "Рок", "1997"));
            collection.AddTrack(new MusicTrack("Singer", "Song", "Style", "2020"));
            return collection;
        }

        // ---------- JSON ----------

        [TestMethod]
        public void ExportToJson_CreatesFileWithAllTracks()
        {
            var collection = CreateCollection();
            string path = FilePath("collection.json");

            collection.ExportToJson(path);

            Assert.IsTrue(File.Exists(path));
            string text = File.ReadAllText(path);
            StringAssert.Contains(text, "Кипелов");
            StringAssert.Contains(text, "Штиль");
            StringAssert.Contains(text, "2020");
        }

        [TestMethod]
        public void ImportFromJson_Replace_RestoresSameCollection()
        {
            var source = CreateCollection();
            string path = FilePath("collection.json");
            source.ExportToJson(path);

            var target = new MusicCollection();
            target.AddTrack(new MusicTrack("Другой", "Трек", "Поп", "2010"));
            int added = target.ImportFromJson(path, true);

            Assert.AreEqual(3, added);
            CollectionAssert.AreEqual(source.Tracks.ToList(), target.Tracks.ToList());
        }

        [TestMethod]
        public void ImportFromJson_Append_AddsTracksToCurrent()
        {
            string path = FilePath("collection.json");
            CreateCollection().ExportToJson(path);

            var target = new MusicCollection();
            target.AddTrack(new MusicTrack("Другой", "Трек", "Поп", "2010"));
            int added = target.ImportFromJson(path, false);

            Assert.AreEqual(3, added);
            Assert.AreEqual(4, target.Tracks.Count);
            Assert.AreEqual("Другой", target.Tracks[0].Artist);
        }

        [TestMethod]
        public void ImportFromJson_Append_SkipsDuplicates()
        {
            var collection = CreateCollection();
            string path = FilePath("collection.json");
            collection.ExportToJson(path);

            int added = collection.ImportFromJson(path, false);

            Assert.AreEqual(0, added);
            Assert.AreEqual(3, collection.Tracks.Count);
        }

        [TestMethod]
        public void ExportImport_EmptyCollection_GivesEmptyCollection()
        {
            string path = FilePath("empty.json");
            new MusicCollection().ExportToJson(path);

            var target = CreateCollection();
            int added = target.ImportFromJson(path, true);

            Assert.AreEqual(0, added);
            Assert.AreEqual(0, target.Tracks.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ImportFromJson_FileNotFound_ThrowsFileNotFoundException()
        {
            new MusicCollection().ImportFromJson(FilePath("missing.json"), true);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void ImportFromJson_CorruptedFile_ThrowsInvalidDataException()
        {
            string path = FilePath("broken.json");
            File.WriteAllText(path, "это не JSON");

            new MusicCollection().ImportFromJson(path, true);
        }

        [TestMethod]
        public void ImportFromJson_InvalidTrack_CollectionNotChanged()
        {
            string path = FilePath("invalid.json");
            File.WriteAllText(path,
                "[{\"Artist\":\"Ария\",\"Title\":\"Штиль\",\"Genre\":\"Рок\",\"Year\":\"1997\"}," +
                "{\"Artist\":\"Кипелов\",\"Title\":\"\",\"Genre\":\"Метал\",\"Year\":\"2004\"}]");
            var collection = new MusicCollection();
            collection.AddTrack(new MusicTrack("Singer", "Song", "Style", "2020"));

            Assert.ThrowsException<InvalidDataException>(() => collection.ImportFromJson(path, true));
            Assert.AreEqual(1, collection.Tracks.Count);
            Assert.AreEqual("Singer", collection.Tracks[0].Artist);
        }

        [TestMethod]
        public void ImportFromJson_YearInFuture_ThrowsInvalidDataException()
        {
            string path = FilePath("future.json");
            File.WriteAllText(path,
                "[{\"Artist\":\"Ария\",\"Title\":\"Штиль\",\"Genre\":\"Рок\",\"Year\":\"" + (DateTime.Now.Year + 1) + "\"}]");

            var ex = Assert.ThrowsException<InvalidDataException>(() => new MusicCollection().ImportFromJson(path, true));
            StringAssert.Contains(ex.Message, "Трек №1");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ExportToJson_EmptyPath_ThrowsArgumentException()
        {
            CreateCollection().ExportToJson("");
        }

        // ---------- CSV ----------

        [TestMethod]
        public void ExportToCsv_WritesHeaderAndOneLinePerTrack()
        {
            string path = FilePath("collection.csv");

            CreateCollection().ExportToCsv(path);

            var lines = File.ReadAllLines(path);
            Assert.AreEqual(4, lines.Length);
            Assert.AreEqual("Исполнитель;Название;Жанр;Год", lines[0]);
            Assert.AreEqual("Кипелов;Вавилон;Метал;2004", lines[1]);
        }

        [TestMethod]
        public void Csv_RoundTrip_KeepsSeparatorAndQuotes()
        {
            var source = new MusicCollection();
            source.AddTrack(new MusicTrack("Кипелов", "Вавилон; live", "Метал \"рок\"", "2004"));
            string path = FilePath("special.csv");

            source.ExportToCsv(path);
            var target = new MusicCollection();
            target.ImportFromCsv(path, true);

            Assert.AreEqual(1, target.Tracks.Count);
            Assert.AreEqual("Вавилон; live", target.Tracks[0].Title);
            Assert.AreEqual("Метал \"рок\"", target.Tracks[0].Genre);
        }

        [TestMethod]
        public void ImportFromCsv_WithoutHeader_ImportsTracks()
        {
            string path = FilePath("noheader.csv");
            File.WriteAllLines(path, new[] { "Ария;Штиль;Рок;1997", "", "Singer;Song;Style;2020" });

            var collection = new MusicCollection();
            int added = collection.ImportFromCsv(path, true);

            Assert.AreEqual(2, added);
        }

        [TestMethod]
        public void ImportFromCsv_WrongFieldCount_ThrowsInvalidDataException()
        {
            string path = FilePath("wrong.csv");
            File.WriteAllLines(path, new[] { "Исполнитель;Название;Жанр;Год", "Ария;Штиль;1997" });

            var ex = Assert.ThrowsException<InvalidDataException>(() => new MusicCollection().ImportFromCsv(path, true));
            StringAssert.Contains(ex.Message, "Строка 2");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void ImportFromCsv_YearNotNumber_ThrowsInvalidDataException()
        {
            string path = FilePath("year.csv");
            File.WriteAllLines(path, new[] { "Ария;Штиль;Рок;год" });

            new MusicCollection().ImportFromCsv(path, true);
        }

        // ---------- Выбор формата по расширению ----------

        [TestMethod]
        public void ExportAndImportFromFile_ChooseFormatByExtension()
        {
            var source = CreateCollection();
            string csv = FilePath("c.csv");
            string json = FilePath("c.json");

            source.Export(csv);
            source.Export(json);

            StringAssert.StartsWith(File.ReadAllLines(csv)[0], "Исполнитель;");
            StringAssert.StartsWith(File.ReadAllText(json).TrimStart(), "[");

            var fromCsv = new MusicCollection();
            var fromJson = new MusicCollection();
            Assert.AreEqual(3, fromCsv.ImportFromFile(csv, true));
            Assert.AreEqual(3, fromJson.ImportFromFile(json, true));
            CollectionAssert.AreEqual(fromCsv.Tracks.ToList(), fromJson.Tracks.ToList());
        }
    }
}
