using System.Data;
using Hibernation;

namespace NHibernatorFramework
{
    public class NHibernatorTransaction : HibernationTransaction
    {
        public NHibernatorTransaction()
            : base(string.Empty) {}

        public NHibernatorTransaction(string sessionFactoryName)
            : base(sessionFactoryName) {}

        public NHibernatorTransaction(string sessionFactoryName, IsolationLevel isolationLevel)
            : base(sessionFactoryName, isolationLevel) {}
    }
}
