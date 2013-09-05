using System.Data.SqlClient;
using Hibernation;
using NHibernate;

namespace NHibernatorFramework
{
    public class NHibernator
    {
        public static bool HttpModuleUndefined
        {
            get { return !Hibernator.GetHttpModuleDefined(); }
            set { Hibernator.SetHttpModuleDefined(!value); }
        }

        public static string GetFactoryName(string alias)
        {
            return Hibernator.GetFactoryName(alias);
        }

        public static SqlConnection GetConnection()
        {
            return GetConnection(string.Empty);
        }

        public static SqlConnection GetConnection(string sessionFactoryName)
        {
            return (SqlConnection)Hibernator.GetConnection(sessionFactoryName);
        }

        public static ISession GetSession()
        {
            return GetSession(string.Empty);
        }

        public static ISession GetSession(string sessionFactoryName)
        {
            return Hibernator.GetSession(sessionFactoryName);
        }

        public static IStatelessSession GetStatelessSession()
        {
            return GetStatelessSession(string.Empty);
        }

        public static IStatelessSession GetStatelessSession(string sessionFactoryName)
        {
            return Hibernator.GetStatelessSession(sessionFactoryName);
        }

        public static bool SessionExist()
        {
            return SessionExist(string.Empty);
        }

        public static bool SessionExist(string sessionFactoryName)
        {
            return Hibernator.HasOpenSession(sessionFactoryName);
        }

        public static void OpenSession()
        {
            OpenSession(string.Empty);
        }

        public static void OpenSession(string sessionFactoryName)
        {
            Hibernator.OpenSession(sessionFactoryName);
        }

        public static void CloseSession()
        {
            CloseSession(string.Empty);
        }

        public static void CloseSession(string sessionFactoryName)
        {
            Hibernator.CloseSession(sessionFactoryName);
        }

        public static void RestartSession()
        {
            RestartSession(string.Empty);
        }

        public static void RestartSession(string sessionFactoryName)
        {
            Hibernator.RestartSession(sessionFactoryName);
        }
    }
}
