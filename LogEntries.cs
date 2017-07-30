using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LogFileExplorer
{
    /// <summary>
    /// Reads a log file, and adds each log entry into the the collection, and indexes tokens within each line.
    /// </summary>
    public class LogEntries
    {
        /// <summary>
        /// All Asyncronous tasks started to process the Log file
        /// </summary>
        List<Task> openTasks = new List<Task>();

        /// <summary>
        /// All entries from the log file
        /// </summary>
        List<string> logEntries = new List<string>();

        /// <summary>
        /// File reader for the log file
        /// </summary>
        TextReader fileReader;

        /// <summary>
        /// The index container that will hold the reverse index of each log entry
        /// </summary>
        InvertedIndex index;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">file path to log file</param>
        /// <param name="index">index object that is associated with this log file</param>
        public LogEntries(string filename, InvertedIndex index)
        {
            this.index = index;

            try
            {
                fileReader = new StreamReader(filename);
            }
            catch (Exception )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not read file {0}", filename);
                Console.ForegroundColor = ConsoleColor.White;
                logEntries = null;
            }

            string line;
            int stopCount = int.MaxValue;
            while (null != (line = fileReader.ReadLine()))
            {
                int lineNo = logEntries.Count();
                string prefix = string.Format("{0}, ", lineNo);
                if (!line.StartsWith(prefix))
                {
                    line = prefix + line;
                }

                logEntries.Add(line);

#if NOTASKS
                index.AddLogEntry(lineNo, line);
#else
                StartTask(lineNo, line, index);
#endif
                if (lineNo > stopCount) break;

            }

#if NOTASKS
#else
            foreach (var task in openTasks)
            {
                task.Wait();
            }
#endif

        }

        /// <summary>
        /// Getter for an item in log entry table
        /// </summary>
        /// <param name="entryId">entry Id</param>
        /// <returns>the original log entry from the log file</returns>
        public string this[EntryId entryId]
        {
            get
            {
                if (entryId.Value >= 0 && entryId.Value < logEntries.Count)
                {
                    return logEntries[entryId.Value];
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("index out of range for long entry {0}>={1}", entryId, logEntries.Count);
                    Console.ForegroundColor = ConsoleColor.White;
                    return null;
                }
            }
        }

        /// <summary>
        /// is true if the log file was completely read.
        /// </summary>
        public bool Valid
        {
            get
            {
                return logEntries != null;
            }
        }

        /// <summary>
        /// starts new new asyncronous task to index a single log entry
        /// </summary>
        /// <param name="entryId">The index of the log entry</param>
        /// <param name="logEntry">the complete log entry</param>
        /// <param name="index">The reverse index for this log</param>
        void StartTask(int entryId, string logEntry, InvertedIndex index)
        {
            Task t = Task.Run(() =>
            {
                index.AddLogEntry(entryId, logEntry);
            });

            openTasks.Add(t);
        }
    }
}
