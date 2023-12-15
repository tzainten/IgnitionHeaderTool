using System.Diagnostics;
using System.Text;

namespace IgnitionHeaderTool;

internal class Program
{
    static void Main(string[] args)
    {
        Stopwatch sw = new();
        sw.Start();

        FileSystem.FindUProjectPath();

        List<string> modulePaths = FileSystem.GetAllModules();
        List<string> intermediatePaths = FileSystem.GetAllIntermediateFolders(modulePaths);

        int index = 0;
        foreach (var modulePath in modulePaths)
        {
            if (!Directory.Exists($@"{modulePath}\Public"))
                continue;

            List<string> includes = new();
            List<string> cppBody = new();

            StringBuilder builder = new();

            foreach (var headerFile in Directory.GetFiles($@"{modulePath}\Public", "*.h", SearchOption.AllDirectories))
            {
                string fileContents = File.ReadAllText(headerFile);

                Parser parser = new();
                if (parser.Parse(fileContents))
                {
                    builder = new();

                    foreach (UClass uClass in parser.Classes)
                    {
                        builder.AppendLine($"\t{{ // START: {uClass.Identifier}");
                        builder.AppendLine("\t\tTArray<FIgnitionMethodInfo> __MethodList;");

                        foreach (UFunction uFunction in uClass.Methods)
                        {
                            builder.AppendLine("\t\t{");
                            builder.AppendLine("\t\t\tFIgnitionMethodInfo __MethodInfo;");
                            builder.AppendLine($"\t\t\t__MethodInfo.EventName = FName( TEXT( \"{uFunction.EventName}\" ) );");
                            builder.AppendLine($"\t\t\t__MethodInfo.ParamCount = {uFunction.ParamCount};");
                            builder.AppendLine($"\t\t\t__MethodInfo.Function = &{uClass.Identifier}::__IHT_{uFunction.MethodName};");
                            builder.AppendLine("\t\t\t__MethodList.Add( __MethodInfo );");
                            builder.AppendLine("\t\t}");
                        }

                        builder.AppendLine();
                        builder.AppendLine($"\t\tFIgnitionEventSystem::AddClassMethods( {uClass.Identifier}::StaticClass(), __MethodList );");

                        builder.AppendLine($"\t}} // END: {uClass.Identifier}");

                        cppBody.Add(builder.ToString());

                        builder = new();
                        builder.AppendLine("#undef __IGNITION_GENERATED_BODY");
                        builder.AppendLine("#define __IGNITION_GENERATED_BODY \\");
                        builder.AppendLine("public: \\");

                        foreach (UFunction uFunction in uClass.Methods)
                        {
                            builder.AppendLine($"\tstatic void __IHT_{uFunction.MethodName}( void* __Root, FIgnitionEventParams __Params ) \\");
                            builder.AppendLine("\t{ \\");
                            builder.AppendLine($"\t\t{uFunction.OwningClass}* __CastedRoot = ({uFunction.OwningClass}*)__Root; \\");

                            int i = 0;
                            foreach (var arg in uFunction.Args)
                            {
                                builder.AppendLine($"\t\t{arg.Type} __Param{i} = {(arg.IsPointer ? string.Empty : "*")}({arg.Type}{(arg.IsPointer ? string.Empty : "*")})__Params[{i}]; \\");
                                i++;
                            }

                            builder.Append($"\t\t__CastedRoot->{uFunction.MethodName}(");

                            for (i = 0; i < uFunction.Args.Count; i++)
                            {
                                if (i == 0)
                                    builder.Append(" ");

                                builder.Append($"__Param{i}");
                                if (i < uFunction.Args.Count - 1)
                                    builder.Append(", ");
                                else
                                    builder.Append(" ");
                            }

                            builder.Append("); \\\r\n");
                            builder.AppendLine("\t} \\");
                        }

                        builder.AppendLine("private:");

                        string moduleName = new DirectoryInfo(modulePath).Name;
                        string uhtPath = $@"{intermediatePaths[index]}\Build\Win64\UnrealEditor\Inc\{moduleName}\UHT";
                        if (Directory.Exists(uhtPath))
                        {
                            File.WriteAllText($@"{uhtPath}\{Path.GetFileNameWithoutExtension(headerFile)}.ignitiongenerated.h", builder.ToString());
                        }
                    }

                    if (parser.Classes.Count <= 0)
                        continue;

                    includes.Add(Path.GetFileName(headerFile));

                    if (!fileContents.Contains(".ignitiongenerated.h"))
                    {
                        string includeText = $"#include \"{Path.GetFileNameWithoutExtension(headerFile)}.generated.h\"";
                        string newIncludeText = $"#include \"{Path.GetFileNameWithoutExtension(headerFile)}.ignitiongenerated.h\"\r\n" + includeText;
                        fileContents = fileContents.Replace(includeText, newIncludeText);

                        File.WriteAllText(headerFile, fileContents);
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: IgnitionHeaderTool failed to parse {headerFile}!");
                    return;
                }
            }

            if (includes.Count > 0)
            {
                string moduleName = new DirectoryInfo(modulePath).Name;

                builder = new();
                if (!File.Exists($@"{moduleName}EventSubsystem.h"))
                {
                    builder.AppendLine("// Fill out your copyright notice in the Description page of Project Settings.");
                    builder.AppendLine();
                    builder.AppendLine("#pragma once");
                    builder.AppendLine();
                    builder.AppendLine("#include \"CoreMinimal.h\"");
                    builder.AppendLine("#include \"Subsystems/GameInstanceSubsystem.h\"");
                    builder.AppendLine($"#include \"{moduleName}EventSubsystem.generated.h\"");
                    builder.AppendLine();
                    builder.AppendLine("/**");
                    builder.AppendLine("*");
                    builder.AppendLine("*/");
                    builder.AppendLine("UCLASS()");
                    builder.AppendLine($"class {moduleName.ToUpper()}_API U{moduleName}EventSubsystem : public UGameInstanceSubsystem");
                    builder.AppendLine("{");
                    builder.AppendLine("\tGENERATED_BODY()");
                    builder.AppendLine();
                    builder.AppendLine("public:");
                    builder.AppendLine("\t//Begin USubsystem");
                    builder.AppendLine("\tvirtual void Initialize( FSubsystemCollectionBase& Collection ) override;");
                    builder.AppendLine("\tvirtual void Deinitialize() override;");
                    builder.AppendLine("\t// End USubsystem");
                    builder.AppendLine("};");

                    File.WriteAllText($@"{modulePath}/{moduleName}EventSubsystem.h", builder.ToString());
                }

                builder = new();
                builder.AppendLine("// Fill out your copyright notice in the Description page of Project Settings.");
                builder.AppendLine();
                builder.AppendLine($"#include \"{moduleName}EventSubsystem.h\"");
                builder.AppendLine($"#include \"IgnitionEventSystem.h\"");

                foreach (var include in includes)
                {
                    builder.AppendLine($"#include \"{include}\"");
                }

                builder.AppendLine();
                builder.AppendLine($"void U{moduleName}EventSubsystem::Initialize( FSubsystemCollectionBase& Collection )");
                builder.AppendLine("{");

                foreach (var body in cppBody)
                {
                    builder.Append(body);
                }

                builder.AppendLine("}");
                builder.AppendLine();
                builder.AppendLine($"void U{moduleName}EventSubsystem::Deinitialize()");
                builder.AppendLine("{");
                builder.AppendLine("}");

                File.WriteAllText($@"{modulePath}/{moduleName}EventSubsystem.cpp", builder.ToString());
            }

            index++;
        }

        sw.Stop();
        Console.WriteLine($"Generated reflection code for Ignition in {sw.Elapsed.TotalSeconds} seconds");
    }
}
