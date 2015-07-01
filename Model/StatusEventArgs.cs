using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeTest.Common;

namespace TypeTest.Model
{
    public class StatusEventArgs : EventArgs
    {
        public List<Board> Boards;
        public TestStatus TestStatus;
    }
}
