using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NTI___Query_Session
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private int countdownValue;
        private string AdminUsername = "Administrator";
        public MainWindow()
        {
            InitializeComponent();
            PopulateSessionList();
            // Inizializza il timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            // Imposta il valore iniziale del conto alla rovescia
            countdownValue = 60;
            UpdateLabel();

            // Avvia il timer
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            countdownValue--;
            UpdateLabel();
            // Verifica se il conto alla rovescia ha raggiunto lo zero
            if (countdownValue == 0)
            {
                timer.Stop();
                CheckSession();
            }
        }

        private void UpdateLabel()
        {
            // Aggiorna il contenuto della Label con il valore corrente del conto alla rovescia
            countdownLabel.Content = countdownValue.ToString();
        }

        /// <summary>
        /// Metodo per resettare le sessioni
        /// </summary>
        private void CheckSession()
        {
            sessionListBox.Items.Clear();
            List<(int, string)> sessioniDisc = PopulateSessionList();
            if (sessioniDisc != null)
            {
                foreach (var i in sessioniDisc)
                {
                    ResetSession(i);
                }
            }
            ResetTimer();
        }

        private void ResetTimer()
        {
            // Resetta il conto alla rovescia e riavvia il timer
            countdownValue = 60;
            UpdateLabel();
            timer.Start();
        }

        /// <summary>
        /// Metodo per aggiornare la lbl delle sessioni, ritorna gli id delle sessioni disconnesse
        /// </summary>
        /// <returns></returns>
        private List<(int, string)> PopulateSessionList()
        {
            try
            {
                List<(int, string)> SessioniDisc = new List<(int, string)>();   
                // Creazione di un processo per eseguire il comando "query session"
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "query",
                        Arguments = "session",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                // Avvio del processo e lettura dell'output
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Divisione dell'output in righe
                string[] lines = output.Split('\n');

                //Rimuovo gli spazi in eccesso
                string[] resultArray = new string[lines.Length];
                for (int i = 0; i < lines.Length; i++)
                {
                    string originalString = lines[i];
                    string cleanedString = RemoveDuplicateSpaces(originalString);
                    resultArray[i] = cleanedString;
                }

                // Iterazione sulle righe, iniziando dalla terza riga (dove inizia l'elenco delle sessioni)
                foreach (string line in resultArray.Skip(2))
                {
                    // Divisione della riga in colonne
                    string[] columns = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    // Verifica che ci siano almeno 4 colonne e che la seconda colonna sia il nome utente
                    if (columns.Length >= 3 && columns[1] != null)
                    {
                        string username = "";
                        string id = "";
                        if (columns.Length-3 >= 0)
                        {
                            username = columns[columns.Length - 3];
                            id = columns[columns.Length - 2];
                        }
                        
                        string status = columns[columns.Length-1];

                        // Creazione di una Label
                        Label label = new Label
                        {
                            Content = $"{id} - {username} - {status}",
                            Padding = new Thickness(5),
                            FontSize = 14
                        };

                        // Impostazione del colore di sfondo in base allo stato
                        if (status.ToLower() == "disc")
                        {
                            label.Background = Brushes.Red;
                        }
                        else
                        {
                            label.Background = Brushes.LightGreen;
                        }
                        int numId;
                        //se contiene un id numerico e lo astato è o Attivo o Disc lo aggiungo
                        if(!username.Equals(this.AdminUsername) && int.TryParse(id, out numId) && (status.Equals("Attivo") || status.Equals("Disc")))
                        {
                            sessionListBox.Items.Add(label);

                            if (status.Equals("Disc"))
                            {
                                SessioniDisc.Add((int.Parse(id), username));
                            }
                        }
                    }
                }
                return SessioniDisc;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'esecuzione del comando 'query session': {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<(int, string)>();
            }
        }
        private static string RemoveDuplicateSpaces(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            char[] inputChars = input.ToCharArray();
            int inputLength = inputChars.Length;

            int currentIndex = 0;
            bool isSpace = char.IsWhiteSpace(inputChars[currentIndex]);

            for (int i = 1; i < inputLength; i++)
            {
                if (char.IsWhiteSpace(inputChars[i]))
                {
                    if (!isSpace)
                    {
                        currentIndex++;
                        inputChars[currentIndex] = ' ';
                        isSpace = true;
                    }
                }
                else
                {
                    currentIndex++;
                    inputChars[currentIndex] = inputChars[i];
                    isSpace = false;
                }
            }

            return new string(inputChars, 0, currentIndex + 1);
        }


        private void ResetSession((int, string) session)
        {
            try
            {
                // Creazione di un processo per eseguire il comando "reset session"
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "reset",
                        Arguments = $"session {session.Item1}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                // Avvio del processo e attesa della sua terminazione
                process.Start();
                process.WaitForExit();

                DateTime now = DateTime.Now;
                Label label = new Label
                {
                    Content = $"Rest session {session.Item1} ({session.Item2}) - {now.ToString("dd-MM-yyyy HH:mm:ss")}",
                    Padding = new Thickness(5),
                    FontSize = 14
                };
                Cronologia.Items.Add(label);

                // Aggiungi la stessa informazione al file di log
                string logFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LogSessioni.txt");
                string logMessage = $"Rest session {session.Item1} ({session.Item2}) - {now.ToString("dd-MM-yyyy HH:mm:ss")}";
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'esecuzione del comando 'reset session': {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}