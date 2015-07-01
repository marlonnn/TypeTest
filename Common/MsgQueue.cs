using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Summer.System.Collections.Concurrent;
using TypeTest.Protocol;

namespace TypeTest.Common
{
    public class MsgQueue : ConcurrentQueue<BaseMessage>
    {

    }
}
