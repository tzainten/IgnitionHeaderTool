using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EpicGames.Core;
using EpicGames.UHT.Parsers;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Tokenizer;
using EpicGames.UHT.Types;
using EpicGames.UHT.Utils;

namespace IgnitionHeaderTool;

[UnrealHeaderTool]
[UhtEngineClass( Name = "Float32Property", IsProperty = true )]
public class UhtFloat32Property : UhtNumericProperty
{
    /// <inheritdoc/>
    public override string EngineClassName => "FloatProperty";

    /// <inheritdoc/>
    protected override string CppTypeText => "float32";

    /// <summary>
    /// Construct a new property
    /// </summary>
    /// <param name="propertySettings">Property settings</param>
    public UhtFloat32Property( UhtPropertySettings propertySettings ) : base( propertySettings )
    {
        PropertyCaps |= UhtPropertyCaps.CanExposeOnSpawn | UhtPropertyCaps.IsParameterSupportedByBlueprint | UhtPropertyCaps.IsMemberSupportedByBlueprint | UhtPropertyCaps.SupportsRigVM;
    }

    /// <inheritdoc/>
    public override StringBuilder AppendMemberDecl( StringBuilder builder, IUhtPropertyMemberContext context, string name, string nameSuffix, int tabs )
    {
        return AppendMemberDecl( builder, context, name, nameSuffix, tabs, "FFloatPropertyParams" );
    }

    /// <inheritdoc/>
    public override StringBuilder AppendMemberDef( StringBuilder builder, IUhtPropertyMemberContext context, string name, string nameSuffix, string? offset, int tabs )
    {
        AppendMemberDefStart( builder, context, name, nameSuffix, offset, tabs, "FFloatPropertyParams", "UECodeGen_Private::EPropertyGenFlags::Float" );
        AppendMemberDefEnd( builder, context, name, nameSuffix );
        return builder;
    }

    /// <inheritdoc/>
    public override bool SanitizeDefaultValue( IUhtTokenReader defaultValueReader, StringBuilder innerDefaultValue )
    {
        innerDefaultValue.AppendFormat( CultureInfo.InvariantCulture, "{0:F6}", defaultValueReader.GetConstFloatExpression() );
        return true;
    }

    /// <inheritdoc/>
    public override bool IsSameType( UhtProperty other )
    {
        return other is UhtFloatProperty;
    }

    #region Keywords
    [UhtPropertyType( Keyword = "float32", Options = UhtPropertyTypeOptions.Simple | UhtPropertyTypeOptions.Immediate )]
    [SuppressMessage( "CodeQuality", "IDE0051:Remove unused private members", Justification = "Attribute accessed method" )]
    [SuppressMessage( "Style", "IDE0060:Remove unused parameter", Justification = "Attribute accessed method" )]
    private static UhtProperty? FloatProperty( UhtPropertyResolvePhase resolvePhase, UhtPropertySettings propertySettings, IUhtTokenReader tokenReader, UhtToken matchedToken )
    {
        return new UhtFloatProperty( propertySettings );
    }
    #endregion
}

[UnrealHeaderTool]
[UhtEngineClass( Name = "Float64Property", IsProperty = true )]
public class UhtFloat64Property : UhtNumericProperty
{
    /// <inheritdoc/>
    public override string EngineClassName => "DoubleProperty";

    /// <inheritdoc/>
    protected override string CppTypeText => "float64";

    /// <summary>
    /// Construct a new property
    /// </summary>
    /// <param name="propertySettings">Property settings</param>
    public UhtFloat64Property( UhtPropertySettings propertySettings ) : base( propertySettings )
    {
        PropertyCaps |= UhtPropertyCaps.IsParameterSupportedByBlueprint | UhtPropertyCaps.IsMemberSupportedByBlueprint | UhtPropertyCaps.SupportsRigVM;
    }

