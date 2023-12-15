using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal struct Cursor
{
    internal int Position = 0;
    internal int Line = 0;
    internal int Column = 0;

    public Cursor() { }
}