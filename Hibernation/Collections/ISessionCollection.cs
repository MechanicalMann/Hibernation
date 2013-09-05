using System.Collections.Generic;
using NHibernate;

namespace Hibernation.Collections
{
    internal interface ISessionCollection : IEnumerable<ISession>
    {
        SessionStartMode SessionMode { get; set; }
        bool ContainsKey(string key);
        void Add(string sessionFactoryKey, ISession session);
        ISession Get(string sessionFactoryKey);
        void Remove(string sessionFactoryKey);
        ISession this[string sessionFactoryKey] { get; set; }
        IEnumerable<string> Keys { get; }
        IEnumerable<ISession> Values { get; }
    }
}
