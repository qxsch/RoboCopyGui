using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboCopy
{
    public class RoboCopyCommandResult
    {
        public string StandardOutput = "";
        public string StandardError = "";
        public int ReturnCode = -1;

        public RoboCopyReturnCode ReturnCodeFlags
        {
            get { if (ReturnCode >= 0) return (RoboCopyReturnCode)ReturnCode; else return RoboCopyReturnCode.RoboCopyCommandErrror; }
        }
    }
}
