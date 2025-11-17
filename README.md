
# StuffItReader

StuffItReader is a .NET library for reading and parsing classic Mac resource fork files. It provides a simple API to extract resources, inspect headers, and work with resource data in modern .NET applications.

---

## Features

- Read and parse resource fork files (e.g., from classic Mac applications)
- Access resource types, IDs, names, and data
- Support for common resource record types
- Utility methods for working with resource data
- Low-level access to resource fork structure (headers, maps, attributes)

---

## Installation

Install via NuGet:

```sh
dotnet add package StuffItReader
```

Or via the NuGet Package Manager:

```sh
Install-Package StuffItReader
```

---

## Quick Start Example

```csharp
using StuffItReader;
using System.IO;

// Open a resource fork file
using var stream = File.OpenRead("MyResourceFile.res");

// Parse the resource fork
var resourceFork = new ResourceFork(stream);

// List all resource types and entries
foreach (var (type, entries) in resourceFork.Map.Types)
{
    Console.WriteLine($"Type: {type}");
    foreach (var entry in entries)
    {
        Console.WriteLine($"  ID: {entry.ID}, Attributes: {entry.Attributes}");
        // Read resource data
        byte[] data = resourceFork.GetResourceData(entry);
        // ... process data ...
    }
}
```

---

## API Overview

### ResourceFork

- `ResourceFork(Stream stream)`: Parses a resource fork from a seekable, readable stream.
- `Header`: Gets the `ResourceForkHeader` (offsets and lengths for data/map sections).
- `Map`: Gets the `ResourceForkMap` (resource types, entries, and structure).
- `GetResourceData(ResourceListEntry entry)`: Returns the resource data as a byte array.
- `GetResourceData(ResourceListEntry entry, Stream output)`: Writes resource data to a stream.

### ResourceForkHeader

- `DataOffset`, `MapOffset`, `DataLength`, `MapLength`: Offsets and lengths for resource data and map sections.

### ResourceForkMap

- `Types`: `Dictionary<string, List<ResourceListEntry>>` mapping 4-char type codes (e.g., "CODE", "STR#") to resource entries.
- `ResourceTypeCount`: Number of resource types in the map.

### ResourceTypeListItem

- `Type`: 4-character resource type code.
- `ResourceCount`: Number of resources of this type.
- `ResourceListOffset`: Offset to the list of resources of this type.

### ResourceListEntry

- `ID`: Resource ID (ushort).
- `NameOffset`: Offset to the resource name (if present).
- `Attributes`: Resource attributes (see below).
- `DataOffset`: Offset to the resource data.
- `ReservedHandle`: Reserved for handle to resource.

### ResourceAttributes (enum)

- `None`, `Changed`, `Preload`, `Protected`, `Locked`, `Purgeable`, `SystemHeap`, etc.

---

## Advanced Usage

### Reading Resource Data to a Stream

```csharp
using var output = File.Create("resource.bin");
int length = resourceFork.GetResourceData(entry, output);
Console.WriteLine($"Wrote {length} bytes");
```

### Inspecting Resource Fork Structure

```csharp
Console.WriteLine($"DataOffset: {header.DataOffset}, MapOffset: {header.MapOffset}");
Console.WriteLine($"Resource Types: {map.ResourceTypeCount}");
```

### Working with String Resources

```csharp
using StuffItReader.Records;

// Read a 'STR ' resource
var strEntries = resourceFork.Map.Types["STR "];
foreach (var entry in strEntries)
{
    var stringRecord = new StringRecord(resourceFork.GetResourceData(entry));
    Console.WriteLine($"String: {stringRecord.Value}");
}

// Read a 'STR#' (string list) resource
var strListEntries = resourceFork.Map.Types["STR#"];
foreach (var entry in strListEntries)
{
    var stringList = new StringListRecord(resourceFork.GetResourceData(entry));
    foreach (var str in stringList.Values)
    {
        Console.WriteLine($"  - {str}");
    }
}
```

---

## Resource Fork Structure

- **Header**: Contains offsets and lengths for the data and map sections.
- **Map**: Contains a copy of the header, resource type list, and resource name list.
- **Types**: Each type (e.g., "CODE", "STR#") has a list of entries.
- **Entries**: Each entry has an ID, attributes, data offset, and optional name.

---

## License

MIT License.
