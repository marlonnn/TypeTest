using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeTest.Model
{
    [Serializable]
    public class ErrorDescription
    {
        Dictionary<int, string> dict;

        public string GetDescription(int code)
        {
            if (dict.ContainsKey(code))
            {
                return dict[code];
            }
            return "未知错误码";
        }
    }
}
