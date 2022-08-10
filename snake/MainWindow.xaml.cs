using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.Data;

namespace snake
{
    //This struct is used when defining the arrays which hold coordinates
    public struct Coordinates
    {
        public double X { get; private set; }
        public double Y { get; private set; }

        public Coordinates(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    //this is used when using usernames and scores for the leaderboard
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

    //This is my implementation of a stack, and it stores ellipses in the stack
    public class PowerUpStack
    {
        int top = -1;
        Ellipse[] stack;
        public int StackSize { get; private set; }
        public PowerUpStack(int stackSize)
        {
            //when a new PowerUpStack is created
            StackSize = stackSize;
            top = -1;
            stack = new Ellipse[stackSize];
        }
        public void Push(Ellipse powerUpEllipse)
        {
            if (top >= StackSize)
            {
                MessageBox.Show("Stack Overflow");
            }
            else
            {
                stack[++top] = powerUpEllipse;
            }
        }
        public void Pop()
        {
            //if top < 0 there is a stack underflow
            if (top >= 0)
            {
                top--;
            }
        }
        public Ellipse Peek()
        {
            //if top < 0 there is a stack underflow (breaks) 
            return (stack[top]);
        }
        public int Count()
        {
            return (top + 1);
        }
    }

    public partial class MainWindow : Window
    {
        //These are used when working with the direction list to avoid confusion
        private const int UP = 0;
        private const int DOWN = 1;
        private const int LEFT = 2;
        private const int RIGHT = 3;

        //this connects it to the database I am using
        public static SqlConnection publicSqlConnection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Jack\Desktop\Programming\C#\snake\snakeDatabase.mdf;Integrated Security=True;Connect Timeout=30");

        Random random = new Random();
        public int snakeLength = 2;
        public int score = 2;
        public int numOfLives = 3;
        int head = 0; //I am also using this to point to which position to add a new element to (head of snake + 1)
        int tail = 0; //this points to the coordinates of the last element on the snake
        List<int> direction = new List<int>();
        //I am implementing the coordinates array as a circular array
        Coordinates[] snakeCoordinates = new Coordinates[625]; //canvas width * canvas height

        //Question items
        Coordinates[] itemCoordinates = new Coordinates[4]; //number of items that will be on the screen
        double[] itemValues = new double[4]; //this stores the answer value for each item when a question is asked
        TextBox[] items = new TextBox[4]; //used to remove the items from the canvas
        string[] lettersArray = new string[4]; //this is used to store the letters used in making the question items so that they can refer an answer
        string[] questionArray = new string[3]; //This stores the strings of the different parts of the question

        //power up items
        Coordinates[] powerUpCoords = new Coordinates[numOfPowUps];
        Ellipse[] powerUp = new Ellipse[numOfPowUps];

        //power up items display on the text box
        Ellipse[] readyPowerUp = new Ellipse[numOfPowUps];
        int readyPowUpPointer = 0;

        bool snakeAlive = true; //used for when the snake hits the border
        bool removePart = true; //this is used to decide whether or not to increase the snake's length
        bool firstMove = true; //this is used to stop the user from being able to move in the opposite direction to the direction it starts off facing in
        bool noSpam = true; //stops the user being able to make the snake move in the opposite direction if they spam a key
        Timer snakeTimer = new Timer(); //this is used so that the snake can move around at each interval
        Timer powerUpTimer = new Timer(); //this is used to make th power ups last for five seconds
        string username;

        //stack
        static int numOfPowUps = 6;
        PowerUpStack UserStack = new PowerUpStack(numOfPowUps);

        public MainWindow(string username)
        {
            this.username = username;
            InitializeComponent();

            snakeTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            snakeTimer.Interval = 500; //this is used to set the speed of the snake
            snakeTimer.Enabled = true;
            powerUpTimer.Elapsed += PowerUpTimer_Elapsed;
            powerUpTimer.Interval = 1000;
            powerUpTimer.Enabled = false;

            //This adds the coordinates of the snake when it is added onto the canvas when this window is opened
            snakeCoordinates[head] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake) - 20, Canvas.GetTop(Snake)));
            head++;
            snakeCoordinates[head] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake), Canvas.GetTop(Snake)));
            DrawSnake((head - 1) % 624);
            lettersArray[0] = "A";
            lettersArray[1] = "B";
            lettersArray[2] = "C";
            lettersArray[3] = "D";
            for (int i = 0; i < lettersArray.Length; i++)
            {
                this.Dispatcher.Invoke(() => DrawItem(i, lettersArray[i]));
            }

            for (int i = 0; i < powerUpCoords.Length; i++)
            {
                this.Dispatcher.Invoke(() => DrawPowerUp(i));
            }
            GetQuestion();
        }

        //this generates a random number
        public int RandomNumber(int lower, int upper)
        {
            return random.Next(lower, upper);
        }
        //---------------------------------------------------------------------------------------------------//
        int effectNum; //stores the effect of the power up it has just collided into

        //this draws a power up onto the canvas into a random position
        public void DrawPowerUp(int arrayPosition)
        {
            powerUpCoords[arrayPosition] = this.Dispatcher.Invoke(() => new Coordinates((RandomNumber(0, 25)) * 20, (RandomNumber(0, 25)) * 20));

            //stops the power up from spawning on an item
            for (int i = 0; i < itemCoordinates.Length; i++)
            {
                if ((powerUpCoords[arrayPosition].X == itemCoordinates[i].X) && (powerUpCoords[arrayPosition].Y == itemCoordinates[i].Y))
                {
                    DrawPowerUp(arrayPosition);
                    return;
                }
            }

            //stops the power up from spawning on itself
            for (int i = 0; i < powerUpCoords.Length; i++)
            {
                if ((powerUpCoords[arrayPosition].X == powerUpCoords[i].X) && (powerUpCoords[arrayPosition].Y == powerUpCoords[i].Y) && (arrayPosition != i))
                {
                    DrawPowerUp(arrayPosition);
                    return;
                }
            }
            //stop the power ups from spawning on the snake
            if ((head % 624) > (tail % 624))
            {
                for (int a = (tail % 624); a <= (head % 624); a++)
                {
                    for (int b = 0; b < powerUpCoords.Length; b++)
                    {
                        if ((snakeCoordinates[a].X == powerUpCoords[b].X) && (snakeCoordinates[a].Y == powerUpCoords[b].Y)) // && (a != b)
                        {
                            //respawns the power up if it spawns on the snake
                            DrawPowerUp(arrayPosition);
                            return;
                        }
                    }
                }
            }
            else if ((head % 624) < (tail % 624))
            {
                for (int a = (tail % 624); a < 625; a++)
                {
                    for (int b = 0; b < powerUpCoords.Length; b++)
                    {
                        if ((snakeCoordinates[a].X == powerUpCoords[b].X) && (snakeCoordinates[a].Y == powerUpCoords[b].Y))
                        {
                            //respawns the item if it spawns on the snake
                            DrawPowerUp(arrayPosition);
                            return;
                        }
                    }
                }
            }

            //creates a new ellipse and randomly chooses its colour
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20
            };
            int randomNum = RandomNumber(1, 4);
            if (randomNum == 1)
            {
                ellipse.Fill = Brushes.Green; //this colour causes the snake to shorten by 1
            }
            else if (randomNum == 2)
            {
                ellipse.Fill = Brushes.Blue; //this colour slows down the snake for 5 seconds
            }
            else
            {
                ellipse.Fill = Brushes.Purple; //this colour tells the user the answer for 5 seconds
            }
            Canvas.SetLeft(ellipse, powerUpCoords[arrayPosition].X);
            Canvas.SetTop(ellipse, powerUpCoords[arrayPosition].Y);
            powerUp[arrayPosition] = ellipse;
            SnakeCanvas.Children.Add(ellipse);
        }

        //This draws a question item box into a random place on the canvas
        public void DrawItem(int arrayPosition, string letter)
        {
            itemCoordinates[arrayPosition] = this.Dispatcher.Invoke(() => new Coordinates((RandomNumber(0, 25)) * 20, (RandomNumber(0, 25)) * 20));

            //stops the items from spawning on the power ups
            for (int i = 0; i < powerUpCoords.Length; i++)
            {
                if ((itemCoordinates[arrayPosition].X == powerUpCoords[i].X) && (itemCoordinates[arrayPosition].Y == powerUpCoords[i].Y))
                {
                    DrawItem(arrayPosition, letter);
                    return;
                }
            }

            //stops the items from spawning in the same place as each other
            for (int i = 0; i < itemCoordinates.Length; i++)
            {
                if ((itemCoordinates[arrayPosition].X == itemCoordinates[i].X) && (itemCoordinates[arrayPosition].Y == itemCoordinates[i].Y) && (arrayPosition != i))
                {
                    DrawItem(arrayPosition, letter);
                    return;
                }
            }
            //Stops the items from spawning on the snake
            if ((head % 624) > (tail % 624))
            {
                for (int a = (tail % 624); a <= (head % 624); a++)
                {
                    for (int b = 0; b < itemCoordinates.Length; b++)
                    {
                        if ((snakeCoordinates[a].X == itemCoordinates[b].X) && (snakeCoordinates[a].Y == itemCoordinates[b].Y)) // && (a != b)
                        {
                            //respawns the item if it spawns on the snake
                            DrawItem(arrayPosition, letter);
                            return;
                        }
                    }
                }
            }
            else if ((head % 624) < (tail % 624))
            {
                for (int a = (tail % 624); a < 625; a++)
                {
                    for (int b = 0; b < itemCoordinates.Length; b++)
                    {
                        if ((snakeCoordinates[a].X == itemCoordinates[b].X) && (snakeCoordinates[a].Y == itemCoordinates[b].Y))
                        {
                            //respawns the item if it spawns on the snake
                            DrawItem(arrayPosition, letter);
                            return;
                        }
                    }
                }
            }

            //Draws a new textbox onto the canvas which represents a question item box
            TextBox textBox = new TextBox
            {
                Text = letter,
                Width = 20,
                Height = 20,
                Background = Brushes.Red
            };
            Canvas.SetLeft(textBox, itemCoordinates[arrayPosition].X);
            Canvas.SetTop(textBox, itemCoordinates[arrayPosition].Y);
            items[arrayPosition] = textBox;
            SnakeCanvas.Children.Add(textBox);
        }

        //this writes data into the Leaderboard table
        private void SqlUpdateDb(LeaderboardPlayer player, string playerName, int playerScore)
        {
            if (player == null || player.Score < playerScore)
            {
                string sqlQuery = null;
                if (player != null)
                {
                    //read database score
                    sqlQuery = "UPDATE Leaderboard SET Score = '" + playerScore + "' WHERE Username = '" + playerName + "';";
                }
                else
                {
                    sqlQuery = "INSE" +
                        ".RT INTO Leaderboard (Username, Score) values('" + playerName + "', '" + playerScore + "');";
                }

                SqlDataAdapter sqlWrite = new SqlDataAdapter(sqlQuery, publicSqlConnection);
                publicSqlConnection.Open();
                sqlWrite.SelectCommand.ExecuteNonQuery();
                publicSqlConnection.Close();
            }
        }
        //this returns the player's name and score from the database
        public LeaderboardPlayer SqlReadDb(string playerName)
        {
            publicSqlConnection.Open();
            string sqlQuery = "SELECT TOP 1 Score FROM Leaderboard WHERE Username = '" + playerName + "';";
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


        //----------------------------------Question items----------------------------------//
        //GetQuestion() is used to randomly choose which type of question to ask the user
        public void GetQuestion()
        {
            int randomNum = RandomNumber(1, 4);
            if (randomNum == 1)
            {
                GetPercentageQuestion();
            }
            else if (randomNum == 2)
            {
                GetPythagQuestion();
            }
            else
            {
                GetMultQuestion();
            }
        }
        double answer;

        //this is used to generate a random multiplication question
        public void GetMultQuestion()
        {
            double a = Convert.ToDouble(RandomNumber(1, 21));
            double b = Convert.ToDouble(RandomNumber(1, 21));
            answer = a * b;
            questionArray[0] = "What is ";
            questionArray[1] = " multiplied by\n";
            questionArray[2] = "?";
            DisplayAnswers(a, b, true);
        }
        //this is used to generate a random percentage question
        public void GetPercentageQuestion()
        {
            double a = Convert.ToDouble(RandomNumber(1, 21)); //random number
            double b = (RandomNumber(1, 11)) * 10; //percentage
            answer = (a * b) / 100;
            questionArray[0] = "What is ";
            questionArray[1] = " percent of ";
            questionArray[2] = "?";
            DisplayAnswers(a, b, false);
        }
        //this is used to generate a random pythagoras question
        public void GetPythagQuestion()
        {
            //a and b are the length of the short sides of the right angled triangle, and the other side is the hypotenuse
            double a = Convert.ToDouble(RandomNumber(1, 16));
            double b = Convert.ToDouble(RandomNumber(1, 16));
            answer = Math.Pow((Math.Pow(a, 2) + Math.Pow(b, 2)), 0.5);
            if (answer % 1.0 == 0.0)
            {
                questionArray[0] = "In a right-angled triangle,\none side has a length of ";
                questionArray[1] = ",\nthe other side has a length\nof ";
                questionArray[2] = ".\nWhat is the hypotenuse?";
                DisplayAnswers(a, b, true);
            }
            else
            {
                GetPythagQuestion(); //call this method again until the hypotenuse is an integer
            }
        }
        //this is used to output the question
        public void DisplayAnswers(double a, double b, bool answerInt)
        {
            //this randomly chooses which letter to assign the correct answer to
            int n1 = RandomNumber(0, 4);
            for (int i = 0; i < itemValues.Length; i++)
            {
                if (n1 == i)
                {
                    itemValues[i] = answer;
                }
                else
                {
                    itemValues[i] = GetWrongAnswer(answer, answerInt);
                }
            }
            if (n1 == 0)
            {
                QuestionBox.Text = questionArray[0] + b + questionArray[1] + a + questionArray[2] + "\nA = " + answer + "\nB = " + itemValues[1] + "\nC = " + itemValues[2] + "\nD = " + itemValues[3];
            }
            else if (n1 == 1)
            {
                QuestionBox.Text = questionArray[0] + b + questionArray[1] + a + questionArray[2] + "\nA = " + itemValues[0] + "\nB = " + answer + "\nC = " + itemValues[2] + "\nD = " + itemValues[3];
            }
            else if (n1 == 2)
            {
                QuestionBox.Text = questionArray[0] + b + questionArray[1] + a + questionArray[2] + "\nA = " + itemValues[0] + "\nB = " + itemValues[1] + "\nC = " + answer + "\nD = " + itemValues[3];
            }
            else
            {
                QuestionBox.Text = questionArray[0] + b + questionArray[1] + a + questionArray[2] + "\nA = " + itemValues[0] + "\nB = " + itemValues[1] + "\nC = " + itemValues[2] + "\nD = " + answer;
            }
        }
        string[] traceTable = new string[7];
        //this generates an answer simimlar to the correct answer and makes sure that there aren't any duplicate answers
        public double GetWrongAnswer(double correctAnswer, bool answerInt)
        {
            traceTable[0] = correctAnswer.ToString();
            traceTable[1] = answerInt.ToString();
            //get a random number which has a range of 20 between itself and the correctAnswer
            int i = 10;
            traceTable[2] = i.ToString();

            //makes sure that the random number is greater than 0
            if (answerInt)
            {
                bool running = true;
                while (running)
                {
                    if ((correctAnswer - i) > 0)
                    {
                        running = false;
                    }
                    else
                    {
                        if (i > 0)
                        {
                            i--;
                        }
                        else
                        {
                            running = false;
                        }
                    }
                }
            }

            bool running2 = true;
            double wrongAnswer = 0;
            while (running2)
            {
                running2 = false;
                if (answerInt)
                {
                    wrongAnswer = RandomNumber((int)Math.Round(correctAnswer) - i, (int)Math.Round(correctAnswer) + 10);
                }
                else
                {
                    wrongAnswer = correctAnswer + (Math.Round(random.NextDouble() * 10.0) / 10.0);
                }
                for (int j = 0; j < itemValues.Length; j++)
                {
                    if ((wrongAnswer == itemValues[j]) || (wrongAnswer == correctAnswer)) //correctAnswer == itemValues[j]
                    {
                        running2 = true;
                    }
                }
            }
            return wrongAnswer;
        }
        //this is used to check whether or not the user got the question right
        public void CompareAnswers(int itemNo)
        {
            if (itemValues[itemNo] == answer)
            {
                CommentBox.Text = "Correct answer";
                AddSnakeParts();
                GetQuestion();
            }
            else
            {
                numOfLives--;
                LivesBox.Text = "Number of lives: " + numOfLives;
                CommentBox.Text = "Incorrect answer";
            }
        }
        //this checks whether or not the user got the question right, removes the item from the canvas and adds a new item onto the canvas with the same letter
        public void ItemCollision(int itemNo) //itemNo: A = 0, B = 1, C = 2, D = 3
        {
            CompareAnswers(itemNo);
            SnakeCanvas.Children.Remove(items[itemNo]);
            DrawItem(itemNo, lettersArray[itemNo]);
        }

        //Power ups
        int powerUpX = 510; //coordinates of where to add the next power up onto the text box
        int powerUpY = 330; //coordinates of where to add the next power up onto the text box

        //this adds the power up to the stack if the stack isn't full, and draw it onto the canvas
        public void PowerUpCollision(int powerUpNo)
        {
            if (UserStack.Count() < numOfPowUps)
            {
                //adds the power up to the stack
                UserStack.Push(powerUp[powerUpNo]);
                //draws the ellipse onto the text box showing the available ellipses
                Ellipse ellipse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = powerUp[powerUpNo].Fill
                };
                Canvas.SetLeft(ellipse, powerUpX);
                Canvas.SetTop(ellipse, powerUpY);
                readyPowerUp[readyPowUpPointer] = ellipse;
                readyPowUpPointer++;
                SnakeCanvas.Children.Add(ellipse);
                powerUpX += 22;
            }
            SnakeCanvas.Children.Remove(powerUp[powerUpNo]);
            DrawPowerUp(powerUpNo);
        }

        int time = 5; //this variable is used to determine how long the power up effects should last for
        //this timer outputs the effects of the power ups until it runs out of time
        private void PowerUpTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (time == 0)
            {
                powerUpTimer.Enabled = false;
                time = 5;
                snakeTimer.Interval = 50;
                this.Dispatcher.Invoke(() => CommentBox.Text = "");
            }
            else
            {
                if (effectNum == 1)
                {
                    snakeTimer.Interval = 200;
                    this.Dispatcher.Invoke(() => CommentBox.Text = "Slowing down snake\ntime left: " + time + " seconds");
                }
                if (effectNum == 2)
                {
                    this.Dispatcher.Invoke(() => CommentBox.Text = "Answer = " + answer + "\ntime left: " + time + " seconds");
                }
                time--;
            }
        }

        //this draws a new snake part (square) depending on the coordinates of the snake in snakeCoordinates
        public void DrawSnake(int arrayPosition) //arrayPosition is the position of the coordinates it should get from the snakeCoordinates array
        {
            Rectangle myRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Orange),
                Fill = new SolidColorBrush(Colors.Orange),
                Width = 20,
                Height = 20
            };
            Canvas.SetLeft(myRectangle, snakeCoordinates[arrayPosition].X);
            Canvas.SetTop(myRectangle, snakeCoordinates[arrayPosition].Y);
            SnakeCanvas.Children.Add(myRectangle);
        }
        //this timer runs for the whole time this game screen is open as it allows the snake to move around
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (snakeAlive)
            {
                if (direction.Count >= 1)
                {
                    //this puts the Canvas.GetLeft and Canvas.GetTop methods in the same thread as the GUI because otherwise it would be in the timer thread and the canvas doesn't exist there
                    if ((this.Dispatcher.Invoke(() => Canvas.GetLeft(Snake)) <= 0) && (direction[direction.Count - 1] == LEFT) || (this.Dispatcher.Invoke(() => Canvas.GetLeft(Snake)) >= 480) && (direction[direction.Count - 1] == RIGHT))
                    {
                        snakeAlive = false;
                        SqlUpdateDb(SqlReadDb(username), username, score);
                        snakeTimer.Dispose();
                        powerUpTimer.Dispose();
                    }
                    else if ((this.Dispatcher.Invoke(() => Canvas.GetTop(Snake)) <= 0) && (direction[direction.Count - 1] == UP) || (this.Dispatcher.Invoke(() => Canvas.GetTop(Snake)) >= 480) && (direction[direction.Count - 1] == DOWN))
                    {
                        snakeAlive = false;
                        SqlUpdateDb(SqlReadDb(username), username, score);
                        snakeTimer.Dispose();
                        powerUpTimer.Dispose();
                    }
                    else
                    {
                        if (numOfLives == 0)
                        {
                            snakeAlive = false;
                            SqlUpdateDb(SqlReadDb(username), username, score);
                            snakeTimer.Dispose();
                            powerUpTimer.Dispose();
                        }
                        else
                        {
                            SnakeMove();
                            removePart = true;
                            noSpam = true;
                        }
                    }
                }
            }
        }
        //this outputs the effects of the snake moving
        public void SnakeMove()
        {
            //if there is at least 1 element in the direction list and the most recent element is left
            if (direction.Count > 0 && direction[direction.Count - 1] == LEFT)
            {
                OppositeDirection(LEFT, RIGHT, true, true);
            }
            else if (direction.Count > 0 && direction[direction.Count - 1] == RIGHT)
            {
                OppositeDirection(RIGHT, LEFT, true, false);
            }
            else if (direction.Count > 0 && direction[direction.Count - 1] == UP)
            {
                OppositeDirection(UP, DOWN, false, true);
            }
            else if (direction.Count > 0 && direction[direction.Count - 1] == DOWN)
            {
                OppositeDirection(DOWN, UP, false, false);
            }
        }
        //AddSnakeParts() stops removeSnakeParts() from being called which causes the snake's length to increase by 1
        public void AddSnakeParts()
        {
            if (removePart)
            {
                snakeLength++;
                score++;
            }
            removePart = false; //stops part of the snake from being automatically removed at that timer interval
        }
        //this removes the part of the snake at the tail to make it shorter by 1
        public void RemoveSnakeParts(bool normalMove)
        {
            int counter = 1;
            bool running = true;
            //if the number of snake parts is less than or equal to the snake length + 1 (makes the smallest snake length 2)
            if ((snakeLength > 2) || normalMove) //stops the snake from having a snake length of less than 2
            {
                if (!normalMove)
                {
                    snakeLength--;
                    score--;
                }
                tail++;
                while (running)
                {
                    if (SnakeCanvas.Children[counter] is Rectangle)
                    {
                        SnakeCanvas.Children.RemoveAt(counter);
                        running = false;
                    }
                    counter++;
                }
            }
        }
        //this is a method to stop the user from being able to move in the opposite direction to itself
        public void OppositeDirection(int d1, int d2, bool horizontal, bool posOrNeg) //d1 = left, d2 = right
        {
            //if there is at least 1 element in the direction list and the most recent element is left
            if (direction.Count > 0 && direction[direction.Count - 1] == d1)
            {
                //if there is at least 2 elements in the direction list
                if (direction.Count > 1)
                {
                    //if the 2nd most recent element is right make the most recent and the 2nd most recent elements left
                    if (direction[direction.Count - 2] == d2)
                    {
                        direction.Add(d2);
                        direction.Add(d2);
                        SnakeMove();
                    }
                    else
                    {
                        if (snakeAlive)
                        {
                            head++;
                            for (int i = 0; i < itemCoordinates.Length; i++)
                            {
                                if ((snakeCoordinates[(head - 1) % 624].X == itemCoordinates[i].X) && (snakeCoordinates[(head - 1) % 624].Y == itemCoordinates[i].Y))
                                {
                                    this.Dispatcher.Invoke(() => ItemCollision(i));
                                }
                            }
                            for (int i = 0; i < powerUpCoords.Length; i++)
                            {
                                if ((snakeCoordinates[(head - 1) % 624].X == powerUpCoords[i].X) && (snakeCoordinates[(head - 1) % 624].Y == powerUpCoords[i].Y))
                                {
                                    this.Dispatcher.Invoke(() => PowerUpCollision(i));
                                }
                            }

                            if (removePart)
                            {
                                this.Dispatcher.Invoke(() => RemoveSnakeParts(true));
                            }
                            if (horizontal && posOrNeg) //if the most rececnt element in the directions list is left
                            {
                                //move the snake left
                                snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake) - 20, Canvas.GetTop(Snake)));
                            }
                            else if (horizontal && !posOrNeg)
                            {
                                //move the snake right
                                snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake) + 20, Canvas.GetTop(Snake)));
                            }
                            else if (!horizontal && posOrNeg) //if the most recent element in the directions list is up
                            {
                                //move the snake up
                                snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake), Canvas.GetTop(Snake) - 20));
                            }
                            else
                            {
                                //move the snake down
                                snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake), Canvas.GetTop(Snake) + 20));
                            }
                        }

                        //stops the snake from moving if it collides into itself
                        if ((head % 624) > (tail % 624))
                        {
                            for (int a = (tail % 624); a <= (head % 624); a++)
                            {
                                for (int b = (tail % 624); b <= (head % 624); b++)
                                {
                                    if ((snakeCoordinates[a].X == snakeCoordinates[b].X) && (snakeCoordinates[a].Y == snakeCoordinates[b].Y) && (a != b))
                                    {
                                        snakeAlive = false;
                                    }
                                }
                            }
                        }
                        else if ((head % 624) < (tail % 624))
                        {
                            for (int a = (tail % 624); a < 625; a++)
                            {
                                for (int b = (tail % 624); b < 625; b++)
                                {
                                    if ((snakeCoordinates[a].X == snakeCoordinates[b].X) && (snakeCoordinates[a].Y == snakeCoordinates[b].Y) && (a != b))
                                    {
                                        snakeAlive = false;
                                    }
                                }
                                for (int c = 0; c <= (head % 624); c++)
                                {
                                    if ((snakeCoordinates[a].X == snakeCoordinates[c].X) && (snakeCoordinates[a].Y == snakeCoordinates[c].Y) && (a != c))
                                    {
                                        snakeAlive = false;
                                    }
                                }
                            }
                            for (int a = 0; a <= (head % 624); a++)
                            {
                                for (int b = (tail % 624); b < 625; b++)
                                {
                                    if ((snakeCoordinates[a].X == snakeCoordinates[b].X) && (snakeCoordinates[a].Y == snakeCoordinates[b].Y) && (a != b))
                                    {
                                        snakeAlive = false;
                                    }
                                }
                                for (int c = 0; c <= (head % 624); c++)
                                {
                                    if ((snakeCoordinates[a].X == snakeCoordinates[c].X) && (snakeCoordinates[a].Y == snakeCoordinates[c].Y) && (a != c))
                                    {
                                        snakeAlive = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //win game
                            snakeAlive = false;
                            MessageBox.Show("Congratulations, you won");
                        }
                        //-----------------------------------------------------------------



                        if (snakeAlive)
                        {
                            this.Dispatcher.Invoke(() => Canvas.SetLeft(Snake, snakeCoordinates[(head % 624)].X));
                            this.Dispatcher.Invoke(() => Canvas.SetTop(Snake, snakeCoordinates[(head % 624)].Y));
                            this.Dispatcher.Invoke(() => DrawSnake((head - 1) % 624));
                        }
                        else
                        {
                            SqlUpdateDb(SqlReadDb(username), username, score);
                            snakeTimer.Dispose();
                            powerUpTimer.Dispose();
                        }
                    }
                }
                else
                {
                    if (snakeAlive)
                    {
                        head++;
                        for (int i = 0; i < itemCoordinates.Length; i++)
                        {
                            if ((snakeCoordinates[head - 1].X == itemCoordinates[i].X) && (snakeCoordinates[head - 1].Y == itemCoordinates[i].Y))
                            {
                                this.Dispatcher.Invoke(() => ItemCollision(i));
                            }
                        }
                        for (int i = 0; i < powerUpCoords.Length; i++)
                        {
                            if ((snakeCoordinates[head - 1].X == powerUpCoords[i].X) && (snakeCoordinates[head - 1].Y == powerUpCoords[i].Y))
                            {
                                this.Dispatcher.Invoke(() => PowerUpCollision(i));
                            }
                        }

                        if (removePart)
                        {
                            this.Dispatcher.Invoke(() => RemoveSnakeParts(true));
                        }
                        if (horizontal && posOrNeg)
                        {
                            //Move the snake left
                            snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake) - 20, Canvas.GetTop(Snake)));
                        }
                        else if (horizontal && !posOrNeg)
                        {
                            //Move the snake right
                            snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake) + 20, Canvas.GetTop(Snake)));
                        }
                        else if (!horizontal && posOrNeg)
                        {
                            //Move the snake up
                            snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake), Canvas.GetTop(Snake) - 20));
                        }
                        else
                        {
                            //Move the snake down
                            snakeCoordinates[(head % 624)] = this.Dispatcher.Invoke(() => new Coordinates(Canvas.GetLeft(Snake), Canvas.GetTop(Snake) + 20));
                        }
                    }

                    //checks to see if the snake has collided into itself
                    if ((head % 624) > (tail % 624))
                    {
                        for (int a = (tail % 624); a <= (head % 624); a++)
                        {
                            for (int b = (tail % 624); b <= (head % 624); b++)
                            {
                                if ((snakeCoordinates[a].X == snakeCoordinates[b].X) && (snakeCoordinates[a].Y == snakeCoordinates[b].Y) && (a != b))
                                {
                                    snakeAlive = false;
                                }
                            }
                        }
                    }
                    else if ((head % 624) < (tail % 624))
                    {
                        for (int a = (tail % 624); a < 625; a++)
                        {
                            for (int b = (tail % 624); b < 625; b++)
                            {
                                if ((snakeCoordinates[a].X == snakeCoordinates[b].X) && (snakeCoordinates[a].Y == snakeCoordinates[b].Y) && (a != b))
                                {
                                    snakeAlive = false;
                                }
                            }
                            for (int c = 0; c <= (head % 624); c++)
                            {
                                if ((snakeCoordinates[a].X == snakeCoordinates[c].X) && (snakeCoordinates[a].Y == snakeCoordinates[c].Y) && (a != c))
                                {
                                    snakeAlive = false;
                                }
                            }
                        }
                        for (int a = 0; a <= (head % 624); a++)
                        {
                            for (int b = (tail % 624); b < 625; b++)
                            {
                                if ((snakeCoordinates[a].X == snakeCoordinates[b].X) && (snakeCoordinates[a].Y == snakeCoordinates[b].Y) && (a != b))
                                {
                                    snakeAlive = false;
                                }
                            }
                            for (int c = 0; c <= (head % 624); c++)
                            {
                                if ((snakeCoordinates[a].X == snakeCoordinates[c].X) && (snakeCoordinates[a].Y == snakeCoordinates[c].Y) && (a != c))
                                {
                                    snakeAlive = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        //win game
                        snakeAlive = false;
                        MessageBox.Show("Congratulations, you won");
                    }
                    //-----------------------------------------------------------------



                    if (snakeAlive)
                    {
                        this.Dispatcher.Invoke(() => Canvas.SetLeft(Snake, snakeCoordinates[(head % 624)].X));
                        this.Dispatcher.Invoke(() => Canvas.SetTop(Snake, snakeCoordinates[(head % 624)].Y));
                        this.Dispatcher.Invoke(() => DrawSnake((head - 1) % 624));
                    }
                    else
                    {
                        SqlUpdateDb(SqlReadDb(username), username, score);
                        snakeTimer.Dispose();
                        powerUpTimer.Dispose();
                    }
                }
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (noSpam)
            {
                if (e.Key == Key.Left) //if the left arrow key is pressed
                {
                    if (!firstMove)
                    {
                        direction.Add(LEFT);
                        noSpam = false;
                    }
                }
                if (e.Key == Key.Right)
                {
                    direction.Add(RIGHT);
                    firstMove = false;
                    noSpam = false;
                }
                if (e.Key == Key.Up)
                {
                    direction.Add(UP);
                    firstMove = false;
                    noSpam = false;
                }
                if (e.Key == Key.Down)
                {
                    direction.Add(DOWN);
                    firstMove = false;
                    noSpam = false;
                }
            }
            //this needs to be pressed to use the poer ups
            if (e.Key == Key.P)
            {
                //remove the power up from the stack and output the effect depending on what colour it is
                if (UserStack.Count() > 0)
                {
                    readyPowUpPointer--;
                    powerUpX -= 22;
                    if (UserStack.Peek().Fill == Brushes.Green)
                    {
                        effectNum = 0;
                        this.Dispatcher.Invoke(() => RemoveSnakeParts(false));
                    }
                    else if (UserStack.Peek().Fill == Brushes.Blue)
                    {
                        effectNum = 1;
                    }
                    else if (UserStack.Peek().Fill == Brushes.Purple)
                    {
                        effectNum = 2;
                    }
                    powerUpTimer.Enabled = true;
                    UserStack.Pop();
                    SnakeCanvas.Children.Remove(readyPowerUp[readyPowUpPointer]);
                }
            }
            if (e.Key == Key.R)
            {
                CommentBox.Text = "UserStack.Peek():";
                Ellipse ellipse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = UserStack.Peek().Fill
                };
                Canvas.SetLeft(ellipse, 510);
                Canvas.SetTop(ellipse, 422);
                SnakeCanvas.Children.Add(ellipse);
            }
            if (e.Key == Key.T)
            {
                CommentBox.Text = "UserStack.Count(): " + UserStack.Count();
            }
        }
        private void QuitGameButton_Click(object sender, RoutedEventArgs e)
        {
            MainMenu dashboard = new MainMenu(username);
            dashboard.Show();
            this.Close();
        }
    }
}