using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Summer.System.Log;
using Spring.Scheduling.Quartz;
using Quartz;
using TypeTest.Common;
using TypeTest.Protocol;

namespace TypeTest.Model
{
    [Serializable]
    public class ErrorCodeMsgFile
    {
        static StreamWriter sw;
        static object lockFile = new object();
        ErrorDescription errorDescription;

        public void Open(string key)
        {
            try
            {
                lock (lockFile)
                {
                    sw = new StreamWriter(Util.GetBasePath() + "\\Report\\Data\\" + key + ".log");
                }
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<ErrorCodeMsgFile>().Error(ee.Message);
                LogHelper.GetLogger<ErrorCodeMsgFile>().Error(ee.StackTrace);
            }
        }

        public void Close()
        {
            try
            {
                lock (lockFile)
                {
                    if (sw != null)
                    {
                        sw.Close();
                        sw = null;
                    }
                }
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<ErrorCodeMsgFile>().Error(ee.Message);
                LogHelper.GetLogger<ErrorCodeMsgFile>().Error(ee.StackTrace);
            }
        }

        public void Append(BaseMessage msg,string rackName,string boardName)
        {
            try
            {
                lock (lockFile)
                {
                    if (sw != null)
                    {
                        sw.WriteLine("{0},{1:D2},{2},{3:D2},{4},0x{5:X2},{6}",
                            Util.FormateDateTime3(msg.DtTime),
                            msg.RackNo,
                            rackName,
                            msg.SlotNo,
                            boardName,
                            msg.Code,
                            errorDescription.GetDescription(msg.Code)
                            );
                    }
                }
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<ErrorCodeMsgFile>().Error(ee.Message);
                LogHelper.GetLogger<ErrorCodeMsgFile>().Error(ee.StackTrace);
            }
        }

        public void Flush()
        {
            LogHelper.GetLogger("job").Debug("Flush Job Start.");
            lock (lockFile)
            {
                if (sw != null)
                {
                    sw.Flush();
                }
            }
            LogHelper.GetLogger("job").Debug("Flush Job Finish.");
        }
    }
}
