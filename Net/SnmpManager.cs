using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Summer.ES.Snmp;
using Dart.Snmp;
using TypeTest.Common;
using Dart.Snmp.Trap;

namespace TypeTest.Net
{
    public class SnmpManager
    {
        SnmpTrapManager snmpTrapManager;
        RxQueue rxQueue;

        public void TrapRxStart()
        {
            snmpTrapManager.Start((Manager manager, MessageBase message, object state) =>
            {
                if (message is TrapMessage)
                {
                    TrapMessage tm = message as TrapMessage;
                    if (tm.GenericTrap == SpecificTrap.GenericTrapConst)
                    {
                        OriginalSnmp snmpMsg = new OriginalSnmp();
                        snmpMsg.Data = tm.SpecificTrap;
                        snmpMsg.RxTime = DateTime.Now;
                        snmpMsg.RemoteIpEndPoint = tm.Origin;
                        rxQueue.Push(snmpMsg);
                    }
                }
            }, null);
        }
    }
}
