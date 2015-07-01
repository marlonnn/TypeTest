using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Summer.System.Log;
using System.Windows.Forms;
using TypeTest.Common;
using TypeTest.Protocol;

namespace TypeTest.Model
{
    [Serializable]
    public class TestedRacks
    {
        //Key(分别对应二进制文件和日志文件)
        public string Key;
        //测试人
        public string Tester = "";
        //待测试机笼和板卡
        public List<Rack> Racks = new List<Rack>();
        //测试状态
        public TestStatus TestStatus;
        //开始测试时间
        public DateTime StartTime;
        //测试时长(单位：秒)
        public long RunningTime;
        //上一次的key
        public string LastKey;

        [field:NonSerialized]
        public event EventHandler OnTestStatusChange;
        [field: NonSerialized]
        public event EventHandler OnTimeout;

        [field: NonSerialized]
        public event EventHandler OnBoardStatusChange;

        [NonSerialized]
        MsgQueue msgQueue;                   //spring初始化完成后后，重载也不覆盖，单态唯一
        [NonSerialized]
        ErrorCodeMsgFile errorCodeMsgFile; //spring初始化完成后后，重载也不覆盖，单态唯一        
        [NonSerialized]
        int preTimeout;                     //spring初始化完成后后，重载也不覆盖，单态唯一
        [NonSerialized]
        int runTimeout;                    //spring初始化完成后后，重载也不覆盖，单态唯一

        DateTime preTestTime;
        Dictionary<Board, bool> preTimeoutDic; //开始测试时自动初始化
        Dictionary<Board, DateTime> runTimeoutDic;//开始测试时自动初始化
        Dictionary<int, Rack> rackNameDict;//开始测试时自动初始化
        Dictionary<int, Board> boardNameDict;//开始测试时自动初始化
        Dictionary<Board, Rack> boardRackDict;//开始测试时自动初始化

