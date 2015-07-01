using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;

namespace TypeTest.Protocol
{
    public class HeartTimeoutMsg : ErrorCodeMsg
    {
        public override BaseMessage CreateEmptyInstance()
        {
            return new HeartTimeoutMsg();
        }

        public static HeartTimeoutMsg CreateNewMsg(int rackNo, int slotNo)
        {
            HeartTimeoutMsg msg = new HeartTimeoutMsg();
            msg.DtTime = DateTime.Now;
            msg.RackNo = rackNo;
            msg.SlotNo = slotNo;
            msg.Code = 0x7E;
            return msg;
        }
    }
}
