using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHeaderTool;

internal struct HunkInfo
{
    public HunkQueryResult Result;
    public int Line;
}

internal enum HunkQueryResult : int
{
    Invalid = 0,
    NoBrace,
    FoundBrace
}

internal class ExportHunk
{
    public FileInfo FileInfo;

    public int BeaconLine = 0;
    public int Line = 0;

    public bool IsHeader = true;

    public List<string> Beacons = new();
    public List<string> HunkText = new();

    public string MetaData = string.Empty;

    public static string OpenBrace = "/* {*/ \\";
    public static string CloseBrace = "/* }*/ \\";

    public ExportHunk(FileInfo fileInfo)
    {
        FileInfo = fileInfo;
    }

    public HunkInfo FindHunkText(int line)
    {
        FileInfo.Contents = File.ReadAllLines(FileInfo.Path);
        string content = FileInfo.Contents[line++];

        int i = 0;
        if (content.Contains(Beacons[i]))
        {
            for (i++; i < Beacons.Count; i++, line++)
            {
                content = FileInfo.Contents[line];

                if (!content.Contains(Beacons[i]))
                    return new HunkInfo
                    {
                        Result = HunkQueryResult.Invalid
                    };
            }

            return new HunkInfo
            {
                Result = !FileInfo.Contents[line].Contains(OpenBrace) ? HunkQueryResult.NoBrace : HunkQueryResult.FoundBrace,
                Line = line - 1
            };
        }
        else
        {
            return new HunkInfo
            {
                Result = HunkQueryResult.Invalid
            };
        }
    }

    public void WriteHunk()
    {
        FileInfo.Contents = File.ReadAllLines(FileInfo.Path);

        List<string> lines = new();

        for (int i = 0; i < BeaconLine + 1; i++)
        {
            lines.Add(FileInfo.Contents[i]);
        }

        lines.Add(OpenBrace);
        foreach (string line in HunkText)
            lines.Add(line);
        lines.Add(CloseBrace);

        for (int i = BeaconLine + 1; i < FileInfo.Contents.Length; i++)
        {
            lines.Add(FileInfo.Contents[i]);
        }

        File.WriteAllLines(FileInfo.Path, lines.ToArray());
    }

    public void OverwriteHunk()
    {
        FileInfo.Contents = File.ReadAllLines(FileInfo.Path);

        List<string> hunkLines = new();

        hunkLines.Add(OpenBrace);
        foreach (string line in HunkText)
            hunkLines.Add(line);
        hunkLines.Add(CloseBrace);

        bool shouldOverwrite = false;

        int i = BeaconLine + 1;
        int j = 0;
        int endLine = 0;
        while (true)
        {
            string content = FileInfo.Contents[i++];

            if (hunkLines[j++] != content)
            {
                shouldOverwrite = true;
            }

            if (content.Contains(CloseBrace))
            {
                endLine = i;
                break;
            }
        }

        if (shouldOverwrite)
        {
            List<string> lines = new();
            for (int line = 0; line < BeaconLine + 1; line++)
            {
                lines.Add(FileInfo.Contents[line]);
            }

            lines.Add(OpenBrace);
            foreach (string line in HunkText)
                lines.Add(line);
            lines.Add(CloseBrace);

            for (int line = endLine; line < FileInfo.Contents.Length; line++)
            {
                lines.Add(FileInfo.Contents[line]);
            }

            foreach (var line in lines)
                Console.WriteLine(line);

            File.WriteAllLines(FileInfo.Path, lines);
        }

        Console.WriteLine(shouldOverwrite);
    }
};