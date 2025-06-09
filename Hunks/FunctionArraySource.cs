using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool.Hunks;

internal class FunctionArraySource : ExportHunk
{
    public FunctionArraySource(FileInfo fileInfo) : base(fileInfo)
    {
        Beacons.Add("static const FNameNativePtrPair Funcs[] = {");
        if (fileInfo.Class == null)
        {
            throw new NotImplementedException();
        }
        HunkText.Add("\t\t\t{ \"ConstructEventParams\", &" + fileInfo.Class.Type?.SourceName + "::execConstructEventParams },");
        HunkText.Add("\t\t\t{ \"GetEventFunctionNameMap\", &" + fileInfo.Class.Type?.SourceName + "::execGetEventFunctionNameMap },");
    }
}