//make this check if the user exists before outputting their user. If the user does exist set their score to zero or just leave their score out
using System;
using System.Windows;
using System.Windows.Input;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Controls;
using System.Windows.Media;

namespace snake
{
    public partial class LeaderboardScreen : Window
    {
        public class LeaderboardPlayer
        {
            public string Name { get; private set; }
            public int Score { get; private set; }

            public LeaderboardPlayer(string name, int score)
            {
                Name = name;
                Score = score;
            }
        }

        public static SqlConnection publicSqlConnection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Jack\Desktop\Programming\C#\snake\snakeDatabase.mdf;Integrated Security=True;Connect Timeout=30");
        string username;
        int rowTracker = 1; //keeps track of which row to add the player name and score to in the grid
        int j = 0; //points to the playersList index in which the next player is so that it can be added to the leaderboard
        LeaderboardPlayer[] playersList = new LeaderboardPlayer[10];
        int rank = 0;
        public LeaderboardScreen(string username)
        {
            this.username = username;
            InitializeComponent();
            GetTopScores();
            //Math.Min returns the lowest number of the two arguments
            for (int i = 0; i < numOfPlayers; i++)
            {
                rank++;
                AddRow(playersList[j]);
            }
            //check if the current user is on the leaderboard
            //loop through playersList and check if any players are equal to the current user (username)
            bool running = true;
            int k = 0;
            bool userOnLeaderboard = false;
            while (running)
            {
                if (k == Math.Min(playersList.Length, numOfPlayers))
                {
                    running = false;
                }
                else
                {
                    if (username == playersList[k].Name)
                    {
                        running = false;
                        userOnLeaderboard = true;
                    }
                    k++;
                }
            }
            if (!userOnLeaderboard)
            {
                //current user isn't in the top 10 highest scores
                rank = GetUserRank(username);
                AddRow(SqlReadDb(username));
                numOfPlayers++;
            }
            SetColBorder(1);
            SetColBorder(2);
        }

        public void SetColBorder(int col)
        {
            Border colBorder = new Border();
            colBorder.SetValue(Grid.ColumnProperty, col);
            colBorder.SetValue(Grid.RowSpanProperty, numOfPlayers + 1);
            colBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            colBorder.BorderThickness = new Thickness(1, 0, 0, 0);
            LeaderboardGrid.Children.Add(colBorder);
        }
        public int GetUserRank(string personName)
        {
            publicSqlConnection.Open();
            string sqlQuery = ("SELECT Username FROM Leaderboard ORDER BY Score DESC;");
            SqlDataAdapter sqlRead = new SqlDataAdapter(sqlQuery, publicSqlConnection);
            DataTable dataTable = new DataTable();
            sqlRead.Fill(dataTable);
            bool running = true;
            int i = 0;
            while (running)
            {
                //if the database isn't empty
                if (dataTable.Rows.Count > i)
                {
                    string name = dataTable.Rows[i].Field<string>("Username");

                    if (personName == name)
                    {
                        running = false;
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    //player isn't in the database
                    running = false;
                }
            }
            publicSqlConnection.Close();
            return i + 1;
        }

        //--------------------------------------------------------
        int numOfPlayers; //counts the number of players in the database
        public void GetTopScores()
        {
            publicSqlConnection.Open();
            string sqlQuery = ("SELECT TOP 10 Username, Score FROM Leaderboard ORDER BY Score DESC;");
            SqlDataAdapter sqlRead = new SqlDataAdapter(sqlQuery, publicSqlConnection);
            DataTable dataTable = new DataTable();
            sqlRead.Fill(dataTable);
            LeaderboardPlayer result = null;
            bool running = true;
            int i = 0;
            while (running)
            {
                //if the database isn't empty
                if (i < 10)
                {
                    if (dataTable.Rows.Count > i)
                    {
                        string name = dataTable.Rows[i].Field<string>("Username");
                        int score = dataTable.Rows[i].Field<int>("Score");

                        result = new LeaderboardPlayer(name, score);
                        playersList[i] = result;
                        i++;
                    }
                    else
                    {
                        running = false;
                    }
                }
                else
                {
                    running = false;
                }
            }
            numOfPlayers = i;
            publicSqlConnection.Close();
        }

        public void AddRow(LeaderboardPlayer player)
        {
            //swap username for the username of the person at the top of the leaderboard
            //LeaderboardPlayer player = SqlReadDb(username); //SqlReadDb(username at position 0)

            RowDefinition row = new RowDefinition();
            row.Height = GridLength.Auto;
            LeaderboardGrid.RowDefinitions.Add(row);

            Label gridRank = new Label
            {
                Content = rank, //rank
                FontSize = 15
            };
            Grid.SetColumn(gridRank, 0);
            Grid.SetRow(gridRank, rowTracker);     //Grid.SetRow(label1, 2);
            LeaderboardGrid.Children.Add(gridRank);

            //--------------------------------------------------------

            Label gridUsername = new Label
            {
                Content = player.Name, //username
                FontSize = 15
            };
            Grid.SetColumn(gridUsername, 1);
            Grid.SetRow(gridUsername, rowTracker);     //Grid.SetRow(label1, 2);
            LeaderboardGrid.Children.Add(gridUsername);

            //--------------------------------------------------------

            Label gridScore = new Label
            {
                Content = player.Score, //score
                FontSize = 15
            };
            Grid.SetColumn(gridScore, 2);
            Grid.SetRow(gridScore, rowTracker);     //Grid.SetRow(label2, 2);
            LeaderboardGrid.Children.Add(gridScore);

            //--------------------------------------------------------

            rowTracker++;
            j++;
        }

        //make this check if the user exists because if someone registers,
        //then tries to access the leaderboard it returns an error
        public LeaderboardPlayer SqlReadDb(string playerName)
        {
            publicSqlConnection.Open();
            string sqlQuery = ("SELECT TOP 1 Score FROM Leaderboard WHERE Username = '" + playerName + "';");
            SqlDataAdapter sqlRead = new SqlDataAdapter(sqlQuery, publicSqlConnection);
            DataTable dataTable = new DataTable();
            sqlRead.Fill(dataTable);
            LeaderboardPlayer result = null;
            //if the database isn't empty
            if (dataTable.Rows.Count > 0)
            {
                var row = dataTable.Rows[0];

                string name = playerName;
                int score = row.Field<int>("Score");

                result = new LeaderboardPlayer(name, score);
            }
            publicSqlConnection.Close();
            return result;
        }
    }
}