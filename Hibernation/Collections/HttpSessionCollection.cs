using System.Collections;
using System.Collections.Generic;
using System.Web;
using NHibernate;

namespace Hibernation.Collections
{
    internal class HttpSessionCollection : ISessionCollection
    {
        private const string HibernationSessionKey = "Hibernation.Session";
        private const string HibernationStartmodeKey = "Hibernation.StartMode";

        private Dictionary<string, ISession> _sessions
        {
            get
            {
                Dictionary<string, ISession> sessionMap = HttpContext.Current.Items[HibernationSessionKey] as Dictionary<string, ISession>;
                if (sessionMap == null)
                {
                    sessionMap = new Dictionary<string, ISession>();
                    HttpContext.Current.Items[HibernationSessionKey] = sessionMap;
                }
                return sessionMap;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return _sessions.Keys;
            }
        }

        public IEnumerable<ISession> Values
        {
            get
            {
                return _sessions.Values;
            }
        }

        #region ISessionCollection Members

        public SessionStartMode SessionMode
        {
            get
            {
                object openMode = HttpContext.Current.Items[HibernationStartmodeKey];
                if (openMode != null)
                {
                    return (SessionStartMode)openMode;
                }
                return SessionStartMode.Unknown;
            }
            set
            {
                HttpContext.Current.Items[HibernationStartmodeKey] = value;
            }
        }

        #endregion

        #region IEnumerable<ISession> Members

        public IEnumerator<ISession> GetEnumerator()
        {
            return _sessions.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _sessions.GetEnumerator();
        }

        #endregion

        public ISession this[string sessionFactoryKey]
        {
            get { return Get(sessionFactoryKey); }
            set { _sessions[sessionFactoryKey] = value; }
        }

        public ISession Get(string sessionFactoryKey)
        {
            if (_sessions == null)
                throw new HibernateException("No SessionFactories have been configured.");
            if (!_sessions.ContainsKey(sessionFactoryKey))
                throw new HibernationException("No SessionFactory found with that name.");

            return _sessions[sessionFactoryKey];
        }

        public void Add(string sessionFactoryKey, ISession session)
        {
            _sessions.Add(sessionFactoryKey, session);
        }

        public void Remove(string sessionFactoryKey)
        {
            _sessions.Remove(sessionFactoryKey);
        }

        public bool ContainsKey(string key)
        {
            return _sessions.ContainsKey(key);
        }
    }
}
