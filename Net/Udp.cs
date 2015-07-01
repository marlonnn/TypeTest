using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using TypeTest.Common;
using TypeTest.UI;
using Summer.System.NET;
using Summer.System.Log;
using TypeTest.Model;
using Summer.System.Util;

namespace TypeTest.Net
{
    public class Udp
    {
        public int ListenPort;
        RxQueue rxQueue;

        private UdpNetServer udpNetServer;

        public Udp()
        {

        }

        //启动侦听端口并接收数据
        public void UpdRxStart()
        {
            try
            {
	            udpNetServer.AsyncRxProcessCallBack += new NetAsyncRxDataCallBack(this.ReceiveBytes);
	            udpNetServer.Open();
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                udpNetServer.ReceiveAsync(remoteIpEndPoint);
            }
            catch (System.Exception e)
            {
                LogHelper.GetLogger<Udp>().Error(e.Message);
                LogHelper.GetLogger<Udp>().Error(e.StackTrace);
            }
        }

        //关闭Udp
        public void UdpClose()
        {
            try
            {
	            udpNetServer.AsyncRxProcessCallBack -= new NetAsyncRxDataCallBack(this.ReceiveBytes);
            }
            catch (System.Exception e)
            {
                LogHelper.GetLogger<Udp>().Error(e.Message);
                LogHelper.GetLogger<Udp>().Error(e.StackTrace);
            }
            finally
            {
                udpNetServer.Close();
            }
        }

        private void ReceiveBytes(byte[] receiveBytes, IPEndPoint remoteIpEndPoint)
        {
            try
            {
                if (rxQueue != null)
                {
                    rxQueue.Push(new OriginalBytes(DateTime.Now, remoteIpEndPoint, receiveBytes));
                }
                IPEndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
                udpNetServer.ReceiveAsync(remoteIp);
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<Udp>().Error(ee.Message);
                LogHelper.GetLogger<Udp>().Error(ee.StackTrace);
            }
        }
    }
}
