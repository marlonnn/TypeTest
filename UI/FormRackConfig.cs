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
using iptb;
using DevComponents.DotNetBar.Controls;
using TypeTest.Model;

namespace TypeTest.UI
{
    public partial class FormRackConfig : Office2007RibbonForm
    {
        List<string> rackTypes;
        List<int> rackTypesPort;
        List<List<string>> rackBoardTypes;
        int rackNum;
        int rackStartNum;
        int slotNum;
        int slotStartNum;
        TestedRacks testedRacks;

        List<ComboBoxEx> lbRacks = new List<ComboBoxEx>();
        List<IPTextBox> ipTextBoxs = new List<IPTextBox>();
        List<List<ComboBoxEx>> lbSlots = new List<List<ComboBoxEx>>();

        public FormRackConfig()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            //有效性检查
            for (int i = 0; i < rackNum; i++)
            {
                ComboBoxEx cb = lbRacks[i];
                if (cb.SelectedIndex > 0)//如果是非空板卡，必须设定IP地址
                {
                    if (ipTextBoxs[i].Text.Length == 0 || !ipTextBoxs[i].IsValid())
                    {
                        Rack r = cb.Tag as Rack;
                        MessageBox.Show(string.Format("{0}#机笼未设置有效的IP地址", r.No));
                        return;
                    }
                }
            }
            //更新testedRacks机笼配置数据（所有板卡变成非待测板卡，不过滤掉空机笼和空板卡）
            for (int i = 0; i < rackNum; i++)
            {
                ComboBoxEx cb = lbRacks[i];
                Rack r = cb.Tag as Rack;
                r.Name = rackTypes[cb.SelectedIndex];
                r.IsEmpty = cb.SelectedIndex == 0 ? true : false;
                r.IP = ipTextBoxs[i].Text;
                r.Port = rackTypesPort[cb.SelectedIndex];

                List<ComboBoxEx> lbRackSlots = lbSlots[i];
                for (int j = 0; j < slotNum; j++)
                {
                    ComboBoxEx cbb = lbRackSlots[j];
                    Board b = cbb.Tag as Board;
                    b.Name = rackBoardTypes[cb.SelectedIndex][cbb.SelectedIndex];
                    b.IsTested = false;
                    b.IsEmpty = cbb.SelectedIndex == 0 ? true : false;
                    b.SN = "";
                    b.IsPassed = false;
                }
            }
            testedRacks.TestStatus = TestStatus.EXPECTED_FINNISH;
            //序列化用户当前的配置
            testedRacks.SaveThis();
            this.DialogResult = DialogResult.OK;
        }

