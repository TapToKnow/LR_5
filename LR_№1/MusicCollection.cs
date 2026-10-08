using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LR__1
{
    public class MusicCollection
    {
        private List<MusicTrack> tracks = new List<MusicTrack>();
        private ListView listView;

        // Доступ к трекам только на чтение (используется в модульных тестах)
        public IReadOnlyList<MusicTrack> Tracks => tracks;

        public MusicCollection(ListView listView)
        {
            this.listView = listView;
            LoadTracks();
        }

        // Конструктор без формы: коллекция работает без ListView и окон сообщений (для модульных тестов)
        public MusicCollection() : this(null)
        {
        }

        // Окно сообщения показывается только при работе с формой
        private void ShowMessage(string text)
        {
            if (listView != null)
                MessageBox.Show(text);
        }

        private void LoadTracks()
        {
            if (listView == null) return;
            listView.Items.Clear();
            foreach (var track in tracks)
            {
                listView.Items.Add(new ListViewItem(new[] { track.Artist, track.Title,
track.Genre.ToString(), track.Year.ToString() }));
            }
        }
        public void AddTrack(MusicTrack track)
        {
            tracks.Add(track);
            LoadTracks();
            ShowMessage("Трек добавлен.");
        }
        public void RemoveTrack(MusicTrack track)
        {
            if (tracks.Contains(track))
            {
                tracks.Remove(track);
                LoadTracks();
                ShowMessage("Трек удалён.");
            }
            else
            {
                ShowMessage("Трек не найден в коллекции.");
            }
        }
        public void SearchByArtist(string artist)
        {
            var foundTracks = tracks.Where(t => t.Artist.Contains(artist)).ToList();
            if (foundTracks.Any())
            {
                if (listView != null)
                {
                    listView.Items.Clear();
                    foreach (var track in foundTracks)
                    {
                        listView.Items.Add(new ListViewItem(new[] { track.Artist, track.Title,
track.Genre.ToString(), track.Year.ToString() }));
                    }
                }
                ShowMessage("Найденные треки:");
            }
            else
            {
                ShowMessage("Треки не найдены.");
            }
        }
        public void SortByYear()
        {
            var sortedTracks = tracks.OrderBy(t => t.Year).ToList();
            if (listView != null)
            {
                listView.Items.Clear();
                foreach (var track in sortedTracks)
                {
                    listView.Items.Add(new ListViewItem(new[] { track.Artist, track.Title,
track.Genre.ToString(), track.Year.ToString() }));
                }
            }
            ShowMessage("Сортировка по году выполнена.");
        }

        // ---------- Экспорт и импорт коллекции (ЛР №5) ----------

        // Запись трека в файле JSON
        [DataContract]
        private class TrackRecord
        {
            [DataMember(Order = 1)] public string Artist { get; set; }
            [DataMember(Order = 2)] public string Title { get; set; }
            [DataMember(Order = 3)] public string Genre { get; set; }
            [DataMember(Order = 4)] public string Year { get; set; }
        }

        // Сохраняет всю коллекцию в файл JSON
        public void ExportToJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Не указан путь к файлу.", nameof(path));

            var records = tracks.Select(t => new TrackRecord
            {
                Artist = t.Artist,
                Title = t.Title,
                Genre = t.Genre,
                Year = t.Year
            }).ToList();

            var serializer = new DataContractJsonSerializer(typeof(List<TrackRecord>));
            using (var stream = File.Create(path))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true))
            {
                serializer.WriteObject(writer, records);
            }
        }

        // Читает треки из файла JSON (коллекция при этом не меняется)
        public List<MusicTrack> ReadJson(string path)
        {
            CheckFile(path);
            List<TrackRecord> records;
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(List<TrackRecord>));
                using (var stream = File.OpenRead(path))
                {
                    records = (List<TrackRecord>)serializer.ReadObject(stream);
                }
            }
            catch (SerializationException)
            {
                throw new InvalidDataException("Файл повреждён или не является коллекцией в формате JSON.");
            }
            if (records == null)
                throw new InvalidDataException("Файл не содержит треков.");

            var result = new List<MusicTrack>();
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                if (r == null)
                    throw new InvalidDataException($"Трек №{i + 1}: пустая запись.");
                result.Add(CreateTrack(r.Artist, r.Title, r.Genre, r.Year, $"Трек №{i + 1}"));
            }
            return result;
        }

        // Загружает коллекцию из файла JSON.
        // replace = true — коллекция заменяется содержимым файла,
        // replace = false — треки из файла добавляются к текущим (дубликаты пропускаются).
        // Возвращает количество добавленных треков.
        public int ImportFromJson(string path, bool replace)
        {
            return Import(ReadJson(path), replace);
        }

        // ---------- Формат CSV ----------

        private const char CsvSeparator = ';';
        private static readonly string[] CsvHeader = { "Исполнитель", "Название", "Жанр", "Год" };

        // Сохраняет всю коллекцию в файл CSV (разделитель «;», кодировка UTF-8 — файл открывается в Excel)
        public void ExportToCsv(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Не указан путь к файлу.", nameof(path));

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(CsvSeparator.ToString(), CsvHeader));
            foreach (var t in tracks)
            {
                sb.AppendLine(string.Join(CsvSeparator.ToString(),
                    new[] { t.Artist, t.Title, t.Genre, t.Year }.Select(EscapeCsv)));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        // Читает треки из файла CSV (коллекция при этом не меняется)
        public List<MusicTrack> ReadCsv(string path)
        {
            CheckFile(path);
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            var result = new List<MusicTrack>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                var fields = ParseCsvLine(lines[i], i + 1);
                if (i == 0 && fields.Length == CsvHeader.Length &&
                    string.Equals(fields[0].Trim(), CsvHeader[0], StringComparison.OrdinalIgnoreCase))
                    continue; // строка заголовка
                if (fields.Length != CsvHeader.Length)
                    throw new InvalidDataException(
                        $"Строка {i + 1}: ожидается {CsvHeader.Length} поля (исполнитель; название; жанр; год), найдено {fields.Length}.");
                result.Add(CreateTrack(fields[0].Trim(), fields[1].Trim(), fields[2].Trim(), fields[3].Trim(), $"Строка {i + 1}"));
            }
            return result;
        }

        // Загружает коллекцию из файла CSV (параметр replace — как в ImportFromJson)
        public int ImportFromCsv(string path, bool replace)
        {
            return Import(ReadCsv(path), replace);
        }

        // Экспорт с выбором формата по расширению файла (.json или .csv)
        public void Export(string path)
        {
            if (IsCsv(path))
                ExportToCsv(path);
            else
                ExportToJson(path);
        }

        // Импорт с выбором формата по расширению файла (.json или .csv)
        public int ImportFromFile(string path, bool replace)
        {
            return IsCsv(path) ? ImportFromCsv(path, replace) : ImportFromJson(path, replace);
        }

        private static bool IsCsv(string path)
        {
            return string.Equals(Path.GetExtension(path ?? string.Empty), ".csv", StringComparison.OrdinalIgnoreCase);
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { CsvSeparator, '"', '\r', '\n' }) >= 0)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static string[] ParseCsvLine(string line, int lineNumber)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else if (c == '"') inQuotes = false;
                    else current.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == CsvSeparator) { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
            if (inQuotes)
                throw new InvalidDataException($"Строка {lineNumber}: не закрыта кавычка.");
            fields.Add(current.ToString());
            return fields.ToArray();
        }

        // ---------- Резервное копирование ----------

        // Папка резервных копий по умолчанию — Backups рядом с программой
        public static string DefaultBackupFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

        // Создаёт резервную копию коллекции (JSON) с датой и временем в имени файла.
        // Возвращает полный путь к созданному файлу.
        public string CreateBackup(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("Не указана папка для резервной копии.", nameof(folder));

            Directory.CreateDirectory(folder);
            string name = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            string path = Path.Combine(folder, name + ".json");
            for (int n = 2; File.Exists(path); n++)
                path = Path.Combine(folder, $"{name}_{n}.json");

            ExportToJson(path);
            return path;
        }

        // Список резервных копий в папке: от новых к старым
        public static List<string> GetBackups(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return new List<string>();
            return Directory.GetFiles(folder, "backup_*.json")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ThenByDescending(f => f, StringComparer.Ordinal)
                .ToList();
        }

        // Восстанавливает коллекцию из последней резервной копии в папке
        public int RestoreLatestBackup(string folder)
        {
            var backups = GetBackups(folder);
            if (backups.Count == 0)
                throw new FileNotFoundException("Резервные копии не найдены.", folder);
            return ImportFromJson(backups[0], true);
        }

        private int Import(List<MusicTrack> imported, bool replace)
        {
            if (replace)
                tracks.Clear();

            int added = 0;
            foreach (var track in imported)
            {
                if (!tracks.Contains(track))
                {
                    tracks.Add(track);
                    added++;
                }
            }
            LoadTracks();
            return added;
        }

        private static void CheckFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Не указан путь к файлу.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл не найден.", path);
        }

        // Создание трека из файла с той же проверкой данных, что и при ручном вводе
        private static MusicTrack CreateTrack(string artist, string title, string genre, string year, string place)
        {
            try
            {
                return new MusicTrack(artist, title, genre, year);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException($"{place}: {ex.Message.Split('\n')[0].Trim()}");
            }
            catch (NullReferenceException)
            {
                throw new InvalidDataException($"{place}: не заполнены обязательные поля.");
            }
        }
    }
}
