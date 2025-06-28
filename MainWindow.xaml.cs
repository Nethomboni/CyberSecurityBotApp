using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic; // Make sure this is included for List<T>

namespace CyberSecurityBotApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Static instances of our logic classes
        private static TaskManager taskManager = new TaskManager();
        private static QuizManager quizManager = new QuizManager();
        private static ChatbotNLP chatbotNLP = new ChatbotNLP();
        private static ActivityLogger activityLogger = new ActivityLogger();

        private string userName = "Guest"; // Default user name

        public MainWindow()
        {
            InitializeComponent();
            // Set initial chat output background to black for better contrast with colored text
            ChatOutputTextBlock.Background = Brushes.Black;
        }

        // --- Initialization ---
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Simulate voice greeting and log it
            AppendToChatOutput("(Simulating voice greeting...) Hello! I'm your Cybersecurity Awareness Bot. Let's learn to stay safe online!", Brushes.Green);
            activityLogger.LogAction("Voice greeting played (simulated).", "Initialization");

            // Prompt for user name (simple input dialog for GUI)
            InputUserName();

            // Initial welcome message
            WelcomeMessageTextBlock.Text = $"Welcome, {userName}! I'm your Cybersecurity Awareness Bot. Let's keep you safe online!";
            activityLogger.LogAction($"User '{userName}' started the bot.", "Bot Start");

            // Display initial commands/help in chat area
            DisplayAvailableCommands();

            // Setup task list box data context
            TasksListBox.ItemsSource = taskManager.GetAllTasks();

            // Set up timer for reminders (simplified for this example)
            System.Windows.Threading.DispatcherTimer reminderTimer = new System.Windows.Threading.DispatcherTimer();
            reminderTimer.Interval = TimeSpan.FromMinutes(1); // Check every minute
            reminderTimer.Tick += ReminderTimer_Tick;
            reminderTimer.Start();
        }

        private void InputUserName()
        {
            // Simple input dialog for user name
            string input = Microsoft.VisualBasic.Interaction.InputBox("Hello! What's your name?", "Welcome", "Guest");
            if (!string.IsNullOrWhiteSpace(input))
            {
                userName = input.Trim();
            }
        }

        private void AppendToChatOutput(string text, Brush color = null)
        {
            Dispatcher.Invoke(() => // Ensure UI update is on the correct thread
            {
                if (color == null)
                {
                    color = Brushes.White; // Default color
                }

                // Create a new Run for each message to apply specific color
                System.Windows.Documents.Run run = new System.Windows.Documents.Run(text + Environment.NewLine);
                run.Foreground = color;
                ChatOutputTextBlock.Inlines.Add(run);

                ChatOutputTextBlock.Text += Environment.NewLine; // Add an extra newline for spacing
                // Scroll to the end
                var scrollViewer = GetScrollViewer(ChatOutputTextBlock);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToEnd();
                }
            });
        }

        // Helper to get ScrollViewer from TextBlock (needed for scrolling)
        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer viewer)
            {
                return viewer;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                ScrollViewer result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }


        private void DisplayAvailableCommands()
        {
            string commands = "\nYou can ask me about:\n" +
                              "- What is cybersecurity?\n" +
                              "- Password safety\n" +
                              "- Phishing\n" +
                              "- Safe Browse\n" +
                              "- Social engineering\n" +
                              "- Two-factor authentication\n" +
                              "- Ransomware\n\n" +
                              "You can also use these commands:\n" +
                              "- 'Add task: <title>; <description>; [reminder: <YYYY-MM-DD HH:MM>]'\n" +
                              "- 'List tasks'\n" +
                              "- 'Complete task: <task ID or part of title>'\n" +
                              "- 'Delete task: <task ID or part of title>'\n" +
                              "- 'Start quiz'\n" +
                              "- 'View log'\n" +
                              "(Type 'exit' to quit.)\n";
            AppendToChatOutput(commands, Brushes.Cyan);
        }

        // --- Chat Interaction ---
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessUserInput();
        }

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessUserInput();
                e.Handled = true; // Prevent default Enter key behavior (e.g., newline in textbox)
            }
        }

        private void ProcessUserInput()
        {
            string userInput = UserInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userInput) || userInput == "Type your question or command here...")
            {
                return;
            }

            AppendToChatOutput($"[You]: {userInput}", Brushes.Yellow);
            activityLogger.LogAction($"User input: '{userInput}'", "User Input");

            UserInputTextBox.Text = ""; // Clear input box

            string lowerUserInput = userInput.ToLower();

            if (lowerUserInput == "exit")
            {
                AppendToChatOutput("\n[Chatbot]: Stay safe online! Goodbye. 👋", Brushes.White);
                activityLogger.LogAction("User exited the bot.", "Bot End");
                Application.Current.Shutdown(); // Close the application
                return;
            }

            // --- Handle specific commands ---
            if (HandleTaskCommands(lowerUserInput)) { /* Handled */ }
            else if (HandleQuizCommands(lowerUserInput)) { /* Handled */ }
            else if (HandleLogCommands(lowerUserInput)) { /* Handled */ }
            else
            {
                // General chatbot response
                string botResponse = chatbotNLP.GetResponse(userInput);
                AppendToChatOutput($"[Chatbot]: {botResponse}", Brushes.White);
                activityLogger.LogAction($"Bot response: '{botResponse.Replace("\n", " ").Substring(0, Math.Min(botResponse.Length, 100))}'", "Bot Response");
            }
        }

        // --- Task Management Handlers ---
        private bool HandleTaskCommands(string lowerUserQuestion)
        {
            if (lowerUserQuestion.StartsWith("add task:"))
            {
                ParseAndAddTaskFromChat(lowerUserQuestion);
                return true;
            }
            else if (lowerUserQuestion == "list tasks" || lowerUserQuestion == "show tasks")
            {
                ListTasks();
                return true;
            }
            else if (lowerUserQuestion.StartsWith("complete task:"))
            {
                string query = lowerUserQuestion.Substring("complete task:".Length).Trim();
                HandleTaskActionFromChat(query, "complete");
                return true;
            }
            else if (lowerUserQuestion.StartsWith("delete task:"))
            {
                string query = lowerUserQuestion.Substring("delete task:".Length).Trim();
                HandleTaskActionFromChat(query, "delete");
                return true;
            }
            return false;
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text.Trim();
            string description = TaskDescriptionTextBox.Text.Trim();
            DateTime? reminderTime = null;

            if (ReminderDatePicker.SelectedDate.HasValue)
            {
                // Combine date with time from TextBox
                string timeString = ReminderTimeTextBox.Text.Trim();
                if (TimeSpan.TryParse(timeString, out TimeSpan time))
                {
                    reminderTime = ReminderDatePicker.SelectedDate.Value.Date + time;
                }
                else
                {
                    AppendToChatOutput("[Chatbot]: Invalid reminder time format. Please use HH:MM. Task added without reminder.", Brushes.Red);
                    activityLogger.LogAction($"Failed to parse reminder time: '{timeString}'", "Error");
                }
            }

            if (string.IsNullOrEmpty(title))
            {
                AppendToChatOutput("[Chatbot]: Task title cannot be empty.", Brushes.Red);
                activityLogger.LogAction("Attempted to add task with empty title.", "Error");
                return;
            }

            taskManager.AddTask(title, description, reminderTime);
            AppendToChatOutput($"[Chatbot]: Task '{title}' added successfully.", Brushes.Green);
            activityLogger.LogAction($"Added task: '{title}'", "Task Added");

            // Clear input fields and refresh list
            TaskTitleTextBox.Clear();
            TaskDescriptionTextBox.Clear();
            ReminderDatePicker.SelectedDate = null;
            ReminderTimeTextBox.Text = "HH:MM";
            RefreshTasksListBox();
        }

        private void MarkTaskCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListBox.SelectedItem is CyberSecurityTask selectedTask)
            {
                if (taskManager.MarkTaskAsCompleted(selectedTask.Id))
                {
                    AppendToChatOutput($"[Chatbot]: Task '{selectedTask.Title}' marked as completed.", Brushes.Green);
                    activityLogger.LogAction($"Task '{selectedTask.Title}' marked as completed.", "Task Completed");
                }
                else
                {
                    AppendToChatOutput($"[Chatbot]: Task '{selectedTask.Title}' is already completed or could not be found.", Brushes.Yellow);
                }
                RefreshTasksListBox();
            }
            else
            {
                AppendToChatOutput("[Chatbot]: Please select a task to mark as complete.", Brushes.Red);
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListBox.SelectedItem is CyberSecurityTask selectedTask)
            {
                if (MessageBox.Show($"Are you sure you want to delete '{selectedTask.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    if (taskManager.DeleteTask(selectedTask.Id))
                    {
                        AppendToChatOutput($"[Chatbot]: Task '{selectedTask.Title}' deleted.", Brushes.Green);
                        activityLogger.LogAction($"Task '{selectedTask.Title}' deleted.", "Task Deleted");
                    }
                    else
                    {
                        AppendToChatOutput($"[Chatbot]: Could not delete task '{selectedTask.Title}'.", Brushes.Red);
                    }
                    RefreshTasksListBox();
                }
            }
            else
            {
                AppendToChatOutput("[Chatbot]: Please select a task to delete.", Brushes.Red);
            }
        }

        private void RefreshTasksListBox()
        {
            TasksListBox.ItemsSource = null; // Clear existing items
            TasksListBox.ItemsSource = taskManager.GetAllTasks(); // Rebind to refresh
        }

        // Helper for parsing task commands from chat input
        private void ParseAndAddTaskFromChat(string input)
        {
            string taskDetails = input.Substring("add task:".Length).Trim();
            string title = "";
            string description = "";
            DateTime? reminderTime = null;

            string[] parts = taskDetails.Split(new char[] { ';' }, 3);

            if (parts.Length > 0) { title = parts[0].Trim(); }
            if (parts.Length > 1) { description = parts[1].Trim(); }

            if (parts.Length > 2)
            {
                string reminderPart = parts[2].Trim();
                if (reminderPart.StartsWith("reminder:", StringComparison.OrdinalIgnoreCase))
                {
                    string dateTimeString = reminderPart.Substring("reminder:".Length).Trim();
                    if (DateTime.TryParse(dateTimeString, out DateTime parsedDateTime))
                    {
                        reminderTime = parsedDateTime;
                    }
                    else
                    {
                        AppendToChatOutput("[Chatbot]: Invalid reminder date/time format. Please use YYYY-MM-DD HH:MM. Task added without reminder.", Brushes.Red);
                        activityLogger.LogAction($"Failed to parse reminder from chat: '{dateTimeString}'", "Error");
                    }
                }
            }

            if (string.IsNullOrEmpty(title))
            {
                AppendToChatOutput("[Chatbot]: Task title cannot be empty. Please use format: 'Add task: <title>; <description>; [reminder: <YYYY-MM-DD HH:MM>]'", Brushes.Red);
                activityLogger.LogAction("Attempted to add task via chat with empty title.", "Error");
                return;
            }

            taskManager.AddTask(title, description, reminderTime);
            AppendToChatOutput($"[Chatbot]: Task '{title}' added successfully.", Brushes.Green);
            activityLogger.LogAction($"Added task via chat: '{title}'", "Task Added");
            RefreshTasksListBox();
        }

        private void ListTasks()
        {
            var tasks = taskManager.GetAllTasks();
            if (tasks.Any())
            {
                string taskListString = "\n[Chatbot]: Here are your tasks:\n";
                foreach (var task in tasks)
                {
                    taskListString += $"  ID: {task.Id.ToString().Substring(0, 8)}... | {task}\n";
                }
                AppendToChatOutput(taskListString, Brushes.Green);
            }
            else
            {
                AppendToChatOutput("[Chatbot]: You don't have any tasks yet. Try 'Add task: <title>; <description>'", Brushes.White);
            }
            activityLogger.LogAction("Listed all tasks.", "Task List");
        }

        private void HandleTaskActionFromChat(string query, string actionType)
        {
            Guid taskId;
            bool byId = Guid.TryParse(query, out taskId);
            bool success = false;
            string taskTitle = "";

            if (byId)
            {
                var task = taskManager.GetAllTasks().FirstOrDefault(t => t.Id == taskId);
                if (task != null) taskTitle = task.Title;
                if (actionType == "complete")
                    success = taskManager.MarkTaskAsCompleted(taskId);
                else if (actionType == "delete")
                    success = taskManager.DeleteTask(taskId);
            }
            else
            {
                var matchingTasks = taskManager.GetAllTasks()
                                               .Where(t => t.Title.ToLower().Contains(query.ToLower()) && !t.IsCompleted)
                                               .ToList();
                if (matchingTasks.Count == 1)
                {
                    taskTitle = matchingTasks[0].Title;
                    if (actionType == "complete")
                        success = taskManager.MarkTaskAsCompleted(matchingTasks[0].Id);
                    else if (actionType == "delete")
                        success = taskManager.DeleteTask(matchingTasks[0].Id);
                }
                else if (matchingTasks.Count > 1)
                {
                    string ambiguousMessage = $"[Chatbot]: Multiple tasks match '{query}'. Please be more specific or use the full task ID:\n";
                    foreach (var task in matchingTasks)
                    {
                        ambiguousMessage += $"  ID: {task.Id.ToString().Substring(0, 8)}... | {task.Title}\n";
                    }
                    AppendToChatOutput(ambiguousMessage, Brushes.Yellow);
                    return;
                }
            }

            if (success)
            {
                AppendToChatOutput($"[Chatbot]: Task '{taskTitle}' successfully {actionType}d.", Brushes.Green);
                activityLogger.LogAction($"Task '{taskTitle}' {actionType}d successfully via chat.", $"Task {actionType.CapitalizeFirst()}");
                RefreshTasksListBox();
            }
            else
            {
                AppendToChatOutput($"[Chatbot]: No pending task found matching '{query}' or task already completed/deleted.", Brushes.Red);
                activityLogger.LogAction($"Failed to {actionType} task: '{query}' (not found).", "Error");
            }
        }

        // --- Reminder Timer ---
        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            var dueReminders = taskManager.GetDueReminders();
            foreach (var task in dueReminders)
            {
                // In a real app, you might use a dedicated notification system
                MessageBox.Show($"Reminder: Task '{task.Title}' is due now!\nDescription: {task.Description}", "CyberSecurity Bot Reminder", MessageBoxButton.OK, MessageBoxImage.Information);
                taskManager.MarkTaskAsCompleted(task.Id); // Mark as complete after reminder (optional)
                activityLogger.LogAction($"Reminder for task '{task.Title}' displayed.", "Reminder");
                RefreshTasksListBox(); // Refresh list to show task as completed if marked so
            }
        }

        private void ReminderTimeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ReminderTimeTextBox.Text == "HH:MM")
            {
                ReminderTimeTextBox.Text = "";
                ReminderTimeTextBox.Foreground = Brushes.Black; // Change color when user types
            }
        }

        private void ReminderTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReminderTimeTextBox.Text))
            {
                ReminderTimeTextBox.Text = "HH:MM";
                ReminderTimeTextBox.Foreground = Brushes.Gray; // Change back to default color
            }
        }


        // --- Quiz Game Handlers ---
        private bool HandleQuizCommands(string lowerUserQuestion)
        {
            if (lowerUserQuestion == "start quiz")
            {
                StartQuizUI();
                return true;
            }
            return false;
        }

        private void StartQuizButton_Click(object sender, RoutedEventArgs e)
        {
            StartQuizUI();
        }

        private void StartQuizUI()
        {
            quizManager.ResetQuiz();
            activityLogger.LogAction("Started a new cybersecurity quiz.", "Quiz Attempt");
            QuizFeedbackTextBlock.Text = "";
            QuizScoreTextBlock.Text = "";
            StartQuizButton.Visibility = Visibility.Collapsed; // Hide start button
            DisplayNextQuizQuestionUI();
        }

        private void DisplayNextQuizQuestionUI()
        {
            QuizQuestion currentQuestion = quizManager.GetCurrentQuestion();
            if (currentQuestion != null)
            {
                QuizQuestionTextBlock.Text = $"Question {quizManager.GetQuestionNumber()} of {quizManager.GetTotalQuestions()}:\n{currentQuestion.Question}";
                Option1RadioButton.Content = currentQuestion.Options.Count > 0 ? currentQuestion.Options[0] : "";
                Option2RadioButton.Content = currentQuestion.Options.Count > 1 ? currentQuestion.Options[1] : "";
                Option3RadioButton.Content = currentQuestion.Options.Count > 2 ? currentQuestion.Options[2] : "";
                Option4RadioButton.Content = currentQuestion.Options.Count > 3 ? currentQuestion.Options[3] : "";

                // Reset radio buttons
                Option1RadioButton.IsChecked = false;
                Option2RadioButton.IsChecked = false;
                Option3RadioButton.IsChecked = false;
                Option4RadioButton.IsChecked = false;

                // Make options visible based on count
                Option1RadioButton.Visibility = currentQuestion.Options.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                Option2RadioButton.Visibility = currentQuestion.Options.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                Option3RadioButton.Visibility = currentQuestion.Options.Count > 2 ? Visibility.Visible : Visibility.Collapsed;
                Option4RadioButton.Visibility = currentQuestion.Options.Count > 3 ? Visibility.Visible : Visibility.Collapsed;

            }
            else
            {
                DisplayQuizResultsUI();
            }
        }

        private void QuizOption_Click(object sender, RoutedEventArgs e)
        {
            RadioButton clickedButton = sender as RadioButton;
            if (clickedButton == null) return;

            int selectedOptionIndex = -1;
            if (clickedButton == Option1RadioButton) selectedOptionIndex = 0;
            else if (clickedButton == Option2RadioButton) selectedOptionIndex = 1;
            else if (clickedButton == Option3RadioButton) selectedOptionIndex = 2;
            else if (clickedButton == Option4RadioButton) selectedOptionIndex = 3;

            if (selectedOptionIndex != -1)
            {
                SubmitQuizAnswerUI(selectedOptionIndex);
            }
        }

        private void SubmitQuizAnswerUI(int selectedOptionIndex)
        {
            if (quizManager.IsQuizFinished())
            {
                QuizFeedbackTextBlock.Text = "The quiz is already finished! Click 'Start Quiz' to play again.";
                return;
            }

            bool isCorrect = quizManager.SubmitAnswer(selectedOptionIndex);
            QuizQuestion currentQuestion = quizManager.GetCurrentQuestion(); // Get question *before* advancing for feedback

            if (isCorrect)
            {
                QuizFeedbackTextBlock.Text = "Correct! 🎉";
                QuizFeedbackTextBlock.Foreground = Brushes.Green;
            }
            else
            {
                QuizFeedbackTextBlock.Text = $"Incorrect. The correct answer was: {currentQuestion.Options[currentQuestion.CorrectAnswerIndex]}";
                QuizFeedbackTextBlock.Foreground = Brushes.Red;
            }
            activityLogger.LogAction($"Answered quiz question {quizManager.GetQuestionNumber()}: {(isCorrect ? "Correct" : "Incorrect")}", "Quiz Answer");

            quizManager.NextQuestion();
            if (!quizManager.IsQuizFinished())
            {
                // Give a moment for user to read feedback before next question
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => DisplayNextQuizQuestionUI());
                });
            }
            else
            {
                DisplayQuizResultsUI();
            }
        }

        private void DisplayQuizResultsUI()
        {
            QuizQuestionTextBlock.Text = "Quiz Finished!";
            string resultText = $"You scored {quizManager.GetScore()} out of {quizManager.GetTotalQuestions()}!\n";
            if (quizManager.GetScore() >= quizManager.GetTotalQuestions() * 0.7)
            {
                resultText += "Excellent work! You're a cybersecurity pro! 🌟";
                QuizScoreTextBlock.Foreground = Brushes.Gold;
            }
            else if (quizManager.GetScore() >= quizManager.GetTotalQuestions() * 0.5)
            {
                resultText += "Good effort! Keep learning to boost your cybersecurity knowledge. 👍";
                QuizScoreTextBlock.Foreground = Brushes.LightGreen;
            }
            else
            {
                resultText += "You did well! There's always more to learn about cybersecurity. Let's keep exploring! 💪";
                QuizScoreTextBlock.Foreground = Brushes.Orange;
            }
            QuizScoreTextBlock.Text = resultText;
            QuizFeedbackTextBlock.Text = ""; // Clear feedback

            StartQuizButton.Visibility = Visibility.Visible; // Show start button again
            activityLogger.LogAction($"Quiz finished. Score: {quizManager.GetScore()}/{quizManager.GetTotalQuestions()}", "Quiz Result");

            // Hide radio buttons
            Option1RadioButton.Visibility = Visibility.Collapsed;
            Option2RadioButton.Visibility = Visibility.Collapsed;
            Option3RadioButton.Visibility = Visibility.Collapsed;
            Option4RadioButton.Visibility = Visibility.Collapsed;
        }

        // --- Activity Log Handler ---
        private bool HandleLogCommands(string lowerUserQuestion)
        {
            if (lowerUserQuestion == "view log" || lowerUserQuestion == "show log" || lowerUserQuestion == "activity log")
            {
                DisplayActivityLogUI();
                return true;
            }
            return false;
        }

        private void DisplayActivityLogUI()
        {
            var log = activityLogger.GetActivityLog();
            if (log.Any())
            {
                string logText = "";
                foreach (var entry in log)
                {
                    logText += entry.ToString() + Environment.NewLine;
                }
                ActivityLogTextBlock.Text = logText;
                ActivityLogTextBlock.Foreground = Brushes.LightGray; // Make log text a bit lighter
            }
            else
            {
                ActivityLogTextBlock.Text = "The activity log is currently empty.";
                ActivityLogTextBlock.Foreground = Brushes.Gray;
            }
        }

        // Ensure your core logic classes are defined here or in separate files in the same namespace
        // (Copy/paste the CyberSecurityTask, TaskManager, QuizQuestion, QuizManager, ChatbotNLP,
        // ActivityLogEntry, ActivityLogger, and StringExtensions classes here, outside the MainWindow class
        // but within the CyberSecurityBotApp namespace)
    }

    // --- Core Logic Classes (Copy these from the previous full code block) ---

    // Place all classes below this line, within the 'namespace CyberSecurityBotApp' block
    // but outside the 'MainWindow' class.

    // --- Helper Extensions for string capitalization ---
    public static class StringExtensions
    {
        public static string CapitalizeFirst(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }

    // --- Task Management Classes ---
    public class CyberSecurityTask
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReminderTime { get; set; }
        public bool IsCompleted { get; set; }

        public CyberSecurityTask()
        {
            Id = Guid.NewGuid();
        }

        public override string ToString()
        {
            string status = IsCompleted ? "[Completed]" : "[Pending]";
            string reminder = ReminderTime.HasValue ? $" (Reminder: {ReminderTime.Value:g})" : "";
            return $"{status} {Title}: {Description}{reminder}";
        }
    }

    public class TaskManager
    {
        private List<CyberSecurityTask> tasks = new List<CyberSecurityTask>();

        public void AddTask(string title, string description, DateTime? reminderTime = null)
        {
            CyberSecurityTask newTask = new CyberSecurityTask
            {
                Title = title,
                Description = description,
                ReminderTime = reminderTime,
                IsCompleted = false
            };
            tasks.Add(newTask);
        }

        public List<CyberSecurityTask> GetAllTasks()
        {
            return tasks;
        }

        public bool MarkTaskAsCompleted(Guid taskId)
        {
            CyberSecurityTask task = tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && !task.IsCompleted)
            {
                task.IsCompleted = true;
                return true;
            }
            return false;
        }

        public bool DeleteTask(Guid taskId)
        {
            CyberSecurityTask taskToRemove = tasks.FirstOrDefault(t => t.Id == taskId);
            if (taskToRemove != null)
            {
                tasks.Remove(taskToRemove);
                return true;
            }
            return false;
        }

        public List<CyberSecurityTask> GetDueReminders()
        {
            // Note: In a production WPF app, consider a more robust timer/background service for reminders
            // This is checked periodically by the DispatcherTimer in MainWindow
            return tasks.Where(t => t.ReminderTime.HasValue && t.ReminderTime.Value <= DateTime.Now && !t.IsCompleted).ToList();
        }
    }


    // --- Quiz Game Classes ---
    public class QuizQuestion
    {
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public int CorrectAnswerIndex { get; set; } // 0-based index
    }

    public class QuizManager
    {
        private List<QuizQuestion> questions;
        private int currentQuestionIndex;
        private int score;

        public QuizManager()
        {
            questions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What is phishing?",
                    Options = new List<string> { "A type of fishing sport", "A malicious attempt to obtain sensitive information", "A method for encrypting files", "A secure way to browse the internet" },
                    CorrectAnswerIndex = 1
                },
                new QuizQuestion
                {
                    Question = "What makes a strong password?",
                    Options = new List<string> { "Easy to remember", "Short and simple", "Includes a mix of uppercase, lowercase, numbers, and symbols", "Your birth date" },
                    CorrectAnswerIndex = 2
                },
                new QuizQuestion
                {
                    Question = "What does 'HTTPS' in a website address signify?",
                    Options = new List<string> { "HyperText Transfer Protocol Standard", "HyperText Transfer Protocol Secure", "Highly Targeted Transfer Protocol System", "Home Page Transfer Protocol Service" },
                    CorrectAnswerIndex = 1
                },
                new QuizQuestion
                {
                    Question = "What is Two-Factor Authentication (2FA)?",
                    Options = new List<string> { "Using two different passwords for one account", "An extra layer of security beyond just a password", "A method for quickly accessing your account", "A way to share your account with two people" },
                    CorrectAnswerIndex = 1
                },
                new QuizQuestion
                {
                    Question = "What is ransomware?",
                    Options = new List<string> { "Software that helps you organize your files", "Malware that encrypts your files and demands payment for their release", "A type of antivirus software", "A tool for creating secure backups" },
                    CorrectAnswerIndex = 1
                },
                new QuizQuestion
                {
                    Question = "Which of these is a common social engineering technique?",
                    Options = new List<string> { "Brute-force attack", "DDoS attack", "Pretexting", "Malware injection" },
                    CorrectAnswerIndex = 2
                },
                new QuizQuestion
                {
                    Question = "Why is it important to keep your software updated?",
                    Options = new List<string> { "To make your computer run faster", "To get new features", "To patch security vulnerabilities", "To change the user interface" },
                    CorrectAnswerIndex = 2
                },
                new QuizQuestion
                {
                    Question = "What should you do if you receive a suspicious email asking for your password?",
                    Options = new List<string> { "Reply to ask for more information", "Click on the link provided", "Delete the email and report it if possible", "Forward it to all your contacts" },
                    CorrectAnswerIndex = 2
                },
                new QuizQuestion
                {
                    Question = "What is the purpose of a firewall?",
                    Options = new List<string> { "To block spam emails", "To prevent unauthorized access to or from a private network", "To speed up internet Browse", "To scan for viruses" },
                    CorrectAnswerIndex = 1
                },
                new QuizQuestion
                {
                    Question = "Which of the following is an example of strong password practice?",
                    Options = new List<string> { "Using your pet's name", "Using '123456'", "Creating a long, random phrase with mixed characters", "Reusing the same password for multiple accounts" },
                    CorrectAnswerIndex = 2
                }
            };
            ResetQuiz();
        }

        public void ResetQuiz()
        {
            currentQuestionIndex = 0;
            score = 0;
            ShuffleQuestions(); // Shuffle for variety
        }

        private void ShuffleQuestions()
        {
            Random rng = new Random();
            int n = questions.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                QuizQuestion value = questions[k];
                questions[k] = questions[n];
                questions[n] = value;
            }
        }

        public QuizQuestion GetCurrentQuestion()
        {
            if (currentQuestionIndex < questions.Count)
            {
                return questions[currentQuestionIndex];
            }
            return null;
        }

        public bool SubmitAnswer(int selectedOptionIndex)
        {
            if (currentQuestionIndex < questions.Count)
            {
                if (questions[currentQuestionIndex].CorrectAnswerIndex == selectedOptionIndex)
                {
                    score++;
                    return true; // Correct
                }
                return false; // Incorrect
            }
            return false; // No current question
        }

        public void NextQuestion()
        {
            currentQuestionIndex++;
        }

        public bool IsQuizFinished()
        {
            return currentQuestionIndex >= questions.Count;
        }

        public int GetScore()
        {
            return score;
        }

        public int GetTotalQuestions()
        {
            return questions.Count;
        }

        public int GetQuestionNumber()
        {
            return currentQuestionIndex + 1;
        }
    }


    // --- NLP Simulation Class ---
    public class ChatbotNLP
    {
        private Dictionary<string, List<string>> keywordsAndResponses;

        public ChatbotNLP()
        {
            keywordsAndResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "cybersecurity", new List<string> { "🛡️ Cybersecurity is the practice of protecting systems, networks, and data from cyber threats. It includes measures to prevent unauthorized access, data breaches, and cyber attacks." } },
                { "password", new List<string> { "🔑 Use strong, unique passwords and enable two-factor authentication.", "A strong password combines uppercase, lowercase, numbers, and symbols. Don't reuse them!" } },
                { "phishing", new List<string> { "⚠️ Be cautious of emails with urgent requests, links, or attachments. Always verify the sender.", "Phishing is a deceptive attempt to trick you into revealing personal info. Look out for suspicious emails!" } },
                { "Browse", new List<string> { "🌐 Use secure websites (HTTPS), avoid downloading files from unknown sources.", "For safe Browse, always check for HTTPS in the URL and be wary of untrusted websites." } },
                { "social engineering", new List<string> { "🎭 Cybercriminals may trick you into revealing personal information. Always verify requests for sensitive data.", "Social engineering manipulates people into performing actions or divulging confidential information." } },
                { "two-factor authentication", new List<string> { "📱 Enable 2FA to add an extra layer of security to your accounts.", "2FA adds an extra layer of protection, usually with a code from your phone, after your password." } },
                { "ransomware", new List<string> { "💰 Ransomware encrypts your files and demands payment. Keep backups and avoid clicking suspicious links.", "Protect against ransomware by regularly backing up your data and being cautious about suspicious emails or downloads." } },
                { "hello", new List<string> { "Hello there! How can I assist you with cybersecurity today?", "Hi! Ready to learn about staying safe online?" } },
                { "how are you", new List<string> { "As an AI, I don't have feelings, but I'm functioning perfectly and ready to help you with cybersecurity!", "I'm always ready to assist! How can I help you stay cyber-safe?" } },
                { "purpose", new List<string> { "I'm here to provide cybersecurity awareness and help you stay safe online. 🔒" } },
                { "thank you", new List<string> { "You're welcome! Stay safe out there.", "Glad I could help!" } },
                { "what is your name", new List<string> { "I am your Cybersecurity Awareness Bot!", "You can call me the CyberSafe Bot." } }
            };
        }

        public string GetResponse(string userInput)
        {
            string lowerInput = userInput.ToLower();

            // Check for keywords and return a random response from the list
            foreach (var entry in keywordsAndResponses)
            {
                if (lowerInput.Contains(entry.Key))
                {
                    Random rand = new Random();
                    return entry.Value[rand.Next(entry.Value.Count)];
                }
            }

            return "🤔 Hmm... I'm not sure about that. Try asking about cybersecurity topics, managing tasks, playing the quiz, or viewing the activity log!";
        }
    }


    // --- Activity Log Classes ---
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string Type { get; set; } // e.g., "Task Added", "Quiz Attempt", "Chat Interaction"

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Type}] {Description}";
        }
    }

    public class ActivityLogger
    {
        private List<ActivityLogEntry> logEntries = new List<ActivityLogEntry>();

        public void LogAction(string description, string type = "Chat Interaction")
        {
            logEntries.Add(new ActivityLogEntry
            {
                Timestamp = DateTime.Now,
                Description = description,
                Type = type
            });
        }

        public List<ActivityLogEntry> GetActivityLog()
        {
            return logEntries.OrderByDescending(e => e.Timestamp).ToList(); // Show most recent first
        }
    }
}