        //根据testedRacks配置更新界面
        private void ReloadPanel()
        {
            int Top = 12;
            int Left = 12;
            int ControlHeight = 20;
            int LblWidth = 80;
            int InputWidth = 120;
            //每次载入重新来过，所以如果编辑了再取消不受影响
            this.panel.Controls.Clear();
            lbRacks.Clear();
            ipTextBoxs.Clear();
            lbSlots.Clear();
            //根据机笼和板卡数量初始化配置界面
            for (int i = 0; i < rackNum; i++)
            {
                //机笼选择区
                Label lbl = new Label();
                lbl.Text = string.Format("{0}号机笼：", i + rackStartNum);
                lbl.Top = Top;
                lbl.Left = Left + 10 + 250 * i;
                lbl.Width = LblWidth;
                lbl.Height = ControlHeight;
                this.panel.Controls.Add(lbl);
                ComboBoxEx lb = new ComboBoxEx();
                lb.Top = Top;
                lb.Left = lbl.Left + lbl.Width + 15;
                lb.Width = InputWidth;
                lb.Height = ControlHeight;
                lb.DropDownStyle = ComboBoxStyle.DropDownList;
                lb.Style = eDotNetBarStyle.Windows7;
                lb.Tag = testedRacks.Racks[i];
                lb.SelectedIndexChanged += new System.EventHandler(this.rack_SelectedIndexChanged);
                this.panel.Controls.Add(lb);
                lbRacks.Add(lb);
                //IP地址输入区
                Label lbIP = new Label();
                lbIP.Text = string.Format("通信板IP：", i + rackStartNum);
                lbIP.Top = Top +25;
                lbIP.Left = Left + 10 + 250 * i;
                lbIP.Width = LblWidth;
                lbIP.Height = ControlHeight;
                this.panel.Controls.Add(lbIP);
                IPTextBox ipTB = new IPTextBox();
                ipTB.Top = lbIP.Top;
                ipTB.Left = lbl.Left + lbl.Width + 15;
                ipTB.Width = InputWidth;
                ipTB.Height = ControlHeight;
                ipTB.Tag = testedRacks.Racks[i];
                this.panel.Controls.Add(ipTB);
                ipTextBoxs.Add(ipTB);

                //板卡选择区
                List<ComboBoxEx> lbRackSlots = new List<ComboBoxEx>();
                for (int j = 0; j < slotNum; j++)
                {
                    Label lblb = new Label();
                    lblb.Text = string.Format("{0}号槽道：", j + slotStartNum);
                    lblb.Top = Top + 25 * (j + 2);
                    lblb.Left = Left + 10 + 250 * i;
                    lblb.Width = LblWidth;
                    lblb.Height = ControlHeight;
                    this.panel.Controls.Add(lblb);
                    ComboBoxEx lbb = new ComboBoxEx();
                    lbb.Top = lblb.Top;
                    lbb.Left = lblb.Left + lblb.Width + 15;
                    lbb.Width = InputWidth;
                    lbb.Height = ControlHeight;
                    lbb.Tag = testedRacks.Racks[i].Boards[j];
                    lbb.Style = eDotNetBarStyle.Windows7;
                    lbb.DropDownStyle = ComboBoxStyle.DropDownList;
                    this.panel.Controls.Add(lbb);
                    lbRackSlots.Add(lbb);
                }
                lbSlots.Add(lbRackSlots);
            }

            //重置窗口大小和按钮位置
            this.panel.Height = (ControlHeight+5) * (slotNum + 3);
            btnOk.Top = this.panel.Bottom + 10;
            btnCancel.Top = btnOk.Top;
            btnTestLink.Top = btnOk.Top;
            this.Height = btnOk.Bottom + 35;

            //初始化下拉框数据和选择项目
            ResetComoboItemsAndSelect();
        }

        private void ResetComoboItemsAndSelect()
        {
            for (int i = 0; i < rackNum; i++)
            {
                ComboBoxEx cb = lbRacks[i];
                Rack r = cb.Tag as Rack;
                cb.Items.Clear();
                cb.Items.AddRange(rackTypes.ToArray());
                cb.SelectedIndex = GetIndexByText(rackTypes, r.Name);

                IPTextBox ip = ipTextBoxs[i];
                ip.Text = r.IP;

                List<ComboBoxEx> lbRackSlots = lbSlots[i];
                for (int j = 0; j < slotNum; j++)
                {
                    ComboBox cbb = lbRackSlots[j];
                    Board b = cbb.Tag as Board;
                    cbb.Items.Clear();
                    cbb.Items.AddRange(rackBoardTypes[cb.SelectedIndex].ToArray());
                    cbb.SelectedIndex = GetIndexByText(rackBoardTypes[cb.SelectedIndex], b.Name);
                }
            }
        }

        private int GetIndexByText(List<string> list,string finds)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(finds))
                {
                    return i;
                }
            }
            return 0;
        }

        private void rack_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxEx cb = (ComboBoxEx)sender;
            int index = (cb.Tag as Rack).No - rackStartNum;
            List<ComboBoxEx> lbRackSlots = lbSlots[index];
            foreach (ComboBoxEx cbb in lbRackSlots)
            {
                cbb.Items.Clear();
                cbb.Items.AddRange(rackBoardTypes[cb.SelectedIndex].ToArray());
                cbb.SelectedIndex = 0;
            }
        }

        private void formRackConfig_Load(object sender, EventArgs e)
        {
            //如果初始数据非法，则重新初始化
            if (testedRacks.Racks.Count != rackNum || testedRacks.Racks[0].Boards.Count != slotNum)
            {
                for (int i = 0; i < rackNum; i++)
                {
                    Rack r = new Rack();
                    r.No = i + rackStartNum;
                    r.Name = rackTypes[0];
                    r.IsEmpty = true;
                    r.IP = "";
                    r.Port = 0;
                    for (int j = 0; j < slotNum; j++)
                    {
                        Board b = new Board();
                        b.No = j + slotStartNum;
                        b.Name = rackBoardTypes[i][0];
                        b.IsEmpty = true;
                        b.IsTested = false;
                        b.SN = "";
                        r.Boards.Add(b);
                    }
                    testedRacks.Racks.Add(r);
                }
            }
            //由Docment触发的View更新
            ReloadPanel();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
