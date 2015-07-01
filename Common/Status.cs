using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeTest.Common
{
    [Serializable]
    public enum TestStatus
    {
        THRESHOLD,
        UNEXPECTED_FINNISH,
        RUNNING,
        EXPECTED_FINNISH,
    }
}
