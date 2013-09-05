using NHibernate.Linq.Functions;

namespace Hibernation.CustomHqlGeneration
{
    /// <summary>
    /// The default Hibernation Linq-to-HQL registry class.
    /// </summary>
    public class HibernationLinqToHqlRegistry : DefaultLinqToHqlGeneratorsRegistry
    {
        /// <summary>
        /// Initializes a new instance of the HibernationLinqToHqlRegistry class.
        /// </summary>
        public HibernationLinqToHqlRegistry()
        {
            var factory = CustomLinqToHqlGeneratorFactory.Instance;

            foreach (var methodGenerator in factory.MethodGenerators)
            {
                foreach (var signature in methodGenerator.SupportedMethods)
                {
                    RegisterGenerator(signature, methodGenerator);
                }
            }

            foreach (var propertyGenerator in factory.PropertyGenerators)
            {
                foreach (var signature in propertyGenerator.SupportedProperties)
                {
                    RegisterGenerator(signature, propertyGenerator);
                }
            }
        }
    }
}
