using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;

namespace TypeTest.Model
{
    public class TestStatusEventArgs : EventArgs
    {
        public TestStatus LastStatus;
        public TestStatus CurStatus;
    }
}
