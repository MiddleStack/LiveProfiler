using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.PerfCounters
{
#pragma warning disable 1591
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        /// <summary>
        ///     Initializes a new instance of <see cref="Installer"/>.
        /// </summary>
        public Installer()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            new LiveProfilerPerfCounters().Install();
        }

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            new LiveProfilerPerfCounters().Uninstall();
        }
    }
#pragma warning restore 1591
}
