using Spectre.Console;
using StuffItReader;

if (args.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Usage: StuffItDumper <archive-file>[/]");
    return 1;
}

var filePath = args[0];
if (!File.Exists(filePath))
{
    AnsiConsole.MarkupLine($"[red]File not found: {filePath}[/]");
    return 1;
}

try
{
    using var stream = File.OpenRead(filePath);
    var archive = new StuffItArchive(stream);

    // Dump archive header
    DumpArchiveHeader(archive);

    // Dump entries
    AnsiConsole.WriteLine();
    DumpEntries(archive);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}

return 0;

static void DumpArchiveHeader(StuffItArchive archive)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Blue)
        .Title($"[bold blue]StuffIt Archive Header (Version {archive.Version})[/]")
        .AddColumn("[grey]Property[/]")
        .AddColumn("[grey]Value[/]");

    if (archive.Version == 1 && archive.HeaderV1.HasValue)
    {
        var header = archive.HeaderV1.Value;
        table.AddRow("Signature1", $"0x{header.Signature1:X8}");
        table.AddRow("Signature2", $"0x{header.Signature2:X8}");
        table.AddRow("Version", header.Version.ToString());
        table.AddRow("Reserved", $"0x{header.Reserved:X2}");
        table.AddRow("Root Entry Count", header.RootDirectoryEntryCount.ToString());
        table.AddRow("Total Size", FormatSize(header.TotalSize));
        table.AddRow("Header Size", header.HeaderSize.ToString());
        table.AddRow("Header CRC", $"0x{header.HeaderCRC:X4}");
    }
    else if (archive.Version == 5 && archive.HeaderV5.HasValue)
    {
        var header = archive.HeaderV5.Value;
        table.AddRow("Signature", Markup.Escape(header.Signature.TrimEnd('\r', '\n')));
        table.AddRow("Version", header.Version.ToString());
        table.AddRow("Flags", FormatFlags(header.Flags));
        table.AddRow("Total Size", FormatSize(header.TotalSize));
        table.AddRow("Root Entry Count", header.RootDirectoryEntryCount.ToString());
        table.AddRow("Root Entry Offset", $"0x{header.RootDirectoryEntryOffset:X8} ({header.RootDirectoryEntryOffset})");
        table.AddRow("Reserved1", $"0x{header.Reserved1:X4}");
        table.AddRow("Reserved2", $"0x{header.Reserved2:X8}");
        table.AddRow("Header CRC", $"0x{header.HeaderCRC:X4}");
    }

    AnsiConsole.Write(table);
}

static void DumpEntries(StuffItArchive archive)
{
    var entries = archive.GetRootEntries();

    var root = new Tree("[bold yellow]Archive Contents[/]");
    foreach (var entry in entries)
    {
        AddEntryToTree(archive, root, entry);
    }

    AnsiConsole.Write(root);
}

static void AddEntryToTree(StuffItArchive archive, IHasTreeNodes parent, StuffItArchiveEntry entry)
{
    if (entry is StuffItArchiveDirectory directory)
    {
        var dirNode = parent.AddNode($"[bold blue]📁 {Markup.Escape(entry.Name)}[/]");
        AddDirectoryDetails(archive, directory, dirNode);

        var childEntries = archive.GetEntries(directory);
        foreach (var child in childEntries)
        {
            AddEntryToTree(archive, dirNode, child);
        }
    }
    else if (entry is StuffItArchiveFile file)
    {
        var fileNode = parent.AddNode($"[green]📄 {Markup.Escape(entry.Name)}[/]");
        AddFileDetails(file, fileNode);
    }
}

static void AddDirectoryDetails(StuffItArchive archive, StuffItArchiveDirectory directory, TreeNode node)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey)
        .AddColumn("[grey]Property[/]")
        .AddColumn("[grey]Value[/]");

    table.AddRow("Entry Count", directory.EntryCount.ToString());
    table.AddRow("Total Size", FormatSize(directory.TotalSize));

    if (directory is StuffItArchiveDirectoryV1 v1Dir)
    {
        AddEntryHeaderV1Details(table, v1Dir.Header);
    }
    else if (directory is StuffItArchiveDirectoryV5 v5Dir)
    {
        AddEntryHeaderV5Details(table, directory.EntryHeaderV5);
        AddDirectoryHeaderV5Details(table, v5Dir.Header);
    }

    node.AddNode(table);
}

