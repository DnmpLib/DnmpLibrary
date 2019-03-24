using System.Collections.Generic;
using System.Linq;

namespace DnmpLibrary.Util
{
    internal class Dsu<T>
    {
        private readonly Dictionary<T, T> parents;

        public Dsu()
        {
            parents = new Dictionary<T, T>();
        }

        public Dsu(IEnumerable<T> fromEnumerable)
        {
            parents = fromEnumerable.ToDictionary(x => x);
        }

        private T GetRoot(T obj)
        {
            if (parents[obj].Equals(obj))
                return obj;
            return parents[obj] = GetRoot(parents[obj]);
        }

        public void MergeSets(T obj1, T obj2)
        {
            parents[GetRoot(obj1)] = GetRoot(obj2);
        }

        public bool InOneSet(T obj1, T obj2)
        {
            return GetRoot(obj1).Equals(GetRoot(obj2));
        }
    }
}