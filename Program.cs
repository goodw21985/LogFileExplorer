#define NOTASKS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static System.Diagnostics.Debugger;

namespace LogFileExplorer
{
    class Program
    {
        // log file default path
        static string logFilename = @"runlog.csv";

        // everything you see also goes to this file
        const string outputLogFilename = "ScanLog.txt";

        // And variables used in a session a persisted to this file
        const string persistentVariablesFilename = "LogVariables.tsv";

        /// <summary>
        /// The object that reads the log file and maintains its contents
        /// </summary>
        static LogEntries logFile;

        /// <summary>
        /// The object that keeps a revers index of the logfile
        /// </summary>
        static InvertedIndex index = new InvertedIndex();

        /// <summary>
        /// The object that manages communication with the user
        /// </summary>
        static UI ui;

        /// <summary>
        /// Construct the objects and start the progam
        /// </summary>
        /// <param name="args">command line arguments to specify log file path</param>
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            if (args.Count() > 0 && (args[0]=="/?" || args[0]=="-?"))
            {
                Console.WriteLine(@"
LogScan <filepath> 
scans and indexes the file, and then lets you find things from a command prompt.
");
                return;
            }

            if (args.Count() > 0) logFilename = args[0];
            logFile = new LogEntries(logFilename, index);
            if (logFile.Valid)
            {
                ui = new UI(logFile, index, outputLogFilename, persistentVariablesFilename);
                ui.Main();
            }
        }
    }
}
