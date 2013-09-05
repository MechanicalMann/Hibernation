using System;
using System.Data;
using NHibernate;

namespace Hibernation
{
    /// <summary>
    /// Wraps an NHibernate ITransaction in a disposable object, providing
    /// helper methods for transaction initiation and access through
    /// Hibernator and automatic rollback on exceptions and disposal.
    /// </summary>
    public class HibernationTransaction : IDisposable
    {
        private string SessionFactoryName { get; set; }

        /// <summary>
        /// A reference to the underlying transaction.
        /// </summary>
        public ITransaction Transaction { get; protected set; }

        /// <summary>
        /// A reference to the session that is hosting the current transaction.
        /// </summary>
        public ISession Session { get; protected set; }

        /// <summary>
        /// Begins a new transaction on Hibernator's default session factory.
        /// </summary>
        public HibernationTransaction()
            : this(string.Empty, null)
        {
        }

        /// <summary>
        /// Begins a new transaction on the named Hibernator session factory.
        /// </summary>
        /// <param name="factoryName"></param>
        public HibernationTransaction(string factoryName)
            : this(factoryName, null)
        {
        }

        /// <summary>
        /// Begins a new transaction on the named Hibernator session factory,
        /// using the requested isolation level.
        /// </summary>
        /// <param name="factoryName"></param>
        /// <param name="isolationLevel"></param>
        public HibernationTransaction(string factoryName, IsolationLevel? isolationLevel)
        {
            this.SessionFactoryName = factoryName;
            this.Session = Hibernator.GetSession(factoryName);
            this.Transaction = Hibernator.BeginTransaction(factoryName, isolationLevel);
        }

        internal HibernationTransaction(string factoryName, ISession session, ITransaction transaction)
        {
            this.SessionFactoryName = factoryName;
            this.Session = session;
            this.Transaction = transaction;
        }

        /// <summary>
        /// Commit the transaction.
        /// </summary>
        /// <remarks>
        /// If an exception is thrown during transaction commit, a rollback is
        /// automatically attempted before the exception bubbles up further.
        /// </remarks>
        public void Commit()
        {
            try
            {
                Hibernator.CommitTransaction(SessionFactoryName);
            }
            catch (Exception ex)
            {
                Hibernator.RollbackTransaction(SessionFactoryName);
                throw new HibernationException("An exception occurred while attempting to commit the transaction.  Transaction has been rolled back.", ex);
            }
        }

        /// <summary>
        /// Roll back the transaction.
        /// </summary>
        public void Rollback()
        {
            Hibernator.RollbackTransaction(SessionFactoryName);
        }

        #region IDisposable Members

        /// <summary>
        /// Rolls back an uncommitted transaction when the object is disposed.
        /// </summary>
        public void Dispose()
        {
            if (Hibernator.HasOpenSession(SessionFactoryName))
            {
                if ((!Transaction.WasCommitted) && (!Transaction.WasRolledBack))
                {
                    Hibernator.RollbackTransaction(SessionFactoryName);
                }
            }
        }

        #endregion
    }
}