        public void CheckTstConfig()
        {
            int testCount = 0;
            if (this.Tester == "")
            {
                throw new Exception("请输入测试人员姓名。");
            }
            foreach (Rack rack in this.Racks)
            {
                foreach (Board board in rack.Boards)
                {
                    if (board.IsTested)
                    {
                        testCount++;
                    }
                }
            }
            if (testCount == 0)
            {
                throw new Exception("未选中一块板卡，无法进行测试！");
            }

            foreach (Rack r in this.Racks)
            {
                foreach (Board b in r.Boards)
                {
                    if (b.IsTested)
                    {
                        if (b.SN.Length == 0)
                        {
                            throw new Exception(string.Format("{0}#{1} {2}#{3}未设置SN号",r.No,r.Name, b.No, b.Name));
                        }
                    }
                }
            }
        }
        //开始测试(点击开始测试按钮，执行测试检查，检查通过后才执行真正的测试)
        public void StartTest()
        {
            preTestTime = DateTime.Now;

            rackNameDict = new Dictionary<int, Rack>();
            boardNameDict = new Dictionary<int, Board>();
            boardRackDict = new Dictionary<Board, Rack>();
            preTimeoutDic = new Dictionary<Board, bool>();
            runTimeoutDic = new Dictionary<Board, DateTime>();
            foreach (Rack r in Racks)
            {
                rackNameDict.Add(r.No, r);
                foreach (Board b in r.Boards)
                {
                    b.IsPassed = false; //默认是false，检查通过后才全部置为true，开始真正测试
                    boardNameDict.Add(1000 * r.No + b.No, b);
                    boardRackDict.Add(b, r);
                    if (b.IsTested && !b.IsEmpty)
                    {
                        preTimeoutDic.Add(b, false);
                        runTimeoutDic.Add(b, DateTime.Now);                        
                    }
                }
            }
            //进入临界状态
            GenTestStatusChangeEvent(TestStatus, TestStatus.THRESHOLD);
            TestStatus = TestStatus.THRESHOLD;
            try
            {
                CheckTstConfig();
            }
            catch (Exception ee)
            {
                //执行异常处理，并转入异常状态
                FinishUnExpectedTest();
                throw;
            }

        }
        //开始一次正常测试
        protected void StartNomalTest()
        {
            Key = Util.GenrateKey();
            StartTime = DateTime.Now;
            RunningTime = 0;
            errorCodeMsgFile.Open(Key);

            foreach(Rack r in Racks)
            {
                foreach(Board b in r.Boards)
                {
                    b.IsPassed = true;//全部置为true，因为和上次的待测板卡不一定相同
                }
            }

            TestStatus = TestStatus.RUNNING;
            GenTestStatusChangeEvent(TestStatus.THRESHOLD, TestStatus);
        }
        //结束一次正常测试
        public void FinishExpectedTest()
        {
            //先切换状态，然后执行耗时操作
            TestStatus = TestStatus.EXPECTED_FINNISH;
            GenTestStatusChangeEvent(TestStatus.RUNNING, TestStatus);

            LastKey = Key;
            RunningTime = (DateTime.Now.Ticks - StartTime.Ticks) / 10000000;
            errorCodeMsgFile.Close();
            Save(Util.GetBasePath() + "//Report//Data//" + Key + ".trs");
        }
        //获得已测时间
        public long GetRuningDuration()
        {
            return (DateTime.Now.Ticks - StartTime.Ticks) / 10000000;
        }
        //结束一次非正常测试
        public void FinishUnExpectedTest()
        {
            RunningTime = 0;

            TestStatus = TestStatus.UNEXPECTED_FINNISH;
            GenTestStatusChangeEvent(TestStatus.THRESHOLD, TestStatus);
        }
        //没有消息的时候，界面会周期性发送0xFF消息
        public void AppendLog(BaseMessage msg)
        {
            if (TestStatus == TestStatus.THRESHOLD)
            {
                //检查被测板卡的心跳是否都已经收到
                CheckPreTstHeart(msg);
            }

            //生成心跳超时
            if (TestStatus == TestStatus.RUNNING)
            {
                if (msg is IdleMsg)  //idle消息会每秒钟发出来
                {
                    List<Board> boards = GetTimeoutBoard(runTimeoutDic, runTimeout);
                    foreach (var b in boards)
                    {
                        Rack r = GetRack(b);
                        if (r != null)
                        {
                            HeartTimeoutMsg mymsg = HeartTimeoutMsg.CreateNewMsg(r.No, b.No);
                            if (msgQueue != null)
                                msgQueue.Push(mymsg);
                            //下一次再进行超时判断
                            runTimeoutDic[b] = DateTime.Now;
                        }
                    }
                }
                else //心跳消息、错误码消息都算是心跳，更新收到上次心跳的时间
                {
                    Board board = GetBoard(msg.RackNo, msg.SlotNo);
                    runTimeoutDic[board] = DateTime.Now;
                }
            }

            //只在测试过程中记录日志，并且不记录心跳数据
            if (TestStatus == TestStatus.RUNNING && errorCodeMsgFile != null && !IsHeartMsg(msg))
            {
                Board b = GetBoard(msg.RackNo, msg.SlotNo);
                if(b.IsPassed)
                {
                    b.IsPassed = false;
                    GenBoardStatusChangeEvent(b);
                }
                errorCodeMsgFile.Append(msg, GetRack(msg.RackNo).Name, GetBoard(msg.RackNo, msg.SlotNo).Name);
            }
        }
        //判断此次测试是否通过
        public bool IsPass()
        {
            foreach (Rack r in Racks)
            {
                foreach (Board b in r.Boards)
                {
                    //有一块板卡未通过就失败
                    if (b.IsTested && !b.IsPassed)
                        return false;
                }
            }
            return true;
        }
        //生成状态切换事件
        protected void GenTestStatusChangeEvent(TestStatus last, TestStatus cur)
        {
            TestStatusEventArgs e = new TestStatusEventArgs();
            e.LastStatus = last;
            e.CurStatus = cur;
            EventHandler temp = OnTestStatusChange;
            if (temp != null)
            {
                temp(this, e);
            }
        }
        //生成超时事件
        protected void GenTimeoutEvent(List<Board> boards, TestStatus cur)
        {
            TimeoutEventArgs e = new TimeoutEventArgs();
            e.Boards = boards;
            e.TestStatus = cur;
            EventHandler temp = OnTimeout;
            if (temp != null)
            {
                temp(this, e);
            }
        }

