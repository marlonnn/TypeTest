using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using TypeTest.Common;
using DevComponents.AdvTree;
using Summer.System.Core;
using Summer.System.Log;
using TypeTest.Model;
using TypeTest.Net;
using TypeTest.Protocol;

namespace TypeTest.UI
{
    public partial class FormMain : Office2007RibbonForm
    {
        public delegate void DisplayTreeHandler(TestedRacks testedRacks);

        MsgQueue msgQueue;
        FormRackConfig formRackConfig;
        FormTestConfig formTestConfig;
        FormAbout formAbout;
        TestedRacks testedRacks;
        Report report;
        ErrorDescription errorDescription;

        DateTime dtTstStart;

        Udp udp;
        SnmpManager snmpManager;

        private Dictionary<Board, DateTime> preTstDic = new Dictionary<Board, DateTime>();
        private Dictionary<Board, DateTime> tstDic = new Dictionary<Board, DateTime>();
        private readonly static DateTime NULLDateTime = new DateTime();

        private void resetDict(IDictionary<Board, DateTime> dict)
        {
            dict.Clear();

            foreach (Rack r in testedRacks.Racks)
            {
                foreach (Board b in r.Boards)
                {
                    if (b.IsTested)
                    {
                        dict.Add(b, NULLDateTime);
                    }
                }
            }
        }

        public FormMain()
        {
            InitializeComponent();
        }

