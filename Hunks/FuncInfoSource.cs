using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool.Hunks;

internal class FuncInfoSource : ExportHunk
{
    public FuncInfoSource(FileInfo fileInfo) : base(fileInfo)
    {
        if (fileInfo.Class == null)
        {
            throw new NotImplementedException();
        }
        Beacons.Add("static constexpr FClassFunctionLinkInfo FuncInfo[] = {");
        /*DefaultInterpolatedStringHandler val = default(DefaultInterpolatedStringHandler);
        ((DefaultInterpolatedStringHandler)(ref val))..ctor(235, 3);
        ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("\t\t{ &Z_Construct_UFunction_");
        ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted(fileInfo.Class.Type?.SourceName);
        ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("_ConstructEventParams, \"ConstructEventParams\" },\r\n\t\t{ &Z_Construct_UFunction_");
        ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted(fileInfo.Class.Type?.SourceName);
        ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("_GetFunctionsForEvent, \"GetFunctionsForEvent\" },\r\n\t\t{ &Z_Construct_UFunction_");
        ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted(fileInfo.Class.Type?.SourceName);
        ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("_GetEventFunctionNameMap, \"GetEventFunctionNameMap\" },");
        string text = ((DefaultInterpolatedStringHandler)(ref val)).ToStringAndClear();*/
        HunkText.Add($"\t\t{{ &Z_Construct_UFunction_{fileInfo.Class.Type?.SourceName}_ConstructEventParams, \"ConstructEventParams\" }},\r\n\t\t{{ &Z_Construct_UFunction_{fileInfo.Class.Type?.SourceName}_GetFunctionsForEvent, \"GetFunctionsForEvent\" }},\r\n\t\t{{ &Z_Construct_UFunction_{fileInfo.Class.Type?.SourceName}_GetEventFunctionNameMap, \"GetEventFunctionNameMap\" }},");
    }
}