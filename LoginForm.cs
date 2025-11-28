using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public class LoginForm : Form
    {
        private TextBox txtLogin;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnCancel;
        private Label lblLogin;
        private Label lblPassword;
        private Label lblInfo;

        public LoggedInUser AuthenticatedUser { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Авторизация";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(320, 180);

            lblLogin = new Label
            {
                Text = "Логин:",
                Left = 20,
                Top = 20,
                Width = 80
            };
            txtLogin = new TextBox
            {
                Left = 110,
                Top = 18,
                Width = 170
            };

            lblPassword = new Label
            {
                Text = "Пароль:",
                Left = 20,
                Top = 55,
                Width = 80
            };
            txtPassword = new TextBox
            {
                Left = 110,
                Top = 53,
                Width = 170,
                UseSystemPasswordChar = true
            };

            btnLogin = new Button
            {
                Text = "Войти",
                Left = 110,
                Top = 90,
                Width = 80
            };
            btnLogin.Click += BtnLogin_Click;

            btnCancel = new Button
            {
                Text = "Отмена",
                Left = 200,
                Top = 90,
                Width = 80
            };
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            lblInfo = new Label
            {
                Left = 20,
                Top = 130,
                Width = 260,
                ForeColor = System.Drawing.Color.Red,
                AutoSize = true
            };

            this.AcceptButton = btnLogin;
            this.CancelButton = btnCancel;

            this.Controls.Add(lblLogin);
            this.Controls.Add(txtLogin);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnCancel);
            this.Controls.Add(lblInfo);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            lblInfo.Text = "";

            var login = txtLogin.Text.Trim();
            var password = txtPassword.Text; // здесь можно заранее хэшировать

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                lblInfo.Text = "Введите логин и пароль";
                return;
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    const string sql = @"
                    SELECT id, login, role, full_name
                    FROM users
                    WHERE login = $login
                      AND password_hash = $password
                      AND is_active = 1
                    LIMIT 1;
                    ";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("$login", login);
                        cmd.Parameters.AddWithValue("$password", password); // или хэш

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                AuthenticatedUser = new LoggedInUser
                                {
                                    Id = reader.GetInt32(0),
                                    Login = reader.GetString(1),
                                    Role = reader.GetString(2),
                                    FullName = reader.IsDBNull(3) ? "" : reader.GetString(3)
                                };

                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            else
                            {
                                lblInfo.Text = "Неверный логин или пароль";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка при подключении к БД",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}