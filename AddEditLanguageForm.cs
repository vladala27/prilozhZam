using System;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class AddEditLanguageForm : Form
    {
        private TextBox txtName;
        private TextBox txtCode;
        private Button btnSave;
        private Button btnCancel;

        private readonly int? _id;

        public AddEditLanguageForm()
        {
            _id = null;
            InitializeComponent();
        }

        public AddEditLanguageForm(int id) : this()
        {
            _id = id;
            this.Text = "Редактировать язык";
            LoadLanguage();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить язык";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(350, 160);

            Label lblName = new Label { Text = "Название:", Left = 20, Top = 20, Width = 100 };
            txtName = new TextBox { Left = 120, Top = 18, Width = 200 };

            Label lblCode = new Label { Text = "Код:", Left = 20, Top = 55, Width = 100 };
            txtCode = new TextBox { Left = 120, Top = 53, Width = 200 };

            btnSave = new Button { Text = "Сохранить", Left = 120, Top = 95, Width = 90 };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "Отмена", Left = 230, Top = 95, Width = 90 };
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblCode);
            Controls.Add(txtCode);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }

        private void LoadLanguage()
        {
            if (!_id.HasValue) return;

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = "SELECT name, code FROM languages WHERE id = $id;";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$id", _id.Value);
                        using (SqliteDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtName.Text = r.GetString(0);
                                txtCode.Text = r.GetString(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки языка: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название языка.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                MessageBox.Show("Введите код языка.");
                return;
            }

            try
            {
                using (SqliteConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    if (_id.HasValue)
                    {
                        const string sql = "UPDATE languages SET name = $name, code = $code WHERE id = $id;";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$id", _id.Value);
                            cmd.Parameters.AddWithValue("$name", txtName.Text.Trim());
                            cmd.Parameters.AddWithValue("$code", txtCode.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        const string sql = "INSERT INTO languages (name, code) VALUES ($name, $code);";
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("$name", txtName.Text.Trim());
                            cmd.Parameters.AddWithValue("$code", txtCode.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения языка: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}