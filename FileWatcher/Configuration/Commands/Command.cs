using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Commands
{
    /// <summary>
    /// A command to run for a change.
    /// </summary>
    public class Command : RunnableBase
    {
        private Process process;

        /// <summary>
        /// Gets or sets the arguments associated with the file to execute.
        /// </summary>
        [XmlElement("arguments")]
        public string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the full path to the file to executed.
        /// </summary>
        [XmlElement("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the triggers of the action.
        /// </summary>
        [XmlElement("triggers")]
        public Triggers Triggers { get; set; } = new Triggers();

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the command.
        /// </param>
        public override void Run(string watchPath, string fullPath, TriggerType trigger)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            if (Triggers == null || Triggers.TriggerList == null)
            {
                return;
            }

            if (Triggers.TriggerList.Count <= 0 || !Triggers.Current.HasFlag(trigger))
            {
                return;
            }

            string commandPath = GetCommand(watchPath, fullPath);
            string arguments = GetArguments(watchPath, fullPath);

            if (!File.Exists(commandPath))
            {
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = commandPath;
                startInfo.Arguments = arguments;

                process = new Process();
                process.StartInfo = startInfo;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.EnableRaisingEvents = true;
                process.Exited += OnProcessExit;
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Could not run the command '{commandPath} {arguments}'. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the arguments value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The command path string value.
        /// </returns>
        private string GetArguments(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Arguments))
            {
                return null;
            }

            return ReplacePlaceholders(Arguments, watchPath, fullPath);
        }

        /// <summary>
        /// Gets the command path value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The command path string value.
        /// </returns>
        private string GetCommand(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                return null;
            }

            return ReplacePlaceholders(Path, watchPath, fullPath);
        }

        /// <summary>
        /// The event that is raised when the process has exied.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The event arguments.
        /// </param>
        private void OnProcessExit(object sender, EventArgs args)
        {
            if (process == null)
            {
                return;
            }

            Logger.WriteLine($"The execution {process.StartInfo.FileName} {process.StartInfo.Arguments} has exited. Exit code: {process.ExitCode}.");
        }
    }
}
