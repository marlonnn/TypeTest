using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;

namespace TypeTest.Protocol
{
    public class HeartMsg : BaseMessage
    {
        public override BaseMessage CreateEmptyInstance()
        {
            return new HeartMsg();
        }
    }
}
