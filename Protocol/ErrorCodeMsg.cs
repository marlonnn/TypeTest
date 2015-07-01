using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;
using TypeTest.Model;

namespace TypeTest.Protocol
{
    public class ErrorCodeMsg : BaseMessage
    {
        public override BaseMessage CreateEmptyInstance()
        {
            return new ErrorCodeMsg();
        }
    }
}
