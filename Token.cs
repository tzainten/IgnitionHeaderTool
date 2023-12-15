namespace IgnitionHeaderTool;

internal enum TokenType : uint
{
    None = 0,
    Real
}

internal class Token
{
    internal string Identifier = String.Empty;
    internal TokenType Type = TokenType.None;

    internal int StartPosition = 0;
    internal int StartLine = 0;
}