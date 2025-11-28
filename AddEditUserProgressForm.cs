using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class AddEditUserProgressForm : Form
    {
        private ComboBox cbUser;
        private ComboBox cbWord;
        private CheckBox chkFavorite;
        private CheckBox chkLearned;
        private NumericUpDown numCorrect;
        private NumericUpDown numWrong;
        private DateTimePicker dtLast;
        private DateTimePicker dtNext;
        private Button btnSave;
        private Button btnCancel;

        private readonly int? _id;

        public AddEditUserProgressForm()
        {
            _id = null;
            InitializeComponent();
            LoadDictionaries();
        }

        public AddEditUserProgressForm(int id) : this()
        {
            _id = id;
            this.Text = "Редактировать прогресс";
            LoadProgress();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить прогресс";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(520, 280);

            Label lblUser = new Label { Text = "Пользователь:", Left = 20, Top = 20, Width = 110 };
            cbUser = new ComboBox
            {
                Left = 140,
                Top = 18,
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblWord = new Label { Text = "Слово:", Left = 20, Top = 50, Width = 110 };
            cbWord = new ComboBox
            {
                Left = 140,
                Top = 48,
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            chkFavorite = new CheckBox { Text = "Избранное", Left = 140, Top = 80, Width = 120 };
            chkLearned = new CheckBox { Text = "Выучено", Left = 270, Top = 80, Width = 120 };

            Label lblCorrect = new Label { Text = "Верных ответов:", Left = 20, Top = 110, Width = 110 };
            numCorrect = new NumericUpDown
            {
                Left = 140,
                Top = 108,
                Width = 80,
                Minimum = 0,
                Maximum = 100000
            };

            Label lblWrong = new Label { Text = "Неверных:", Left = 240, Top = 110, Width = 80 };
            numWrong = new NumericUpDown
            {
                Left = 330,
                Top = 108,
                Width = 80,
                Minimum = 0,
                Maximum = 100000
            };

            Label lblLast = new Label { Text = "Последняя тренировка:", Left = 20, Top = 140, Width = 140 };
            dtLast = new DateTimePicker
            {
                Left = 170,
                Top = 138,
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                ShowCheckBox = true
            };

            Label lblNext = new Label { Text = "Следующее повторение:", Left = 20, Top = 170, Width = 140 };
            dtNext = new DateTimePicker
            {
                Left = 170,
                Top = 168,
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                ShowCheckBox = true
            };

            btnSave = new Button { Text = "Сохранить", Left = 170, Top = 210, Width = 100 };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "Отмена", Left = 280, Top = 210, Width = 100 };
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblUser);
            Controls.Add(cbUser);
            Controls.Add(lblWord);
            Controls.Add(cbWord);
            Controls.Add(chkFavorite);
            Controls.Add(chkLearned);
            Controls.Add(lblCorrect);
            Controls.Add(numCorrect);
            Controls.Add(lblWrong);
            Controls.Add(numWrong);
            Controls.Add(lblLast);
            Controls.Add(dtLast);
            Controls.Add(lblNext);
            Controls.Add(dtNext);
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

                    // пользователи
                    using (SqliteCommand cmd = new SqliteCommand(
                        @"SELECT id, login FROM users ORDER BY login;", conn))
                    using (SqliteDataReader r = cmd.ExecuteReader())
                    {
                        DataTable t = new DataTable();
                        t.Load(r);
                        cbUser.DataSource = t;
                        cbUser.DisplayMember = "login";
                        cbUser.ValueMember = "id";
                    }

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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProgress()
        {
            if (!_id.HasValue) return;

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT user_id, word_id, is_favorite, is_learned,
       correct_answers, wrong_answers,
       last_trained_at, next_review_at
FROM user_word_progress
WHERE id = $id;";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$id", _id.Value);
                        using (SqliteDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                cbUser.SelectedValue = Convert.ToInt32(r["user_id"]);
                                cbWord.SelectedValue = Convert.ToInt32(r["word_id"]);
                                chkFavorite.Checked = Convert.ToInt32(r["is_favorite"]) == 1;
                                chkLearned.Checked = Convert.ToInt32(r["is_learned"]) == 1;
                                numCorrect.Value = Convert.ToDecimal(r["correct_answers"]);
                                numWrong.Value = Convert.ToDecimal(r["wrong_answers"]);

                                if (r["last_trained_at"] == DBNull.Value)
                                {
                                    dtLast.Checked = false;
                                }
                                else
                                {
                                    dtLast.Checked = true;
                                    dtLast.Value = DateTime.Parse(r["last_trained_at"].ToString());
                                }

                                if (r["next_review_at"] == DBNull.Value)
                                {
                                    dtNext.Checked = false;
                                }
                                else
                                {
                                    dtNext.Checked = true;
                                    dtNext.Value = DateTime.Parse(r["next_review_at"].ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки прогресса: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cbUser.SelectedValue == null)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }
            if (cbWord.SelectedValue == null)
            {
                MessageBox.Show("Выберите слово.");
                return;
            }

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    int userId = Convert.ToInt32(cbUser.SelectedValue);
                    int wordId = Convert.ToInt32(cbWord.SelectedValue);

                    object last = dtLast.Checked ? (object)dtLast.Value : (object)DBNull.Value;
                    object next = dtNext.Checked ? (object)dtNext.Value : (object)DBNull.Value;

                    if (_id.HasValue)
                    {
                        const string sql = @"
UPDATE user_word_progress
SET user_id = $user_id,
    word_id = $word_id,
    is_favorite = $is_favorite,
    is_learned = $is_learned,
    correct_answers = $correct,
    wrong_answers = $wrong,
    last_trained_at = $last,
    next_review_at = $next
WHERE id = $id;";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$id", _id.Value);
                            cmd.Parameters.AddWithValue("$user_id", userId);
                            cmd.Parameters.AddWithValue("$word_id", wordId);
                            cmd.Parameters.AddWithValue("$is_favorite", chkFavorite.Checked ? 1 : 0);
                            cmd.Parameters.AddWithValue("$is_learned", chkLearned.Checked ? 1 : 0);
                            cmd.Parameters.AddWithValue("$correct", (int)numCorrect.Value);
                            cmd.Parameters.AddWithValue("$wrong", (int)numWrong.Value);
                            cmd.Parameters.AddWithValue("$last", last);
                            cmd.Parameters.AddWithValue("$next", next);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        const string sql = @"
INSERT INTO user_word_progress
(user_id, word_id, is_favorite, is_learned,
 correct_answers, wrong_answers, last_trained_at, next_review_at)
VALUES ($user_id, $word_id, $is_favorite, $is_learned,
        $correct, $wrong, $last, $next);";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$user_id", userId);
                            cmd.Parameters.AddWithValue("$word_id", wordId);
                            cmd.Parameters.AddWithValue("$is_favorite", chkFavorite.Checked ? 1 : 0);
                            cmd.Parameters.AddWithValue("$is_learned", chkLearned.Checked ? 1 : 0);
                            cmd.Parameters.AddWithValue("$correct", (int)numCorrect.Value);
                            cmd.Parameters.AddWithValue("$wrong", (int)numWrong.Value);
                            cmd.Parameters.AddWithValue("$last", last);
                            cmd.Parameters.AddWithValue("$next", next);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения прогресса: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}