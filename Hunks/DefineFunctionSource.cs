using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool.Hunks;

internal class DefineFunctionSource : ExportHunk
{
    public DefineFunctionSource(FileInfo fileInfo) : base(fileInfo)
    {
        Beacons.Add("// End Cross Module References");
        if (fileInfo.Class == null)
        {
            throw new NotImplementedException();
        }
        HunkText.Add("\tDEFINE_FUNCTION(" + fileInfo.Class.Type?.SourceName + "::execConstructEventParams)");
        HunkText.Add("\t{");
        HunkText.Add("\t\tP_GET_PROPERTY(FNameProperty,Z_Param_InEventName);");
        HunkText.Add("\t\tP_FINISH;");
        HunkText.Add("\t\tP_NATIVE_BEGIN;");
        HunkText.Add("\t\tP_THIS->ConstructEventParams(Z_Param_InEventName);");
        HunkText.Add("\t\tP_NATIVE_END;");
        HunkText.Add("\t}");
        HunkText.Add("\tDEFINE_FUNCTION(" + fileInfo.Class.Type?.SourceName + "::execGetFunctionsForEvent)");
        HunkText.Add("\t{");
        HunkText.Add("\t\tP_GET_PROPERTY(FNameProperty,Z_Param_InEventName);");
        HunkText.Add("\t\tP_FINISH;");
        HunkText.Add("\t\tP_NATIVE_BEGIN;");
        HunkText.Add("\t\tP_THIS->GetFunctionsForEvent(Z_Param_InEventName);");
        HunkText.Add("\t\tP_NATIVE_END;");
        HunkText.Add("\t}");
        HunkText.Add("\tDEFINE_FUNCTION(" + fileInfo.Class.Type?.SourceName + "::execGetEventFunctionNameMap)");
        HunkText.Add("\t{");
        HunkText.Add("\t\tP_FINISH;");
        HunkText.Add("\t\tP_NATIVE_BEGIN;");
        HunkText.Add("\t\tP_THIS->GetEventFunctionNameMap();");
        HunkText.Add("\t\tP_NATIVE_END;");
        HunkText.Add("\t}");
    }
}