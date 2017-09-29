using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboCopy
{
    [Flags]
    public enum RoboCopyCopyFlags
    {
        Data = 1,
        Attributes = 2,
        Timestamps = 4,
        Acls = 8,
        OwnerInfo = 16,
        AuditingInfo = 32
    }
}
