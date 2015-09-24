using System;
using System.Collections.Generic;
using System.Management;
using Tether.Plugins;

namespace Tether.CoreChecks
{
    public class SystemStatsCheck : ICheck
    {
        #region ICheck Members

        public string Key
        {
            get { return "systemStats"; }
        }

        public object DoCheck()
        {
            try
            {
                using (var query = new ManagementObjectSearcher("SELECT Name, NumberOfCores FROM Win32_Processor"))
                {
                    Dictionary<string, object> results = new Dictionary<string, object>();
                    foreach (ManagementObject obj in query.Get())
                    {
                        results.Add("winV", Environment.OSVersion.VersionString);
                        results.Add("netV", Environment.Version.ToString());
                        results.Add("netA", LookupNetVersion());
                        results.Add("platform", Environment.OSVersion.Platform.ToString());
                        results.Add("cpuCores", obj.GetPropertyValue("NumberOfCores"));
                        results.Add("processor", obj.GetPropertyValue("Name"));
                        results.Add("machine", Machine());
                        results.Add("pythonV", string.Empty);
                        return results;
                    }
                    return results;
                }
            }
            catch
            {
                // NumberOfCores is not supported on Windows 2003.
                using (var query = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    Dictionary<string, object> results = new Dictionary<string, object>();
                    foreach (ManagementObject obj in query.Get())
                    {
                        results.Add("winV", Environment.OSVersion.VersionString);
                        results.Add("netV", Environment.Version.ToString());
                        results.Add("netA", LookupNetVersion());
                        results.Add("platform", Environment.OSVersion.Platform.ToString());
                        //results.Add("cpuCores", obj.GetPropertyValue("NumberOfCores"));
                        results.Add("processor", obj.GetPropertyValue("Name"));
                        results.Add("machine", Machine());
                        results.Add("pythonV", string.Empty);
                        return results;
                    }
                    return results;
                }
            }
        }

        #endregion

        private string Machine()
        {
            using (var query = new ManagementObjectSearcher("SELECT SystemType FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in query.Get())
                {
                    using (obj)
                    {
                        return (string)obj.GetPropertyValue("SystemType");
                    }
                }
                return string.Empty;
            }
        }

        private string _netVersion = "init";

        private string LookupNetVersion()
        {
            string versions = "unable to query";
            if (_netVersion != "init")
            {
                versions = _netVersion;
            }
            else
            {
                try
                {
                    Microsoft.Win32.RegistryKey key;
                    key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\5.0\User Agent\Post Platform");
                    string[] keys = key.GetValueNames();
                    key.Close();
                    versions = "";
                    foreach (string k in keys)
                    {
                        if (k.ToLower().StartsWith(".net"))
                        {
                            versions += k + ",";
                        }
                    }
                    versions = versions.TrimEnd(new char[] { ',' });

                }
                catch (Exception)
                {
                    // do nothing
                }
            }
            return versions;
        }
    }
}