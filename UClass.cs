using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal class UClass
{
    internal int Line = 0;
    internal string Identifier = string.Empty;
    internal List<UFunction> Methods = new();
}
