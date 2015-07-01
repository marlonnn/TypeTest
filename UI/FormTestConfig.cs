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
using TypeTest.Model;

namespace TypeTest.UI
{
    public partial class FormTestConfig : Office2007RibbonForm
    {
        TestedRacks testedRacks; 

        public FormTestConfig()
        {
            InitializeComponent();
        }

        private void ReloadFrm()
        {
            int Top = panel.Top;
            int Left = panel.Left;

            int tabIndex = tbTester.TabIndex + 1;
            int maxBoardNumPerRack = 0;
            //根据待测试板卡初始化配置界面
            this.panel.Controls.Clear();
            int ControlHeight = 25;
            int LblWidth = 150;
            int InputWidth = 150;
            for (int i = 0; i < testedRacks.Racks.Count; i++)
            {
                Rack r = testedRacks.Racks[i];

                int tMax = 0;
                for (int j = 0; j < testedRacks.Racks[i].Boards.Count; j++)
                {
                    Board b = testedRacks.Racks[i].Boards[j];
                    if (b.IsTested)
                    {
                        Label lbl = new Label();
                        lbl.Text = string.Format("{0}#{2}-{1}#{3}：", r.No, b.No, r.Name, b.Name);
                        lbl.Top = 5 + 30 * tMax;
                        lbl.Left = 5 + 360 * i;
                        lbl.Width = LblWidth;
                        lbl.Height = ControlHeight;
                        this.panel.Controls.Add(lbl);

                        TextBox tb = new TextBox();
                        tb.Text = b.SN;
                        tb.Left = lbl.Right + 10;
                        tb.Top = lbl.Top;
                        tb.Width = InputWidth;
                        tb.Height = ControlHeight;
                        tb.TabIndex = tabIndex++;
                        tb.Tag = b;
                        tb.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbTester_KeyDown);
                        this.panel.Controls.Add(tb);

                        tMax++;
                    }
                }
                if (tMax > maxBoardNumPerRack)
                    maxBoardNumPerRack = tMax;
            }
            if (maxBoardNumPerRack == 0)
            {
                Label lbl = new Label();
                lbl.Text = "没有待测试板卡，请先到主界面的左边板卡树上选择需要待测试的板卡，然后设置SN号等信息。";
                lbl.Top = 5;
                lbl.Left = 5;
                lbl.Width = 600;
                lbl.Height = 25;
                this.panel.Controls.Add(lbl);
            }

            //重置窗口大小和按钮位置
            this.panel.Height = (ControlHeight + 5) * (maxBoardNumPerRack > 0 ? maxBoardNumPerRack : 1) + 5;
            btnOk.Top = this.panel.Bottom + 10;
            btnOk.TabIndex = tabIndex++;
            btnCancel.Top = btnOk.Top;
            btnCancel.TabIndex = tabIndex++;
            btnReadBack.Top = btnOk.Top;
            btnReadBack.TabIndex = tabIndex++;
            this.Height = btnOk.Bottom + 35;

            tbTester.Text = testedRacks.Tester;
            tbTester.Focus();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (tbTester.Text.Length == 0)
            {
                MessageBox.Show("请输入测试人员姓名。");
                tbTester.Focus();
                return;
            }
            foreach (Control c in this.panel.Controls)
            {
                if (c is TextBox)
                {
                    if (c.Tag is Board)
                    {
                        if (c.Text.Length == 0)
                        {
                            Board b = (Board)c.Tag;
                            Rack r = testedRacks.GetRack(b);
                            MessageBox.Show(string.Format("{0}#{1} {2}#{3}未设置SN号",r.No,r.Name, b.No, b.Name));
                            c.Focus();
                            return;
                        }
                    }
                }
            }

            testedRacks.Tester = tbTester.Text;
            foreach (Control c in this.panel.Controls)
            {
                if (c is TextBox)
                {
                    if (c.Tag is Board)
                    {
                        if (c.Text.Length > 0)
                        {
                            Board b = (Board)c.Tag;
                            b.SN = c.Text;
                        }
                    }
                }
            }
            testedRacks.TestStatus = TestStatus.EXPECTED_FINNISH;
            testedRacks.SaveThis();
            this.DialogResult = DialogResult.OK;
        }

        //回车后换到下一个文本框
        private void tbTester_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{tab}");
            }
        }

        private void btnReadBack_Click(object sender, EventArgs e)
        {
            TestedRacks tr = testedRacks.LoadByKey(testedRacks.LastKey);
            if (tr != null)
            {
                this.Enabled = false;
                foreach (Rack r in testedRacks.Racks)
                {
                    foreach (Board b in r.Boards)
                    {
                        if (b.IsTested)
                        {
                            b.SN = findSnByRackBoard(tr, r.No, b.No);
                        }
                    }
                }
                ReloadFrm();
                this.Enabled = true;
            }
            else
            {
                MessageBox.Show("历史SN号读取失败。");
            }
        }

        private string findSnByRackBoard(TestedRacks lastTestedRacks,int rack, int board)
        {
            foreach (Rack r in lastTestedRacks.Racks)
            {
                foreach (Board b in r.Boards)
                {
                    if (r.No == rack && b.No == board)
                    {
                        return b.SN;
                    }
                }
            }
            return "";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void formTestConfig_Load(object sender, EventArgs e)
        {
            ReloadFrm();
        }
    }
}
