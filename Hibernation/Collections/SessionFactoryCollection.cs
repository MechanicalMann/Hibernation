using System.Collections;
using System.Collections.Generic;
using NHibernate;

namespace Hibernation.Collections
{
    internal class SessionFactoryCollection : IDictionary<string, ISessionFactory>
    {
        private Dictionary<string, ISessionFactory> sessionFactories;

        public SessionFactoryCollection()
        {
            sessionFactories = new Dictionary<string, ISessionFactory>();
        }

        public ISessionFactory this[string factoryName]
        {
            get
            {
                return sessionFactories[factoryName.ToUpper()];
            }
            set
            {
                sessionFactories[factoryName.ToUpper()] = value;
            }
        }

        public bool ContainsKey(string key)
        {
            return sessionFactories.ContainsKey(key.ToUpper());
        }

        public int Count
        {
            get
            {
                return sessionFactories.Count;
            }
        }

        #region IDictionary<string,ISessionFactory> Members

        public void Add(string key, ISessionFactory value)
        {
            sessionFactories.Add(key.ToUpper(), value);
        }

        public ICollection<string> Keys
        {
            get { return sessionFactories.Keys; }
        }

        public bool Remove(string key)
        {
            return sessionFactories.Remove(key.ToUpper());
        }

        public bool TryGetValue(string key, out ISessionFactory value)
        {
            return sessionFactories.TryGetValue(key.ToUpper(), out value);
        }

        public ICollection<ISessionFactory> Values
        {
            get { return sessionFactories.Values; }
        }

        #endregion

        #region ICollection<KeyValuePair<string,ISessionFactory>> Members

        public void Add(KeyValuePair<string, ISessionFactory> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            sessionFactories.Clear();
        }

        public bool Contains(KeyValuePair<string, ISessionFactory> item)
        {
            return (sessionFactories.ContainsKey(item.Key.ToUpper()) && sessionFactories.ContainsValue(item.Value));
        }

        public void CopyTo(KeyValuePair<string, ISessionFactory>[] array, int arrayIndex)
        {
            (sessionFactories as IDictionary<string, ISessionFactory>).CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, ISessionFactory> item)
        {
            if (ContainsKey(item.Key) && sessionFactories.ContainsValue(item.Value))
                return Remove(item.Key);
            return false;
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,ISessionFactory>> Members

        IEnumerator<KeyValuePair<string, ISessionFactory>> IEnumerable<KeyValuePair<string, ISessionFactory>>.GetEnumerator()
        {
            return sessionFactories.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (sessionFactories as IEnumerable).GetEnumerator();
        }

        #endregion
    }
}
