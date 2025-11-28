using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class AddEditTranslationForm : Form
    {
        private ComboBox cbWord;
        private ComboBox cbTargetLanguage;
        private TextBox txtTranslation;
        private TextBox txtExample;
        private TextBox txtExampleTranslation;
        private Button btnSave;
        private Button btnCancel;

        private readonly int? _id;

        public AddEditTranslationForm()
        {
            _id = null;
            InitializeComponent();
            LoadDictionaries();
        }

        public AddEditTranslationForm(int id) : this()
        {
            _id = id;
            this.Text = "Редактировать перевод";
            LoadTranslation();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить перевод";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(500, 320);

            Label lblWord = new Label { Text = "Слово:", Left = 20, Top = 20, Width = 120 };
            cbWord = new ComboBox
            {
                Left = 150,
                Top = 18,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblLang = new Label { Text = "Язык перевода:", Left = 20, Top = 50, Width = 120 };
            cbTargetLanguage = new ComboBox
            {
                Left = 150,
                Top = 48,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblTrans = new Label { Text = "Перевод:", Left = 20, Top = 80, Width = 120 };
            txtTranslation = new TextBox { Left = 150, Top = 78, Width = 320 };

            Label lblEx = new Label { Text = "Пример:", Left = 20, Top = 110, Width = 120 };
            txtExample = new TextBox
            {
                Left = 150,
                Top = 108,
                Width = 320,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            Label lblExTr = new Label { Text = "Перевод примера:", Left = 20, Top = 175, Width = 120 };
            txtExampleTranslation = new TextBox
            {
                Left = 150,
                Top = 173,
                Width = 320,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            btnSave = new Button { Text = "Сохранить", Left = 150, Top = 245, Width = 100 };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "Отмена", Left = 260, Top = 245, Width = 100 };
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblWord);
            Controls.Add(cbWord);
            Controls.Add(lblLang);
            Controls.Add(cbTargetLanguage);
            Controls.Add(lblTrans);
            Controls.Add(txtTranslation);
            Controls.Add(lblEx);
            Controls.Add(txtExample);
            Controls.Add(lblExTr);
            Controls.Add(txtExampleTranslation);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }

        private void LoadDictionaries()
        {
            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // слова
                    using (SqliteCommand cmd = new SqliteCommand(
                        @"SELECT id, word || ' (' || id || ')' AS title FROM words ORDER BY word;", conn))
                    using (SqliteDataReader r = cmd.ExecuteReader())
                    {
                        DataTable t = new DataTable();
                        t.Load(r);
                        cbWord.DataSource = t;
                        cbWord.DisplayMember = "title";
                        cbWord.ValueMember = "id";
                    }

                    // языки
                    using (SqliteCommand cmd = new SqliteCommand(
                        @"SELECT id, name FROM languages ORDER BY name;", conn))
                    using (SqliteDataReader r = cmd.ExecuteReader())
                    {
                        DataTable t = new DataTable();
                        t.Load(r);
                        cbTargetLanguage.DataSource = t;
                        cbTargetLanguage.DisplayMember = "name";
                        cbTargetLanguage.ValueMember = "id";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTranslation()
        {
            if (!_id.HasValue) return;

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT word_id, target_language_id, translation,
       example_sentence, example_translation
FROM word_translations
WHERE id = $id;";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$id", _id.Value);
                        using (SqliteDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                cbWord.SelectedValue = Convert.ToInt32(r["word_id"]);
                                cbTargetLanguage.SelectedValue = Convert.ToInt32(r["target_language_id"]);
                                txtTranslation.Text = r["translation"].ToString();
                                txtExample.Text = r["example_sentence"] == DBNull.Value
                                    ? ""
                                    : r["example_sentence"].ToString();
                                txtExampleTranslation.Text = r["example_translation"] == DBNull.Value
                                    ? ""
                                    : r["example_translation"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки перевода: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cbWord.SelectedValue == null)
            {
                MessageBox.Show("Выберите слово.");
                return;
            }
            if (cbTargetLanguage.SelectedValue == null)
            {
                MessageBox.Show("Выберите язык перевода.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTranslation.Text))
            {
                MessageBox.Show("Введите перевод.");
                return;
            }

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    int wordId = Convert.ToInt32(cbWord.SelectedValue);
                    int targetLangId = Convert.ToInt32(cbTargetLanguage.SelectedValue);

                    if (_id.HasValue)
                    {
                        const string sql = @"
UPDATE word_translations
SET word_id = $word_id,
    target_language_id = $target_language_id,
    translation = $translation,
    example_sentence = $example_sentence,
    example_translation = $example_translation
WHERE id = $id;";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$id", _id.Value);
                            cmd.Parameters.AddWithValue("$word_id", wordId);
                            cmd.Parameters.AddWithValue("$target_language_id", targetLangId);
                            cmd.Parameters.AddWithValue("$translation", txtTranslation.Text.Trim());
                            cmd.Parameters.AddWithValue("$example_sentence",
                                string.IsNullOrWhiteSpace(txtExample.Text)
                                    ? (object)DBNull.Value
                                    : txtExample.Text.Trim());
                            cmd.Parameters.AddWithValue("$example_translation",
                                string.IsNullOrWhiteSpace(txtExampleTranslation.Text)
                                    ? (object)DBNull.Value
                                    : txtExampleTranslation.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        const string sql = @"
INSERT INTO word_translations
(word_id, target_language_id, translation, example_sentence, example_translation)
VALUES ($word_id, $target_language_id, $translation, $example_sentence, $example_translation);";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$word_id", wordId);
                            cmd.Parameters.AddWithValue("$target_language_id", targetLangId);
                            cmd.Parameters.AddWithValue("$translation", txtTranslation.Text.Trim());
                            cmd.Parameters.AddWithValue("$example_sentence",
                                string.IsNullOrWhiteSpace(txtExample.Text)
                                    ? (object)DBNull.Value
                                    : txtExample.Text.Trim());
                            cmd.Parameters.AddWithValue("$example_translation",
                                string.IsNullOrWhiteSpace(txtExampleTranslation.Text)
                                    ? (object)DBNull.Value
                                    : txtExampleTranslation.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения перевода: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}