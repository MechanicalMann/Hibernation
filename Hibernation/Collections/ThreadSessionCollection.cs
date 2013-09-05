using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate;

namespace Hibernation.Collections
{
    internal class ThreadSessionCollection : ISessionCollection
    {
        [ThreadStatic]
        private static Dictionary<string, ISession> sessions;
        private static Dictionary<string, ISession> Sessions
        {
            get { return sessions ?? (sessions = new Dictionary<string, ISession>()); }
        }

        [ThreadStatic]
        private static SessionStartMode sessionStartMode;

        public IEnumerable<string> Keys
        {
            get
            {
                return Sessions.Keys;
            }
        }

        public IEnumerable<ISession> Values
        {
            get
            {
                return Sessions.Values;
            }
        }

        #region ISessionCollection Members

        public SessionStartMode SessionMode
        {
            get
            {
                return sessionStartMode;
            }
            set
            {
                sessionStartMode = value;
            }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return Sessions.GetEnumerator();
        }

        #endregion

        #region IEnumerable<ISession> Members

        IEnumerator<ISession> IEnumerable<ISession>.GetEnumerator()
        {
            return Sessions.Values.GetEnumerator();
        }

        #endregion

        public ISession this[string sessionFactoryKey]
        {
            get { return Get(sessionFactoryKey); }
            set { Sessions[sessionFactoryKey] = value; }
        }

        public ISession Get(string sessionFactoryKey)
        {
            if (Sessions == null)
                throw new HibernateException("No SessionFactories have been configured.");
            if (!Sessions.ContainsKey(sessionFactoryKey))
                throw new HibernationException("No SessionFactory found with that name.");

            return Sessions[sessionFactoryKey];
        }

        public void Add(string sessionFactoryKey, ISession session)
        {
            Sessions.Add(sessionFactoryKey, session);
        }

        public void Remove(string sessionFactoryKey)
        {
            Sessions.Remove(sessionFactoryKey);
        }

        public bool ContainsKey(string key)
        {
            return Sessions.ContainsKey(key);
        }
    }
}
