using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeTest.Model
{
    [Serializable]
    public class Board
    {
        public bool IsTested;

        public bool IsEmpty;

        public int No;

        public string Name;

        public string SN;

        public bool IsPassed;
    }
}
