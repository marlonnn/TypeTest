using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;

namespace TypeTest.Protocol
{
    public class BaseMessage
    {
        public DateTime DtTime;
        public int RackNo;
        public int SlotNo;
        public int Code;

        public virtual BaseMessage CreateEmptyInstance()
        {
            return new BaseMessage();
        }
    }
}
