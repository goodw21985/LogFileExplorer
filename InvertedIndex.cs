using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogFileExplorer
{
    public class InvertedIndex
    {
        /// <summary>
        /// The reverse index for text
        /// </summary>
        Dictionary<string, MatchSet> tIndex = new Dictionary<string, MatchSet>();

        /// <summary>
        /// the reverse index for numbers
        /// </summary>
        Dictionary<UInt64, MatchSet> nIndex = new Dictionary<UInt64, MatchSet>();

        /// <summary>
        /// wildcard searches within numbers use the 'x' character as a wildcard
        /// </summary>
        const char numberWildcardChar = 'x';

        /// <summary>
        /// wildcard searches within text use the '*' character as a wildcard
        /// </summary>
        const char stringWildcardChar = '*';

        /// <summary>
        /// Gets a list of all unque number keys in the index within a range
        /// </summary>
        /// <param name="lower">lowest applicable number</param>
        /// <param name="upper">highest applicable number</param>
        /// <param name="errors">place for error messagers</param>
        /// <returns>a list of unique number keys</returns>
        List<UInt64> GetNumberKeys(UInt64 lower, UInt64 upper, List<string> errors = null)
        {
            List<UInt64> numbers = new List<UInt64>();
            foreach (UInt64 v in nIndex.Keys)
            {
                if (v >= lower && v <= upper)
                {
                    numbers.Add(v);
                }
            }
            return numbers;
        }

        /// <summary>
        /// Gets the unique set of number keys in the index given that the key description token is in the range number format
        /// </summary>
        /// <param name="token">key description token in format ###-###</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>list of unique number keys</returns>
        List<UInt64> GetRangeNumberKeys(string token, List<string> errors = null)
        {
            string[] range = token.Split('-');
            if (range.Count() != 2 || range[0] == "" || range[1] == "")
            {
                if (errors!=null) errors.Add(string.Format("number range format incorrect {0}", token));
            }

            UInt64 lower = UInt64.Parse(range[0], System.Globalization.NumberStyles.HexNumber);
            UInt64 upper = UInt64.Parse(range[1], System.Globalization.NumberStyles.HexNumber);
            return GetNumberKeys(lower, upper, errors);
        }

        /// <summary>
        /// Gets the unique set of number keys in the index given that the key description token is in the wildcard number format
        /// </summary>
        /// <param name="token">key description token</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>list of unique number keys</returns>
        List<UInt64> GetWildcardNumberKeys(string token, List<string> errors = null)
        {
            int digits;
            for (digits = 0; token[token.Length - digits - 1] == numberWildcardChar; digits++) ;
            int i = token.IndexOf(numberWildcardChar);
            if (i >= 0 && i < token.Length - digits)
            {
                if (errors!=null) errors.Add(string.Format("number wildcard can only be used on last digits of wildcard {0}", token));
            }

            token = token.Replace(numberWildcardChar, '0');
            UInt64 lower = UInt64.Parse(token, System.Globalization.NumberStyles.HexNumber);
            UInt64 upper = lower + (1UL << (4 * digits)) - 1;
            return GetNumberKeys(lower, upper, errors);
        }

        /// <summary>
        /// Gets the key in the index that is the exact number provided, or returns an empty list.
        /// </summary>
        /// <param name="token">Key description token this is a hexadecimal number</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>a list of unique number keys</returns>
        List<UInt64> GetExactNumberKey(string token, List<string> errors = null)
        {
            MatchSet matches = new MatchSet();
            UInt64 address = UInt64.Parse(token, System.Globalization.NumberStyles.HexNumber);

            List<UInt64> list = new List<ulong>();
            if (nIndex.ContainsKey(address))
            {
                list.Add(address);
            }

            if (list.Count() == 0)
            {
                if (errors!=null) errors.Add(string.Format("number {0} not found in index ", token));
            }

            return list;
        }

        /// <summary>
        /// Gets the unique keys in the index that are matches with a string with wildcards
        /// </summary>
        /// <param name="token">the key description token in the format *a*b</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>a list of unique string keys</returns>
        List<string> GetWildcardStringKeys(string token, List<string> errors = null)
        {
            string[] parts = token.Split('*');
            bool starBegin = token.StartsWith("*");
            bool starEnd = token.EndsWith("*");
            List<string> results = new List<string>();
            foreach (string candidate in tIndex.Keys)
            {
                if (!starBegin)
                {
                    if (!candidate.StartsWith(parts[0])) continue;
                }
                if (!starEnd)
                {
                    if (!candidate.EndsWith(parts.Last())) continue;
                }
                int pos = 0;
                foreach (string part in parts)
                {
                    if (part!="" && pos>=0)
                    {
                        pos = candidate.IndexOf(part,pos);
                        if (pos >= 0) 
                        pos += part.Length;
                    }
                }

                if (pos >= 0)
                {
                    results.Add(candidate);
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the unique key in the index that matches an exact string, or returns an empty list
        /// </summary>
        /// <param name="token">the key description token in the format *a*b</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>a list of unique string keys</returns>
        List<string> GetExactStringKey(string token, List<string> errors = null)
        {
            token = IndexableString(token);
            List<string> list = new List<string>();
            if (tIndex.ContainsKey(token))
            {
                list.Add(token);
            }

            if (list.Count() == 0)
            {
                if (errors!=null) errors.Add(string.Format("key {0} not found in index ", token));
            }

            return list;
        }

        /// <summary>
        /// Gets the set of keys in the index that matches the key description token
        /// </summary>
        /// <param name="token">A key description token in any format</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>a list of number and string keys in string format</returns>
        public List<string> GetKeys(string token, List<string> errors = null)
        {
            bool isNum = IsNumber(token);
            bool isRange = token.StartsWith("#") && token.Contains("-");

            if (isRange)
            {
                string[] parts = token.Substring(1).Split('-');
                if (token.LastIndexOf("#") != 0 || parts.Count() != 2)
                {
                    if (errors!=null) errors.Add(string.Format("line range format is incorrect {0}", token));
                    return new List<string>();
                }

                return new List<string>() { parts[0], parts[1] };
            }
            else if (isNum)
            {
                List<string> results = new List<string>();
                foreach (UInt64 num in GetNumberKeys(token, errors))
                {
                    results.Add(string.Format("{0:X}", num));
                }
                return results;
            }
            else
            {
                List<string> results = GetStringKeys(token, errors);
                results.Sort();
                return results;
            }
        }

        /// <summary>
        /// Gets the set of unique number keys in the index that match key description token
        /// </summary>
        /// <param name="token">A key description token in any supported format for number keys</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>a list of unique number keys</returns>
        List<UInt64> GetNumberKeys(string token, List<string> errors = null)
        {
            if (token.Contains(numberWildcardChar))
            {
                return GetWildcardNumberKeys(token, errors);
            }
            else if (token.Contains('-'))
            {
                return GetRangeNumberKeys(token, errors);
            }
            else
            {
                return GetExactNumberKey(token, errors);
            }
        }

        /// <summary>
        /// Gets all unique string keys that match a key desciption token
        /// </summary>
        /// <param name="token">A key description token of any supported format for string keys</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>a list of unique string keys</returns>
        List<string> GetStringKeys(string token, List<string> errors = null)
        {
            if (token.Contains(stringWildcardChar))
            {
                return GetWildcardStringKeys(token, errors);
            }
            else
            {
                return GetExactStringKey(token, errors);
            }
        }

        /// <summary>
        /// Determines if a character is alphanumeric
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>true if c is alphanumeric</returns>
        public static bool IsAlphaNum(char c)
        {
            return (c >= '0' && c <= '9' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z');
        }

        /// <summary>
        /// Determines if a character is a hexidecimal digit
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>true if c is a hexidecimal digit</returns>
        public static bool IsHex(char c)
        {
            return (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');
        }

        /// <summary>
        /// Determines if a key desciption token describes a number
        /// </summary>
        /// <param name="token">a key description token</param>
        /// <returns>true if this token describes a number token</returns>
        public static bool IsNumber(string token)
        {
            bool nonwildcardseen = false;
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (c != numberWildcardChar) nonwildcardseen = true;
                if (!IsHex(c) & c != '-' & c != numberWildcardChar)
                    return false;
            }

            return nonwildcardseen;
        }

        /// <summary>
        /// Finds all matches from any of the provided number keys.
        /// </summary>
        /// <param name="numberKeys">set of number keys to find in index</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>match set</returns>
        public MatchSet Matches(List<UInt64> numberKeys, List<string> errors = null)
        {
            MatchSet matches = new MatchSet();
            foreach (UInt64 v in numberKeys)
            {
                matches.Or(nIndex[v]);
            }

            return matches;
        }

        /// <summary>
        /// Finds all matches from any of the provided string keys.
        /// </summary>
        /// <param name="stringKeys">set of string keys to find in index</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>match set</returns>
        public MatchSet Matches(List<string> stringKeys, List<string> errors = null)
        {
            MatchSet matches = new MatchSet();
            foreach (string v in stringKeys)
            {
                matches.Or(tIndex[v]);
            }

            return matches;
        }

        /// <summary>
        /// Provides a set of matches for a range query (range of log entry line numbers)
        /// </summary>
        /// <param name="token">key desctiption token describing the entry number range in the format '#30-40'</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>match set</returns>
        public MatchSet RangeMatches(string token, List<string> errors = null)
        {
            string[] parts = token.Substring(1).Split('-');
            if (token.LastIndexOf("#") != 0 || parts.Count() != 2)
            {
                if (errors!=null) errors.Add(string.Format("line range format is incorrect {0}", token));
                return new MatchSet();
            }
            int n1 = 0;
            int n2 = Count - 1;
            try
            {
                n1 = int.Parse(parts[0]);
                n2 = int.Parse(parts[1]);
            }
            catch
            {
                if (errors!=null) errors.Add(string.Format("line range format is incorrect {0}", token));
                return new MatchSet();
            };

            MatchSet m = new MatchSet();
            for (int i = n1; i <= n2; i++)
            {
                m.Add(new EntryId(i));
            }

            return m;
        }

        /// <summary>
        /// Returns all matches for a key desciption token of any format
        /// </summary>
        /// <param name="token">key desctiption token in any format</param>
        /// <param name="errors">Place to add error messages</param>
        /// <returns>Match set</returns>
        public MatchSet Matches(string token, List<string> errors = null)
        {

            bool isNum = IsNumber(token);
            bool isRange = token.StartsWith("#") && token.Contains("-");

            if (isRange)
            {
                return RangeMatches(token, errors);
            }
            else if (isNum)
            {
                return Matches(GetNumberKeys(token, errors), errors);
            }
            else
            {
                return Matches(GetStringKeys(token, errors), errors);
            }
        }

        /// <summary>
        /// parsing state for use when parsing a line from a log file
        /// </summary>
        public enum states
        {
            linenum, 
            text,
            number
        };

        /// <summary>
        /// number of lines read so far from log file
        /// </summary>
        int count = 0;

        /// <summary>
        /// Gets the number of log entries read from the file
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// The characters in the log file which are not indexed, and delimit the rest of the log entry into keys
        /// </summary>
        char[] delims = new char[] { ':', ',', '(', ')', '[', ']', '-', '>', '<', '-', '=', '*', '.' };

        /// <summary>
        /// Converts a substring from the log entry into a valid key
        /// A key contains letters and numbers and '_'
        /// All non delimiters (including '_') are turned into '_'
        /// keys never start or end with '_', nor have two '_' in a row.
        /// </summary>
        /// <param name="input">the substring from the log file</param>
        /// <returns>a valid key string</returns>
        static string IndexableString(string input)
        {
            var in1 = input.Trim().ToLower();
            StringBuilder sb = new StringBuilder();

            char last_c = ' ';
            for (int i = 0; i < in1.Length; i++)
            {
                char c = in1[i];
                if (c >= '0' && c <= '9' || c >= 'a' && c <= 'z')
                {
                    sb.Append(c);
                    last_c = c;
                }
                else if (last_c != '_')
                {
                    sb.Append(' ');
                    last_c = '_';
                }
            }

            string result = sb.ToString().Trim().Replace(" ", "_");

            return result;
        }

        /// <summary>
        /// add a log entry to the index
        /// 
        /// This method parse the entryString into Key tokens.  
        /// A number is determined when an alphanumeric sequence of characters is all hexidecimal characters.
        /// 'badfeed' is a number, 'badfood' is not.
        /// The rest of the string is split into substrings, delimited by any number or any delimiter character.
        /// Each substring is converted into a valid key (trimming, and using '_' for spaces, etc.)
        /// Each string key and number key is added to the index to help find this entry.
        /// </summary>
        /// <param name="entryId">the index into the log</param>
        /// <param name="entryString">The string that describes the log entry</param>
        public void AddLogEntry(int entryId, string entryString)
        {
            count = Math.Max(count, entryId + 1);
            List<UInt64> numberKeys = new List<ulong>();
            List<string> stringKeys = new List<string>();

            states state = states.linenum;

            int lastc = 0;
            char prev = '0';
            for (int i = 0; i < entryString.Count(); i++)
            {
                char c = entryString[i];
                switch (state)
                {
                    case states.linenum:
                        if (c == ',')
                        {
                            state = states.text;
                            lastc = i + 1;
                        }
                        break;
                    case states.text:
                        bool isStartOfNumber = false;
                        if (c == ' ' && i == lastc) lastc++;
                        if (IsHex(c) && !IsAlphaNum(prev))
                        {
                            isStartOfNumber = true;
                            for (int j = i + 1; j < entryString.Length; j++)
                            {
                                char c2 = entryString[j];
                                if (IsHex(c2))
                                {
                                    // still ok.
                                }
                                else if (c2 >= 'a' && c2 <= 'z')
                                {
                                    isStartOfNumber = false;
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        if (isStartOfNumber)
                        {
                            if (i > lastc)
                            {
                                var p1 = entryString.Substring(lastc, i - lastc);
                                stringKeys.Add(p1);
                            }

                            lastc = i;
                            state = states.number;
                        }
                        break;
                    case states.number:
                        if (!IsHex(c))
                        {
                            if (i > lastc)
                            {
                                string num = entryString.Substring(lastc, i - lastc);
                                numberKeys.Add(UInt64.Parse(num, System.Globalization.NumberStyles.HexNumber));
                            }

                            lastc = i;
                            state = states.text;
                        }
                        break;
                }

                prev = c;
            }

            // cleanup
            switch (state)
            {
                case states.linenum:

                    break;
                case states.text:

                    stringKeys.Add(entryString.Substring(lastc));
                    break;
                case states.number:
                    {
                        string num = entryString.Substring(lastc);
                        numberKeys.Add(UInt64.Parse(num, System.Globalization.NumberStyles.HexNumber));
                    }
                    break;
            }

            // add stringkeys to index
            foreach (string s in stringKeys)
            {
                string[] parts = s.Split(delims);
                foreach (string part in parts)
                {
                    string p = IndexableString(part);
                    if (p == "") continue;
                    lock (tIndex)
                    {
                        if (!tIndex.ContainsKey(p))
                        {
                            tIndex[p] = new MatchSet();
                        }
                    }

                    lock (tIndex[p])
                    {
                        tIndex[p].Add(new EntryId(entryId));
                    }
                }
            }

            // add number keys to index
            foreach (UInt64 n in numberKeys)
            {
                lock (nIndex)
                {
                    if (!nIndex.ContainsKey(n))
                    {
                        nIndex[n] = new MatchSet();
                    }

                    nIndex[n].Add(new EntryId(entryId));
                }
            }
        }
    }
}