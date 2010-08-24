using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using System.Reflection;
using System.Threading;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Tasks;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Configuration;
using System.Security;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace Microsoft.SPOT.Tasks
{
    public class CreateInteropFeatureProj : Task
    {
        private string m_stubsPath;
        [Required]
        public string StubsPath
        {
            set { m_stubsPath = value; }
            get { return m_stubsPath; }
        }

        private string m_name;
        [Required]
        public string Name
        {
            set { m_name = value; }
            get { return m_name; }
        }

        private string m_assemblyName;
        [Required]
        public string AssemblyName
        {
            set { m_assemblyName = Path.GetFileName(value); }
            get { return m_assemblyName; }
        }

        private string m_nativeProjectFile;
        [Required]
        public string NativeProjectFile
        {
            set { m_nativeProjectFile = value; }
            get { return m_nativeProjectFile; }
        }

        private string m_managedProjectFile;
        [Required]
        public string ManagedProjectFile
        {
            set { m_managedProjectFile = value; }
            get { return m_managedProjectFile; }
        }

        public override bool Execute()
        {
            bool result = true;

            string file = Path.Combine(m_stubsPath, m_name + ".featureproj");

            try
            {
                Project proj = new Project();
                proj.DefaultToolsVersion = "3.5";
                proj.DefaultTargets = "Build";

                BuildPropertyGroup bpg = proj.AddNewPropertyGroup(true);
                bpg.AddNewProperty("FeatureName", m_name);
                bpg.AddNewProperty("Guid", System.Guid.NewGuid().ToString("B"));
                bpg.AddNewProperty("Description", "");
                bpg.AddNewProperty("Groups", "");

                BuildItemGroup big = proj.AddNewItemGroup();
                big.AddNewItem("InteropFeature", Path.GetFileNameWithoutExtension(m_assemblyName).Replace('.', '_'));
                big.AddNewItem("DriverLibs", Path.GetFileNameWithoutExtension( m_assemblyName ).Replace( '.', '_' ) + ".$(LIB_EXT)");
                big.AddNewItem("MMP_DAT_CreateDatabase", "$(BUILD_TREE_CLIENT)\\pe\\" + m_assemblyName);
                big.AddNewItem("RequiredProjects", Path.Combine(m_stubsPath, m_nativeProjectFile));

                proj.Save(file);
            }
            catch(Exception e)
            {
                Log.LogError("Error trying to create feature project file \"" + file + "\": " + e.Message);
                result = false;
            }

            return result;
        }
    }
}
