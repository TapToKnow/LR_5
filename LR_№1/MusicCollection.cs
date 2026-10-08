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
                result.Add(CreateTrack(r.Artist, r.Title, r.Genre, r.Year, i + 1));
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
        private static MusicTrack CreateTrack(string artist, string title, string genre, string year, int number)
        {
            try
            {
                return new MusicTrack(artist, title, genre, year);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException($"Трек №{number}: {ex.Message.Split('\n')[0].Trim()}");
            }
            catch (NullReferenceException)
            {
                throw new InvalidDataException($"Трек №{number}: не заполнены обязательные поля.");
            }
        }
    }
}
