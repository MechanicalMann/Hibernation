using System.Configuration;
using System.Xml;

namespace Hibernation.Configuration
{
    /// <summary>
    /// Handles configuration for the Hibernation library.
    /// </summary>
    public class HibernationConfigurationHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Creates an instance of HibernationConfiguration containing any
        /// session factories configured in the application configuration.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configContext"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            var hibernationConf = new HibernationConfiguration();
            if (section == null)
                return hibernationConf;

            foreach (XmlNode factoryNode in section.ChildNodes)
            {
                hibernationConf.FactoryConfigurations.Add(factoryNode);
            }

            return hibernationConf;
        }
    }
}
