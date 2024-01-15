using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal class Parser : Tokenizer
{
    internal List<string> Includes = new();
    internal List<UClass> Classes = new();

    internal bool Parse( string input )
    {
        Reset( input );
        Includes.Clear();

        while ( ParseStatement() ) { }

        return true;
    }

    internal bool ParseStatement()
    {
        Token token = new();

        if ( !GetToken( ref token ) )
            return false;

        if ( token.Identifier.EndsWith( "_API" ) )
        {
            token = new();
            if ( !GetToken( ref token ) )
                return false;
        }

        if ( !ParseDeclaration( token ) )
            return false;

        //Console.WriteLine($"Identifier: {token.Identifier}, StartLine: {token.StartLine + 1}");

        return true;
    }

    internal bool ParseDeclaration( Token token )
    {
        if ( token.Identifier == "#" )
            return ParseDirective();
        if ( token.Identifier.EndsWith( "_API" ) )
            return true;
        else if ( token.Identifier == "UCLASS" )
        {
            int Line = 0;
            string ClassIdentifier = string.Empty;

            List<UFunction> uFunctions = new();
            bool result = ParseUClass( ref uFunctions, ref Line, ref ClassIdentifier );

            if ( ClassIdentifier != string.Empty && uFunctions.Count <= 0 )
            {
                UClass uClass = new();
                uClass.Identifier = ClassIdentifier;
                uClass.Line = Line;
                Classes.Add( uClass );
            }

            if ( uFunctions.Count > 0 )
            {
                /*Console.WriteLine($"{uFunctions[0].OwningClass}:");
                foreach (var item in uFunctions)
                {
                    Console.WriteLine($"\t{item.MethodName} :: {item.EventName}");
                }*/

                UClass uClass = new();
                uClass.Identifier = uFunctions[ 0 ].OwningClass;
                uClass.Methods = uFunctions;
                uClass.Line = Line;
                Classes.Add( uClass );
            }

            return result;
        }
        else
        {
            //Console.WriteLine($"Skipping delcaration for {token.Identifier} at line: {token.StartLine + 1}");
            return SkipDeclaration();
        }
    }

    internal bool ParseUClass( ref List<UFunction> uFunctions, ref int Line, ref string ClassIdentifier )
    {
        Token token = new();
        GetToken( ref token );

        if ( token.Identifier == "(" )
        {
            Token closingToken = new();

            int scopeDepth = 1;
            while ( GetToken( ref closingToken ) )
            {
                if ( closingToken.Identifier == "(" )
                {
                    scopeDepth++;
                }

                if ( closingToken.Identifier == ")" )
                {
                    scopeDepth--;
                    if ( scopeDepth == 0 )
                    {
                        break;
                    }
                }

                closingToken = new();
            }

            Token classToken = new();
            GetToken( ref classToken );

            if ( classToken.Identifier == "class" )
            {
                Token classIdentifier = new();
                GetToken( ref classIdentifier );

                if ( classIdentifier.Identifier.EndsWith( "_API" ) )
                {
                    classIdentifier = new();
                    GetToken( ref classIdentifier );
                }

                int braceScope = 0;
                while ( true )
                {
                    Token memberToken = new();

                    if ( !GetToken( ref memberToken ) )
                        break;

                    if ( memberToken.Identifier == "IGNITION_BODY" )
                    {
                        Line = memberToken.StartLine + 1;
                        ClassIdentifier = classIdentifier.Identifier;
                    }

                    if ( memberToken.Identifier == "UFUNCTION" )
                    {
                        //Console.WriteLine($"Parsing UFUNCTION @ line {memberToken.StartLine + 1}");

                        UFunction uFunction = new();
                        uFunction.OwningClass = classIdentifier.Identifier;

                        if ( ParseUFunction( ref uFunction ) )
                            uFunctions.Add( uFunction );
                    }

                    if ( memberToken.Identifier == "UPROPERTY" )
                    {
                        ParseUProperty();
                    }

                    if ( memberToken.Identifier == "{" )
                        braceScope++;

                    if ( memberToken.Identifier == "}" )
                    {
                        braceScope--;

                        Token semiColonToken = new();
                        if ( !GetToken( ref semiColonToken ) )
                            break;

                        if ( semiColonToken.Identifier == ";" )
                        {
                            //Console.WriteLine($"BREAKING @ line {semiColonToken.StartLine + 1}");
                            if ( braceScope == 0 )
                                break;
                        }
                    }
                }
            }

            return true;
        }

        return false;
    }

    internal void ParseUProperty()
    {
        Token token = new();

        while ( GetToken( ref token ) )
        {
            if ( token.Identifier == "(" )
            {
                Token closingToken = new();

                int scopeDepth = 1;
                while ( GetToken( ref closingToken ) )
                {
                    if ( closingToken.Identifier == "(" )
                    {
                        scopeDepth++;
                    }

                    if ( closingToken.Identifier == ")" )
                    {
                        scopeDepth--;
                        if ( scopeDepth == 0 )
                        {
                            break;
                        }
                    }

                    closingToken = new();
                }

                Token semiColonToken = new();
                GetToken( ref semiColonToken );

                if ( semiColonToken.Identifier != ";" )
                {
                    UngetToken( ref semiColonToken );
                }
            }

            if ( token.Identifier == ";" )
                break;

            token = new();
        }
    }

    internal bool ParseUFunction( ref UFunction uFunction )
    {
        Token token = new();
        GetToken( ref token );

        if ( token.Identifier == "(" )
        {
            Token closingToken = new();

            bool hasEventMeta = false;
            int scopeDepth = 1;
            while ( GetToken( ref closingToken ) )
            {
                if ( closingToken.Identifier == "(" )
                {
                    scopeDepth++;
                }

                if ( closingToken.Identifier == ")" )
                {
                    scopeDepth--;
                    if ( scopeDepth == 0 )
                    {
                        //Console.WriteLine($"CLOSING @ LINE: {closingToken.StartLine + 1}");
                        break;
                    }
                }

                if ( closingToken.Identifier.ToLower() == "event" )
                {
                    hasEventMeta = true;

                    Token equalToken = new();
                    if ( !GetToken( ref equalToken ) )
                        return false;

                    if ( equalToken.Identifier != "=" )
                        return false;

                    Token openQuoteToken = new();
                    if ( !GetToken( ref openQuoteToken ) )
                        return false;

                    if ( openQuoteToken.Identifier != "\"" )
                        return false;

                    Token eventNameToken = new();
                    while ( true )
                    {
                        Token nextToken = new();
                        if ( !GetToken( ref nextToken ) )
                            return false;

                        if ( nextToken.Identifier == "\"" )
                            break;

                        eventNameToken.Identifier += nextToken.Identifier;
                    }

                    uFunction.EventName = eventNameToken.Identifier;
                }

                closingToken = new();
            }

            if ( !hasEventMeta )
            {
                bool shouldLoop = true;
                while ( shouldLoop )
                {
                    Token _token = new();
                    if ( !GetToken( ref _token ) )
                        break;

                    if ( _token.Identifier.EndsWith( "_API" ) )
                    {
                        _token = new();
                        if ( !GetToken( ref _token ) )
                            break;
                    }

                    if ( _token.Identifier == "(" )
                    {
                        while ( true )
                        {
                            Token closingParanthesesToken = new();
                            if ( !GetToken( ref closingParanthesesToken ) )
                            {
                                shouldLoop = false;
                                break;
                            }

                            if ( closingParanthesesToken.Identifier == ")" )
                            {
                                //Console.WriteLine($"close @ {closingParanthesesToken.StartLine + 1}");

                                Token semiColonToken = new();
                                if ( !GetToken( ref semiColonToken ) )
                                {
                                    shouldLoop = false;
                                    break;
                                }

                                if ( semiColonToken.Identifier == ";" )
                                {
                                    shouldLoop = false;
                                    break;
                                }

                                if ( semiColonToken.Identifier == "const" )
                                {
                                    semiColonToken = new();
                                    if ( !GetToken( ref semiColonToken ) )
                                    {
                                        shouldLoop = false;
                                        break;
                                    }

                                    if ( semiColonToken.Identifier == ";" )
                                    {
                                        shouldLoop = false;
                                        break;
                                    }

                                    if ( semiColonToken.Identifier == "{" )
                                    {
                                        int braceScopeDepth = 1;
                                        while ( true )
                                        {
                                            Token closeBraceToken = new();
                                            if ( !GetToken( ref closeBraceToken ) )
                                            {
                                                shouldLoop = false;
                                                break;
                                            }

                                            if ( closeBraceToken.Identifier == "{" )
                                                braceScopeDepth++;

                                            if ( closeBraceToken.Identifier == "}" )
                                            {
                                                braceScopeDepth--;
                                                if ( braceScopeDepth == 0 )
                                                {
                                                    shouldLoop = false;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                /*Console.WriteLine("Returning early!");
                Console.WriteLine(Cursor.Line + 1);*/

                return false;
            }

            Token returnTypeToken = new();
            GetToken( ref returnTypeToken );

            if ( returnTypeToken.Identifier.EndsWith( "_API" ) )
            {
                returnTypeToken = new();
                GetToken( ref returnTypeToken );
            }

            if ( returnTypeToken.Identifier == "virtual" )
            {
                returnTypeToken = new();
                GetToken( ref returnTypeToken );
            }

            if ( returnTypeToken.Identifier != "void" )
            {
                Console.WriteLine( $"ERROR: UFUNCTION @ Line: {returnTypeToken.StartLine + 1} is an event function and must have a return type of void!" );
                return false;
            }

            Token methodNameToken = new();
            GetToken( ref methodNameToken );

            uFunction.MethodName = methodNameToken.Identifier;

            scopeDepth = 0;
            while ( true )
            {
                Token semiColonToken = new();
                if ( !GetToken( ref semiColonToken ) )
                    break;

                // Parse arguments
                if ( semiColonToken.Identifier == "(" )
                {
                    int argsScopeDepth = 0;

                    UArg Arg = new();
                    while ( true )
                    {
                        Token argToken = new();
                        if ( !GetToken( ref argToken ) )
                            break;

                        if ( argToken.Identifier == ")" )
                        {
                            argsScopeDepth--;
                            if ( scopeDepth == 0 )
                            {
                                if ( Arg.Type.Length > 0 )
                                {
                                    string[] argArray = Arg.Type.Split( ' ' );
                                    Arg.Type = string.Empty;
                                    for ( int i = 0; i < argArray.Length - 1; i++ )
                                    {
                                        argArray[ i ] = argArray[ i ].Trim();
                                        Arg.Type += argArray[ i ];
                                    }

                                    uFunction.Args.Add( Arg );
                                }

                                break;
                            }
                        }

                        if ( argToken.Identifier == "," )
                        {
                            if ( Arg.Type.Length > 0 )
                            {
                                string[] argArray = Arg.Type.Split( ' ' );
                                Arg.Type = string.Empty;
                                for ( int i = 0; i < argArray.Length - 1; i++ )
                                {
                                    argArray[ i ] = argArray[ i ].Trim();
                                    Arg.Type += argArray[ i ];
                                }

                                uFunction.Args.Add( Arg );
                            }
                            Arg = new();
                            continue;
                        }

                        Arg.Type += (Arg.Type.Length == 0 ? "" : " ") + argToken.Identifier;
                    }
                }

                if ( semiColonToken.Identifier == "{" )
                    scopeDepth++;

                if ( semiColonToken.Identifier == "}" )
                {
                    scopeDepth--;
                    if ( scopeDepth == 0 )
                        break;
                }

                if ( semiColonToken.Identifier == ";" && scopeDepth == 0 )
                    break;
            }

            return true;
        }

        return false;
    }

    internal bool ParseDirective()
    {
        Token token = new();
        GetToken( ref token );

        Token nextToken = new();
        if ( token.Identifier == "include" )
            return ParseInclude();
        else if ( token.Identifier == "pragma" )
            return ParsePragma( token.StartLine );
        else if ( token.Identifier == "define" )
            return ParseDefine();
        else if ( token.Identifier == "ifdef" || token.Identifier == "ifndef" || token.Identifier == "if" )
            return ParseIf();
        else if ( token.Identifier == "line" )
            return ParseLine();

        return true;
    }

    internal bool ParseLine()
    {
        Token token = new();
        GetToken( ref token );

        int line = token.StartLine;

        while ( GetToken( ref token ) )
        {
            if ( token.StartLine > line )
            {
                UngetToken( ref token );
                break;
            }

            token = new();
        }

        return true;
    }

    internal bool ParseIf()
    {
        int scopeDepth = 1;

        Token token = new();
        Token previousToken = new();

        while ( GetToken( ref token ) )
        {
            if ( previousToken.Identifier == "#" )
            {
                if ( token.Identifier == "ifdef" || token.Identifier == "ifndef" || token.Identifier == "if" )
                    scopeDepth++;

                if ( token.Identifier == "endif" )
                {
                    scopeDepth--;
                    if ( scopeDepth == 0 )
                        break;
                }
            }

            previousToken = token;
            token = new();
        }

        return true;
    }

    internal bool ParsePragma( int startingLine )
    {
        Token token = new();
        while ( true )
        {
            GetToken( ref token );

            if ( token.StartLine > startingLine )
            {
                UngetToken( ref token );
                break;
            }

            token = new();
        }

        return true;
    }

    internal bool ParseInclude()
    {
        Token openToken = new();
        GetToken( ref openToken );

        string include = string.Empty;

        if ( openToken.Identifier == "\"" )
        {

            Token nextToken;
            while ( true )
            {
                nextToken = new();
                GetToken( ref nextToken );

                if ( nextToken.Identifier == "\"" )
                    break;

                include += nextToken.Identifier;
            }

            Includes.Add( include );

            return true;
        }

        if ( openToken.Identifier == "<" )
        {
            Token nextToken = new();
            while ( true )
            {
                nextToken = new();
                GetToken( ref nextToken );

                if ( nextToken.Identifier == ">" )
                    break;

                include += nextToken.Identifier;
            }

            Includes.Add( include );

            return true;
        }

        return false;
    }

    internal bool ParseDefine()
    {
        Token defineIdentifier = new();
        if ( !GetToken( ref defineIdentifier ) )
            return false;

        int line = defineIdentifier.StartLine;

        Token token = new();
        Token previousToken = new();

        while ( GetToken( ref token ) )
        {
            if ( token.StartLine > line )
            {
                if ( previousToken.Identifier == "\\" )
                {
                    line++;
                }
                else
                {
                    UngetToken( ref token );
                    break;
                }
            }

            previousToken = token;
            token = new();
        }

        return true;
    }

    internal bool SkipDeclaration()
    {
        Token token = new();

        int scopeDepth = 0;
        while ( GetToken( ref token ) )
        {
            if ( token.Identifier == ";" && scopeDepth == 0 )
                break;

            if ( token.Identifier == "{" )
                scopeDepth++;

            if ( token.Identifier == "}" )
            {
                scopeDepth--;

                Token semiColonToken = new();
                if ( !GetToken( ref semiColonToken ) )
                    break;

                if ( semiColonToken.Identifier == ";" )
                    break;
                else
                    UngetToken( ref semiColonToken );

                if ( scopeDepth == 0 )
                    break;
            }

            token = new();
        }

        //Console.WriteLine($"\tLanded at line: {token.StartLine + 1}");

        return true;
    }
}