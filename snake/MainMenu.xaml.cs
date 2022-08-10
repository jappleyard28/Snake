using System;
using System.Windows;

namespace snake
{
    public partial class MainMenu : Window
    {
        string username;

        public MainMenu(string username)
        {
            InitializeComponent();
            this.username = username;
        }

        private void PlaySnakeButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow dashboard = new MainWindow(username);
            dashboard.Show();
            this.Close();
        }

        private void LeaderboardButton_Click(object sender, RoutedEventArgs e)
        {
            //get data from the leaderboard table
            LeaderboardScreen dashboard = new LeaderboardScreen(username);
            dashboard.Show();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This is a snake game in which maths question will be asked and you will get four possible answers with 'A', 'B', 'C' or 'D' next to them. Use the arrow keys to navigate the snake to the item with the letter you think is the right answer. If you get the answer right, your score and snake length increases by one and if you get the question wrong, you lose one life. Every time you answer a question correctly, a new maths question is added.\n\nUse the power ups to help yourself win the game. There are three types of power ups: the blue power up slows down the snake for five seconds, the green power up shortens the snake by one without it affecting the score, and the purple power up tells the user the correct answer for five seconds. Collect a power up by colliding into it, and each time you do this it is added to a stack which is visible in the text box below the maths question. Use the power up at the top of the stack by pressing 'P' at any point in the game.\n\nThe game finishes if you crash into yourself, crash into the border, run out of lives or if the snake takes up all of the board (in which case you win the game) and once you die your score is added to the leaderboard if it beats your previous high score");
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginScreen dashboard = new LoginScreen();
            dashboard.Show();
            this.Close();
        }
    }
}
