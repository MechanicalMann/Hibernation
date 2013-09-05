using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NHibernate.Linq.Functions;

namespace Hibernation.CustomHqlGeneration
{
    /// <summary>
    /// A singleton factory that configures and instantiates any custom HQL
    /// generators defined in an assembly.
    /// </summary>
    public class CustomLinqToHqlGeneratorFactory
    {
        /// <summary>
        /// A list containing all custom HQL generators for methods.
        /// </summary>
        public List<IHqlGeneratorForMethod> MethodGenerators { get; protected set; }
        /// <summary>
        /// A list containing configured method signatures.
        /// </summary>
        protected List<MethodInfo> MethodSignatures { get; set; }

        /// <summary>
        /// A list containing all custom HQL generators for properties.
        /// </summary>
        public List<IHqlGeneratorForProperty> PropertyGenerators { get; protected set; }
        /// <summary>
        /// A list containing configured property signatures.
        /// </summary>
        protected List<PropertyInfo> PropertySignatures { get; set; }

        private CustomLinqToHqlGeneratorFactory()
        {
            Clear();
        }

        /// <summary>
        /// Removes all previously configured generators and signatures.
        /// </summary>
        public void Clear()
        {
            MethodGenerators = new List<IHqlGeneratorForMethod>();
            MethodSignatures = new List<MethodInfo>();
            PropertyGenerators = new List<IHqlGeneratorForProperty>();
            PropertySignatures = new List<PropertyInfo>();
        }

        /// <summary>
        /// Finds and instantiates all custom HQL generators defined in a given
        /// Assembly.
        /// </summary>
        /// <param name="targetAssembly">
        /// The Assembly that will be searched for any custom HQL generators.
        /// </param>
        public void AddGeneratorsFromAssembly(Assembly targetAssembly)
        {
            try
            {
                Type[] allTypes = targetAssembly.GetTypes();

                // Get all custom HQL generators defined in the target assembly
                var generatorTypes = allTypes.Where(t =>
                    (typeof(IHqlGeneratorForMethod).IsAssignableFrom(t) || typeof(IHqlGeneratorForProperty).IsAssignableFrom(t))
                    && !t.IsAbstract
                    && !t.IsInterface);

                foreach (var generatorType in generatorTypes)
                {
                    var generator = Activator.CreateInstance(generatorType);

                    var methodGeneratorInstance = generator as BaseHqlGeneratorForMethod;
                    if (methodGeneratorInstance != null)
                        AddMethodGenerator(methodGeneratorInstance);

                    var propertyGeneratorInstance = generator as BaseHqlGeneratorForProperty;
                    if (propertyGeneratorInstance != null)
                        PropertyGenerators.Add(propertyGeneratorInstance);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var sb = new StringBuilder();
                foreach (var loadEx in ex.LoaderExceptions)
                {
                    sb.AppendLine(loadEx.Message);
                    sb.AppendLine(loadEx.ToString());
                }

                throw new HibernationException(string.Format("Error loading types for {0}: {1}", targetAssembly.FullName, sb), ex);
            }
        }

        /// <summary>
        /// Adds an initialized method generator to the factory.
        /// </summary>
        /// <param name="methodGeneratorInstance"></param>
        protected void AddMethodGenerator(BaseHqlGeneratorForMethod methodGeneratorInstance)
        {
            MethodGenerators.Add(methodGeneratorInstance);
            foreach (MethodInfo supportedSignature in methodGeneratorInstance.SupportedMethods)
            {
                if (MethodSignatures.Contains(supportedSignature))
                    throw new Exception(String.Format("Method signature {0} loaded by method hql generator {1} has already been added", supportedSignature, methodGeneratorInstance));

                MethodSignatures.Add(supportedSignature);
            }
        }

        /// <summary>
        /// Adds an initialized property generator to the factory.
        /// </summary>
        /// <param name="propertyGeneratorInstance"></param>
        protected void AddPropertyGenerator(BaseHqlGeneratorForProperty propertyGeneratorInstance)
        {
            PropertyGenerators.Add(propertyGeneratorInstance);
            foreach (PropertyInfo supportedSignature in propertyGeneratorInstance.SupportedProperties)
            {
                if (PropertySignatures.Contains(supportedSignature))
                    throw new Exception(String.Format("Property signature {0} loaded by property hql generator {1} has already been added", supportedSignature, propertyGeneratorInstance));

                PropertySignatures.Add(supportedSignature);
            }
        }


        #region Singleton Implementation

        private static volatile CustomLinqToHqlGeneratorFactory instance;
        private static object syncRoot = new Object();

        /// <summary>
        /// The singleton instance of this factory.
        /// </summary>
        public static CustomLinqToHqlGeneratorFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new CustomLinqToHqlGeneratorFactory();
                    }
                }

                return instance;
            }
        }

        #endregion
    }
}
