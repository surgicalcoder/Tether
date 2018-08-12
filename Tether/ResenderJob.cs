using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Newtonsoft.Json;
using NLog;
using Quartz;

namespace Tether
{
    [PersistJobDataAfterExecution]
    [DisallowConcurrentExecution]
    public class ResenderJob : IJob
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public void Execute(IJobExecutionContext context)
        {
            var retransmitRootPath = Path.Combine(basePath, "_retransmit");

            if (!Directory.Exists(retransmitRootPath))
            {
                return;
            }


            foreach (var file in Directory.GetFiles(retransmitRootPath, "*.json", SearchOption.AllDirectories))
            {
                var values = File.ReadAllText(file);

                var transmitValues = PayloadPoster.TransmitValues(values, bypassSave:true);

                if (transmitValues)
                {
                    File.Delete(file);
                }
                else
                {
                    var partOfPath = file.Replace(retransmitRootPath, "");

                    if (partOfPath.StartsWith("\\"))
                    {
                        partOfPath = partOfPath.Substring(1);
                    }

                    string[] splitPath = partOfPath.Split('\\');

                    int Number = Convert.ToInt32(splitPath[0]);

                    if (Number == Config.ConfigurationSingleton.Instance.Config.RetriesCount-1)
                    {
                        File.Move(file, Path.Combine(retransmitRootPath, (Number).ToString(), Path.GetFileNameWithoutExtension(file) +".failed"));
                    }
                    else
                    {
                        File.Move(file, Path.Combine(retransmitRootPath, (Number + 1).ToString(), Path.GetFileName(file)));
                    }
                }
                

            }
        }
    }
}