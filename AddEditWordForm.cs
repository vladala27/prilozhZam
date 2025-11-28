using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class AddEditWordForm : Form
    {
        private ComboBox cbLanguage;
        private ComboBox cbTopic;
        private TextBox txtWord;
        private TextBox txtTranscription;
        private TextBox txtPartOfSpeech;
        private Button btnSave;
        private Button btnCancel;

        private readonly int? _wordId;

        public AddEditWordForm()
        {
            _wordId = null;
            InitializeComponent();
            LoadDictionaries();
        }

        public AddEditWordForm(int wordId) : this()
        {
            _wordId = wordId;
            this.Text = "Редактировать слово";
            LoadWord();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить слово";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(420, 230);

            var lblLanguage = new Label
            {
                Text = "Язык:",
                Left = 20,
                Top = 20,
                Width = 100
            };
            cbLanguage = new ComboBox
            {
                Left = 130,
                Top = 18,
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblTopic = new Label
            {
                Text = "Тема:",
                Left = 20,
                Top = 50,
                Width = 100
            };
            cbTopic = new ComboBox
            {
                Left = 130,
                Top = 48,
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblWord = new Label
            {
                Text = "Слово:",
                Left = 20,
                Top = 80,
                Width = 100
            };
            txtWord = new TextBox
            {
                Left = 130,
                Top = 78,
                Width = 250
            };

            var lblTranscription = new Label
            {
                Text = "Транскрипция:",
                Left = 20,
                Top = 110,
                Width = 100
            };
            txtTranscription = new TextBox
            {
                Left = 130,
                Top = 108,
                Width = 250
            };

            var lblPos = new Label
            {
                Text = "Часть речи:",
                Left = 20,
                Top = 140,
                Width = 100
            };
            txtPartOfSpeech = new TextBox
            {
                Left = 130,
                Top = 138,
                Width = 250
            };

            btnSave = new Button
            {
                Text = "Сохранить",
                Left = 130,
                Top = 175,
                Width = 100
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Отмена",
                Left = 240,
                Top = 175,
                Width = 100
            };
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(lblLanguage);
            this.Controls.Add(cbLanguage);
            this.Controls.Add(lblTopic);
            this.Controls.Add(cbTopic);
            this.Controls.Add(lblWord);
            this.Controls.Add(txtWord);
            this.Controls.Add(lblTranscription);
            this.Controls.Add(txtTranscription);
            this.Controls.Add(lblPos);
            this.Controls.Add(txtPartOfSpeech);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadDictionaries()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Языки
                    using (var cmd = new SqliteCommand(
                               "SELECT id, name FROM languages ORDER BY name;", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var table = new DataTable();
                        table.Load(reader);
                        cbLanguage.DataSource = table;
                        cbLanguage.DisplayMember = "name";
                        cbLanguage.ValueMember = "id";
                    }

                    // Темы
                    using (var cmd = new SqliteCommand(
                               "SELECT id, name FROM topics ORDER BY name;", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var table = new DataTable();
                        table.Load(reader);
                        cbTopic.DataSource = table;
                        cbTopic.DisplayMember = "name";
                        cbTopic.ValueMember = "id";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadWord()
        {
            if (!_wordId.HasValue)
                return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT language_id, topic_id, word, transcription, part_of_speech
FROM words
WHERE id = $id;
";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$id", _wordId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var languageId = reader.GetInt32(0);
                                var topicId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                                var word = reader.GetString(2);
                                var trans = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                var pos = reader.IsDBNull(4) ? "" : reader.GetString(4);

                                cbLanguage.SelectedValue = languageId;
                                if (topicId.HasValue)
                                    cbTopic.SelectedValue = topicId.Value;

                                txtWord.Text = word;
                                txtTranscription.Text = trans;
                                txtPartOfSpeech.Text = pos;
                            }
                            else
                            {
                                MessageBox.Show("Слово не найдено в базе.",
                                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.DialogResult = DialogResult.Cancel;
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки слова: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cbLanguage.SelectedValue == null)
            {
                MessageBox.Show("Выберите язык.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtWord.Text))
            {
                MessageBox.Show("Введите слово.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    if (_wordId.HasValue)
                    {
                        UpdateWord(conn);
                    }
                    else
                    {
                        InsertWord(conn);
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InsertWord(SqliteConnection conn)
        {
            const string sql = @"
INSERT INTO words (language_id, topic_id, word, transcription, part_of_speech)
VALUES ($language_id, $topic_id, $word, $transcription, $part_of_speech);
";
            using (var cmd = new SqliteCommand(sql, conn))
            {
                // ВАЖНО: используем Convert.ToInt32, а не (int)
                int languageId = Convert.ToInt32(cbLanguage.SelectedValue);

                object topicId = DBNull.Value;
                if (cbTopic.SelectedValue != null && cbTopic.SelectedValue != DBNull.Value)
                    topicId = Convert.ToInt32(cbTopic.SelectedValue);

                cmd.Parameters.AddWithValue("$language_id", languageId);
                cmd.Parameters.AddWithValue("$topic_id", topicId);
                cmd.Parameters.AddWithValue("$word", txtWord.Text.Trim());
                cmd.Parameters.AddWithValue("$transcription",
                    string.IsNullOrWhiteSpace(txtTranscription.Text)
                        ? (object)DBNull.Value
                        : txtTranscription.Text.Trim());
                cmd.Parameters.AddWithValue("$part_of_speech",
                    string.IsNullOrWhiteSpace(txtPartOfSpeech.Text)
                        ? (object)DBNull.Value
                        : txtPartOfSpeech.Text.Trim());

                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateWord(SqliteConnection conn)
        {
            const string sql = @"
UPDATE words
SET language_id    = $language_id,
    topic_id       = $topic_id,
    word           = $word,
    transcription  = $transcription,
    part_of_speech = $part_of_speech
WHERE id = $id;
";
            using (var cmd = new SqliteCommand(sql, conn))
            {
                int languageId = Convert.ToInt32(cbLanguage.SelectedValue);

                object topicId = DBNull.Value;
                if (cbTopic.SelectedValue != null && cbTopic.SelectedValue != DBNull.Value)
                    topicId = Convert.ToInt32(cbTopic.SelectedValue);

                cmd.Parameters.AddWithValue("$id", _wordId.Value);
                cmd.Parameters.AddWithValue("$language_id", languageId);
                cmd.Parameters.AddWithValue("$topic_id", topicId);
                cmd.Parameters.AddWithValue("$word", txtWord.Text.Trim());
                cmd.Parameters.AddWithValue("$transcription",
                    string.IsNullOrWhiteSpace(txtTranscription.Text)
                        ? (object)DBNull.Value
                        : txtTranscription.Text.Trim());
                cmd.Parameters.AddWithValue("$part_of_speech",
                    string.IsNullOrWhiteSpace(txtPartOfSpeech.Text)
                        ? (object)DBNull.Value
                        : txtPartOfSpeech.Text.Trim());

                cmd.ExecuteNonQuery();
            }
        }
    }
}