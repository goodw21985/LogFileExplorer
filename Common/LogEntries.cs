//#define NOTASKS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;

namespace Common
{
    public class LoadEventArgs : EventArgs
    {
        public int line_number=0;
        public string err="";
    };

    delegate void ReportToUIDelegate(int n);
    /// <summary>
    /// Reads a log file, and adds each log entry into the the collection, and indexes tokens within each line.
    /// </summary>
    public class LogEntries
    {
        public event EventHandler<LoadEventArgs> ProgressUpdate=null;

        /// <summary>
        /// All Asyncronous tasks started to process the Log file
        /// </summary>
        List<Task> openTasks = new List<Task>();

        /// <summary>
        /// All entries from the log file
        /// </summary>
        List<string> logEntries = new List<string>();

        bool valid = false;

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
        public LogEntries(string filename, InvertedIndex index,  EventHandler<LoadEventArgs> ProgressUpdate=null)
        {
            this.index = index;
            this.ProgressUpdate = ProgressUpdate;
            try
            {
                fileReader = new StreamReader(filename);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not read file {0}", filename);
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            valid = true;
        }

        public void Build()
        {
            if (fileReader == null) return;
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

                lock (logEntries)
                {
                    logEntries.Add(line);
                }
#if NOTASKS
                index.AddLogEntry(lineNo, line);
#else
                StartParseLineTask(lineNo, line, index);
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
        /// Gets the item count for the collection
        /// </summary>
        public int Count
        {
            get
            {
                lock (logEntries)
                {
                    return logEntries.Count();
                }
            }
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
                    lock (logEntries)
                    {
                        return logEntries[entryId.Value];
                    }
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
                return valid;
            }
        }

        /// <summary>
        /// starts new new asyncronous task to index a single log entry
        /// </summary>
        /// <param name="entryId">The index of the log entry</param>
        /// <param name="logEntry">the complete log entry</param>
        /// <param name="index">The reverse index for this log</param>
        void StartParseLineTask(int entryId, string logEntry, InvertedIndex index)
        {
            Task t = Task.Run(() =>
            {
                index.AddLogEntry(entryId, logEntry);
                if (entryId % 100 == 99 && ProgressUpdate != null)
                {
                    ProgressUpdate(this, new LoadEventArgs() { line_number = entryId + 1 });
                }
            });

            openTasks.Add(t);
        }
    }
}
