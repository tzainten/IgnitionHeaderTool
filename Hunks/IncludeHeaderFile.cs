using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool.Hunks;

internal class IncludeHeaderFile : ExportHunk
{
    public IncludeHeaderFile(FileInfo fileInfo) : base(fileInfo)
    {
        Beacons.Add("#include \"UObject/ScriptMacros.h\"");
        HunkText.Add("#include \"IgnitionMinimal.h\"");
    }
}