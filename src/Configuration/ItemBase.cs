using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileWatcher.Configuration
{
    public abstract class ItemBase
    {
        /// <summary>
        /// Gets or sets the change information.
        /// </summary>
        protected static ChangeInfo Change { get; set; }
    }
}
