using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace IgnitionHeaderTool;

internal class Program
{
    static string CreateMD5( string input )
    {
        using ( MD5 md5 = MD5.Create() )
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes( input );
            byte[] hashBytes = md5.ComputeHash( inputBytes );

            return Convert.ToHexString( hashBytes );
        }
    }

    static string GetPathForIncludeFile( string includeFile )
    {
        int publicIndex = includeFile.LastIndexOf( "Public" );
        int privateIndex = includeFile.LastIndexOf( "Private" );

        string subPath = string.Empty;
        if ( publicIndex != -1 )
            subPath = includeFile.Substring( publicIndex ).Replace( "Public\\", string.Empty ).Replace( "\\", "/" );

        if ( privateIndex != -1 )
            subPath = includeFile.Substring( privateIndex ).Replace( "Private\\", string.Empty ).Replace( "\\", "/" );

        return subPath;
    }

    static void Main( string[] args )
    {
        Stopwatch sw = new();
        sw.Start();

        FileSystem.FindUProjectPath();

        List<string> modulePaths = FileSystem.GetAllModules();
        List<string> intermediatePaths = FileSystem.GetAllIntermediateFolders( modulePaths );

        int index = 0;
        foreach ( var modulePath in modulePaths )
        {
            if ( !Directory.Exists( $@"{modulePath}\Public" ) )
                continue;

            List<string> includes = new();
            List<string> cppBody = new();

            StringBuilder builder = new();

            bool moduleHasEvents = false;
            foreach ( var headerFile in Directory.GetFiles( $@"{modulePath}\Public", "*.h", SearchOption.AllDirectories ) )
            {
                string fileContents = File.ReadAllText( headerFile );

                string currentFileId = "Ignition_FID_" + headerFile.Substring( 3 ).Replace( ".", "_" ).Replace( "\\", "_" ).Replace( " ", "_" );

                Parser parser = new();
                if ( parser.Parse( fileContents ) )
                {
                    moduleHasEvents = parser.Classes.Count > 0;
                    builder = new();

                    List<string> macroStringBuilders = new();

                    foreach ( UClass uClass in parser.Classes )
                    {
                        builder = new();

                        if ( uClass.Line == 0 )
                            throw new Exception( $"Failed to resolve the line number of IGNITION_BODY for {uClass.Identifier}" );

                        builder.AppendLine( $"\t{{ // START: {uClass.Identifier}" );
                        builder.AppendLine( "\t\tTArray<FIgnitionMethodInfo> __MethodList;" );

                        foreach ( UFunction uFunction in uClass.Methods )
                        {
                            builder.AppendLine( "\t\t{" );
                            builder.AppendLine( "\t\t\tFIgnitionMethodInfo __MethodInfo;" );
                            builder.AppendLine( $"\t\t\t__MethodInfo.EventName = FName( TEXT( \"{uFunction.EventName}\" ) );" );
                            builder.AppendLine( $"\t\t\t__MethodInfo.ParamCount = {uFunction.ParamCount};" );
                            builder.AppendLine( $"\t\t\t__MethodInfo.Function = &{uClass.Identifier}::__IHT_{uFunction.MethodName};" );
                            builder.AppendLine( "\t\t\t__MethodList.Add( __MethodInfo );" );
                            builder.AppendLine( "\t\t}" );
                        }

                        builder.AppendLine();
                        builder.AppendLine( $"\t\tFIgnitionEventSystem::AddClassMethods( {uClass.Identifier}::StaticClass(), __MethodList );" );

                        builder.AppendLine( $"\t}} // END: {uClass.Identifier}" );

                        cppBody.Add( builder.ToString() );

                        builder = new();

                        string macro = currentFileId + $"_{uClass.Line}";
                        builder.AppendLine( $"#define {macro} \\" );
                        builder.AppendLine( "public: \\" );

                        foreach ( UFunction uFunction in uClass.Methods )
                        {
                            builder.AppendLine( $"\tstatic void __IHT_{uFunction.MethodName}( void* __Root, FIgnitionEventParams __Params ) \\" );
                            builder.AppendLine( "\t{ \\" );
                            builder.AppendLine( $"\t\t{uFunction.OwningClass}* __CastedRoot = ({uFunction.OwningClass}*)__Root; \\" );

                            int i = 0;
                            foreach ( var arg in uFunction.Args )
                            {
                                builder.AppendLine( $"\t\t{arg.Type} __Param{i} = {(arg.IsPointer ? string.Empty : "*")}({arg.Type}{(arg.IsPointer ? string.Empty : "*")})__Params[{i}]; \\" );
                                i++;
                            }

                            builder.Append( $"\t\t__CastedRoot->{uFunction.MethodName}(" );

                            for ( i = 0; i < uFunction.Args.Count; i++ )
                            {
                                if ( i == 0 )
                                    builder.Append( " " );

                                builder.Append( $"__Param{i}" );
                                if ( i < uFunction.Args.Count - 1 )
                                    builder.Append( ", " );
                                else
                                    builder.Append( " " );
                            }

                            builder.Append( "); \\\r\n" );
                            builder.AppendLine( "\t} \\" );
                        }

                        builder.AppendLine( "private:" );
                        builder.AppendLine();
                        macroStringBuilders.Add( builder.ToString() );
                    }

                    if ( parser.Classes.Count <= 0 )
                    {
                        if ( fileContents.Contains( ".ignitiongenerated.h" ) )
                        {
                            string includeText = $"#include \"{Path.GetFileNameWithoutExtension( headerFile )}.ignitiongenerated.h\"\r\n";
                            fileContents = fileContents.Replace( includeText, string.Empty );

                            File.WriteAllText( headerFile, fileContents );
                        }

                        continue;
                    }

                    StringBuilder ignitionGeneratedHeader = new();
                    foreach ( var item in macroStringBuilders )
                        ignitionGeneratedHeader.Append( item );

                    ignitionGeneratedHeader.AppendLine();
                    ignitionGeneratedHeader.AppendLine( $"#undef __IGNITION_CURRENT_FILE_ID__" );
                    ignitionGeneratedHeader.AppendLine( $"#define __IGNITION_CURRENT_FILE_ID__ {currentFileId}" );

                    string moduleName = new DirectoryInfo( modulePath ).Name;
                    string uhtPath = $@"{intermediatePaths[ index ]}\Build\Win64\UnrealEditor\Inc\{moduleName}\UHT";
                    if ( Directory.Exists( uhtPath ) )
                    {
                        File.WriteAllText( $@"{uhtPath}\{Path.GetFileNameWithoutExtension( headerFile )}.ignitiongenerated.h", ignitionGeneratedHeader.ToString() );
                    }

                    includes.Add( GetPathForIncludeFile( headerFile ) );

                    if ( !fileContents.Contains( ".ignitiongenerated.h" ) )
                    {
                        string includeText = $"#include \"{Path.GetFileNameWithoutExtension( headerFile )}.generated.h\"";
                        string newIncludeText = $"#include \"{Path.GetFileNameWithoutExtension( headerFile )}.ignitiongenerated.h\"\r\n" + includeText;
                        fileContents = fileContents.Replace( includeText, newIncludeText );

                        File.WriteAllText( headerFile, fileContents );
                    }
                }
                else
                {
                    Console.WriteLine( $"ERROR: IgnitionHeaderTool failed to parse {headerFile}!" );
                    return;
                }
            }

            if ( !moduleHasEvents ) continue;

            {
                string moduleName = new DirectoryInfo( modulePath ).Name;

                builder = new();
                builder.AppendLine( "// Fill out your copyright notice in the Description page of Project Settings." );
                builder.AppendLine();
                builder.AppendLine( "#pragma once" );
                builder.AppendLine();
                builder.AppendLine( "#include \"CoreMinimal.h\"" );
                builder.AppendLine( "#include \"Subsystems/EngineSubsystem.h\"" );
                builder.AppendLine( $"#include \"{moduleName}EventSubsystem.generated.h\"" );
                builder.AppendLine();
                builder.AppendLine( "/**" );
                builder.AppendLine( "*" );
                builder.AppendLine( "*/" );
                builder.AppendLine( "UCLASS()" );
                builder.AppendLine( $"class {moduleName.ToUpper()}_API U{moduleName}EventSubsystem : public UEngineSubsystem" );
                builder.AppendLine( "{" );
                builder.AppendLine( "\tGENERATED_BODY()" );
                builder.AppendLine();
                builder.AppendLine( "public:" );
                builder.AppendLine( "\t//Begin USubsystem" );
                builder.AppendLine( "\tvirtual void Initialize( FSubsystemCollectionBase& Collection ) override;" );
                builder.AppendLine( "\tvirtual void Deinitialize() override;" );
                builder.AppendLine( "\t// End USubsystem" );
                builder.AppendLine( "};" );

                if ( File.Exists( $@"{modulePath}/{moduleName}EventSubsystem.h" ) )
                {
                    string builderHash = CreateMD5( builder.ToString() );
                    string fileHash = CreateMD5( File.ReadAllText( $@"{modulePath}/{moduleName}EventSubsystem.h" ) );

                    if ( builderHash != fileHash )
                        File.WriteAllText( $@"{modulePath}/{moduleName}EventSubsystem.h", builder.ToString() );
                }

                builder = new();
                builder.AppendLine( "// Fill out your copyright notice in the Description page of Project Settings." );
                builder.AppendLine();
                builder.AppendLine( $"#include \"{moduleName}EventSubsystem.h\"" );
                builder.AppendLine( $"#include \"Core/IgnitionEventSystem.h\"" );

                foreach ( var include in includes )
                {
                    builder.AppendLine( $"#include \"{include}\"" );
                }

                builder.AppendLine();
                builder.AppendLine( $"void U{moduleName}EventSubsystem::Initialize( FSubsystemCollectionBase& Collection )" );
                builder.AppendLine( "{" );

                foreach ( var body in cppBody )
                {
                    builder.Append( body );
                }

                builder.AppendLine( "}" );
                builder.AppendLine();
                builder.AppendLine( $"void U{moduleName}EventSubsystem::Deinitialize()" );
                builder.AppendLine( "{" );
                builder.AppendLine( "}" );

                string builderString = builder.ToString();
                string cppFilePath = $@"{modulePath}/{moduleName}EventSubsystem.cpp";

                if ( File.Exists( cppFilePath ) )
                {
                    string builderHash = CreateMD5( builderString );
                    string fileHash = CreateMD5( File.ReadAllText( cppFilePath ) );

                    if ( builderHash == fileHash )
                    {
                        index++;
                        continue;
                    }
                }

                File.WriteAllText( cppFilePath, builder.ToString() );
            }

            index++;
        }

        sw.Stop();
        Console.WriteLine( $"Generated reflection code for Ignition in {sw.Elapsed.TotalSeconds} seconds" );
    }
}