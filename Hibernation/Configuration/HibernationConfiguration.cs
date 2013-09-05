using System.Collections.Generic;
using System.Xml;

namespace Hibernation.Configuration
{
    /// <summary>
    /// Configuration for the Hibernation library.
    /// </summary>
    public class HibernationConfiguration
    {
        /// <summary>
        /// A list containing all session factory configurations defined in the
        /// Hibernation configuration section.
        /// </summary>
        public List<XmlNode> FactoryConfigurations { get; set; }

        /// <summary>
        /// Initializes an instance of the HibernationConfiguration class.
        /// </summary>
        public HibernationConfiguration()
        {
            this.FactoryConfigurations = new List<XmlNode>();
        }
    }
}
