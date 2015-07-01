using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;
using Summer.System.Log;

namespace TypeTest.Protocol
{
    public class ProtocolFactory
    {
        RxQueue rxQueue;
        MsgQueue msgQueue;

        BaseMessage defaultDecoder;
        Dictionary<byte, BaseMessage> decoders;

        //周期性的从rxQueue队列中取出原始数据，调用各类解码器解码，解码完成的数据放到msgQueue队列中
        public void ExecuteInternal()
        {
            LogHelper.GetLogger("job").Debug("Begin ProtocolFactory.ExecuteInternal()");
            List<Original> originalData = rxQueue.PopAll();
            //如果没有数据，系统周期性的发送空闲消息报文
            if (originalData.Count == 0)
            {
                msgQueue.Push(IdleMsg.CreateNewMsg());
            }
            else
            {
                foreach (var original in originalData)
                {
                    //如果来自udp消息
                    if (original is OriginalBytes)
                    {
                        OriginalBytes oBytes = original as OriginalBytes;
                        if (oBytes.Data != null && oBytes.Data.Length >= 3)
                        {
                            BaseMessage msg = null;
                            if (decoders.ContainsKey(oBytes.Data[2]))
                            {
                                msg = decoders[oBytes.Data[2]].CreateEmptyInstance();
                            }
                            else
                            {
                                msg = defaultDecoder.CreateEmptyInstance();
                            }
                            if (msg != null)
                            {
                                msg.DtTime = oBytes.RxTime;
                                msg.RackNo = oBytes.Data[0];
                                msg.SlotNo = oBytes.Data[1];
                                msg.Code = oBytes.Data[2];
                                msgQueue.Push(msg);
                            }
                        }
                    }
                    if(original is OriginalSnmp)
                    {
                        OriginalSnmp oBytes = original as OriginalSnmp;

                        BaseMessage msg = null;
                        if (decoders.ContainsKey(oBytes.GetCode()))
                        {
                            msg = decoders[oBytes.GetCode()].CreateEmptyInstance();
                        }
                        else
                        {
                            msg = defaultDecoder.CreateEmptyInstance();
                        }
                        if (msg != null)
                        {
                            msg.DtTime = oBytes.RxTime;
                            msg.RackNo = oBytes.GetRackNo();
                            msg.SlotNo = oBytes.GetSlotNo();
                            msg.Code = oBytes.GetCode();
                            msgQueue.Push(msg);
                        }
                    }
                }
            }
            LogHelper.GetLogger("job").Debug("Finish ProtocolFactory.ExecuteInternal()");
        }
    }
}
