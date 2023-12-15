using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal class UFunction
{
    internal string OwningClass = string.Empty;
    internal string MethodName = string.Empty;
    internal string EventName = string.Empty;

    internal List<UArg> Args = new();
    internal int ParamCount => Args.Count;
}
