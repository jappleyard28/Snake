using System;
using System.Windows;
using System.Data.SqlClient;
using System.Data;

namespace snake
{
    public partial class LoginScreen : Window
    {
        public LoginScreen()
        {
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection publicSqlConnection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Jack\Desktop\Programming\C#\snake\snakeDatabase.mdf;Integrated Security=True;Connect Timeout=30");
            try
            {
                if (publicSqlConnection.State == ConnectionState.Closed)
                {
                    publicSqlConnection.Open();
                }
                string sqlQuery = "SELECT COUNT(1) FROM UserDetails WHERE Username=@Username AND Password=@Password";
                SqlCommand sqlCommand = new SqlCommand(sqlQuery, publicSqlConnection);
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Parameters.AddWithValue("@Username", UsernameText.Text);
                sqlCommand.Parameters.AddWithValue("@Password", PasswordHash(PasswordText.Password));
                int count = Convert.ToInt32(sqlCommand.ExecuteScalar());
                if (count == 1)
                {
                    //this sends the user to the main menu
                    MainMenu dashboard = new MainMenu(UsernameText.Text);
                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Username or password is incorrect");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                publicSqlConnection.Close();
            }
        }

        public static string PasswordHash(string password)
        {
            long result = 0;
            for (int i = 0; i < password.Length; i++)
            {
                result += (char)(password[i] * i);
            }
            result *= 4;
            return result.ToString();
        }
        
        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            SignUpScreen dashboard = new SignUpScreen();
            dashboard.Show();
            this.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