    /// <inheritdoc/>
    public override StringBuilder AppendMemberDecl( StringBuilder builder, IUhtPropertyMemberContext context, string name, string nameSuffix, int tabs )
    {
        return AppendMemberDecl( builder, context, name, nameSuffix, tabs, "FDoublePropertyParams" );
    }

    /// <inheritdoc/>
    public override StringBuilder AppendMemberDef( StringBuilder builder, IUhtPropertyMemberContext context, string name, string nameSuffix, string? offset, int tabs )
    {
        AppendMemberDefStart(builder, context, name, nameSuffix, offset, tabs, "FDoublePropertyParams", "UECodeGen_Private::EPropertyGenFlags::Double");
        AppendMemberDefEnd(builder, context, name, nameSuffix);
        return builder;
    }

    /// <inheritdoc/>
    public override bool SanitizeDefaultValue( IUhtTokenReader defaultValueReader, StringBuilder innerDefaultValue )
    {
        innerDefaultValue.AppendFormat( CultureInfo.InvariantCulture, "{0:F6}", defaultValueReader.GetConstFloatExpression() );
        return true;
    }

    /// <inheritdoc/>
    public override bool IsSameType( UhtProperty other )
    {
        return other is UhtDoubleProperty;
    }

    #region Keywords
    [UhtPropertyType( Keyword = "float64", Options = UhtPropertyTypeOptions.Simple | UhtPropertyTypeOptions.Immediate )]
    [SuppressMessage( "CodeQuality", "IDE0051:Remove unused private members", Justification = "Attribute accessed method" )]
    [SuppressMessage( "Style", "IDE0060:Remove unused parameter", Justification = "Attribute accessed method" )]
    private static UhtProperty? Float64Property( UhtPropertyResolvePhase resolvePhase, UhtPropertySettings propertySettings, IUhtTokenReader tokenReader, UhtToken matchedToken )
    {
        return new UhtFloat64Property( propertySettings );
    }
    #endregion
}

public class EventMethod
{
    public string EventName = string.Empty;

    public UhtFunction Function;

    public EventMethod( UhtFunction function )
    {
        Function = function;
    }
}

public class EventClass
{
    public string? CppClassName;
    public int GeneratedBodyLineNumber = -1;

    public UhtType? Type;
    public UhtHeaderFile? CppHeaderFile;

    public string? Module;
    public string? ModuleBaseDirectory;

    public List<EventMethod> Methods = new();
}

/// <summary>
/// Contains a UHT exporter that generates a reference list containing every specifier and metadata used in the engine
/// and all modules.
/// </summary>
[UnrealHeaderTool]
public static class IgnitionHeaderTool
{
    /// <summary>
    /// The name of the plugin that contains this tool and the viewer module.
    /// </summary>
    private const string PluginName = "Ignition";

    /// <summary>
    /// The name of the Unreal module in the plugin. The generated files will appear under this module's Intermediate
    /// directory. This allows the viewer to access the generated reference list.
    /// </summary>
    private const string ViewerModuleName = "Ignition";

    /// <summary>
    /// The name of this UHT exporter.
    /// </summary>
    private const string ExporterName = "IgnitionHeaderTool";

    private static Dictionary<string, EventClass> EventClasses = new();

    [UhtSpecifier(Extends = UhtTableNames.Function, ValueType = UhtSpecifierValueType.Legacy)]
    private static void DefaultEventSpecifier(UhtSpecifierContext specifierContext)
    {
        UhtFunction function = (UhtFunction)specifierContext.Type;
        
        EFunctionFlags flag = (EFunctionFlags)0x00000020;
        function.FunctionFlags |= flag;
    }
    