        //生成板卡通过状态切换事件（在Running 状态下）
        protected void GenBoardStatusChangeEvent(Board board)
        {
            BoardStatusEventArgs e = new BoardStatusEventArgs();
            e.Board = board;
            EventHandler temp = OnBoardStatusChange;
            if (temp != null)
            {
                temp(this, e);
            }
        }
        //检查是否所有的心跳都已经收到
        private void CheckPreTstHeart(BaseMessage msg)
        {
            if (!IsHeartMsg(msg))
                return;

            //更新心跳记录
            Board b = GetBoard(msg.RackNo, msg.SlotNo);
            if (preTimeoutDic.ContainsKey(b))
                preTimeoutDic[b] = true;
            //如果所有心跳都收到，开始正式测试
            bool isAllOk = true;
            foreach (var kv in preTimeoutDic)
            {
                isAllOk &= kv.Value;
            }
            if (isAllOk)
            {
                StartNomalTest();
                return;
            }
            //如果超时了，还有板卡没有收到消息，进入异常结束
            if (DateTime.Now.Ticks - preTestTime.Ticks > (uint)preTimeout * 10000000)
            {
                List<Board> boards = new List<Board>();
                foreach (var kv in preTimeoutDic)
                {
                    if (kv.Value == false)
                        boards.Add(kv.Key);
                }
                //执行异常结束工作
                FinishUnExpectedTest();
                GenTimeoutEvent(boards, TestStatus.THRESHOLD);
            }
        }
        //是否是心跳或者空闲消息
        private bool IsHeartMsg(BaseMessage msg)
        {
            return (msg is HeartMsg || msg is IdleMsg);
        }
        //获得运行过程中超时的板卡
        private List<Board> GetTimeoutBoard(Dictionary<Board, DateTime> dict, int timeout)
        {
            List<Board> boards = new List<Board>();
            foreach (var b in dict.Keys)
            {
                if (DateTime.Now.Ticks - dict[b].Ticks > (uint)timeout * 10000000)
                {
                    boards.Add(b);
                }
            }
            return boards;
        }
        //清理SN号
        public void ClearSNs()
        {
            //历史SN号清除
            foreach (Rack r in Racks)
            {
                foreach (Board b in r.Boards)
                {
                    b.SN = "";
                }
            }
        }
        //根据机笼号取得机笼
        public Rack GetRack(int rackNo)
        {
            if (rackNameDict.ContainsKey(rackNo))
            {
                return rackNameDict[rackNo];
            }
            Rack r = new Rack();
            r.No = rackNo;
            r.Name = "未知机笼";
            return r;
        }
        //根据板卡取得板卡所在的机笼
        public Rack GetRack(Board board)
        {
            if (boardRackDict == null)
            {
                boardRackDict = new Dictionary<Board, Rack>();
                foreach (Rack r in Racks)
                {
                    foreach (Board b in r.Boards)
                    {
                        boardRackDict.Add(b, r);
                    }
                }
            }
            if (boardRackDict.ContainsKey(board))
            {
                return boardRackDict[board];
            }
            return null;
        }
        //根据机笼号和板卡号获得板卡
        public Board GetBoard(int rackNo, int boardNo)
        {
            if (boardNameDict.ContainsKey(rackNo * 1000 + boardNo))
            {
                return boardNameDict[rackNo * 1000 + boardNo];
            }
            Board b = new Board();
            b.No = boardNo;
            b.Name = "未知板卡";
            return b;
        }

        #region 序列化相关
        //保存当前对象到硬盘，用于程序下次启动时
        public void SaveThis()
        {
            //序列化用户当前的配置
            Save(@".\Config\testedRacks.bin");
        }

        private void Save(string pathAndFilename)
        {
            //序列化用户当前的配置
            System.Runtime.Serialization.IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(pathAndFilename, FileMode.OpenOrCreate, FileAccess.Write);
            using (stream)
            {
                formatter.Serialize(stream, this);
            }
        }

        public TestedRacks Load()
        {
            return Load(@".\Config\testedRacks.bin");
        }

        public TestedRacks LoadByKey(string key)
        {
            return Load(Util.GetBasePath() + "//Report//Data//" + key + ".trs");
        }

        private TestedRacks Load(string pathAndFilename)
        {
            try
            {
                System.Runtime.Serialization.IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(pathAndFilename, FileMode.Open, FileAccess.Read);
                TestedRacks tr;
                using (stream)
                {
                    tr = (TestedRacks)formatter.Deserialize(stream);
                }
                return tr;
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<TestedRacks>().Error(ee.Message);
                LogHelper.GetLogger<TestedRacks>().Error(ee.StackTrace);
            }
            return null;
        }
        //执行一次深度复制
        public void CopyFrom(TestedRacks tr)
        {
            this.Key = tr.Key;
            this.Tester = tr.Tester;
            this.Racks = tr.Racks;
            this.TestStatus = tr.TestStatus;
            this.StartTime = tr.StartTime;
            this.RunningTime = tr.RunningTime;
            this.LastKey = tr.LastKey;

            this.preTestTime = tr.preTestTime;
        }
        #endregion
    }
}
