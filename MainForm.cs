using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class MainForm : Form
    {
        private LoggedInUser _user;

        private DataGridView dgvMain;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRefresh;
        private Button btnLogout;
        private Label lblUserInfo;
        private ComboBox cbTables;   // выбор таблицы

        public MainForm(LoggedInUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            _user = user;

            InitializeComponent();
            this.Load += MainForm_Load;
        }

        private void InitializeComponent()
        {
            this.Text = "Изучение иностранных слов";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 1000;
            this.Height = 550;

            lblUserInfo = new Label
            {
                Left = 10,
                Top = 10,
                Width = 500,
                AutoSize = true
            };

            Label lblTable = new Label
            {
                Text = "Таблица:",
                Left = 520,
                Top = 10,
                Width = 60
            };

            cbTables = new ComboBox
            {
                Left = 590,
                Top = 8,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbTables.SelectedIndexChanged += delegate { LoadCurrentTable(); };

            btnLogout = new Button
            {
                Text = "Выход",
                Left = 810,
                Top = 6,
                Width = 120
            };
            btnLogout.Click += BtnLogout_Click;

            dgvMain = new DataGridView
            {
                Left = 10,
                Top = 40,
                Width = 960,
                Height = 400,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            btnAdd = new Button
            {
                Text = "Добавить",
                Left = 10,
                Top = 460,
                Width = 100
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "Редактировать",
                Left = 120,
                Top = 460,
                Width = 110
            };
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new Button
            {
                Text = "Удалить",
                Left = 240,
                Top = 460,
                Width = 100
            };
            btnDelete.Click += BtnDelete_Click;

            btnRefresh = new Button
            {
                Text = "Обновить",
                Left = 350,
                Top = 460,
                Width = 100
            };
            btnRefresh.Click += delegate { LoadCurrentTable(); };

            this.Controls.Add(lblUserInfo);
            this.Controls.Add(lblTable);
            this.Controls.Add(cbTables);
            this.Controls.Add(btnLogout);
            this.Controls.Add(dgvMain);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
            this.Controls.Add(btnRefresh);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateUserInfo();
            FillTablesCombo();
            LoadCurrentTable();
        }

        private void UpdateUserInfo()
        {
            lblUserInfo.Text = "Пользователь: " + _user.FullName + " (роль: " + _user.Role + ")";
        }

        private bool IsDirector()
        {
            return string.Equals(_user.Role, "директор", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Список таблиц для роли.</summary>
        private void FillTablesCombo()
        {
            cbTables.Items.Clear();

            if (IsDirector())
            {
                cbTables.Items.Add("Слова");
                cbTables.Items.Add("Переводы");
                cbTables.Items.Add("Языки");
                cbTables.Items.Add("Темы");
                cbTables.Items.Add("Пользователи");
                cbTables.Items.Add("Прогресс изучения");
            }
            else
            {
                // клиент видит только слова
                cbTables.Items.Add("Слова");
            }

            if (cbTables.Items.Count > 0)
                cbTables.SelectedIndex = 0;

            UpdateCrudButtonsState();
        }

        /// <summary>Подгрузка выбранной таблицы.</summary>
        private void LoadCurrentTable()
        {
            string table = cbTables.SelectedItem as string;
            if (string.IsNullOrEmpty(table))
                return;

            switch (table)
            {
                case "Слова":
                    LoadWords();
                    break;
                case "Переводы":
                    LoadWordTranslations();
                    break;
                case "Языки":
                    LoadLanguages();
                    break;
                case "Темы":
                    LoadTopics();
                    break;
                case "Пользователи":
                    LoadUsers();
                    break;
                case "Прогресс изучения":
                    LoadUserProgress();
                    break;
            }

            UpdateCrudButtonsState();
        }

        /// <summary>Состояние кнопок CRUD по роли.</summary>
        private void UpdateCrudButtonsState()
        {
            if (!IsDirector())
            {
                btnAdd.Enabled = false;
                btnEdit.Enabled = false;
                btnDelete.Enabled = false;
            }
            else
            {
                // директор — полный доступ ко всем таблицам
                btnAdd.Enabled = true;
                btnEdit.Enabled = true;
                btnDelete.Enabled = true;
            }
        }

        // ============== Загрузка таблиц ==============

        private void LoadWords()
        {
            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT w.id,
       l.name  AS Язык,
       t.name  AS Тема,
       w.word  AS Слово,
       w.transcription AS Транскрипция,
       w.part_of_speech AS Часть_речи
FROM words w
LEFT JOIN languages l ON w.language_id = l.id
LEFT JOIN topics t    ON w.topic_id    = t.id
ORDER BY w.id;
";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable table = new DataTable();
                        table.Load(reader);
                        dgvMain.DataSource = table;
                    }
                }

                if (dgvMain.Columns["id"] != null)
                {
                    dgvMain.Columns["id"].HeaderText = "ID";
                    dgvMain.Columns["id"].Width = 60;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки слов: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLanguages()
        {
            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT id,
       name AS Язык,
       code AS Код
FROM languages
ORDER BY id;
";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable table = new DataTable();
                        table.Load(reader);
                        dgvMain.DataSource = table;
                    }
                }

                if (dgvMain.Columns["id"] != null)
                {
                    dgvMain.Columns["id"].HeaderText = "ID";
                    dgvMain.Columns["id"].Width = 60;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки языков: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTopics()
        {
            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT id,
       name AS Тема,
       description AS Описание
FROM topics
ORDER BY id;
";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable table = new DataTable();
                        table.Load(reader);
                        dgvMain.DataSource = table;
                    }
                }

                if (dgvMain.Columns["id"] != null)
                {
                    dgvMain.Columns["id"].HeaderText = "ID";
                    dgvMain.Columns["id"].Width = 60;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки тем: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadWordTranslations()
        {
            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT wt.id,
       w.word               AS Слово,
       l1.name              AS Язык_оригинала,
       l2.name              AS Язык_перевода,
       wt.translation       AS Перевод,
       wt.example_sentence  AS Пример,
       wt.example_translation AS Перевод_примера
FROM word_translations wt
JOIN words w          ON wt.word_id = w.id
JOIN languages l1     ON w.language_id = l1.id
JOIN languages l2     ON wt.target_language_id = l2.id
ORDER BY wt.id;
";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable table = new DataTable();
                        table.Load(reader);
                        dgvMain.DataSource = table;
                    }
                }

                if (dgvMain.Columns["id"] != null)
                {
                    dgvMain.Columns["id"].HeaderText = "ID";
                    dgvMain.Columns["id"].Width = 60;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки переводов: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT id,
       login       AS Логин,
       role        AS Роль,
       full_name   AS ФИО,
       created_at  AS Создан,
       is_active   AS Активен
FROM users
ORDER BY id;
";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable table = new DataTable();
                        table.Load(reader);
                        dgvMain.DataSource = table;
                    }
                }

                if (dgvMain.Columns["id"] != null)
                {
                    dgvMain.Columns["id"].HeaderText = "ID";
                    dgvMain.Columns["id"].Width = 60;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUserProgress()
        {
            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT uwp.id,
       u.login AS Логин,
       w.word  AS Слово,
       uwp.is_favorite     AS Избранное,
       uwp.is_learned      AS Выучено,
       uwp.correct_answers AS Верно,
       uwp.wrong_answers   AS Неверно,
       uwp.last_trained_at AS Последняя_тренировка,
       uwp.next_review_at  AS Следующее_повторение
FROM user_word_progress uwp
JOIN users u ON uwp.user_id = u.id
JOIN words w ON uwp.word_id = w.id
ORDER BY uwp.id;
";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable table = new DataTable();
                        table.Load(reader);
                        dgvMain.DataSource = table;
                    }
                }

                if (dgvMain.Columns["id"] != null)
                {
                    dgvMain.Columns["id"].HeaderText = "ID";
                    dgvMain.Columns["id"].Width = 60;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки прогресса: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============== Вспомогательное ==============

        private int? GetSelectedRowId()
        {
            if (dgvMain.CurrentRow == null)
                return null;

            object value = dgvMain.CurrentRow.Cells["id"].Value;
            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt32(value);
        }

        // ============== Кнопки CRUD ==============

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!IsDirector())
                return;

            string table = cbTables.SelectedItem as string;
            if (string.IsNullOrEmpty(table))
                return;

            if (table == "Слова")
            {
                using (AddEditWordForm f = new AddEditWordForm())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadWords();
                }
            }
            else if (table == "Пользователи")
            {
                using (AddEditUserForm f = new AddEditUserForm())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadUsers();
                }
            }
            else if (table == "Языки")
            {
                using (AddEditLanguageForm f = new AddEditLanguageForm())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadLanguages();
                }
            }
            else if (table == "Темы")
            {
                using (AddEditTopicForm f = new AddEditTopicForm())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadTopics();
                }
            }
            else if (table == "Переводы")
            {
                using (AddEditTranslationForm f = new AddEditTranslationForm())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadWordTranslations();
                }
            }
            else if (table == "Прогресс изучения")
            {
                using (AddEditUserProgressForm f = new AddEditUserProgressForm())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadUserProgress();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (!IsDirector())
                return;

            int? id = GetSelectedRowId();
            if (!id.HasValue)
            {
                MessageBox.Show("Выберите запись для редактирования.",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string table = cbTables.SelectedItem as string;
            if (string.IsNullOrEmpty(table))
                return;

            if (table == "Слова")
            {
                using (AddEditWordForm f = new AddEditWordForm(id.Value))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadWords();
                }
            }
            else if (table == "Пользователи")
            {
                using (AddEditUserForm f = new AddEditUserForm(id.Value))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadUsers();
                }
            }
            else if (table == "Языки")
            {
                using (AddEditLanguageForm f = new AddEditLanguageForm(id.Value))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadLanguages();
                }
            }
            else if (table == "Темы")
            {
                using (AddEditTopicForm f = new AddEditTopicForm(id.Value))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadTopics();
                }
            }
            else if (table == "Переводы")
            {
                using (AddEditTranslationForm f = new AddEditTranslationForm(id.Value))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadWordTranslations();
                }
            }
            else if (table == "Прогресс изучения")
            {
                using (AddEditUserProgressForm f = new AddEditUserProgressForm(id.Value))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                        LoadUserProgress();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!IsDirector())
                return;

            int? id = GetSelectedRowId();
            if (!id.HasValue)
            {
                MessageBox.Show("Выберите запись для удаления.",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Удалить выбранную запись?",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes)
            {
                return;
            }

            string table = cbTables.SelectedItem as string;
            if (string.IsNullOrEmpty(table))
                return;

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    string sql = null;
                    if (table == "Слова")
                        sql = "DELETE FROM words WHERE id = $id;";
                    else if (table == "Пользователи")
                        sql = "DELETE FROM users WHERE id = $id;";
                    else if (table == "Языки")
                        sql = "DELETE FROM languages WHERE id = $id;";
                    else if (table == "Темы")
                        sql = "DELETE FROM topics WHERE id = $id;";
                    else if (table == "Переводы")
                        sql = "DELETE FROM word_translations WHERE id = $id;";
                    else if (table == "Прогресс изучения")
                        sql = "DELETE FROM user_word_progress WHERE id = $id;";

                    if (sql == null)
                        return;

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$id", id.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                if (table == "Слова") LoadWords();
                else if (table == "Пользователи") LoadUsers();
                else if (table == "Языки") LoadLanguages();
                else if (table == "Темы") LoadTopics();
                else if (table == "Переводы") LoadWordTranslations();
                else if (table == "Прогресс изучения") LoadUserProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============== Выход из аккаунта ==============

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            this.Hide();
            using (LoginForm loginForm = new LoginForm())
            {
                DialogResult result = loginForm.ShowDialog();
                if (result == DialogResult.OK && loginForm.AuthenticatedUser != null)
                {
                    _user = loginForm.AuthenticatedUser;
                    UpdateUserInfo();
                    FillTablesCombo();
                    LoadCurrentTable();
                    this.Show();
                }
                else
                {
                    this.Close();
                }
            }
        }
    }
}