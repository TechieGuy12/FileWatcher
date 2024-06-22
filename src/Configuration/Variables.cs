using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains information about all variables.
    /// </summary>
    [XmlRoot("variables")]
    public class Variables
    {
        /// <summary>
        /// Gets or sets the list of variables.
        /// </summary>
        [XmlElement("variable")]
        public Collection<Variable>? VariableList { get; set; }

        /// <summary>
        /// Gets the flag indicating that all variables have been set. This value
        /// is set by the <see cref="Add(ConcurrentDictionary{string, string}?)"/>
        /// method once all parent and object variables have been added to the
        /// <see cref="AllVariables"/> dictionary.
        /// </summary>
        [XmlIgnore]
        public bool VariablesSet { get; private set; }

        /// <summary>
        /// Gets the dicitonary of all variables that have been added. To add
        /// additional variables outside of the configuration file, use the
        /// <see cref="Add(ConcurrentDictionary{string, string}?)"/> method.
        /// <para>This property will be <c>null</c> unless the
        /// <see cref="Add(ConcurrentDictionary{string, string}?)"/> method
        /// is called.</para>
        /// </summary>
        [XmlIgnore]
        public ConcurrentDictionary<string, string>? AllVariables { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="HasVariablesBase"/> class.
        /// </summary>
        public Variables()
        {
            VariablesSet = false;
        }

        /// <summary>
        /// Adds all variables, the ones part of the <see cref="VariableList"/>
        /// collection and the variables passed in as an argument to a single
        /// <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// <para>A <see cref="ConcurrentDictionary{TKey, TValue}"/> is used for
        /// quick access to a variable and to ensure all variables names are
        /// unique.</para>
        /// </summary>
        /// <param name="variables">
        /// The variables to add to the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </param>
        public void Add(ConcurrentDictionary<string, string>? variables)
        {
            if (VariablesSet)
            {
                return;
            }

            AllVariables ??= new ConcurrentDictionary<string, string>();

            if (VariableList != null)
            {
                // Add the variables for the current object
                foreach (Variable variable in VariableList)
                {
                    if (!AllVariables.TryAdd(variable.Name, variable.Value))
                    {
                        Logger.WriteLine($"The variable '{variable.Name}' already exists.");
                    }
                }
            }

            if (variables != null)
            {
                // Add the variables passed into the method
                foreach (KeyValuePair<string, string> variable in variables)
                {
                    if (!AllVariables.TryAdd(variable.Key, variable.Value))
                    {
                        Logger.WriteLine($"The variable '{variable.Key}' already exists.");
                    }
                }
            }

            VariablesSet = true;
        }
    }
}
