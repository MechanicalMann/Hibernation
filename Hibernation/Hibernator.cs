using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using NHibernate;
using NHibernate.Cfg.ConfigurationSchema;
using NHibernate.Cfg.Loquacious;
using NHibernate.Engine;
using Hibernation.Collections;
using Hibernation.Configuration;
using Hibernation.CustomHqlGeneration;

namespace Hibernation
{
    /// <summary>
    /// A singleton class for managing NHibernate session factories.
    /// </summary>
    public sealed class Hibernator
    {
        private const string ConfigFileKey       = "HibernationConfigurationFiles";
        private const string InterceptorClassKey = "HibernationInterceptorClass";
        private const string FactoryAliasesKey   = "HibernationSessionFactoryAliases";
        private const string DefaultConfigFile   = "hibernate.cfg.xml";

        private string ConfigurationFilePath { get; set; }

        private SessionFactoryCollection SessionFactories { get; set; }
        private ISessionCollection SessionStorage { get; set; }
        private IInterceptor SessionInterceptor { get; set; }

        private Dictionary<string, string> FactoryAliasMap { get; set; }

        private bool RequireHttpModule { get; set; }
        private bool HttpModuleDefined { get; set; }
        private bool IsConfigured { get; set; }

        #region Access

        /// <summary>
        /// Get the default session factory.
        /// </summary>
        /// <returns>
        /// The <c>ISessionFactory</c> defined with the default name
        /// (blank).
        /// </returns>
        public static ISessionFactory GetSessionFactory()
        {
            return GetSessionFactory(string.Empty);
        }

        /// <summary>
        /// Get the indicated session factory.
        /// </summary>
        /// <param name="factoryName">The name of the requested session factory.</param>
        /// <returns>The <c>ISessionFactory</c> defined with the given name.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static ISessionFactory GetSessionFactory(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            if (!Instance.IsConfigured)
                Configure();

            return Instance.SessionFactories[GetFactoryName(factoryName)];
        }


        /// <summary>
        /// Get the default session factory's <c>IDbConnection</c>.
        /// </summary>
        /// <returns>
        /// The <c>IDbConnection</c> for the the default session factory.
        /// </returns>
        public static IDbConnection GetConnection()
        {
            return GetConnection(string.Empty);
        }

