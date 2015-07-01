using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iTextSharp.text.pdf;
using System.Reflection;
using iTextSharp.text;
using System.IO;
using System.Windows.Forms;
using Summer.System.Log;
using Aspose.Cells;
using TypeTest.Common;

namespace TypeTest.Model
{
    //文件存储结构：
    //Report根目录放pdf报告
    //Report目录下的Data放TestRacks二进制文件和日志文件
    //这三种文件使用同一个文件名，后缀分别是pdf trs log
    public class Report
    {
        TestedRacks testedRacks;

        TypeTest.Common.Version version;

        string endString;

        string templetFile;

        string reportTitle;
        string fontFile;
        float fontSizeHead;
        float fontSizeBody;

        public void GeneratePdf()
        {
            GeneratePdf(testedRacks);
        }

        public void ExploreReportFolder()
        {
            //System.Diagnostics.Process.Start("explorer.exe", Util.GetBasePath() + "//Report");
            System.Diagnostics.Process.Start(Util.GetBasePath() + "//Report");
        }

        public void GeneratePdf(TestedRacks tr)
        {
            PdfReader rdr;
            PdfStamper stamper;
            BaseFont baseFont;

            string pdfFile = Util.GetBasePath() + "//Report//" + tr.Key + ".pdf";
            string dataFile = Util.GetBasePath() + "//Report//Data//" + tr.Key + ".trs";
            string logFile = Util.GetBasePath() + "//Report//Data//" + tr.Key + ".log";

            LogHelper.GetLogger<Report>().Debug(pdfFile);
            LogHelper.GetLogger<Report>().Debug(dataFile);
            LogHelper.GetLogger<Report>().Debug(logFile);

            try
            {
                baseFont = BaseFont.CreateFont(fontFile, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);    
            }
            catch(Exception ee)
            {
                LogHelper.GetLogger<Report>().Error(ee.Message);
                LogHelper.GetLogger<Report>().Error(ee.StackTrace);
                MessageBox.Show("报告系统的资源文件载入失败，请与开发人员联系。");
                return;
            }

            try
            {
                rdr = new PdfReader(Util.GetBasePath() + templetFile);
            }
            catch(Exception ee)
            {
                LogHelper.GetLogger<Report>().Error(ee.Message);
                LogHelper.GetLogger<Report>().Error(ee.StackTrace);
                MessageBox.Show(string.Format("模板文件载入失败，请检查'{0}'是否存在。",
                    Util.GetBasePath() + templetFile));
                return;
            }

            try
            {
                stamper = new PdfStamper(rdr, new System.IO.FileStream(pdfFile, System.IO.FileMode.Create));
            }
            catch(Exception ee)
            {
                LogHelper.GetLogger<Report>().Error(ee.Message);
                LogHelper.GetLogger<Report>().Error(ee.StackTrace);
                MessageBox.Show(string.Format("报告文件'{0}'创建失败，请关闭软件重新生成报告。",pdfFile));
                rdr.Close();
                return;
            }
            
            try
            {
                stamper.AcroFields.AddSubstitutionFont(baseFont);

                SetFieldValue(stamper, "pageHead", reportTitle);
                SetFieldValue(stamper, "Head", reportTitle);
                stamper.AcroFields.SetFieldProperty("Head", "textsize", 20.0f, null);

                SetHeadFieldValue(stamper, "ver", string.Format("{0} Build:{1}",version.Ver,version.Build));
                SetHeadFieldValue(stamper, "data", tr.Key + ".trs");
                SetHeadFieldValue(stamper, "gDate", Util.FormateDateTime2(DateTime.Now));

                SetFieldValue(stamper, "tester", tr.Tester);
                SetFieldValue(stamper,"startTime", Util.FormateDateTime(tr.StartTime) );
                SetFieldValue(stamper,"runningTime", Util.FormateDurationSecondsMaxHour(tr.RunningTime) );
                if (tr.IsPass())
                {
                    SetFieldValue(stamper,"IsPass", "PASS");
                    stamper.AcroFields.SetFieldProperty("IsPass", "textsize", 38.0f, null);
                    stamper.AcroFields.SetFieldProperty("IsPass", "textcolor", BaseColor.BLUE,null);
                }
                else
                {
                    SetFieldValue(stamper,"IsPass", "FAIL");
                    stamper.AcroFields.SetFieldProperty("IsPass", "textsize", 38.0f, null);
                    stamper.AcroFields.SetFieldProperty("IsPass", "textcolor", BaseColor.RED, null);
                }
                string rackNo = "";
                string rackName = "";
                string slotNo = "";
                string boardName = "";
                string sn = "";
                string isBoardPass = "";
                foreach (Rack r in tr.Racks)
                {
                    foreach (Board b in r.Boards)
                    {
                        if (!b.IsTested)
                            continue;
                        rackNo += string.Format("{0:D2}\n", r.No);
                        rackName += r.Name + "\n";
                        slotNo += string.Format("{0:D2}\n", b.No);
                        boardName += b.Name + "\n";
                        sn += b.SN + "\n";
                        isBoardPass += b.IsPassed ? "PASS\n":"FAIL\n";
                    }
                }
                
                rackNo += endString;
                rackName += endString;
                slotNo += endString;
                boardName += endString;
                sn += endString;
                isBoardPass += endString;
                SetFieldValue(stamper,"rackNo", rackNo);
                SetFieldValue(stamper,"rackName", rackName);
                SetFieldValue(stamper,"slotNo", slotNo);
                SetFieldValue(stamper,"boardName", boardName);
                SetFieldValue(stamper,"sn", sn);
                SetFieldValue(stamper,"isBoardPass", isBoardPass);

                if (File.Exists(logFile))
                {
                    stamper.AddFileAttachment("Diagnostics logs", null, logFile, "log.txt");
                }                

                stamper.FormFlattening = true;//不允许编辑                
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<Report>().Error(ee.Message);
                LogHelper.GetLogger<Report>().Error(ee.StackTrace);
            }

            try
            {
                stamper.Close();
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<Report>().Error(ee.Message);
                LogHelper.GetLogger<Report>().Error(ee.StackTrace);
            }

            try
            {
                rdr.Close(); 
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<Report>().Error(ee.Message);
                LogHelper.GetLogger<Report>().Error(ee.StackTrace);
            }

            try
            {
                System.Diagnostics.Process.Start(pdfFile);
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<Report>().Error(ee.Message);
                LogHelper.GetLogger<Report>().Error(ee.StackTrace);
            }            
        }

