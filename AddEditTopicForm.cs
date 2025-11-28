using System;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class AddEditTopicForm : Form
    {
        private TextBox txtName;
        private TextBox txtDescription;
        private Button btnSave;
        private Button btnCancel;

        private readonly int? _id;

        public AddEditTopicForm()
        {
            _id = null;
            InitializeComponent();
        }

        public AddEditTopicForm(int id) : this()
        {
            _id = id;
            this.Text = "Редактировать тему";
            LoadTopic();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить тему";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(400, 220);

            Label lblName = new Label { Text = "Название:", Left = 20, Top = 20, Width = 100 };
            txtName = new TextBox { Left = 130, Top = 18, Width = 240 };

            Label lblDesc = new Label { Text = "Описание:", Left = 20, Top = 55, Width = 100 };
            txtDescription = new TextBox
            {
                Left = 130,
                Top = 53,
                Width = 240,
                Height = 90,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            btnSave = new Button { Text = "Сохранить", Left = 130, Top = 160, Width = 100 };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "Отмена", Left = 240, Top = 160, Width = 100 };
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblDesc);
            Controls.Add(txtDescription);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }

        private void LoadTopic()
        {
            if (!_id.HasValue) return;

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = "SELECT name, description FROM topics WHERE id = $id;";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$id", _id.Value);
                        using (SqliteDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtName.Text = r.GetString(0);
                                txtDescription.Text = r.IsDBNull(1) ? "" : r.GetString(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки темы: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название темы.");
                return;
            }

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    if (_id.HasValue)
                    {
                        const string sql = @"
UPDATE topics
SET name = $name,
    description = $description
WHERE id = $id;";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$id", _id.Value);
                            cmd.Parameters.AddWithValue("$name", txtName.Text.Trim());
                            cmd.Parameters.AddWithValue("$description",
                                string.IsNullOrWhiteSpace(txtDescription.Text)
                                    ? (object)DBNull.Value
                                    : txtDescription.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        const string sql = @"
INSERT INTO topics (name, description)
VALUES ($name, $description);";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$name", txtName.Text.Trim());
                            cmd.Parameters.AddWithValue("$description",
                                string.IsNullOrWhiteSpace(txtDescription.Text)
                                    ? (object)DBNull.Value
                                    : txtDescription.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения темы: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}