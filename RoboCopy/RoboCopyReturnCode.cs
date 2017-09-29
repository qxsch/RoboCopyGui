using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboCopy
{
    [Flags]
    public enum RoboCopyReturnCode
    {
        
        // One or more files were copied successfully (that is, new files have arrived).
        SomeFilesSuccessFullyCopied = 1,
        // Some Extra files or directories were detected.No files were copied. Examine the output log for details.
        ExtraFilesDetected = 2,
        // Some Mismatched files or directories were detected. Examine the output log. Housekeeping might be required.
        SomeMismatchedFilesDetected = 4,
        // Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.
        FailedToCopySomeFiles = 8,
        // Serious error. Robocopy did not copy any files. Either a usage error or an error due to insufficient access privileges on the source or destination directories.
        SeriousError = 16,
        // Some error in the c# RoboCopyCommand
        RoboCopyCommandErrror = 32768
    }
}
