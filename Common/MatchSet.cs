using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
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

    public class IMatchSet : IEnumerable<EntryId>
    {
        public virtual int Count
        {
            get
            {
                throw new UnauthorizedAccessException();
            }
        }

        public virtual bool Contains(EntryId id)
        {
            throw new UnauthorizedAccessException();
        }

        public virtual IEnumerator<EntryId> GetEnumerator()
        {
            throw new UnauthorizedAccessException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public static IMatchSet operator |(IMatchSet a, IMatchSet b)
        {
            if (b == null) return a;
            if (a == null) return b;
            MatchSet r = new MatchSet();
            foreach (EntryId n in a)
            {
                r.Add(n);
            }

            foreach (EntryId n in b)
            {
                r.Add(n);
            }

            return r;
        }

        public static IMatchSet operator &(IMatchSet a, IMatchSet b)
        {
            if (b == null) return a;
            if (a == null) return b;
            MatchSet r = new MatchSet();
            if (a.Count < b.Count)
            {
                foreach (EntryId n in a)
                {
                    if (b.Contains(n)) r.Add(n);
                }
            }
            else
            {
                foreach (EntryId n in b)
                {
                    if (a.Contains(n)) r.Add(n);
                }
            }

            return r;
        }
    }

    public class RangeMatchSet : IMatchSet
    {
        int low = int.MaxValue;
        int high = int.MaxValue;
        override public IEnumerator<EntryId> GetEnumerator()
        {
            return new RangeEnumerator(low, high);
        }

        public RangeMatchSet(EntryId from, EntryId to)
        {
            low = from.Value;
            high = to.Value;
        }

        override public int Count
        {
            get { return Math.Min(0, high - low + 1); }
        }

        override public bool Contains(EntryId id)
        {
            return low <= id.Value && high >= id.Value;
        }

        internal class RangeEnumerator : IEnumerator<EntryId>
        {
            int low;
            int high;
            int current;

            public EntryId Current
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return new EntryId(current);
                }
            }

            public RangeEnumerator(int low, int high)
            {
                this.low = low;
                this.high = high;
                this.current = low - 1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                this.current++;
                return (current >= low && current <= high);
            }

            public void Reset()
            {
                this.current = low - 1;
            }
        }
    }
    public class MatchSet : IMatchSet
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

        public void Or(IMatchSet b)
        {
            foreach (EntryId n in b)
            {
                Add(n);
            }
        }

        public void Add(EntryId n)
        {
            matches.Add(n);
        }

        override public int Count
        {
            get
            {
                return matches.Count;
            }
        }

        override public bool Contains(EntryId id)
        {
            return matches.Contains(id);
        }

        override public IEnumerator<EntryId> GetEnumerator()
        {
            return matches.GetEnumerator();
        }
    }
}
