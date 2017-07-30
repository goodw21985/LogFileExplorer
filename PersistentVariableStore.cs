using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace LogFileExplorer
{
    // Persistently saves a version of Dictionary<string,List<string>> to a tsv file on every change.
    // if the file cannot be accessed, syncronization is silently abandoned.
    // does not work well if two programs modifying simultaneously.
    class PersistentVariableStore : IEnumerable<List<string>>
    {
        /// <summary>
        /// The dictionary data that is persisted.
        /// </summary>
        Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();

        /// <summary>
        /// The file path to the dictionary file
        /// </summary>
        string dictionaryFilePath;

        /// <summary>
        /// Creates the dictionary, and reads in prior values from the file
        /// </summary>
        /// <param name="dictionaryFilePath">the file patch to the persistence data</param>
        public PersistentVariableStore(string dictionaryFilePath)
        {
            this.dictionaryFilePath = dictionaryFilePath;

            try
            {
                using (TextReader tr = new StreamReader(dictionaryFilePath))
                {
                    string line;
                    while (null != (line = tr.ReadLine()))
                    {
                        string[] parts = line.Split('\t');
                        List<string> tokens = new List<String>(parts.Count());
                        for (int i = 0; i < parts.Count(); i++)
                        {
                            tokens.Add(parts[i]);
                            dict[parts[0]] = tokens;
                        }
                    }
                }

                Console.WriteLine("Read variables from file " + dictionaryFilePath);
            }
            catch {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not read variables from file " + dictionaryFilePath);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Gets all keys in the dictionary that start with a specific prefic
        /// </summary>
        /// <param name="s">the string prefix to search for</param>
        /// <returns>all values in the collection that start with the prefix</returns>
        public HashSet<string> GetAllKeysThatStartWith(string s)
        {
            HashSet<string> set = new HashSet<string>();
            foreach (var key in dict.Keys)
            {
                if (key.StartsWith(s))
                {
                    string r = string.Join(" ", dict[key]);
                    set.Add(r);
                }
            }
            return set;
        }

        /// <summary>
        /// Getter for an item in the dictionary
        /// returns null if the item does not exist.
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Tokens that describe the key</returns>
        public List<string> this[string key]
        {
            get
            {
                lock (dict)
                {
                    if (dict.ContainsKey(key))
                    {
                        return dict[key];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            set
            {
                lock (dict)
                {
                    dict[key] = value;
                }

                Task.Run(() =>
                 {

                     SaveState();
                 });
            }
        }

        /// <summary>
        /// Writes data to file
        /// </summary>
        void SaveState()
        {
            lock (dict)
            {
                try
                {
                    using (TextWriter tw = new StreamWriter(dictionaryFilePath))
                    {
                        foreach (var kvp in dict)
                        {
                            bool first = true;
                            foreach (var x in kvp.Value)
                            {
                                if (!first) tw.Write("\t");
                                first = false;
                                tw.Write(x);
                            }
                            tw.WriteLine();
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Enumerates all values in the dictionary
        /// </summary>
        /// <returns>enumerator for all values in the dictionary</returns>
        public IEnumerator<List<string>> GetEnumerator()
        {
            return dict.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}