        //public void GeneratePdf(TestedRacks tr)
        //{
        //    string tmplateFile = Util.GetBasePath() + "//Templet//main.xlsx";
        //    string pdfFile = Util.GetBasePath() + "//Report//" + tr.key + ".pdf";
        //    string dataFile = Util.GetBasePath() + "//Report//Data//" + tr.key + ".trs";
        //    string logFile = Util.GetBasePath() + "//Report//Data//" + tr.key + ".log";

        //    LogHelper.GetLogger<Report>().Debug(pdfFile);
        //    LogHelper.GetLogger<Report>().Debug(dataFile);
        //    LogHelper.GetLogger<Report>().Debug(logFile);

        //    try
        //    {
        //        Dictionary<string, object> data = new Dictionary<string, object>();
        //        data.Add("[#title#]", reportTitle);
        //        //data.Add("[#rackNo#]",
        //        List<string> rackNos = new List<string>();
        //        List<string> rackNames = new List<string>();
        //        List<string> slotNos = new List<string>();
        //        List<string> boardNames = new List<string>();
        //        List<string> sns = new List<string>();
        //        List<string> isPass = new List<string>();
        //        foreach (Rack r in tr.Racks)
        //        {
        //            foreach (Board b in r.Boards)
        //            {
        //                if (!b.IsTested)
        //                    continue;
        //                rackNos.Add(string.Format("{0:D2}\n", r.No));
        //                rackNames.Add(r.Name);
        //                slotNos.Add(string.Format("{0:D2}\n", b.No));
        //                boardNames.Add(b.Name);
        //                sns.Add(b.SN);
        //                isPass.Add(b.IsPassed ? "PASS" : "FAIL");
        //            }
        //        }
        //        data.Add("[#rackNo#]", rackNos);
        //        data.Add("[#rackName#]", rackNames);
        //        data.Add("[#slotNo#]", slotNos);
        //        data.Add("[#boardName#]", boardNames);
        //        data.Add("[#sn#]", sns);
        //        data.Add("[#isPass#]", isPass);

        //        Workbook wb = new Workbook(tmplateFile);
        //        Worksheet ws = wb.Worksheets[0];
        //        LogHelper.GetLogger<Report>().Debug(ws.PageSetup.GetFooter(0));
        //        foreach (Cell cell in ws.Cells)
        //        {
        //            LogHelper.GetLogger<Report>().Debug(cell.StringValue);
        //            string key = "[#" + cell.StringValue + "#]";
        //            if (data.ContainsKey(key))
        //            {
        //                if (data[key] is string)
        //                {
        //                    cell.Value = data[key].ToString();
        //                }
        //                else if (data[key] is List<string>)
        //                {
        //                    List<string> list = data[key] as List<string>;
        //                    Cells copyed = Cells;

        //                    ws.Cells.CopyRows(copyed, cell.Row, cell.Row + 1, list.Count - 1);
        //                    ws.Cells.Rows[cell.Row];
        //                }
        //            }
        //        }
        //        wb.Save(pdfFile);
        //    }
        //    catch (Exception ee)
        //    {
        //        LogHelper.GetLogger<Report>().Error(ee.StackTrace);
        //    }

        //    System.Diagnostics.Process.Start(pdfFile);
        //}

        private void SetHeadFieldValue(PdfStamper stamper, string name, string value)
        {
            stamper.AcroFields.SetField(name, value);
            stamper.AcroFields.SetFieldProperty(name, "textsize", fontSizeHead, null);
            stamper.AcroFields.SetFieldProperty(name, "textcolor", BaseColor.GRAY, null);
        }

        private void SetFieldValue(PdfStamper stamper,string name, string value)
        {
            stamper.AcroFields.SetField(name, value);
            stamper.AcroFields.SetFieldProperty(name, "textsize", fontSizeBody, null);
        }
    }
}
