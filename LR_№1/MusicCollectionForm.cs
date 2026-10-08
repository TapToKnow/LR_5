using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LR__1
{
    public class MusicCollectionForm : Form
    {
        private MusicCollection musicCollection;
        private ListView listView;
        private Button addTrackButton;
        private Button removeTrackButton;
        private Button searchByArtistButton;
        private Button sortByYearButton;
        private Button exportButton;
        private Button importButton;
        private Button backupButton;
        private Button restoreButton;
        public MusicCollectionForm()
        {
            this.Text = "Управление музыкальной коллекцией";
            this.Width = 600;
            this.Height = 440;
            CreateControls();
            musicCollection = new MusicCollection(listView);
        }
        private void CreateControls()
        {
            listView = new ListView
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(480, 300),
                View = View.Details,
                FullRowSelect = true
            };
            listView.Columns.Add("Исполнитель", 150);
            listView.Columns.Add("Название", 150);
            listView.Columns.Add("Жанр", 100);
            listView.Columns.Add("Год", 50);
            addTrackButton = new Button
            {
                Location = new System.Drawing.Point(10, 320),
                Text = "Добавить трек",
                Size = new System.Drawing.Size(100, 25)
            };
            addTrackButton.Click += (sender, e) =>
            {
                var addTrackForm = new AddTrackForm();
                addTrackForm.ShowDialog();
                if (addTrackForm.DialogResult == DialogResult.OK)
                {
                    try
                    {
                        var track = new MusicTrack(
                            addTrackForm.Artist,
                            addTrackForm.Title,
                            addTrackForm.Genre,
                            addTrackForm.Year);
                        musicCollection.AddTrack(track);
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка ввода");
                    }
                }
            };
            removeTrackButton = new Button
            {
                Location = new System.Drawing.Point(120, 320),
                Text = "Удалить трек",
                Size = new System.Drawing.Size(100, 25)
            };
            removeTrackButton.Click += (sender, e) =>
            {
                if (listView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Сначала выберите трек для удаления.");
                    return;
                }
                var track = new MusicTrack(
                listView.SelectedItems[0].SubItems[0].Text,
                listView.SelectedItems[0].SubItems[1].Text,
                listView.SelectedItems[0].SubItems[2].Text,
                listView.SelectedItems[0].SubItems[3].Text);
                musicCollection.RemoveTrack(track);
            };
            searchByArtistButton = new Button
            {
                Location = new System.Drawing.Point(230, 320),
                Text = "Поиск по исполнителю",
                Size = new System.Drawing.Size(200, 25)
            };
            searchByArtistButton.Click += (sender, e) =>
            {
                var searchForm = new SearchArtistForm();
                searchForm.ShowDialog();
                if (searchForm.DialogResult == DialogResult.OK)
                {
                    musicCollection.SearchByArtist(searchForm.Artist);
                }
            };
            sortByYearButton = new Button
            {
                Location = new System.Drawing.Point(440, 320),
                Text = "Сортировать по году",
                Size = new System.Drawing.Size(120, 25)
            };
            sortByYearButton.Click += (sender, e) => musicCollection.SortByYear();
            exportButton = new Button
            {
                Name = "exportButton",
                Location = new System.Drawing.Point(10, 355),
                Text = "Экспорт",
                Size = new System.Drawing.Size(100, 25)
            };
            exportButton.Click += (sender, e) => ExportCollection();
            importButton = new Button
            {
                Name = "importButton",
                Location = new System.Drawing.Point(120, 355),
                Text = "Импорт",
                Size = new System.Drawing.Size(100, 25)
            };
            importButton.Click += (sender, e) => ImportCollection();
            backupButton = new Button
            {
                Name = "backupButton",
                Location = new System.Drawing.Point(230, 355),
                Text = "Резервная копия",
                Size = new System.Drawing.Size(200, 25)
            };
            backupButton.Click += (sender, e) => CreateBackup();
            restoreButton = new Button
            {
                Name = "restoreButton",
                Location = new System.Drawing.Point(440, 355),
                Text = "Восстановить",
                Size = new System.Drawing.Size(120, 25)
            };
            restoreButton.Click += (sender, e) => RestoreBackup();
            this.Controls.Add(listView);
            this.Controls.Add(addTrackButton);
            this.Controls.Add(removeTrackButton);
            this.Controls.Add(searchByArtistButton);
            this.Controls.Add(sortByYearButton);
            this.Controls.Add(exportButton);
            this.Controls.Add(importButton);
            this.Controls.Add(backupButton);
            this.Controls.Add(restoreButton);
        }

        private const string FileFilter = "Файлы JSON (*.json)|*.json|Файлы CSV (*.csv)|*.csv";

        // Экспорт коллекции в файл
        private void ExportCollection()
        {
            using (var dialog = new SaveFileDialog
            {
                Title = "Экспорт коллекции",
                Filter = FileFilter,
                FileName = "music_collection"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    musicCollection.Export(dialog.FileName);
                    MessageBox.Show($"Коллекция экспортирована ({musicCollection.Tracks.Count} тр.).", "Экспорт");
                }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
                {
                    MessageBox.Show("Не удалось сохранить файл: " + ex.Message, "Ошибка экспорта");
                }
            }
        }

        // Импорт коллекции из файла
        private void ImportCollection()
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Импорт коллекции",
                Filter = FileFilter
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                bool replace = false;
                if (musicCollection.Tracks.Count > 0)
                {
                    var answer = MessageBox.Show(
                        "Заменить текущую коллекцию треками из файла?\n\n«Да» — заменить, «Нет» — добавить к текущим.",
                        "Импорт", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (answer == DialogResult.Cancel)
                        return;
                    replace = answer == DialogResult.Yes;
                }
                try
                {
                    int added = musicCollection.ImportFromFile(dialog.FileName, replace);
                    MessageBox.Show($"Импортировано треков: {added}.", "Импорт");
                }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
                {
                    MessageBox.Show("Не удалось загрузить коллекцию: " + ex.Message, "Ошибка импорта");
                }
            }
        }

        // Резервная копия коллекции в папку Backups рядом с программой
        private void CreateBackup()
        {
            try
            {
                string path = musicCollection.CreateBackup(MusicCollection.DefaultBackupFolder);
                MessageBox.Show("Резервная копия создана:\n" + path, "Резервная копия");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show("Не удалось создать резервную копию: " + ex.Message, "Ошибка");
            }
        }

        // Восстановление коллекции из последней резервной копии
        private void RestoreBackup()
        {
            var answer = MessageBox.Show(
                "Заменить текущую коллекцию последней резервной копией?",
                "Восстановление", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;
            try
            {
                int count = musicCollection.RestoreLatestBackup(MusicCollection.DefaultBackupFolder);
                MessageBox.Show($"Коллекция восстановлена, треков: {count}.", "Восстановление");
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show("Не удалось восстановить коллекцию: " + ex.Message, "Ошибка");
            }
        }
    }

}
