using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileWatcher.Configuration
{
    internal interface IRunnable
    {
        void Run(ChangeInfo change, TriggerType trigger);
    }
}
