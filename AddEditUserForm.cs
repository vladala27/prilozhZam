using System;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class AddEditUserForm : Form
    {
        private TextBox txtLogin;
        private TextBox txtPassword;
        private ComboBox cbRole;
        private TextBox txtFullName;
        private CheckBox chkActive;
        private Button btnSave;
        private Button btnCancel;

        private readonly int? _userId;

        public AddEditUserForm()
        {
            _userId = null;
            InitializeComponent();
        }

        public AddEditUserForm(int userId) : this()
        {
            _userId = userId;
            this.Text = "Редактировать пользователя";
            LoadUser();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить пользователя";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(400, 250);

            var lblLogin = new Label
            {
                Text = "Логин:",
                Left = 20,
                Top = 20,
                Width = 120
            };
            txtLogin = new TextBox
            {
                Left = 150,
                Top = 18,
                Width = 200
            };

            var lblPassword = new Label
            {
                Text = "Пароль:",
                Left = 20,
                Top = 50,
                Width = 120
            };
            txtPassword = new TextBox
            {
                Left = 150,
                Top = 48,
                Width = 200,
                UseSystemPasswordChar = true
            };

            var lblRole = new Label
            {
                Text = "Роль:",
                Left = 20,
                Top = 80,
                Width = 120
            };
            cbRole = new ComboBox
            {
                Left = 150,
                Top = 78,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbRole.Items.Add("директор");
            cbRole.Items.Add("клиент");

            var lblFullName = new Label
            {
                Text = "ФИО:",
                Left = 20,
                Top = 110,
                Width = 120
            };
            txtFullName = new TextBox
            {
                Left = 150,
                Top = 108,
                Width = 200
            };

            chkActive = new CheckBox
            {
                Text = "Активен",
                Left = 150,
                Top = 140,
                Width = 100,
                Checked = true
            };

            btnSave = new Button
            {
                Text = "Сохранить",
                Left = 150,
                Top = 180,
                Width = 100
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Отмена",
                Left = 260,
                Top = 180,
                Width = 90
            };
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(lblLogin);
            this.Controls.Add(txtLogin);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(lblRole);
            this.Controls.Add(cbRole);
            this.Controls.Add(lblFullName);
            this.Controls.Add(txtFullName);
            this.Controls.Add(chkActive);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadUser()
        {
            if (!_userId.HasValue)
                return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    const string sql = @"
SELECT login, password_hash, role, full_name, is_active
FROM users
WHERE id = $id;
";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$id", _userId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtLogin.Text = reader.GetString(0);
                                txtPassword.Text = reader.GetString(1); // в примере пароль в открытом виде
                                var role = reader.GetString(2);
                                txtFullName.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                chkActive.Checked = reader.GetInt32(4) == 1;

                                cbRole.SelectedItem = role;
                            }
                            else
                            {
                                MessageBox.Show("Пользователь не найден.",
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
                MessageBox.Show("Ошибка загрузки пользователя: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Введите пароль.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cbRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    if (_userId.HasValue)
                        UpdateUser(conn);
                    else
                        InsertUser(conn);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (SqliteException sqlEx)
            {
                // Например, при нарушении уникальности логина
                MessageBox.Show("Ошибка сохранения пользователя: " + sqlEx.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения пользователя: " + ex,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InsertUser(SqliteConnection conn)
        {
            const string sql = @"
INSERT INTO users (login, password_hash, role, full_name, is_active)
VALUES ($login, $password_hash, $role, $full_name, $is_active);
";
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("$login", txtLogin.Text.Trim());
                cmd.Parameters.AddWithValue("$password_hash", txtPassword.Text); // тут можно использовать хэш
                cmd.Parameters.AddWithValue("$role", cbRole.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("$full_name",
                    string.IsNullOrWhiteSpace(txtFullName.Text)
                        ? (object)DBNull.Value
                        : txtFullName.Text.Trim());
                cmd.Parameters.AddWithValue("$is_active", chkActive.Checked ? 1 : 0);

                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateUser(SqliteConnection conn)
        {
            const string sql = @"
UPDATE users
SET login         = $login,
    password_hash = $password_hash,
    role          = $role,
    full_name     = $full_name,
    is_active     = $is_active
WHERE id = $id;
";
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("$id", _userId.Value);
                cmd.Parameters.AddWithValue("$login", txtLogin.Text.Trim());
                cmd.Parameters.AddWithValue("$password_hash", txtPassword.Text);
                cmd.Parameters.AddWithValue("$role", cbRole.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("$full_name",
                    string.IsNullOrWhiteSpace(txtFullName.Text)
                        ? (object)DBNull.Value
                        : txtFullName.Text.Trim());
                cmd.Parameters.AddWithValue("$is_active", chkActive.Checked ? 1 : 0);

                cmd.ExecuteNonQuery();
            }
        }
    }
}