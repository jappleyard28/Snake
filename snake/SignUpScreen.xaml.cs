using System;
using System.Data.SqlClient;
using System.Windows;

namespace snake
{
    public partial class SignUpScreen : Window
    {
        public static SqlConnection publicSqlConnection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Jack\Desktop\Programming\C#\snake\snakeDatabase.mdf;Integrated Security=True;Connect Timeout=30");
        public SignUpScreen()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            InvalidFields.Visibility = Visibility.Hidden;
            bool allowRegister = true;
            //checks whether part of the first name is a number
            bool name1ContainsNum = false;
            for (int i = 0; i < FirstNameText.Text.Length; i++)
            {
                if (char.IsDigit(FirstNameText.Text[i]))
                {
                    name1ContainsNum = true;
                }
            }
            if (name1ContainsNum || String.IsNullOrEmpty(FirstNameText.Text))
            {
                FirstNameAsterisk.Visibility = Visibility.Visible;
                allowRegister = false;
            }
            else
            {
                FirstNameAsterisk.Visibility = Visibility.Hidden;
            }
            //-----------------------------------------------
            //checks whether part of the surname is a number
            bool name2ContainsNum = false;
            for (int i = 0; i < SurnameText.Text.Length; i++)
            {
                if (char.IsDigit(SurnameText.Text[i]))
                {
                    name2ContainsNum = true;
                }
            }
            if (name2ContainsNum || String.IsNullOrEmpty(SurnameText.Text))
            {
                SurnameAsterisk.Visibility = Visibility.Visible;
                allowRegister = false;
            }
            else
            {
                SurnameAsterisk.Visibility = Visibility.Hidden;
            }
            //-----------------------------------------------
            if (String.IsNullOrEmpty(UsernameText.Text) || UsernameText.Text.Contains(" "))
            {
                UsernameAsterisk.Visibility = Visibility.Visible;
                allowRegister = false;
            }
            else
            {
                UsernameAsterisk.Visibility = Visibility.Hidden;
            }
            //-----------------------------------------------
            if (String.IsNullOrEmpty(PasswordText.Password))
            {
                PasswordAsterisk.Visibility = Visibility.Visible;
                allowRegister = false;
            }
            else
            {
                PasswordAsterisk.Visibility = Visibility.Hidden;
            }
            if (allowRegister)
            {
                using (SqlConnection publicSqlConnection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Jack\Desktop\Programming\C#\snake\snakeDatabase.mdf;Integrated Security=True;Connect Timeout=30"))
                {
                    bool exists = false; //this is used to check whether the inputted username already exists in the databases
                    publicSqlConnection.Open();
                    // create a command to check if the username exists
                    using (SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM UserDetails WHERE UserName = @UserName", publicSqlConnection))
                    {
                        sqlCommand.Parameters.AddWithValue("UserName", UsernameText.Text);
                        exists = (int)sqlCommand.ExecuteScalar() > 0;
                    }
                    publicSqlConnection.Close();
                    if (exists)
                    {
                        MessageBox.Show("This username is already being used by someone else");
                    }
                    else
                    {
                        //This adds the user's details into the 'UserDetails' table in the database
                        string sqlQuery = "INSERT INTO UserDetails (FirstName, Surname, Username, Password) values(@FirstNameText, @SurnameText, @UsernameText, @PasswordText);";
                        //"INSERT INTO Leaderboard (Username, Score) values('Hello', '" + playerScore + "');";
                        SqlDataAdapter sqlWrite = new SqlDataAdapter(sqlQuery, publicSqlConnection);
                        sqlWrite.SelectCommand.Parameters.AddWithValue("@FirstNameText", FirstNameText.Text);
                        sqlWrite.SelectCommand.Parameters.AddWithValue("@SurnameText", SurnameText.Text);
                        sqlWrite.SelectCommand.Parameters.AddWithValue("@UsernameText", UsernameText.Text);
                        sqlWrite.SelectCommand.Parameters.AddWithValue("@PasswordText", LoginScreen.PasswordHash(PasswordText.Password));
                        publicSqlConnection.Open();
                        sqlWrite.SelectCommand.ExecuteNonQuery();
                        publicSqlConnection.Close();

                        //This adds the username to the 'Leaderboard' table in the database and sets their score to zero
                        sqlQuery = "INSERT INTO Leaderboard (Username, Score) values(@FirstNameText, @Score);";
                        sqlWrite = new SqlDataAdapter(sqlQuery, publicSqlConnection);
                        sqlWrite.SelectCommand.Parameters.AddWithValue("@FirstNameText", UsernameText.Text);
                        sqlWrite.SelectCommand.Parameters.AddWithValue("@Score", 0);
                        publicSqlConnection.Open();
                        sqlWrite.SelectCommand.ExecuteNonQuery();
                        publicSqlConnection.Close();

                        MainMenu dashboard = new MainMenu(UsernameText.Text);
                        dashboard.Show();
                        this.Close();
                    }
                }
            }
            else
            {
                InvalidFields.Visibility = Visibility.Visible;
            }
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            LoginScreen dashboard = new LoginScreen();
            dashboard.Show();
            this.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}