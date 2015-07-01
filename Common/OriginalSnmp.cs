using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using TypeTest.Model;
using Summer.System.Core;
using Summer.System.Log;

namespace TypeTest.Common
{
    public class OriginalSnmp : Original
    {
        public int Data { get; set; }

        public OriginalSnmp()
        {
        }

        /// <summary>
        /// 根据IP地址来源来判断来自于那个机笼
        /// </summary>
        /// <returns></returns>
        public int GetRackNo()
        {
            try
            {
                TestedRacks testedRacks = SpringHelper.GetObject<TestedRacks>("testedRacks");
                foreach (var r in testedRacks.Racks)
                {
                    if (r.IP.Equals(RemoteIpEndPoint.Address.ToString()))
                    {
                        return r.No;
                    }
                }
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<OriginalSnmp>().Error(ee.InnerException.Message);
                LogHelper.GetLogger<OriginalSnmp>().Error(ee.InnerException.StackTrace);
            }
            return -1;
        }

        public byte GetCode()
        {
            string sepecific = Data.ToString();
            return Byte.Parse(sepecific.Substring(1, 3));
        }

        public int GetSlotNo()
        {
            string sepecific = Data.ToString();
            return Convert.ToInt16(sepecific.Substring(4, 3)); ;
        }
    }
}
