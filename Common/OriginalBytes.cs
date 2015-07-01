using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace TypeTest.Common
{
    public class OriginalBytes : Original
    {
        public byte[] Data { get; set; }

        public OriginalBytes()
        {
        }

        public OriginalBytes(DateTime dt, IPEndPoint ip, byte[] msg)
        {
            RxTime = dt;
            Data = msg;
            RemoteIpEndPoint = ip;
        }
    }
}
