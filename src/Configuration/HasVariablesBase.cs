using System.Collections.Concurrent;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    public abstract class HasVariablesBase
    {
        // A dictionary of variables
        // The dictionary is used so the variable name is the key to make sure
        // variable names are unique
        protected ConcurrentDictionary<string, string>? _variables;

        // Flag indicating that the variables have been set
        private bool _variablesSet;

        /// <summary>
        /// Gets or sets the variables.
        /// </summary>
        [XmlElement("variables")]
        public Variables? Variables { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="HasVariablesBase"/> class.
        /// </summary>
        public HasVariablesBase()
        {
            _variablesSet = false;
        }

        /// <summary>
        /// Add a list of variables to the current list of variables.
        /// </summary>
        /// <param name="variables">
        /// The variables to add to the list.
        /// </param>
        public void AddVariables(Variables? variables)
        {
            if (_variablesSet)
            {
                return;
            }

            _variables ??= new ConcurrentDictionary<string, string>();

            if (Variables != null && Variables.VariableList != null)
            {
                // Add the variables for the current object
                foreach (Variable variable in Variables.VariableList)
                {
                    if (_variables.ContainsKey(variable.Name))
                    {
                        continue;
                    }

                    _variables.TryAdd(variable.Name, variable.Value);
                }
            }

            if (variables != null && variables.VariableList != null)
            {
                // Add the variables passed into the method
                foreach (Variable variable in variables.VariableList)
                {
                    if (_variables.ContainsKey(variable.Name))
                    {
                        Logger.WriteLine($"The variable '{variable.Name}' already exists.");
                        continue;
                    }

                    _variables.TryAdd(variable.Name, variable.Value);
                }
            }

            _variablesSet = true;
        }
    }
}
