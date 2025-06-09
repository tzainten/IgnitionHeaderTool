using EpicGames.UHT.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool.Hunks;

internal class EventConstructMap : ExportHunk
{
    public EventConstructMap(FileInfo fileInfo) : base(fileInfo)
    {
        if (fileInfo.Class == null)
        {
            throw new NotImplementedException();
        }

        Beacons.Add($"#define {fileInfo.FileID}_{fileInfo.GeneratedBodyLine}_GENERATED_BODY");
        Beacons.Add("PRAGMA_DISABLE_DEPRECATION_WARNINGS \\");
        Beacons.Add("public:");
        HunkText.Add("\tTMap <FName, TFunction<void( UFunction*, uint8*, void* [] )>> Z_EventConstructMap = { \\");
        foreach (EventMethod method2 in fileInfo.Class.Methods)
        {
            HunkText.Add("\t\t{ \\");
            HunkText.Add("\t\t\t\"" + method2.EventName + "\", \\");
            HunkText.Add("\t\t\tTFunction< void( UFunction*, uint8*, void* [] )>( []( UFunction* Function, uint8* Params, void* EventParams[] ) \\");
            HunkText.Add("\t\t\t{ \\");
            UhtFunction function2 = method2.Function;
            if (function2 != null && function2.Children.Count > 0)
            {
                int idx = 0;
                foreach (UhtProperty child in function2.Children)
                {
                    StringBuilder typeBuilder = new StringBuilder();
                    child.AppendText(typeBuilder, UhtPropertyTextType.Generic);
                    string type2 = typeBuilder.ToString();
                    bool num = type2.Contains("*");
                    if (type2.Contains("*"))
                    {
                        type2 = type2.Replace("*", string.Empty);
                    }
                    string ptr = (num ? string.Empty : "*");
                    /*List<string> hunkText = HunkText;
                    val = new DefaultInterpolatedStringHandler(58, 6);
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("\t\t\t\t*(");
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted<StringBuilder>(typeBuilder);
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("*)( Params + ");
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted<int>(idx);
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral(" * sizeof( ");
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted<StringBuilder>(typeBuilder);
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral(" ) ) = ");
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted(ptr);
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("(");
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted(type2);
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral("*)EventParams[ ");
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendFormatted<int>(idx);
                    ((DefaultInterpolatedStringHandler)(ref val)).AppendLiteral(" ]; \\");
                    hunkText.Add(((DefaultInterpolatedStringHandler)(ref val)).ToStringAndClear());*/

                    HunkText.Add($"\t\t\t\t*({typeBuilder}*)(Params + {idx} * sizeof({typeBuilder})) = {ptr}({type2}*)EventParams[{idx}]; \\");

                    idx++;
                }
            }
            HunkText.Add("\t\t\t} ) \\");
            HunkText.Add("\t\t}, \\");
        }
        HunkText.Add("\t}; \\");
        Dictionary<string, List<string>> eventFunctionNames = new Dictionary<string, List<string>>();
        foreach (EventMethod method in fileInfo.Class.Methods)
        {
            if (method != null && method.EventName != null && method.Function != null)
            {
                UhtFunction function = method.Function;
                if (!eventFunctionNames.ContainsKey(method.EventName))
                {
                    eventFunctionNames.Add(method.EventName, new List<string>());
                }
                eventFunctionNames[method.EventName].Add(function.SourceName);
            }
        }
        HunkText.Add("\tTMap<FName, TArray<FName>> Z_EventFunctionNameMap = { \\");
        foreach (KeyValuePair<string, List<string>> pair in eventFunctionNames)
        {
            HunkText.Add("\t\t{ \\");
            HunkText.Add("\t\t\t\"" + pair.Key + "\", \\");
            HunkText.Add("\t\t\t{ \\");
            foreach (string functionName in pair.Value)
            {
                HunkText.Add("\t\t\t\t\"" + functionName + "\", \\");
            }
            HunkText.Add("\t\t\t} \\");
            HunkText.Add("\t\t}, \\");
        }
        HunkText.Add("\t}; \\");
        HunkText.Add("\tDECLARE_FUNCTION(execConstructEventParams); \\");
        HunkText.Add("\tDECLARE_FUNCTION(execGetFunctionsForEvent); \\");
        HunkText.Add("\tDECLARE_FUNCTION(execGetEventFunctionNameMap); \\");
        HunkText.Add("\tvoid ConstructEventParams(FName InEventName) \\");
        HunkText.Add("\t{ \\");
        HunkText.Add("\t\tif ( FIgnitionEventSystem::Z_CurrentEventParams == nullptr ) return; \\");
        HunkText.Add("\t\tcheck( Z_EventConstructMap.Contains( InEventName ) ); \\");
        HunkText.Add("\t\tcheck( FIgnitionEventSystem::Z_CurrentEventFunction ); \\");
        HunkText.Add("\t\tcheck( FIgnitionEventSystem::Z_CurrentEventParams ); \\");
        HunkText.Add("\t\tZ_EventConstructMap[ InEventName ]( FIgnitionEventSystem::Z_CurrentEventFunction, FIgnitionEventSystem::Z_CurrentParams, FIgnitionEventSystem::Z_CurrentEventParams ); \\");
        HunkText.Add("\t} \\");
        HunkText.Add("\tvoid GetFunctionsForEvent(FName InEventName) \\");
        HunkText.Add("\t{ \\");
        HunkText.Add("\t\tif ( !FIgnitionEventSystem::ClassEventMap.Contains( InEventName ) ) \\");
        HunkText.Add("\t\t\tFIgnitionEventSystem::ClassEventMap.Add( InEventName, {} ); \\");
        HunkText.Add("\t\tTArray<UFunction*> Result; \\");
        HunkText.Add("\t\tfor ( auto Pair : Z_EventFunctionNameMap ) \\");
        HunkText.Add("\t\t{ \\");
        HunkText.Add("\t\t\tfor ( FName FunctionName : Pair.Value ) \\");
        HunkText.Add("\t\t\t{ \\");
        HunkText.Add("\t\t\t\tUFunction* Function = FindFunction( FunctionName ); \\");
        HunkText.Add("\t\t\t\tif ( !Function ) continue; \\");
        HunkText.Add("\t\t\t\t\tResult.Add( Function ); \\");
        HunkText.Add("\t\t\t} \\");
        HunkText.Add("\t\t} \\");
        HunkText.Add("\t} \\");
        HunkText.Add("\tvoid GetEventFunctionNameMap() \\");
        HunkText.Add("\t{ \\");
        HunkText.Add("\t\tIGNITION_LOG(Display, \"GetEventFunctionNameMap\"); \\");
        HunkText.Add("\t\tFIgnitionEventSystem::Z_CurrentEventFunctionNameMap = &Z_EventFunctionNameMap; \\");
        HunkText.Add("\t} \\");
    }
}