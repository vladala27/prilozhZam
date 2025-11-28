using System;
using System.Windows.Forms;

namespace WordsLearningApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK
                    && loginForm.AuthenticatedUser != null)
                {
                    Application.Run(new MainForm(loginForm.AuthenticatedUser));
                }
            }
        }
    }
}