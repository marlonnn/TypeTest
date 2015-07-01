using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TypeTest.Common;
using TypeTest.UI;
using Summer.System.Log;
using Spring.Context.Support;
using Summer.System.Core;

namespace TypeTest
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                FormMain formMain = SpringHelper.GetObject<FormMain>("formMain");
                Application.Run(formMain);
            }
            catch (Exception ee)
            {
                LogHelper.GetLogger<FormMain>().Error(ee.InnerException.Message);
                LogHelper.GetLogger<FormMain>().Error(ee.InnerException.StackTrace);
            }
        }
    }
}
