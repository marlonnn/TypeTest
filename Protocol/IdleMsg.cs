using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;

namespace TypeTest.Protocol
{
    public class IdleMsg : BaseMessage
    {
        public static IdleMsg CreateNewMsg()
        {
            IdleMsg msg = new IdleMsg();
            msg.DtTime = DateTime.Now;
            msg.RackNo = 0xFF;
            msg.SlotNo = 0xFF;
            msg.Code = 0xFF;
            return msg;
        }

        public override BaseMessage CreateEmptyInstance()
        {
            return new IdleMsg();
        }
    }
}