        /// <summary>
        /// Get the named session factory's <c>IDbConnection</c>.
        /// </summary>
        /// <param name="factoryName">
        /// The name of the session factory whose connection is being requested.
        /// </param>
        /// <returns>
        /// The <c>IDbConnection</c> set for the named session factory.
        /// </returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static IDbConnection GetConnection(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            var sessionFactory = (ISessionFactoryImplementor)GetSessionFactory(factoryName);
            if (sessionFactory == null)
                throw new HibernateException(string.Format("No session factory was found with the name {0}", factoryName));
            return sessionFactory.ConnectionProvider.GetConnection();
        }


        /// <summary>
        /// Opens a new session on the default session factory.
        /// </summary>
        /// <returns>The opened <c>ISession</c>.</returns>
        /// <exception cref="Hibernation.HibernationException">
        /// Thrown if this method is called from a web application but no
        /// HttpModule is defined in the web.config.
        /// </exception>
        public static ISession OpenSession()
        {
            return OpenSession(string.Empty, GetConnection(string.Empty), Instance.SessionInterceptor);
        }

        /// <summary>
        /// Opens a new session on the named session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory that should open the session.
        /// </param>
        /// <returns>The opened <c>ISession</c>.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        /// <exception cref="Hibernation.HibernationException">
        /// Thrown if this method is called from a web application but no
        /// HttpModule is defined in the web.config.
        /// </exception>
        public static ISession OpenSession(string factoryName)
        {
            return OpenSession(factoryName, GetConnection(factoryName), Instance.SessionInterceptor);
        }

        /// <summary>
        /// Opens a new session on the provided <c>IDbConnection</c>, using the
        /// named session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory that should open the session.
        /// </param>
        /// <param name="connection">
        /// The <c>IDbConnection</c> on which to open the session.
        /// </param>
        /// <returns>The opened <c>ISession</c>.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        /// <exception cref="Hibernation.HibernationException">
        /// Thrown if this method is called from a web application but no
        /// HttpModule is defined in the web.config.
        /// </exception>
        public static ISession OpenSession(string factoryName, IDbConnection connection)
        {
            return OpenSession(factoryName, connection, Instance.SessionInterceptor);
        }

        /// <summary>
        /// Opens a new session on the provided <c>IDbConnection</c>, using the
        /// named session factory and the supplied session interceptor.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory that should open the session.
        /// </param>
        /// <param name="connection">
        /// The <c>IDbConnection</c> on which to open the session.
        /// </param>
        /// <param name="interceptor">
        /// An <c>IInterceptor</c> to use with the opened session.
        /// </param>
        /// <returns>The opened <c>ISession</c>.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        /// <exception cref="Hibernation.HibernationException">
        /// Thrown if this method is called from a web application but no
        /// HttpModule is defined in the web.config.
        /// </exception>
        public static ISession OpenSession(string factoryName, IDbConnection connection, IInterceptor interceptor)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");
            if (connection == null)
                throw new ArgumentNullException("connection");

            if (Instance.SessionStorage.SessionMode == SessionStartMode.Unknown)
                Instance.SessionStorage.SessionMode = SessionStartMode.Manual;

            if (Instance.RequireHttpModule && !Instance.HttpModuleDefined)
                throw new HibernationException("HttpModule is not defined.  Remember to declare OpenSessionInViewModule in the web.config.");

            string fact = GetFactoryName(factoryName);

            if (!Instance.SessionFactories.ContainsKey(fact))
                throw new HibernationException("No ISessionFactory exists with the name or alias {0}.  Please check your configuration.", new KeyNotFoundException(), fact);

            var sessionFactory = Instance.SessionFactories[fact];

            // I wonder if we can pass a null interceptor to OpenSession...
            var session = interceptor == null ? sessionFactory.OpenSession(connection) : sessionFactory.OpenSession(connection, interceptor);

            Instance.SessionStorage.Add(fact, session);

            return session;
        }


        /// <summary>
        /// Gets or opens a session from the default session factory.
        /// </summary>
        /// <returns>A new or existing <c>ISession</c>.</returns>
        public static ISession GetSession()
        {
            return GetSession(string.Empty);
        }

        /// <summary>
        /// Gets or opens a session from the named session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory whose session is required.
        /// </param>
        /// <returns>A new or existing <c>ISession</c>.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static ISession GetSession(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            string fact = GetFactoryName(factoryName);
            if (!Instance.SessionStorage.ContainsKey(fact))
                OpenSession(factoryName);

            return Instance.SessionStorage[fact];
        }

        /// <summary>
        /// Gets a new stateless session on the default session factory.
        /// </summary>
        /// <returns>A new stateless session.</returns>
        public static IStatelessSession GetStatelessSession()
        {
            return GetStatelessSession(string.Empty);
        }

        /// <summary>
        /// Gets a new stateless session on the specified session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The name of the session factory on which the new stateless session
        /// will be opened.
        /// </param>
        /// <returns>A new stateless session.</returns>
        public static IStatelessSession GetStatelessSession(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            var sessionFactory = GetSessionFactory(factoryName);
            return sessionFactory.OpenStatelessSession();
        }


        /// <summary>
        /// Indicates whether the default session factory has an open session.
        /// </summary>
        /// <returns>True if the session factory has an open session.</returns>
        public static bool HasOpenSession()
        {
            return HasOpenSession(string.Empty);
        }

        /// <summary>
        /// Indicates whether the named session factory has an open session.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory whose session is required.
        /// </param>
        /// <returns>True if the session factory has an open session.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static bool HasOpenSession(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            string fact = GetFactoryName(factoryName);
            return Instance.SessionStorage.ContainsKey(fact);
        }


        /// <summary>
        /// Closes the session on the default session factory, if open.
        /// </summary>
        public static void CloseSession()
        {
            CloseSession(string.Empty);
        }

        /// <summary>
        /// Closes the session on the named session factory, if open.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory whose session will be closed.
        /// </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static void CloseSession(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            string fact = GetFactoryName(factoryName);
            
            if (!Instance.SessionStorage.ContainsKey(fact))
                return;

            var session = Instance.SessionStorage[fact];

            if (session != null && session.IsOpen)
                session.Close();

            Instance.SessionStorage.Remove(fact);
        }


        /// <summary>
        /// Closes all sessions on all configured session factories.
        /// </summary>
        public static void CloseAllSessions()
        {
            if (Instance.SessionStorage == null)
                return;

            foreach (var sessionKey in Instance.SessionStorage.Keys)
            {
                CloseSession(sessionKey);
            }
        }


        /// <summary>
        /// Closes and re-opens the session on the default session factory.
        /// </summary>
        /// <returns>The re-opened <c>ISession</c>.</returns>
        public static ISession RestartSession()
        {
            return RestartSession(string.Empty);
        }

        /// <summary>
        /// Closes and re-opens the session on the named session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory whose session will be restarted.
        /// </param>
        /// <returns>The re-opened <c>ISession</c>.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static ISession RestartSession(string factoryName)
        {
            CloseSession(factoryName);
            return OpenSession(factoryName);
        }


        /// <summary>
        /// Begin a new unit of work on the default session factory.
        /// </summary>
        /// <returns>An ITransaction representing this unit of work.</returns>
        public static ITransaction BeginTransaction()
        {
            return BeginTransaction(string.Empty, null);
        }

        /// <summary>
        /// Begin a new unit of work on the named session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory on whose session the transaction will operate.
        /// </param>
        /// <returns>An ITransaction representing this unit of work.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static ITransaction BeginTransaction(string factoryName)
        {
            return BeginTransaction(factoryName, null);
        }

        /// <summary>
        /// Begin a new unit of work on the named session factory, using the
        /// specified isolation level.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory on whose session the transaction will operate.
        /// </param>
        /// <param name="isolationLevel">
        /// The requested isolation level.
        /// </param>
        /// <returns>An ITransaction representing this unit of work.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static ITransaction BeginTransaction(string factoryName, IsolationLevel? isolationLevel)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            if (Instance.SessionStorage.SessionMode == SessionStartMode.Unknown)
                Instance.SessionStorage.SessionMode = SessionStartMode.AtuoTransactionScope;

            var session = GetSession(factoryName);

            if (isolationLevel.HasValue)
                return session.BeginTransaction(isolationLevel.Value);
            
            return session.BeginTransaction();
        }


        /// <summary>
        /// Commit the open transaction on the default session factory.
        /// </summary>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if no transaction is open on this session.
        /// </exception>
        public static void CommitTransaction()
        {
            CommitTransaction(string.Empty);
        }

        /// <summary>
        /// Commit the open transaction on the named session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory whose transaction will be committed.
        /// </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if no transaction is open on this session.
        /// </exception>
        public static void CommitTransaction(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            var session = GetSession(factoryName);

            session.Transaction.Commit();

            if (Instance.SessionStorage.SessionMode == SessionStartMode.AtuoTransactionScope)
                CloseSession(factoryName);
        }


        /// <summary>
        /// Roll back the open transaction on the default session factory.
        /// </summary>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if no transaction is open on this session.
        /// </exception>
        public static void RollbackTransaction()
        {
            RollbackTransaction(string.Empty);
        }

        /// <summary>
        /// Roll back the open transaction on the named session factory.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory whose transaction will be rolled back.
        /// </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if no transaction is open on this session.
        /// </exception>
        public static void RollbackTransaction(string factoryName)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            var session = GetSession(factoryName);

            session.Transaction.Rollback();

            if (Instance.SessionStorage.SessionMode == SessionStartMode.AtuoTransactionScope)
                CloseSession(factoryName);
        }


        /// <summary>
        /// Begins a new transaction on the default session factory, and
        /// returns it wrapped in a new <c>HibernationTransaction</c> object.
        /// </summary>
        /// <returns>
        /// A <c>HibernationTransaction</c> containing the new transaction.
        /// </returns>
        public static HibernationTransaction Transaction()
        {
            return Transaction(string.Empty, null);
        }

        /// <summary>
        /// Begins a new transaction on the named session factory, and returns
        /// it wrapped in a new <c>HibernationTransaction</c> object.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory on whose session the transaction will operate.
        /// </param>
        /// <returns>
        /// A <c>HibernationTransaction</c> containing the new transaction.
        /// </returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static HibernationTransaction Transaction(string factoryName)
        {
            return Transaction(factoryName, null);
        }

        /// <summary>
        /// Begins a new transaction on the named session factory using the
        /// requested isolation level, and returns it wrapped in a new
        /// <c>HibernationTransaction</c> object.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory on whose session the transaction will operate.
        /// </param>
        /// <param name="isolationLevel">
        /// The requested isolation level.
        /// </param>
        /// <returns>
        /// A <c>HibernationTransaction</c> containing the new transaction.
        /// </returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown if no <c>ISessionFactory</c> was defined with the requested
        /// name.
        /// </exception>
        public static HibernationTransaction Transaction(string factoryName, IsolationLevel? isolationLevel)
        {
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            var session = GetSession(factoryName);
            var transaction = BeginTransaction(factoryName, isolationLevel);
            return new HibernationTransaction(factoryName, session, transaction);
        }


        /// <summary>
        /// Gets the <c>SessionStartMode</c> flag representing how Hibernator's
        /// internal session storage was initiated.
        /// </summary>
        /// <returns>
        /// The internal session storage object's <c>SessionStartMode</c>.
        /// </returns>
        internal static SessionStartMode GetSessionMode()
        {
            return Instance.SessionStorage.SessionMode;
        }

        /// <summary>
        /// Sets the <c>SessionStartMode</c> flag on Hibernator's internal
        /// session storage.
        /// </summary>
        /// <param name="sessionMode">
        /// A <c>SessionStartMode</c> flag indicating how the session storage
        /// object was initiated.
        /// </param>
        internal static void SetSessionMode(SessionStartMode sessionMode)
        {
            Instance.SessionStorage.SessionMode = sessionMode;
        }


        /// <summary>
        /// Gets the value of <c>HttpModuleDefined</c>, which indicates whether
        /// an HttpModule is configured for the Hibernator instance.
        /// </summary>
        /// <returns>True if an HttpModule is defined.</returns>
        public static bool GetHttpModuleDefined()
        {
            return Instance.HttpModuleDefined;
        }

        /// <summary>
        /// Sets the <c>HttpModuleDefined</c> flag, indicating whether an
        /// HttpModule is configured for the Hibernator instance.
        /// </summary>
        /// <param name="isDefined">
        /// The value to which <c>HttpModuleDefined</c> will be set.
        /// </param>
        public static void SetHttpModuleDefined(bool isDefined)
        {
            Instance.HttpModuleDefined = isDefined;
        }


        /// <summary>
        /// Returns the Hibernator instance's internal session factory
        /// collection.
        /// </summary>
        /// <returns>
        /// A dictionary containing all session factories managed by the
        /// Hibernator instance.
        /// </returns>
        internal static IDictionary<string, ISessionFactory> GetSessionFactoryCollection()
        {
            return Instance.SessionFactories;
        }

        /// <summary>
        /// Replaces the Hibernator instances' internal session factory storage
        /// with the supplied session factory collection.
        /// </summary>
        /// <param name="sessionFactoryCollection">
        /// The session factory collection to replace Hibernator's current
        /// collection.
        /// </param>
        public static void SetSessionFactoryCollection(IDictionary<string, ISessionFactory> sessionFactoryCollection)
        {
            Instance.SessionFactories = (SessionFactoryCollection)sessionFactoryCollection;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Reads non-NHibernate configuration settings from the configuration
        /// file, if any.
        /// </summary>
        private void ConfigureCommon()
        {
            if (IsConfigured)
                return;

            ConfigurationFilePath = ConfigurationManager.AppSettings[ConfigFileKey];
            var configInterceptorClass = ConfigurationManager.AppSettings[InterceptorClassKey];
            var configFactoryAliases = ConfigurationManager.AppSettings[FactoryAliasesKey];

            if (!string.IsNullOrEmpty(configFactoryAliases))
            {
                SetFactoryAliases(configFactoryAliases);
            }

            if (SessionInterceptor == null && !string.IsNullOrEmpty(configInterceptorClass))
            {
                var interceptorClassType = Type.GetType(configInterceptorClass);

                if (interceptorClassType != null)
                {
                    if (interceptorClassType.GetInterface("NHibernate.IInterceptor") != null)
                    {
                        SessionInterceptor = (IInterceptor)Activator.CreateInstance(interceptorClassType);
                    }
                }
            }

            // Just to ensure we don't run this method over and over.
            IsConfigured = true;
        }

        /// <summary>
        /// Loads Hibernation app settings.
        /// Then attempts to load NHibernate configuration from, in order:
        /// a semicolon-separated list of NHibernate configuration files;
        /// a "hibernation" section in the app.config file, containing one or
        /// more hibernate-configuration nodes;
        /// a single hibernate-configuration node in the app.config;
        /// and a hibernate.cfg.xml file in the application directory.
        /// </summary>
        public static void Configure()
        {
            try
            {
                Instance.ConfigureCommon();

                if (!string.IsNullOrEmpty(Instance.ConfigurationFilePath))
                {
                    Configure(Instance.ConfigurationFilePath);
                }
                else
                {
                    // Check for "hibernation" and "nhibernator" for backwards
                    // compatibility (in case the config section type is
                    // updated, but not the name).
                    var hConfig = (HibernationConfiguration)ConfigurationManager.GetSection("hibernation")
                                  ?? (HibernationConfiguration)ConfigurationManager.GetSection("nhibernator");

                    if (hConfig != null)
                    {
                        foreach (var factoryConfig in hConfig.FactoryConfigurations)
                        {
                            List<string> mappedAssemblies;

                            using (var reader = new XmlNodeReader(factoryConfig))
                            {
                                var doc = XDocument.Load(reader);
                                mappedAssemblies = doc.Descendants(XName.Get("mapping", "urn:nhibernate-configuration-2.2"))
                                    .Select(x =>
                                            {
                                                var attribute = x.Attribute("assembly");
                                                return attribute != null ? attribute.Value : null;
                                            })
                                    .ToList();
                            }

                            using (var reader = new XmlNodeReader(factoryConfig))
                            {
                                NHibernate.Cfg.Configuration config = new NHibernate.Cfg.Configuration();
                                config.Configure(reader);

                                AddSessionFactory(config, null, new string[] { }, mappedAssemblies);
                            }
                        }
                    }
                    else
                    {
                        var nhConfig = (HibernateConfiguration)ConfigurationManager.GetSection("hibernate-configuration");
                        if (nhConfig != null)
                        {
                            var config = new NHibernate.Cfg.Configuration();
                            config.Configure();

                            AddSessionFactory(config);
                        }
                    }
                }

                // We have to configure SOMETHING
                if (Instance.SessionFactories == null || Instance.SessionFactories.Count == 0)
                {
                    var config = new NHibernate.Cfg.Configuration();

                    string nhibernateConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultConfigFile);

                    if (File.Exists(nhibernateConfigFile))
                        config.Configure(nhibernateConfigFile);
                    else
                        config.Configure();

                    AddSessionFactory(config);
                }
            }
            catch (Exception ex)
            {
                throw new HibernationException("Unable to configure Hibernator.", ex);
            }
        }

        /// <summary>
        /// Loads Hibernation app settings.
        /// Then attempts to load NHibernate configurations from the input list
        /// of NHibernate configuration files.
        /// </summary>
        /// <param name="configFiles">
        /// A semicolon-separated list of real or relative paths to one or more
        /// valid NHibernate configuration files.
        /// </param>
        public static void Configure(string configFiles)
        {
            if (configFiles == null)
                throw new ArgumentNullException("configFiles");

            Instance.ConfigureCommon();

            foreach (var filePath in configFiles.Split(';'))
            {
                string realPath = 
                    filePath.Trim().StartsWith("~") ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath.Substring(1)) : filePath;

                if (!File.Exists(realPath))
                    continue;

                var configuration = new NHibernate.Cfg.Configuration();

                configuration.Configure(realPath);

                AddSessionFactory(configuration);
            }
        }

        /// <summary>
        /// Loads Hibernation app settings.
        /// Then attempts to load the specified NHibernate configuration.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        public static void Configure(NHibernate.Cfg.Configuration configuration)
        {
            Configure(configuration, string.Empty);
        }

        /// <summary>
        /// Loads Hibernation app settings.
        /// Then attempts to load the specified NHibernate configuration,
        /// using the specified name and aliases for the configured session
        /// factory.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        /// <param name="factoryName">
        /// A unique name for the configured session factory.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to the configured
        /// session factory.
        /// </param>
        public static void Configure(NHibernate.Cfg.Configuration configuration, string factoryName, params string[] factoryAliases)
        {
            Configure(configuration, factoryName, (factoryAliases ?? new string[] { }).ToList());
        }

        /// <summary>
        /// Loads Hibernation app settings.
        /// Then attempts to load the specified NHibernate configuration,
        /// using the specified name and aliases for the configured session
        /// factory.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        /// <param name="factoryName">
        /// A unique name for the configured session factory.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to the configured
        /// session factory.
        /// </param>
        public static void Configure(NHibernate.Cfg.Configuration configuration, string factoryName, IEnumerable<string> factoryAliases)
        {
            Configure(configuration, factoryName, factoryAliases, new string[] { });
        }

        /// <summary>
        /// Loads Hibernation app settings.
        /// Then attempts to load the specified NHibernate configuration,
        /// using the specified name and aliases for the configured session
        /// factory.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        /// <param name="factoryName">
        /// A unique name for the configured session factory.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to the configured
        /// session factory.
        /// </param>
        /// <param name="mappedAssemblies">
        /// A list of Assemblies to include in the session factory.
        /// </param>
        public static void Configure(NHibernate.Cfg.Configuration configuration, string factoryName, IEnumerable<string> factoryAliases, IEnumerable<string> mappedAssemblies)
        {
            Instance.ConfigureCommon();

            AddSessionFactory(configuration, factoryName, factoryAliases, mappedAssemblies);
        }


        /// <summary>
        /// Loads the session factory from the given NHibernate configuration.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        public static void AddSessionFactory(NHibernate.Cfg.Configuration configuration)
        {
            AddSessionFactory(configuration, string.Empty);
        }

        /// <summary>
        /// Loads the session factory from the given NHibernate configuration,
        /// using the specified name.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        /// <param name="factoryName">
        /// A unique name for the configured session factory.
        /// </param>
        public static void AddSessionFactory(NHibernate.Cfg.Configuration configuration, string factoryName)
        {
            AddSessionFactory(configuration, factoryName, new string[] { });
        }

        /// <summary>
        /// Loads the session factory from the given NHibernate configuration,
        /// using the specified name and aliases.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        /// <param name="factoryName">
        /// A unique name for the configured session factory.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to the configured
        /// session factory.
        /// </param>
        public static void AddSessionFactory(NHibernate.Cfg.Configuration configuration, string factoryName, IEnumerable<string> factoryAliases)
        {
            AddSessionFactory(configuration, factoryName, factoryAliases, new string[] { });
        }

        /// <summary>
        /// Loads the session factory from the given NHibernate configuration,
        /// using the specified name and aliases, and including the specified
        /// assemblies.
        /// </summary>
        /// <param name="configuration">
        /// A pre-instantiated NHibernate configuration.
        /// </param>
        /// <param name="factoryName">
        /// A unique name for the configured session factory.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to the configured
        /// session factory.
        /// </param>
        /// <param name="mappedAssemblies">
        /// A list of assemblies to include in the session factory.
        /// </param>
        public static void AddSessionFactory(NHibernate.Cfg.Configuration configuration, string factoryName, IEnumerable<string> factoryAliases, IEnumerable<string> mappedAssemblies)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            if (factoryName == null)
                throw new ArgumentNullException("factoryName");

            try
            {
                // Get or assign the session factory name.
                if (string.IsNullOrEmpty(factoryName))
                {
                    factoryName = 
                        configuration.Properties.ContainsKey("session_factory_name") ? configuration.Properties["session_factory_name"] : string.Empty;
                }

                // Don't do anything if the session factory was already configured.
                if (Instance.SessionFactories.ContainsKey(factoryName))
                    return;

                // Gather up the mapped assemblies
                var assemblies = new List<Assembly>();

                foreach (var mappedClass in configuration.ClassMappings)
                {
                    if (!assemblies.Contains(mappedClass.MappedClass.Assembly))
                    {
                        assemblies.Add(mappedClass.MappedClass.Assembly);
                    }
                }

                if (mappedAssemblies != null)
                {
                    foreach (var mappedAssembly in mappedAssemblies)
                    {
                        var configuredAssembly = Assembly.Load(mappedAssembly);

                        if (!assemblies.Contains(configuredAssembly))
                        {
                            assemblies.Add(configuredAssembly);
                        }
                    }
                }

                // Assign a handler for custom HQL generators.
                CustomLinqToHqlGeneratorFactory.Instance.Clear();

                foreach (var registeredAssembly in assemblies)
                {
                    CustomLinqToHqlGeneratorFactory.Instance.AddGeneratorsFromAssembly(registeredAssembly);
                }

                configuration.LinqToHqlGeneratorsRegistry<HibernationLinqToHqlRegistry>();

                // Build the session factory.
                var sessionFactory = configuration.BuildSessionFactory();

                AddSessionFactory(sessionFactory, factoryName, factoryAliases);
            }
            catch (Exception ex)
            {
                throw new HibernationException("Unable to add SessionFactory.", ex);
            }
        }


        /// <summary>
        /// Adds a session factory to Hibernator's internal storage, using the
        /// given name.
        /// </summary>
        /// <param name="sessionFactory">
        /// A fully-built <c>ISessionFactory</c>.
        /// </param>
        /// <param name="name">
        /// A unique name for the session factory.
        /// </param>
        public static void AddSessionFactory(ISessionFactory sessionFactory, string name)
        {
            AddSessionFactory(sessionFactory, name, new string[] { });
        }

        /// <summary>
        /// Adds a session factory to Hibernator's internal storage, using the
        /// given name and aliases.
        /// </summary>
        /// <param name="sessionFactory">
        /// A fully-built <c>ISessionFactory</c>.
        /// </param>
        /// <param name="name">
        /// A unique name for the session factory.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to this
        /// session factory.
        /// </param>
        public static void AddSessionFactory(ISessionFactory sessionFactory, string name, IEnumerable<string> factoryAliases)
        {
            if (sessionFactory == null)
                throw new ArgumentNullException("sessionFactory");
            if (name == null)
                throw new ArgumentNullException("name");

            if (Instance.SessionFactories.ContainsKey(name))
                return;

            if (factoryAliases != null)
                AddFactoryAliases(name, factoryAliases);

            Instance.SessionFactories.Add(name, sessionFactory);
        }

        #endregion

        #region Aliases

        /// <summary>
        /// Sets Hibernator's factory/alias map to the values supplied in the
        /// mapping string.
        /// </summary>
        /// <remarks>
        /// This method clears the current factory/alias map and creates a new
        /// one from the mapping string.  To add new mappings to the current
        /// map, use <see><cref>AddFactoryAliases</cref></see>.
        /// </remarks>
        /// <param name="factoryAliases">
        /// A semicolon-separated string defining factory name aliases in the
        /// format [alias]:[factory name];...
        /// </param>
        public static void SetFactoryAliases(string factoryAliases)
        {
            Instance.FactoryAliasMap = new Dictionary<string, string>();
            AddFactoryAliases(factoryAliases);
        }

        /// <summary>
        /// Sets Hibernator's factory/alias map to the supplied dictionary.
        /// </summary>
        /// <param name="factoryAliases">
        /// A dictionary mapping alias keys to factory name values.
        /// </param>
        public static void SetFactoryAliases(IDictionary<string, string> factoryAliases)
        {
            Instance.FactoryAliasMap = new Dictionary<string, string>();
            AddFactoryAliases(factoryAliases);
        }

        /// <summary>
        /// Sets Hibernator's factory/alias map to the supplied aliases.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory name being aliased.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to this
        /// session factory.
        /// </param>
        public static void SetFactoryAliases(string factoryName, params string[] factoryAliases)
        {
            SetFactoryAliases(factoryName, factoryAliases.ToList());
        }

        /// <summary>
        /// Sets Hibernator's factory/alias map to the supplied aliases.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory name being aliased.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to this
        /// session factory.
        /// </param>
        public static void SetFactoryAliases(string factoryName, IEnumerable<string> factoryAliases)
        {
            Instance.FactoryAliasMap = new Dictionary<string, string>();
            AddFactoryAliases(factoryName, factoryAliases);
        }


        /// <summary>
        /// Adds the given factory/alias mappings to Hibernator's factory/alias
        /// map.
        /// </summary>
        /// <param name="factoryAliases">
        /// A semicolon-separated string defining factory name aliases in the
        /// format [alias]:[factory name];...
        /// </param>
        public static void AddFactoryAliases(string factoryAliases)
        {
            if (factoryAliases == null)
                throw new ArgumentNullException("factoryAliases");

            foreach (var aliasPair in factoryAliases.Split(';'))
            {
                var aliasMap = aliasPair.Split(':');

                if (aliasMap.Length != 2)
                    continue;

                foreach (var alias in aliasMap[0].Split(','))
                {
                    AddAlias(alias, aliasMap[1]);
                }
            }
        }

        /// <summary>
        /// Adds the given factory/alias mappings to Hibernator's factory/alias
        /// map.
        /// </summary>
        /// <param name="factoryAliases">
        /// A dictionary mapping alias keys to factory name values.
        /// </param>
        public static void AddFactoryAliases(IDictionary<string, string> factoryAliases)
        {
            if (factoryAliases == null)
                throw new ArgumentNullException("factoryAliases");

            foreach (var alias in factoryAliases)
            {
                AddAlias(alias.Key, alias.Value);
            }
        }

        /// <summary>
        /// Adds the given factory/alias mappings to Hibernator's factory/alias
        /// map.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory name being aliased.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to this
        /// session factory.
        /// </param>
        public static void AddFactoryAliases(string factoryName, params string[] factoryAliases)
        {
            if (factoryAliases == null)
                throw new ArgumentNullException("factoryAliases");

            AddFactoryAliases(factoryName, factoryAliases.ToList());
        }

        /// <summary>
        /// Adds the given factory/alias mappings to Hibernator's factory/alias
        /// map.
        /// </summary>
        /// <param name="factoryName">
        /// The session factory name being aliased.
        /// </param>
        /// <param name="factoryAliases">
        /// Alternate names and aliases that will also refer to this
        /// session factory.
        /// </param>
        public static void AddFactoryAliases(string factoryName, IEnumerable<string> factoryAliases)
        {
            if (factoryAliases == null)
                throw new ArgumentNullException("factoryAliases");

            foreach (var alias in factoryAliases)
            {
                AddAlias(alias, factoryName);
            }
        }


        /// <summary>
        /// Adds a session factory name alias to the alias map.
        /// </summary>
        /// <param name="alias">
        /// An alternate name for the session factory.
        /// </param>
        /// <param name="factory">
        /// The session factory name being aliased.
        /// </param>
        public static void AddAlias(string alias, string factory)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");
            if (factory == null)
                throw new ArgumentNullException("factory");

            if (!Instance.FactoryAliasMap.ContainsKey(alias))
                Instance.FactoryAliasMap.Add(alias, factory);
        }


        /// <summary>
        /// Retrieve the actual name of a session factory given one of its 
        /// aliases.
        /// </summary>
        /// <param name="alias">
        /// A session factory name or alias.
        /// </param>
        /// <returns>
        /// The actual unique session factory name referenced by the alias.
        /// </returns>
        public static string GetFactoryName(string alias)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (Instance.FactoryAliasMap.ContainsKey(alias))
                return Instance.FactoryAliasMap[alias];

            if (Instance.SessionFactories.ContainsKey(alias))
                return alias.ToUpper();

            return string.Empty;
        }

        #endregion

        #region Singleton & Constructor

        private Hibernator()
        {
            RequireHttpModule = false;
            HttpModuleDefined = false;
            IsConfigured = false;

            InitCollections();

            InitSessionStorage();
        }

        private void InitCollections()
        {
            FactoryAliasMap = new Dictionary<string, string>();
            SessionFactories = new SessionFactoryCollection();
        }

        private void InitSessionStorage()
        {
            // If we're running an HTTP application, require the HTTP module
            // and use HTTP-optimized session factory storage

            try
            {
                if (HttpRuntime.AppDomainAppId != null)
                {
                    RequireHttpModule = true;
                    SessionStorage = new HttpSessionCollection();
                }
                else
                {
                    SessionStorage = new ThreadSessionCollection();
                }
            }
            catch (Exception ex)
            {
                throw new HibernateException("Unable to initialize internal session storage.", ex);
            }
        }

        private static volatile Hibernator instance;
        private static readonly object syncRoot = new object();

        private static Hibernator Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Hibernator();
                    }
                }

                return instance;
            }
        }

        #endregion
    }
}
