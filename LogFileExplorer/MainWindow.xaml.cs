using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Common;
using System.ComponentModel;

namespace LogFileViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// All the entries in the log file
        /// </summary>
        LogEntries logEntries;

        /// <summary>
        /// The inverted index of the log file
        /// </summary>
        InvertedIndex index;

        /// <summary>
        /// the expression evaluator for queries
        /// </summary>
        Expressions logic;

        ComboBox[] commandBox1;
        ComboBox[] commandBox2;
        ListBox[] logListBox;
        // PersistentVariableStore variables;

        string logFilename = @"C:\Users\bobgood\Downloads\runlog.csv";
        string persistentFileName = "psv.tsv";

        // entrypoint for initializing wpf object
        public MainWindow()
        {
            InitializeComponent();
            commandBox1 = new ComboBox[] { commandBox1a, commandBox1b, commandBox1c, commandBox1d, commandBox1e };
            commandBox2 = new ComboBox[] { commandBox2a, commandBox2b, commandBox2c, commandBox2d, commandBox2e };
            logListBox = new ListBox[] { logListBox1, logListBox2, logListBox3, logListBox4, logListBox5 };
        }
        event EventHandler<LoadEventArgs> ProgressUpdate;

        event EventHandler<CommandEventArgs> CommandUpdate;

        private void Window_Loaded(object sender, RoutedEventArgs eargs)
        {
            statusline2.Content = " lines read";
            ProgressUpdate += (s, e) => Dispatcher.Invoke((Action)delegate ()
            {
                ProgressChanged(s, e);
            });

            CommandUpdate += (s, e) => Dispatcher.Invoke((Action)delegate ()
              {
                  CommandComplete(s, e);
              });
            BackgroundWorker loadWorker = new BackgroundWorker();
            loadWorker.DoWork +=
new DoWorkEventHandler(bw_DoWork);
            loadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_complete);
            loadWorker.RunWorkerAsync();
        }

        private void bw_complete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (logEntries.Count==0)
            {
                errorMessage.Content = "Index file '" + logFilename + "' could not be read";
                statusline1.Content = 0;
                statusline2.Content = "No Content";
            }
            else
            {
                statusline1.Content = "" + logEntries.Count;
                statusline2.Content = "Index complete";
            }
        }

        private bool firstLoad = true;
        private void ProgressChanged(object sender, LoadEventArgs e)
        {
            int n = 0;
            var listbox = logListBox[n];
            if (e.line_number > 0)
                statusline1.Content = "" + e.line_number;

            if (firstLoad)
            {
                listbox.Items.Clear();
                for (int i=0; i< e.line_number;i++)
                {
                    listbox.Items.Add(logEntries[new Common.EntryId(i)]);
                }
            }
            firstLoad = false;
        }

        private List<int> sortedContent = null;
        private void CommandComplete(object sender, CommandEventArgs e)
        {
            int n = e.paneIndex;
            var listbox = this.logListBox[n];
           listbox.Items.Clear();
            if (e.errorMessages.Count()>0)
            {
                listbox.Items.Add(e.command);
                listbox.Items.Add("");
                foreach (string s in e.errorMessages)
                {
                    listbox.Items.Add(s);
                }

                listbox.Foreground = Brushes.Red;
                return;
            }

            if (e.matchSet.Count()==0)
            {
                listbox.Items.Add(e.command);
                listbox.Items.Add("");
                listbox.Items.Add("no matches found");

                listbox.Foreground = Brushes.Red;
                return;

            }

            listbox.Foreground = Brushes.Black;

            IMatchSet m = e.matchSet;
            this.sortedContent = null;
            List<int> sortedList = new List<int>(m.Count);
            foreach (var i in m)
            {
                sortedList.Add(i.Value);
            }

            int max = 5000;
            moreContentButton.Visibility = System.Windows.Visibility.Hidden;
            sortedList.Sort();
            for (int i=0; i<sortedList.Count();i++)
            {
                if (i < max)
                {
                    listbox.Items.Add(logEntries[new Common.EntryId(sortedList[i])]);
                }
            }



            if (sortedList.Count()>=max)
            {
                moreContentButton.Visibility = System.Windows.Visibility.Visible;
                sortedContent = sortedList;
                listbox.Items.Add("");
                listbox.Items.Add("  - press More Content button to see more -");
                statusline1.Content = string.Format("{0} of {1} out of {2}",
        listbox.Items.Count-2, sortedList.Count(), logEntries.Count);
                statusline2.Content = "";
            }
            else
            {
                statusline1.Content = string.Format("{0} out of {1}",
listbox.Items.Count, logEntries.Count);

            }
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker loadWorker = sender as BackgroundWorker;
            index = new InvertedIndex();
            logic = new Expressions(index, persistentFileName);
            logEntries = new LogEntries(logFilename, index, ProgressUpdate);
            if (logEntries!=null)
            {
                logEntries.Build();
            }
        }
        
        private void LogEnter(int paneIndex)
        {
            string a = commandBox1[paneIndex].Text;
            string b = commandBox2[paneIndex].Text;
            string command = a;
            if (a == "") command = b;
            else if (b != "") command = command + " , " + b;
            List<string> errorMessages = new List<string>();
            command = command.Trim().Replace("  ", " ");
            List<string> tokens = Common.Expressions.ParseCommand(command);

            Task<Common.IMatchSet> t = Task<Common.IMatchSet>.Run(() =>
           {
               IMatchSet m= logic.ParseTokens(tokens, errorMessages, 0);
               CommandUpdate(this, new LogFileViewer.CommandEventArgs()
               {
                   paneIndex = paneIndex,
                    matchSet = m,
                    errorMessages = errorMessages,
                    command = string.Join(" ", tokens)
                }); 
                           
                return m;
           });
        }

        int GetPaneIndex(object sender, ComboBox[] list)
        {
            for (int cnt = 0; cnt < list.Count(); cnt++)
            {
                if (list[cnt] == sender) return cnt;
            }
            return -1;
        }

        private void commandBox1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int paneIndex = GetPaneIndex(sender, commandBox1);
            ComboBox c = sender as ComboBox;
            if (e.Text[0]<' ')
            {
                e.Handled = true;
                LogEnter(paneIndex);
            }
        }

        private void commandBox2_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int paneIndex = GetPaneIndex(sender, commandBox2);
            if (e.Text[0] < ' ')
            {
                e.Handled = true;
                LogEnter(paneIndex);
            }
        }

        private void commandBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddMoreContent(object sender, RoutedEventArgs e)
        {
            var n = 0;
            var listbox = logListBox[n];
            var cnt=listbox.Items.Count;
            if (sortedContent == null) return;
            int ocnt = sortedContent.Count;
            if (ocnt > cnt-2)
            {
                listbox.Items.RemoveAt(--cnt);
                listbox.Items.RemoveAt(--cnt);
            }


            for (int i = cnt; i < ocnt && i < 2 * cnt; i++)
            {
                listbox.Items.Add(logEntries[new Common.EntryId(sortedContent[i])]);
            }

            var acnt = listbox.Items.Count;
            if (acnt>=ocnt)
            {
                moreContentButton.Visibility = System.Windows.Visibility.Hidden;
            }

            statusline1.Content = string.Format("{0} of {1} out of {2}",
                acnt, ocnt, logEntries.Count);
            statusline2.Content = "";
        }
    }

    class CommandEventArgs : EventArgs
    {
        public Common.IMatchSet matchSet;
        public List<string> errorMessages;
        public string command;
        public int paneIndex;
    }
}
