using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LogFileExplorer
{
    /// <summary>
    /// manages all communication with the user
    /// </summary>
    class UI
    {
        /// <summary>
        /// All the entries in the log file
        /// </summary>
        LogEntries log;

        /// <summary>
        /// The inverted index of the log file
        /// </summary>
        InvertedIndex index;

        /// <summary>
        /// the expression evaluator for queries
        /// </summary>
        Expressions logic;

        /// <summary>
        /// All output is also directed to this file, if it is open
        /// </summary>
        TextWriter uiLogOutput = null;

        /// <summary>
        /// parts of the output log file name
        /// </summary>
        string outLogLeft, outLogRight = ".csv";
        string outLogFilename;

        /// <summary>
        /// Any error messages accumulated during the processisng of commands and expressions
        /// </summary>
        List<string> errorMessages = new List<string>();

        /// <summary>
        /// The maximum number of entries shown on the console per command
        /// </summary>
        int maxLinesToConsole = 40;

        /// <summary>
        /// The number of entries shown on the console so far this command
        /// </summary>
        int outCount = 0;

        /// <summary>
        /// UI constructor
        /// </summary>
        /// <param name="logFilePath">file path to log file</param>
        /// <param name="index">The inverted index of the log file</param>
        /// <param name="outLogFilePath">The file path to the optional output log</param>
        /// <param name="persistentFileName">The file path to the variable persistence</param>
        public UI(LogEntries logFilePath, InvertedIndex index, string outLogFilePath = "", string persistentFileName="LogVariables.tsv")
        {
            logic = new Expressions(index, persistentFileName);

            if (outLogFilePath != "")
            {
                string[] parts = outLogFilePath.Split('.');
                outLogLeft = parts[0];
                if (parts.Count() == 2)
                {
                    outLogRight = "."+parts[1];
                }

                this.log = logFilePath;
                this.index = index;
                for (int v = 0; uiLogOutput == null && v < 5; v++)
                {
                    if (v == 0)
                    {
                        outLogFilename = outLogLeft + outLogRight;
                    }
                    else
                    {
                        outLogFilename = outLogLeft + v + outLogRight;
                    }
                    try
                    {
                        uiLogOutput = new StreamWriter(outLogFilename);
                    }
                    catch { }
                }

                if (uiLogOutput != null)
                {
                    Console.WriteLine("Writing Console Output to file '{0}'", outLogFilename);
                }
            }
        }

        /// <summary>
        /// Writes all matches to the console
        /// </summary>
        /// <param name="m">The match set</param>
        /// <param name="what">the original command, in case an error message is created</param>
        public void WriteLineResults(MatchSet m, string what)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (EntryId v in m)
            {
                string item = log[v];
                if (outCount++ < maxLinesToConsole)
                    Console.WriteLine(item);
                if (uiLogOutput != null) uiLogOutput.WriteLine(item);
            }
            if (m.Count() > maxLinesToConsole)
            {
                WriteLineGreen("" + m.Count() + " results found for " + what);
            }
            if (m.Count() == 0)
            {
                WriteLineRed("no results found for " + what);
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Writes all strings from a list to the console
        /// </summary>
        /// <param name="m">The match set</param>
        /// <param name="what">the original command, in case an error message is created</param>
        public void WriteLineResults(IEnumerable<string> ns, string what)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (string n in ns)
            {
                if (outCount++ < maxLinesToConsole)
                    Console.WriteLine(n);
                if (uiLogOutput != null) uiLogOutput.WriteLine(n);
            }
            if (ns.Count() > maxLinesToConsole)
            {
                WriteLineGreen("" + ns.Count() + " results found for " + what);
            }
            if (ns.Count() == 0)
            {
                WriteLineRed("no results found for " + what);
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// writes an error message to the console only
        /// </summary>
        /// <param name="errorMessage">the error message</param>
        public void WriteLineRed(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// writes an warning message to the console only
        /// </summary>
        /// <param name="message">the warning message</param>
        public void WriteLineYellow(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }


        /// <summary>
        /// writes a status message to the console only
        /// </summary>
        /// <param name="message">the message</param>
        public void WriteLineGreen(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Parses a command line into tokens.  
        /// '=', '(', ')', '|', '&' tokens
        /// other token (separated by space or delimiter)
        /// always lower case
        /// </summary>
        /// <param name="command">string from command line</param>
        /// <returns>list of tokens</returns>
        public List<string> ParseCommand(string command)
        {
            List<string> tokens = new List<string>();
            string currentToken = null;
            for (int i = 0; i < command.Length; i++)
            {
                char c = command[i];
                if (c >= 'A' && c <= 'Z')
                {
                    c = (char)(c + 'a' - 'A');
                }
                if (c <= ' ')
                {
                    currentToken = null;
                }
                else if (c == '(' | c == ')' || c == '|' || c == '&' || c == '=')
                {
                    currentToken = null;
                    tokens.Add("" + c);
                }
                else
                {
                    if (currentToken == null)
                    {
                        currentToken = "" + c;
                        tokens.Add(currentToken);
                    }
                    else
                    {
                        currentToken += c;
                        tokens[tokens.Count()-1]=currentToken;
                    }
                }
            }

            return tokens;
        }

        /// <summary>
        /// Main UI loop
        /// </summary>
        public void Main()
        {

            for (;;)
            {
                outCount = 0;
                errorMessages = new List<string>();
                Console.Write("> ");
                string command = Console.ReadLine();
                {
                    try
                    {
                        command = command.Trim().Replace("  ", " ");
                        uiLogOutput.WriteLine("> {0}", command);
                        List<string> parts = ParseCommand(command);
                        if (parts.Count == 0) continue;
                        if (parts.Count >= 2 && parts[0].StartsWith("$") && parts[1] == "=")
                        {
                            var m=logic.Assign(parts, errorMessages);
                            if (m.Count()==0)
                            {
                                errorMessages.Add("no matches for "+ string.Join(" ", parts));
                            }
                            else
                            {
                                WriteLineGreen(string.Format("{0} has {1} matches", parts[0], m.Count()));
                            }
                        }
                        else
                        {
                            switch (parts[0].ToLower())
                            {
                                case "m":
                                    Max(parts);
                                    break;

                                case "f":
                                    Find(parts);
                                    break;

                                case "k":
                                    Keys(parts);
                                    break;

                                case "v":
                                    Variables(parts);
                                    break;

                                default:
                                    HelpMessage();
                                    break;
                            }
                        }

                        foreach (var errorMessage in errorMessages)
                        {
                            WriteLineRed(errorMessage);
                        }

                        uiLogOutput.Flush();
                    }
                    catch (Exception exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(exception.Message);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(exception.StackTrace);
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                }
            }
        }

        /// <summary>
        /// Variable command method.  Prints all $xxx variables that start with a given prefix
        /// </summary>
        /// <param name="commandTokens">the tokens from the user command</param>
        void Variables(List<string> commandTokens)
        {
            HashSet<string> variables;
            if (commandTokens.Count() <= 1)
            {

                variables= logic.GetVariableMatches("$");
            }
            else
            {
                variables = new HashSet<string>();
                for (int i = 1; i < commandTokens.Count(); i++)
                {
                    if (commandTokens[i][0] != '$')
                    {
                        errorMessages.Add(string.Format("variable names must start with $: '{0}'", commandTokens[i]));
                    }
                    else
                    {
                        var set = logic.GetVariableMatches(commandTokens[i]);
                        foreach (string s in set)
                        {
                            variables.Add(s);
                        }
                    }
                }
            }

            WriteLineResults(variables.ToArray(), string.Join(" ", commandTokens));
        }

        /// <summary>
        /// The Max command method.  Changes the number of lines printed to the console for each command
        /// </summary>
        /// <param name="commandTokens">the tokens from the command line</param>
        void Max(List<string> commandTokens)
        {
            try
            {
                maxLinesToConsole = int.Parse(commandTokens[1]);
            }
            catch
            {
                errorMessages.Add("could not parse number "+commandTokens[1]);
            }
        }

        /// <summary>
        /// The Key command from the command line for enumerting keys matching key description tokens
        /// </summary>
        /// <param name="commandTokens"></param>
        void Keys(List<string> commandTokens)
        {
            if (commandTokens.Count() < 2)
            {
                errorMessages.Add("command "+commandTokens[0]+" does not have any parameters following");
                return;
            }
            for (int i = 1; i < commandTokens.Count(); i++)
            {
                string part = commandTokens[i];
                List<string> results = index.GetKeys(part, errorMessages);

                WriteLineResults(results, part);
            }
        }

        /// <summary>
        /// The Find command method.  Evaluates an expression, and prints the resulting log entries to the console.
        /// </summary>
        /// <param name="commandTokens"></param>
        void Find(List<string> commandTokens)
        {
            MatchSet m = logic.Parse(commandTokens, errorMessages, 1);
            WriteLineResults(m, string.Join(" ",commandTokens));
        }

        /// <summary>
        /// Prints out the summary of the UI commands to the console
        /// </summary>
        public void HelpMessage()
        {
            WriteLineYellow(
        @"f 3F429         Finds the number 3F429
f 3F42x         Finds numbers in the range of 3F420-3F42F
f 30-3F         Finds numbers in the range 30-3F
f clear_memory  Finds the text 'clear memory' (case insensitive) within log entries
f ab*           Finds text beginning with 'ab'
f *a*           Finds text containing 'a'
f 3 4X ka t*    Finds log entries with all four conditions met
f (3 | 4) & k   Finds logical match
f #200-300      Shows log entries 200 to 300
$a = ab* | cd*  Creates a variable with the match
f $a 3F4ax      Finds entries matching both $a variable and number 3F4aX
k a*            Lists all keys beginning with the string 'a'
k *dog*         Lists all keys containing dog
k 52F00-55000   Shows each unique address found in the logs within the specified range.
k 52xxx         Shows each unique address found in the logs within the specified range.
m 50            Show at most 50 items.
v $a            Shows all variables starting with $a
v               Shows all variables
");

        }
    }
}