        private void btnRack_Click(object sender, EventArgs e)
        {
            if (formRackConfig.ShowDialog() == DialogResult.OK)
            {
                DisplayTree(testedRacks);
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if(formTestConfig.ShowDialog() == DialogResult.OK)
            {
                ReSetNodeColor();
                lstLog.Items.Clear();
                lblResult.Text = "";
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                setUIEnabled(true);
                ReSetNodeColor();
                testedRacks.StartTest();
            }
            catch (System.Exception ex)
            {
                setUIEnabled(false);
                MessageBox.Show((ex.Message));
            }
        }

        private void btnFinish_Click(object sender, EventArgs e)
        {
            if (testedRacks.TestStatus == TestStatus.RUNNING)
            {
                testedRacks.FinishExpectedTest();
                report.GeneratePdf();
                testedRacks.ClearSNs();
                testedRacks.SaveThis();
            }
            else
            {
                testedRacks.FinishUnExpectedTest();
            }
            setUIEnabled(false);
        }

        private void setUIEnabled(bool b)
        {
            btnStart.Enabled = !b;
            btnFinish.Enabled = b;
            btnRack.Enabled = !b;
            btnTest.Enabled = !b;
            rackTree.Enabled = !b;
        }

        //更新日志区列表
        private void mainTimer_Tick(object sender, EventArgs e)
        {
            List<BaseMessage> list = msgQueue.PopAll();

            //转发所有消息
            foreach (BaseMessage em in list)
            {
                testedRacks.AppendLog(em);
            }

            //只有running状态才更新日志列表(除0x7F外所有消息均显示)
            if (testedRacks.TestStatus == TestStatus.RUNNING)
            {
                foreach (var msg in list)
                {
                    if (msg is HeartMsg || msg is IdleMsg)
                        continue;
                    string[] log = new string[]{msg.DtTime.ToString("HH:mm:ss:fff"),
                    string.Format("{0:D2}",msg.RackNo), testedRacks.GetRack(msg.RackNo).Name,
                    string.Format("{0:D2}",msg.SlotNo),testedRacks.GetBoard(msg.RackNo,msg.SlotNo).Name,
                    string.Format("0x{0:X2}",msg.Code),errorDescription.GetDescription(msg.Code) };
                    lstLog.AppendLog(log);
                }                
                lblTime.Text = Util.FormateDurationSecondsMaxHour2(testedRacks.GetRuningDuration());
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            try
            {
                TestedRacks tc = testedRacks.Load();
                if (tc != null)
                {
                    testedRacks.CopyFrom(tc);
                    DisplayTree(testedRacks);
                }

                testedRacks.TestStatus = TestStatus.EXPECTED_FINNISH;
                testedRacks.OnTestStatusChange += new EventHandler(onChangeStatus);
                testedRacks.OnTimeout += new EventHandler(onTimeout);
                testedRacks.OnBoardStatusChange += new EventHandler(onBoardStatus);
                udp.UpdRxStart();
                snmpManager.TrapRxStart();

                setUIEnabled(false);

                lstLog.Timer = mainTimer;
                lstLog.MaxLogRecords = 500;

                mainTimer.Enabled = true;
            }
            catch (Exception ee)
            {
            }
        }

        //初始化左边板卡选择树
        public void DisplayTree(TestedRacks testedRacks)
        {
            rackTree.Nodes.Clear();
            TreeNode baseroot = new TreeNode();

            baseroot.Tag = "型式试验";
            baseroot.Text = "型式试验";
            TreeNode root = null;
            TreeNode teed = null;
            foreach (Rack rack in testedRacks.Racks)
            {
                root = new TreeNode();
                root.Text = string.Format("{0}#-{1}", rack.No, rack.Name);
                root.Tag = rack;
                if(!rack.Name.Contains("空"))
                {
                    if(!rack.IsEmpty)
                    {
                        root.Checked = true;
                    }
                    baseroot.Nodes.Add(root);
                }

                foreach (Board board in rack.Boards)
                {
                    teed = new TreeNode();
                    teed.Text = string.Format("{0}#槽道 - {1}", board.No, board.Name);
                    teed.Tag = board;
                    if (!board.Name.Contains("空"))
                    {
                        if (board.IsTested)
                        {
                            teed.Checked = true;
                        }
                        root.Nodes.Add(teed);
                    }
                }
            }
            baseroot.ExpandAll();
            rackTree.Nodes.Add(baseroot);
            rackTree.Refresh();
        }

        private void ReSetNodeColor()
        {
            foreach (TreeNode basenode in rackTree.Nodes)
            {
                foreach (TreeNode rack in basenode.Nodes)
                {
                    foreach (TreeNode board in rack.Nodes)
                    {
                        Board b = (Board)board.Tag;
                        board.BackColor = Color.Transparent;
                    }
                }
            }
        }

        private void SetNodeColor(TestedRacks testedRacks, TestStatus testStatus)
        {
            foreach (TreeNode basenode in rackTree.Nodes)
            {
                foreach (TreeNode rack in basenode.Nodes)
                {
                    foreach (TreeNode board in rack.Nodes)
                    {
                        Board b = (Board)board.Tag;
                        switch (testStatus)
                        {
                            case TestStatus.THRESHOLD:
                                break;
                            case TestStatus.RUNNING:
                                if (!b.IsPassed)
                                {
                                    board.BackColor = Color.Yellow;
                                }

                                break;
                            case TestStatus.UNEXPECTED_FINNISH:
                                if(b.IsTested)
                                {
                                    board.BackColor = Color.Red;
                                }
                                break;
                            case TestStatus.EXPECTED_FINNISH:
                                if(b.IsTested)
                                {
                                    if(b.IsPassed)
                                    {
                                        board.BackColor = Color.Green;
                                    }
                                    else
                                    {
                                        board.BackColor = Color.Red;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void SetNodeColorByStatus(TreeNode node, Board board, TestStatus testStatus)
        {
            switch (testStatus)
            {
                case TestStatus.THRESHOLD:
                    break;
                case TestStatus.RUNNING:
                    if (!board.IsPassed)
                    {
                        node.BackColor = Color.Yellow;
                    }
                    break;
                case TestStatus.UNEXPECTED_FINNISH:
                    node.BackColor = Color.Red;
                    break;
                case TestStatus.EXPECTED_FINNISH:
                    if (board.IsPassed)
                    {
                        node.BackColor = Color.Green;
                    }
                    else
                    {
                        node.BackColor = Color.Red;
                    }
                    break;
            }
        }

        private void SetLabelResult(TestStatus testStatus)
        {
            switch (testStatus)
            {
                case TestStatus.THRESHOLD:
                    lblResult.Text = "";
                    break;
                case TestStatus.RUNNING:
                    lblResult.ForeColor = Color.Gray;
                    lblResult.Text = "Run...";
                    break;
                case TestStatus.UNEXPECTED_FINNISH:
                    lblResult.ForeColor = Color.Red;
                    lblResult.Text = "Error";
                    break;
                case TestStatus.EXPECTED_FINNISH:
                    if (testedRacks.IsPass())
                    {
                        lblResult.ForeColor = Color.Green;
                        lblResult.Text = "PASS";
                    }
                    else
                    {
                        lblResult.ForeColor = Color.Red;
                        lblResult.Text = "FAIL";
                    }
                    break;
            }
        }

        private void rackTree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);
                }
                else
                {
                    if (e.Node.Tag is Board)
                    {
                        Board board = (Board)e.Node.Tag;
                        board.IsTested = e.Node.Checked;
                        Rack rack = (Rack)e.Node.Parent.Tag;
                    }
                }

                testedRacks.SaveThis();
            }
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                if (node.Tag is Board)
                {
                    Board board = (Board)node.Tag;
                    board.IsTested = nodeChecked;
                    Rack rack = (Rack)node.Parent.Tag;
                }
                string name = node.Text;

                if (node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            formAbout.ShowDialog();
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {

        }

        private void formMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (testedRacks.TestStatus == TestStatus.RUNNING)
            {
                DialogResult result = MessageBox.Show("软件关闭前请先点击 停止测试 按钮！", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
                return;
            }
            else
            {
                try
                {
                    Quartz.Impl.StdScheduler scheduler = (Quartz.Impl.StdScheduler)SpringHelper.GetContext().GetObject("scheduler");
                    scheduler.Shutdown();
                    udp.UdpClose();
                }
                catch (Exception ee)
                {
                    LogHelper.GetLogger<FormMain>().Error(ee.Message);
                    LogHelper.GetLogger<FormMain>().Error(ee.StackTrace);
                }
            }
        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            report.ExploreReportFolder();
        }

        private void onChangeStatus(object sender, EventArgs e)
        {
            TestedRacks tr = sender as TestedRacks;
            TestStatusEventArgs args = e as TestStatusEventArgs;
            switch (args.CurStatus)
            {
                case TestStatus.THRESHOLD:
                    SetNodeColor(testedRacks, TestStatus.THRESHOLD);
                    SetLabelResult(TestStatus.THRESHOLD);
                    break;
                case TestStatus.RUNNING:
                    SetNodeColor(testedRacks, TestStatus.RUNNING);
                    SetLabelResult(TestStatus.RUNNING);
                    dtTstStart = DateTime.Now;
                    break;
                case TestStatus.UNEXPECTED_FINNISH:
                    SetNodeColor(testedRacks, TestStatus.UNEXPECTED_FINNISH);
                    SetLabelResult(TestStatus.UNEXPECTED_FINNISH);
                    setUIEnabled(false);
                    break;
                case TestStatus.EXPECTED_FINNISH:
                    SetNodeColor(testedRacks, TestStatus.EXPECTED_FINNISH);
                    SetLabelResult(TestStatus.EXPECTED_FINNISH);
                    break;
            }
        }

        private void onTimeout(object sender, EventArgs e)
        {
            TestedRacks tr = sender as TestedRacks;
            TimeoutEventArgs args = e as TimeoutEventArgs;

            if (args.TestStatus == TestStatus.THRESHOLD)
            {
                MessageBox.Show("初始化设备心跳超时！");
            }
        }

        private void onBoardStatus(object sender, EventArgs e)
        {
            TestedRacks tr = sender as TestedRacks;
            BoardStatusEventArgs args = e as BoardStatusEventArgs;
            if(!args.Board.IsPassed)
            {
                SetNodeColor(testedRacks, TestStatus.RUNNING);
            }
        }
    }
}
