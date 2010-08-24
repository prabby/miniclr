using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Threading;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;

using Task   = Microsoft.Build.Utilities.Task;

namespace Microsoft.SPOT.Tasks
{
    public class ResolveRuntimeDependencies : Task
    {
        public interface IResolveRuntimeDependenciesHost
        {
            string Assembly {set;}
            string StartProgram {set;}
            string[] AssemblyReferences {set;}
            string[] StartProgramReferences {set;}            
        }

        private string m_assembly;
        private string m_startProgram;
        private ITaskItem[] m_assemblyReferences;
        private ITaskItem[] m_startProgramReferences;

        public string Assembly
        {
            get {return m_assembly;}
            set {m_assembly = value;}
        }

        public string StartProgram
        {
            get {return m_startProgram;}
            set {m_startProgram = value;}
        }

        public  ITaskItem[] AssemblyReferences
        {
            get {return m_assemblyReferences;}
            set {m_assemblyReferences = value;}
        }

        public ITaskItem[] StartProgramReferences
        {
            get {return m_startProgramReferences;}
            set {m_startProgramReferences = value;}
        }

        public override bool Execute()
        {
            IResolveRuntimeDependenciesHost host = this.HostObject as IResolveRuntimeDependenciesHost;

            if (host != null)
            {
                host.Assembly = this.Assembly;
                host.StartProgram = this.StartProgram;
                host.AssemblyReferences = this.GetFullPathFromItems(this.AssemblyReferences);
                host.StartProgramReferences = this.GetFullPathFromItems(this.StartProgramReferences);
            }

            return true;
        }

        private string[] GetFullPathFromItems(ITaskItem[] items)
        {
            if (items == null)
                return new string[0];

            string[] ret = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                ret[i] = items[i].GetMetadata("FullPath");
            }

            return ret;
        }
    }
}