    [UhtSpecifier( Extends = UhtTableNames.Function, ValueType = UhtSpecifierValueType.String, When = UhtSpecifierWhen.Immediate )]
    private static void EventSpecifier( UhtSpecifierContext specifierContext, StringView value )
    {
        UhtFunction function = (UhtFunction)specifierContext.Type;

        EFunctionFlags flag = (EFunctionFlags)0x00000010;
        function.FunctionFlags |= flag;

        if ( function.Outer is not null )
        {
            string cppName = function.Outer.ToString();

            if ( !EventClasses.ContainsKey( cppName ) )
            {
                EventClass eventClass = new();
                eventClass.CppClassName = cppName;
                eventClass.Type = function.Outer;

                if ( eventClass.Type is UhtClass )
                {
                    UhtClass? uhtClass = eventClass.Type as UhtClass;
                    eventClass.GeneratedBodyLineNumber = uhtClass?.GeneratedBodyLineNumber ?? -1;
                }

                eventClass.CppHeaderFile = function.Outer.HeaderFile;

                UhtPackage? package = (UhtPackage?)function.Outer.Outer?.Outer;
                eventClass.ModuleBaseDirectory = package?.Module.BaseDirectory;
                eventClass.Module = package?.Module.ToString();

                EventMethod eventMethod = new( function );
                eventMethod.Function = function;
                eventMethod.EventName = value.ToString();

                eventClass.Methods.Add( eventMethod );

                EventClasses.Add( cppName, eventClass );
            }
            else
            {
                EventClass eventClass = EventClasses[ cppName ];
                EventMethod eventMethod = new( function );
                eventMethod.Function = function;
                eventMethod.EventName = value.ToString();

                eventClass.Methods.Add( eventMethod );
            }
        }
    }

    /// <summary>
    /// Exports a reference list of specifiers and metadata used in the source code.
    /// </summary>
    [UhtExporter(Name = ExporterName, ModuleName = ViewerModuleName, Options = UhtExporterOptions.Default)]
    public static void GenerateReferenceList(IUhtExportFactory factory)
    {
        DateTime startTime = DateTime.Now;

        Console.WriteLine("  Running IgnitionHeaderTool");

        if (EventClasses.Count == 0)
        {
            Console.WriteLine("    Found no events!");
            return;
        }

        int i = 0;
        foreach (var elem in EventClasses)
        {
            i++;
            if (elem.Value.ModuleBaseDirectory is null)
                continue;

            if (factory.Session.Manifest == null)
            {
                throw new NotImplementedException();
            }

            bool isEditor = factory.Session.Manifest.TargetName.EndsWith("Editor");

            bool foundIntermediateFolder = false;

            string path = elem.Value.ModuleBaseDirectory.ToString();
            while (!foundIntermediateFolder)
            {
                DirectoryInfo? directoryInfo = Directory.GetParent(path);
                if (directoryInfo is null)
                    break;

                path = directoryInfo.ToString();

                foundIntermediateFolder = Directory.Exists(path + "\\Intermediate");
                if (foundIntermediateFolder)
                {
                    string folder = (isEditor ? "UnrealEditor" : "UnrealGame");
                    path += $"\\Intermediate\\Build\\Win64\\{folder}\\Inc\\{elem.Value.Module}\\UHT";
                }
            }

            string header = path + $"\\{elem.Value.Type?.EngineName}.generated.h";
            string source = path + $"\\{elem.Value.Type?.EngineName}.gen.cpp";

            string currentFileId = "FID_" + elem.Value.CppHeaderFile?.FilePath.Substring(3).Replace(".", "_")
                .Replace("\\", "_").Replace(" ", "_");

            Exporter exporter = new(new()
            {
                Contents = File.ReadAllLines(header),
                Class = elem.Value,
                Path = header,
                FileID = currentFileId,
                GeneratedBodyLine = elem.Value.GeneratedBodyLineNumber
            }, new()
            {
                Contents = File.ReadAllLines(source),
                Class = elem.Value,
                Path = source,
                FileID = currentFileId,
                GeneratedBodyLine = elem.Value.GeneratedBodyLineNumber
            });
            exporter.Export();

            Console.WriteLine($"    [{i}/{EventClasses.Count}] Write reflection code for {elem.Value.CppHeaderFile}");
        }
    }
}