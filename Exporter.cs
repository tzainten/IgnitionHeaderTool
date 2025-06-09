using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IgnitionHeaderTool.Hunks;

namespace IgnitionHeaderTool;

internal class FileInfo
{
    public string Path = string.Empty;
    public string[]? Contents;
    public string FileID = string.Empty;
    public int GeneratedBodyLine = -1;
    public EventClass? Class = null;
}

internal class Exporter
{
    List<ExportHunk> HeaderHunks = new();
    List<ExportHunk> SourceHunks = new();

    public FileInfo HeaderFile;
    public FileInfo SourceFile;

    public Exporter(FileInfo headerFile, FileInfo sourceFile)
    {
        HeaderFile = headerFile;
        SourceFile = sourceFile;

        HeaderHunks.Add(new IncludeHeaderFile(headerFile));
        HeaderHunks.Add(new EventConstructMap(headerFile));

        SourceHunks.Add(new DefineFunctionSource(sourceFile));
        SourceHunks.Add(new RegisterFunctionsSource(sourceFile));
        SourceHunks.Add(new FuncInfoSource(sourceFile));
        SourceHunks.Add(new FunctionArraySource(sourceFile));
    }

    public void Export()
    {
        foreach (var hunk in HeaderHunks)
        {
            HeaderFile.Contents = File.ReadAllLines(HeaderFile.Path);

            int line = 0;
            foreach (var content in HeaderFile.Contents)
            {
                HunkInfo info = hunk.FindHunkText(line);
                if (info.Result != HunkQueryResult.Invalid)
                {
                    hunk.BeaconLine = info.Line;
                    if (info.Result == HunkQueryResult.NoBrace)
                    {
                        hunk.WriteHunk();
                    }
                    else
                    {
                        hunk.OverwriteHunk();
                    }

                    break;
                }

                line++;
            }
        }

        foreach (var hunk in SourceHunks)
        {
            SourceFile.Contents = File.ReadAllLines(SourceFile.Path);

            int line = 0;
            foreach (var content in SourceFile.Contents)
            {
                HunkInfo info = hunk.FindHunkText(line);
                if (info.Result != HunkQueryResult.Invalid)
                {
                    hunk.BeaconLine = info.Line;
                    if (info.Result == HunkQueryResult.NoBrace)
                    {
                        hunk.WriteHunk();
                    }
                    else
                    {
                        hunk.OverwriteHunk();
                    }

                    break;
                }

                line++;
            }
        }
    }
}