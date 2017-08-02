using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Performas boolean logic on MatchSets,
    /// parses boolean equations.
    /// manages local variables (variables start with character $)
    /// 
    /// Expression operator tokens:
    ///   (  )  |  &
    /// Expression Key decription token formats:
    ///    number:             a4937
    ///    number range:       a4-49b  
    ///    number wildcard:    9bxxx
    ///    log entry id range: #200-300
    ///    text:               atom_split
    ///    text wild card:     atom*split
    ///    variable:           $myVariable
    ///    
    /// ALL tokens are case insensitive.
    /// numbers and strings can be confusing:  badfeed is a number, badfood is a string
    /// 
    /// example:
    /// 
    /// $xyz = (9bxxx | *atom) & (123 & 456 | 928 & #200-300)
    /// spaces are NOT needed around operators.  Spaces should NOT be put around the '-' character in range keys description tokens.
    /// The '&' operator is optional and assumed when it is missing.
    /// </summary>
    public class Expressions
    {
        /// <summary>
        /// Dictionary of all existing variables, keyed
        /// by name, returning matchSets.
        /// </summary>
        Dictionary<string, IMatchSet> variables = new Dictionary<string, IMatchSet>();

        /// <summary>
        /// The reverse index of the logfile
        /// </summary>
        InvertedIndex index;

        /// <summary>
        /// A dictionary that writes to a file
        /// when it is changed,  reloads on startup
        /// holds the variable definitions.
        /// best effort.  If file is not available,
        /// persistence and reload do not happen.
        /// </summary>
        PersistentVariableStore variableDefinitions;

        // a match set with no entries
        readonly MatchSet nullMatches = new MatchSet();

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="index">the index to use for searches</param>
        /// <param name="persistentFileName">the file path for the persistent dictionary</param>
        public Expressions(InvertedIndex index, string persistentFileName)
        {
            variableDefinitions = new PersistentVariableStore(persistentFileName);
            this.index = index;
            foreach (List<string> cmd in variableDefinitions)
            {
                Assign(cmd, null);
            }
        }

        /// <summary>
        /// Assignment operator from the command line
        /// </summary>
        /// <param name="tokens">parsed command line</param>
        /// <param name="errors">place to add error messages</param>
        /// <returns>the matches assigned to this variable</returns>
        public IMatchSet Assign(List<string> tokens, List<string> errors)
        {
            if (tokens.Count < 3 || tokens[1] != "=" || tokens[0][0] != '$')
            {
                if (errors != null)
                    errors.Add(string.Format("Assignement format error {0} ", string.Join(" ", tokens)));
                return new MatchSet();
            }
            else
            {
                string key = tokens[0].ToLower(); ;
                IMatchSet matchSet = ParseTokens(tokens, errors, 2);
                if (errors != null)
                {
                    variableDefinitions[key] = tokens;
                }

                variables[key] = matchSet;
                return matchSet;
            }
        }

        /// <summary>
        /// Gets all the keys (strings or numbers) in the index
        /// that match the command token
        /// </summary>
        /// <param name="token">the command token describing keys</param>
        /// <returns>set of </returns>
        public HashSet<string> GetVariableMatches(string token)
        {
            return variableDefinitions.GetAllKeysThatStartWith(token);
        }

        /// <summary>
        /// Returns the position in the list of the matching close paranthesis, or zero if there is none.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="start">start position where open parenthesis is</param>
        /// <returns>the position of the closing paranthesis, or zero if there was not one</returns>
        public static int FindMatchingCloseParen(List<string> tokens, int start, List<string> errors)
        {
            if (tokens[start] != "(")
            {
                errors.Add("unexpected '(' at position " + start);
                return 0;
            }
            int cnt = 0;
            for (int i = start; i < tokens.Count(); i++)
            {
                if (tokens[i] == "(") cnt++;
                if (tokens[i] == ")")
                {
                    cnt--;
                    if (cnt == 0) return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// Finds the range of tokens that need to evalutated as a group because of precedence after a '|' token
        /// </summary>
        /// <param name="tokens">all the tokens in the command string</param>
        /// <param name="start">the location of the '|' that is the source of this request</param>
        /// <param name="errors">a place to put error messages</param>
        /// <returns>the index of the matching '|' location, the next ')' in this scope, or after the end of the expression</returns>
        public static int FindMatchingOrEnd(List<string> tokens, int start, List<string> errors)
        {
            if (tokens[start] != "|")
            {
                errors.Add("unexpected '|' at position " + start);
                return 0;
            }

            int cnt = 0;
            for (int i = start + 1; i < tokens.Count(); i++)
            {
                if (tokens[i] == "(") cnt++;
                else if (tokens[i] == ")")
                {
                    cnt--;
                    if (cnt == -1) return i;
                }

                else if (tokens[i] == "|")
                {
                    if (cnt == 0) return i;
                }
                else if (tokens[i] == ",")
                {
                    if (cnt == 0) return i;
                }
            }
            return tokens.Count();
        }

        /// <summary>
        /// Finds the range of tokens that need to evalutated as a group because of precedence after a '|' token
        /// </summary>
        /// <param name="tokens">all the tokens in the command string</param>
        /// <param name="start">the location of the '|' that is the source of this request</param>
        /// <param name="errors">a place to put error messages</param>
        /// <returns>the index of the matching '|' location, the next ')' in this scope, or after the end of the expression</returns>
        public static int FindMatchingCommaEnd(List<string> tokens, int start, List<string> errors)
        {
            if (tokens[start] != ",")
            {
                errors.Add("unexpected ',' at position " + start);
                return 0;
            }

            int cnt = 0;
            for (int i = start + 1; i < tokens.Count(); i++)
            {
                if (tokens[i] == "(") cnt++;
                else if (tokens[i] == ")")
                {
                    cnt--;
                    if (cnt == -1) return i;
                }

                else if (tokens[i] == ",")
                {
                    if (cnt == 0) return i;
                }
            }
            return tokens.Count();
        }

        /// <summary>
        /// Parses a command line into tokens.  
        /// '=', '(', ')', '|', '&' tokens
        /// other token (separated by space or delimiter)
        /// always lower case
        /// </summary>
        /// <param name="command">string from command line</param>
        /// <returns>list of tokens</returns>
        public static List<string> ParseCommand(string command)
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
                        tokens[tokens.Count() - 1] = currentToken;
                    }
                }
            }

            return tokens;
        }

        /// <summary>
        /// Parses a list of tokens as a logical expression
        /// This is called recursively when needed for precedence or paranthesis
        /// </summary>
        /// <param name="tokens">list of parsed command tokens</param>
        /// <param name="errors">place to add error messages</param>
        /// <param name="start">end position in tokens, inclusive</param>
        /// <param name="end">end position in tokens, inclusive</param>
        /// <returns>matches for this subexpression</returns>
        public IMatchSet ParseTokens(List<string> tokens, List<string> errors, int start = 0, int end = 0)
        {
            IMatchSet matchSet = null;
            if (end == 0) end = tokens.Count() - 1;

            for (int i = start; i <= end; i++)
            {
                if (tokens[i] == "(")
                {
                    int close = FindMatchingCloseParen(tokens, i, errors);
                    if (close == 0) return nullMatches;
                    IMatchSet phrase = ParseTokens(tokens, errors, i + 1, close - 1);
                    matchSet &= phrase;
                    i = close;
                }
                else if (tokens[i] == "|")
                {
                    int close = FindMatchingOrEnd(tokens, i, errors);
                    IMatchSet phrase = ParseTokens(tokens, errors, i + 1, close - 1);
                    matchSet |= phrase;
                    i = close;
                }
                else if (tokens[i] == ",")
                {
                    int close = FindMatchingCommaEnd(tokens, i, errors);
                    IMatchSet phrase = ParseTokens(tokens, errors, i + 1, close - 1);
                    matchSet |= phrase;
                    i = close;

                }
                else if (tokens[i] == "&")
                {
                    // & is implied, but it is legal too.
                }
                else if (tokens[i] == ")")
                {
                    errors.Add(string.Format("unexpected ')' at position " + i));
                    return matchSet;
                }
                else if (tokens[i].StartsWith("$"))
                {
                    IMatchSet result;
                    if (!variables.TryGetValue(tokens[i].ToLower(), out result))
                    {
                        // if the variable has not been evaluated, look in the persistent variable declaration
                        // and see if it is there.  If it is, evaluate it now.
                        List<string> variableCommand = variableDefinitions[tokens[i].ToLower()];
                        if (variableCommand == null)
                        {
                            errors.Add("Cannot recognize variable " + tokens[i].ToLower());
                        }
                        else
                        {
                            result = Assign(variableCommand, null);
                        }
                    }
                    else
                    {
                        matchSet &= result;
                    }
                }
                else
                {
                    IMatchSet match = index.Matches(tokens[i], errors);
                    matchSet &= match;
                }
            }

            if (matchSet == null) return nullMatches;

            return matchSet;
        }
    }
}
