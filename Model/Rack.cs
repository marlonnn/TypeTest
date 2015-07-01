using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeTest.Model
{
    [Serializable]
    public class Rack
    {
        public int No;

        public bool IsEmpty;

        public string Name;

        public string IP;
        public int Port;

        public List<Board> Boards = new List<Board>();
    }
}
