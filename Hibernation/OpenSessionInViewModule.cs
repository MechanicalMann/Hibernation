using System;
using System.Web;

namespace Hibernation
{
    /// <summary>
    /// An HTTP module that performs per-request NHibernate session management
    /// in conjunction with the Hibernator session manager.
    /// </summary>
    public class OpenSessionInViewModule : IHttpModule
    {
        #region IHttpModule Members

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <summary>
        /// Set our request handlers and inform Hibernator that we're running a
        /// web app.
        /// </summary>
        /// <param name="context"></param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += BeginRequestHandler;
            context.EndRequest += EndRequestHandler;
            Hibernator.SetHttpModuleDefined(true);
        }

        #endregion

        private void BeginRequestHandler(object sender, EventArgs e)
        {
            Hibernator.SetSessionMode(SessionStartMode.HttpRequest);
        }

        private void EndRequestHandler(object sender, EventArgs e)
        {
            Hibernator.CloseAllSessions();
        }
    }
}