static void AddFileDetails(StuffItArchiveFile file, TreeNode node)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey)
        .AddColumn("[grey]Property[/]")
        .AddColumn("[grey]Value[/]");

    table.AddRow("Data Start Offset", $"0x{file.DataStartOffset:X8} ({file.DataStartOffset})");

    if (file is StuffItArchiveFileV1 v1File)
    {
        AddEntryHeaderV1Details(table, v1File.EntryHeaderV1);
        AddForkDetailsV1(table, v1File);
    }
    else if (file is StuffItArchiveFileV5 v5File)
    {
        AddEntryHeaderV5Details(table, v5File.EntryHeaderV5);
        AddFileHeaderV5Details(table, v5File.Header);
        AddFileHeader2V5Details(table, v5File.Header2);
        AddForkDetailsV5(table, v5File);
    }

    node.AddNode(table);
}

static void AddEntryHeaderV1Details(Table table, StuffItArchiveEntryHeaderV1 header)
{
    table.AddRow("[bold]Entry Header (V1)[/]", "");
    table.AddRow("  File Name", Markup.Escape(header.FileName));
    table.AddRow("  File Name Length", header.FileNameLength.ToString());
    table.AddRow("  Type", Markup.Escape(header.Type));
    table.AddRow("  Creator", Markup.Escape(header.Creator));
    table.AddRow("  Finder Flags", $"0x{header.FinderFlags:X4}");
    table.AddRow("  Creation Date", header.CreationDate.ToString("yyyy-MM-dd HH:mm:ss"));
    table.AddRow("  Modification Date", header.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss"));
    table.AddRow("  Reserved1", $"0x{header.Reserved1:X8}");
    table.AddRow("  Reserved2", $"0x{header.Reserved2:X4}");
    table.AddRow("  Header CRC", $"0x{header.HeaderCRC:X4}");
}

static void AddEntryHeaderV5Details(Table table, StuffItArchiveEntryHeaderV5 header)
{
    table.AddRow("[bold]Entry Header (V5)[/]", "");
    table.AddRow("  Magic Number", $"0x{header.MagicNumber:X8}");
    table.AddRow("  Version", header.Version.ToString());
    table.AddRow("  Header Size", header.HeaderSize.ToString());
    table.AddRow("  Flags", FormatEntryFlags(header.Flags));
    table.AddRow("  Creation Date", header.CreationDate.ToString("yyyy-MM-dd HH:mm:ss"));
    table.AddRow("  Modification Date", header.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss"));
    table.AddRow("  Previous Entry Offset", $"0x{header.PreviousEntryOffset:X8}");
    table.AddRow("  Next Entry Offset", $"0x{header.NextEntryOffset:X8}");
    table.AddRow("  Directory Entry Offset", $"0x{header.DirectoryEntryOffset:X8}");
    table.AddRow("  Name Length", header.NameLength.ToString());
    table.AddRow("  Reserved1", $"0x{header.Reserved1:X2}");
    table.AddRow("  Reserved2", $"0x{header.Reserved2:X2}");
    table.AddRow("  Header CRC", $"0x{header.HeaderCRC:X4}");
}

static void AddDirectoryHeaderV5Details(Table table, StuffItArchiveDirectoryHeader header)
{
    table.AddRow("[bold]Directory Header (V5)[/]", "");
    table.AddRow("  First Entry Offset", $"0x{header.FirstEntryOffset:X8}");
    table.AddRow("  Total Size", FormatSize(header.TotalSize));
    table.AddRow("  File Count", header.FileCount.ToString());
    table.AddRow("  Name", Markup.Escape(header.Name));
    table.AddRow("  Reserved", $"0x{header.Reserved:X8}");
    if (header.CommentLength > 0)
    {
        table.AddRow("  Comment Length", header.CommentLength.ToString());
        table.AddRow("  Comment Reserved", $"0x{header.CommentReserved:X4}");
        table.AddRow("  Comment", Markup.Escape(header.Comment));
    }
}

static void AddFileHeaderV5Details(Table table, StuffItArchiveFileHeader header)
{
    table.AddRow("[bold]File Header (V5)[/]", "");
    table.AddRow("  Name", Markup.Escape(header.Name));
    table.AddRow("  Data Fork Compression", FormatCompressionMethod(header.DataForkCompressionMethod));
    table.AddRow("  Data Fork Uncompressed", FormatSize(header.DataForkUncompressedLength));
    table.AddRow("  Data Fork Compressed", FormatSize(header.DataForkCompressedLength));
    table.AddRow("  Data Fork CRC", $"0x{header.DataForkCRC:X4}");
    table.AddRow("  Reserved1", $"0x{header.Reserved1:X4}");
    table.AddRow("  Password Length", header.PasswordLength.ToString());
    if (header.PasswordData.Length > 0)
    {
        table.AddRow("  Password Data", BitConverter.ToString(header.PasswordData));
    }
    if (header.CommentLength.HasValue && header.CommentLength.Value > 0)
    {
        table.AddRow("  Comment Length", header.CommentLength.Value.ToString());
        table.AddRow("  Comment Reserved", $"0x{header.CommentReserved:X4}");
        table.AddRow("  Comment", Markup.Escape(header.Comment ?? ""));
    }
}

static void AddFileHeader2V5Details(Table table, StuffItArchiveFileHeader2 header)
{
    table.AddRow("[bold]File Header 2 (V5)[/]", "");
    table.AddRow("  Flags", FormatFileHeader2Flags(header.Flags));
    table.AddRow("  Type", Markup.Escape(header.Type));
    table.AddRow("  Creator", Markup.Escape(header.Creator));
    table.AddRow("  Finder Flags", $"0x{header.FinderFlags:X4}");
    table.AddRow("  Actual Size", header.ActualSize.ToString());
    table.AddRow("  Resource Fork Compression", FormatCompressionMethod(header.ResourceForkCompressionMethod));
    table.AddRow("  Resource Fork Uncompressed", FormatSize(header.ResourceForkUncompressedLength));
    table.AddRow("  Resource Fork Compressed", FormatSize(header.ResourceForkCompressedLength));
    table.AddRow("  Resource Fork CRC", $"0x{header.ResourceForkCRC:X4}");
    table.AddRow("  Reserved1", $"0x{header.Reserved1:X4}");
    table.AddRow("  Reserved2", $"0x{header.Reserved2:X4}");
    table.AddRow("  Reserved3", $"0x{header.Reserved3:X8}");
    table.AddRow("  Reserved4", $"0x{header.Reserved4:X8}");
    table.AddRow("  Reserved5", $"0x{header.Reserved5:X8}");
    table.AddRow("  Reserved6", $"0x{header.Reserved6:X8}");
    table.AddRow("  Reserved7", $"0x{header.Reserved7:X8}");
    table.AddRow("  Reserved8", $"0x{header.Reserved8:X4}");
    table.AddRow("  Reserved9", $"0x{header.Reserved9:X2}");
}

static void AddForkDetailsV1(Table table, StuffItArchiveFileV1 file)
{
    // Data Fork
    table.AddRow("[bold cyan]Data Fork[/]", "");
    table.AddRow("  Compression Method", FormatCompressionMethod(file.DataForkCompressionMethod));
    table.AddRow("  Uncompressed Size", FormatSize(file.DataForkUncompressedLength));
    table.AddRow("  Compressed Size", FormatSize(file.DataForkCompressedLength));
    table.AddRow("  CRC", $"0x{file.DataForkCRC:X4}");
    if (file.DataForkUncompressedLength > 0 && file.DataForkCompressedLength > 0)
    {
        var ratio = (double)file.DataForkCompressedLength / file.DataForkUncompressedLength * 100;
        table.AddRow("  Compression Ratio", $"{ratio:F1}%");
    }

    // Resource Fork
    table.AddRow("[bold magenta]Resource Fork[/]", "");
    table.AddRow("  Compression Method", FormatCompressionMethod(file.ResourceForkCompressionMethod));
    table.AddRow("  Uncompressed Size", FormatSize(file.ResourceForkUncompressedLength));
    table.AddRow("  Compressed Size", FormatSize(file.ResourceForkCompressedLength));
    table.AddRow("  CRC", $"0x{file.ResourceForkCRC:X4}");
    if (file.ResourceForkUncompressedLength > 0 && file.ResourceForkCompressedLength > 0)
    {
        var ratio = (double)file.ResourceForkCompressedLength / file.ResourceForkUncompressedLength * 100;
        table.AddRow("  Compression Ratio", $"{ratio:F1}%");
    }
}

static void AddForkDetailsV5(Table table, StuffItArchiveFileV5 file)
{
    // Data Fork
    table.AddRow("[bold cyan]Data Fork[/]", "");
    table.AddRow("  Compression Method", FormatCompressionMethod(file.DataForkCompressionMethod));
    table.AddRow("  Uncompressed Size", FormatSize(file.DataForkUncompressedLength));
    table.AddRow("  Compressed Size", FormatSize(file.DataForkCompressedLength));
    table.AddRow("  CRC", $"0x{file.DataForkCRC:X4}");
    if (file.DataForkUncompressedLength > 0 && file.DataForkCompressedLength > 0)
    {
        var ratio = (double)file.DataForkCompressedLength / file.DataForkUncompressedLength * 100;
        table.AddRow("  Compression Ratio", $"{ratio:F1}%");
    }

    // Resource Fork
    if (file.Header2.Flags.HasFlag(StuffItArchiveFileHeader2Flags.HasResourceFork) || file.ResourceForkUncompressedLength > 0)
    {
        table.AddRow("[bold magenta]Resource Fork[/]", "");
        table.AddRow("  Compression Method", FormatCompressionMethod(file.ResourceForkCompressionMethod));
        table.AddRow("  Uncompressed Size", FormatSize(file.ResourceForkUncompressedLength));
        table.AddRow("  Compressed Size", FormatSize(file.ResourceForkCompressedLength));
        table.AddRow("  CRC", $"0x{file.ResourceForkCRC:X4}");
        if (file.ResourceForkUncompressedLength > 0 && file.ResourceForkCompressedLength > 0)
        {
            var ratio = (double)file.ResourceForkCompressedLength / file.ResourceForkUncompressedLength * 100;
            table.AddRow("  Compression Ratio", $"{ratio:F1}%");
        }
    }
    else
    {
        table.AddRow("[grey]Resource Fork[/]", "[grey](none)[/]");
    }
}

static string FormatSize(uint size)
{
    return size switch
    {
        >= 1024 * 1024 => $"{size / (1024.0 * 1024.0):F2} MB ({size:N0} bytes)",
        >= 1024 => $"{size / 1024.0:F2} KB ({size:N0} bytes)",
        _ => $"{size:N0} bytes"
    };
}

static string FormatFlags(StuffItArchiveHeaderFlags flags)
{
    if (flags == StuffItArchiveHeaderFlags.None)
        return "None";

    var parts = new List<string>();
    if (flags.HasFlag(StuffItArchiveHeaderFlags.HasExtendedInfo))
        parts.Add("HasExtendedInfo");
    if (flags.HasFlag(StuffItArchiveHeaderFlags.HasComments))
        parts.Add("HasComments");
    if (flags.HasFlag(StuffItArchiveHeaderFlags.Encrypted))
        parts.Add("Encrypted");

    return $"{string.Join(" | ", parts)} (0x{(byte)flags:X2})";
}

static string FormatEntryFlags(StuffItArchiveEntryHeaderFlags flags)
{
    if (flags == StuffItArchiveEntryHeaderFlags.None)
        return "None (0x00)";

    var parts = new List<string>();
    if (flags.HasFlag(StuffItArchiveEntryHeaderFlags.Unknown0x08))
        parts.Add("Unknown0x08");
    if (flags.HasFlag(StuffItArchiveEntryHeaderFlags.Encrypted))
        parts.Add("Encrypted");
    if (flags.HasFlag(StuffItArchiveEntryHeaderFlags.IsDirectory))
        parts.Add("IsDirectory");

    return $"{string.Join(" | ", parts)} (0x{(byte)flags:X2})";
}

static string FormatFileHeader2Flags(StuffItArchiveFileHeader2Flags flags)
{
    if (flags == StuffItArchiveFileHeader2Flags.None)
        return "None (0x0000)";

    var parts = new List<string>();
    if (flags.HasFlag(StuffItArchiveFileHeader2Flags.HasResourceFork))
        parts.Add("HasResourceFork");

    return $"{string.Join(" | ", parts)} (0x{(ushort)flags:X4})";
}

static string FormatCompressionMethod(StuffItArchiveCompressionMethod method)
{
    return method switch
    {
        StuffItArchiveCompressionMethod.None => $"None ({(int)method})",
        StuffItArchiveCompressionMethod.LZW => $"LZW ({(int)method})",
        StuffItArchiveCompressionMethod.LZSS => $"LZSS ({(int)method})",
        StuffItArchiveCompressionMethod.Arsenic => $"Arsenic ({(int)method})",
        _ => $"Unknown ({(int)method})"
    };
}
