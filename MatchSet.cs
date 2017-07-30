using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogFileExplorer
{
    /// <summary>
    /// A wrapper for int to make it a unique type that descibes a unique id for each entry in the log table
    /// </summary>
    public struct EntryId
    {
        int v;
        public EntryId(int n)
        {
            v = n;
        }

        public int Value
        {
            get
            {
                return v;
            }
        }
    }

    public class MatchSet : IEnumerable<EntryId>
    {
        HashSet<EntryId> matches = null;

        public MatchSet()
        {
            matches = new HashSet<EntryId>();
        }

        public MatchSet(HashSet<EntryId> matches)
        {
            this.matches = matches;
        }

        public MatchSet(EntryId from, EntryId to)
        {
            matches = new HashSet<EntryId>();
            for (int i=from.Value; i<=to.Value; i++)
            {
                matches.Add(new EntryId(i));
            }
        }

        public void Or(MatchSet b)
        {
            foreach (EntryId n in b.matches)
            {
                Add(n);
            }
        }

        public static MatchSet operator |(MatchSet a, MatchSet b)
        {
            if (b == null) return a;
            if (a == null) return b;
            MatchSet r = new MatchSet();
            foreach (EntryId n in a.matches)
            {
                r.Add(n);
            }

            foreach (EntryId n in b.matches)
            {
                r.Add(n);
            }

            return r;
        }

        public static MatchSet operator &(MatchSet a, MatchSet b)
        {
            if (b == null) return a;
            if (a == null) return b;
            MatchSet r = new MatchSet();
            if (a.matches.Count < b.matches.Count)
            {
                foreach (EntryId n in a.matches)
                {
                    if (b.Contains(n)) r.Add(n);
                }
            }
            else
            {
                foreach (EntryId n in b.matches)
                {
                    if (a.Contains(n)) r.Add(n);
                }
            }

            return r;
        }


        public void Add(EntryId n)
        {
            matches.Add(n);
        }

        public int Count
        {
            get
            {
                return matches.Count;
            }
        }

        public IEnumerator<EntryId> GetEnumerator()
        {
            return matches.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
