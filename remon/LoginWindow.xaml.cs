using System;
using System.Linq;
using System.Windows;
using remon.models;

namespace remon
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTb.Text.Trim();
            string password = PasswordPb.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var db = new remonEntities1())
                {
                    var user = db.users.FirstOrDefault(u => u.Login == login && u.Password == password);
                    if (user == null)
                    {
                        MessageBox.Show("Неверный логин или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    MainWindow w = new MainWindow(user);
                    w.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GuestBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow w = new MainWindow(null);
            w.Show();
            this.Close();
        }
    }
}