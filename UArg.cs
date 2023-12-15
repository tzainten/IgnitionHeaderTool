using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal class UArg
{
    internal string Type = string.Empty;
    internal bool IsPointer => Type.Contains("*");
}
