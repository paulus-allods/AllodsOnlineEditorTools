# Allods Online `.bin` database format

This document describes the on-disk layout of the game's `.bin` databases (e.g. `pack.bin`). It pairs
with [`allods_bin.hexpat`](allods_bin.hexpat), an [ImHex](https://imhex.werwolv.net/)
pattern you can apply to a decompressed database to see the structure
interactively.

## Compression

A `.bin` file is a single **zlib** stream (RFC 1950). Everything below describes the **decompressed** payload.

Decompress a database with this repo's own CLI:

```sh
EditorCLI utils decompress <file.bin> -o <file.raw>
```

Then open `<file.raw>` in ImHex and apply the pattern (see
[Using the ImHex pattern](#using-the-imhex-pattern)).

## Chunk framing

The decompressed payload is a flat sequence of **chunks** in a fixed order.
Every value is **little-endian** unless noted. Most chunks share the framing:

| Field   | Type  | Notes                                  |
| ------- | ----- | -------------------------------------- |
| `id`    | `s32` | Chunk id (see table below)             |
| `size`  | `s32` | Size of the chunk body                 |
| `body`  | …     | `size` (× entry size for some chunks)  |

| Id | Chunk         | Present     | `size` counts        |
| -- | ------------- | ----------- | -------------------- |
| 0  | Header        | always      | bytes (always 8)     |
| 1  | TxtFiles      | always      | bytes                |
| 2  | Metadata      | always      | bytes                |
| 3  | Data          | always      | bytes                |
| 4  | Fixes         | always      | entries (8 bytes each) |
| 5  | PakFileRefs   | optional    | entries (4 bytes each) |
| 6  | Packs         | optional    | see below            |

Chunks 5 and 6 are only present in some databases, and always together. The
reader detects them by checking whether any bytes remain after the Fixes
chunk.

## Chunks

### 0: Header

| Field     | Type     | Notes                                |
| --------- | -------- | ------------------------------------ |
| `id`      | `s32`    | `0`                                  |
| `size`    | `s32`    | `8`                                  |
| `version` | `u8[8]`  | Version hash                          |

`version` is probably a hash of the game's structure definitions rather than a
release number: it stays constant across client versions that use the same
structs/enums for the database, and only changes when those definitions
change. Being a hash, it is just an opaque 8-byte value with no meaningful
endianness; the reader renders it as a big-endian `u64` so the hex digits
match the byte order in the file. The known values are mapped to client
versions in [`GameVersions.resx`](../ClientResources/GameVersions.resx).

### 1: TxtFiles

A relative-pointer table mapping integer ids to text-file reference names. The
chunk body is a self-contained blob addressed from its own start:

- `s32 offset`: where the entry array begins (relative to the blob start)
- `s32 count`: number of entries
- at `offset`, `count` entries of:
  - `s32 dataRel`: data position = *(address of the `dataRel` field itself)*
    `+ dataRel`
  - `s32 dataSize`: length of the name in bytes
  - `s32 id`: the entry's id
  - the name is `dataSize` bytes of UTF-8 at the computed data position,
    trailing `\0` trimmed

The `id` identifies one **`TextFileRef` occurrence**: a struct field in the
`Data` chunk that references a localized `.txt` file (whose contents live in
the localization paks). Ids are assigned globally across all databases of a
build: there is one entry per `TextFileRef` occurrence, but only the main
database (`pack.bin`) carries the table. A `TextFileRef` in the `Data` chunk
stores both the `.txt` path and this id.

### 2: Metadata

Several bucketed relative-pointer tables packed into one blob. It opens with a
directory of four `(s32 offset, s32 count)` pairs: one per table, each offset
relative to the address of the offset field itself: followed by an
`s32 resourceSystemVersion`, then the tables:

- **DbId → File**: filenames keyed by database id, bucketed by the Adler-32
  (RFC 1950) checksum of the name modulo `65521` (`count` is the bucket count,
  `65521` or `0`). Each stored name is validated against its Adler-32
  checksum. The inverse File → DbId mapping is not stored; the reader derives
  it from this table.
- **Structs**: the list of struct type names used by this database (the
  `struct NDb::` prefix is stripped). These are the types the `Data` chunk is
  built from.
- **ResId → DbId** and **DbId → ResId**: two separately stored tables mapping
  resource ids to database ids and back, each bucketed by its key modulo
  `65521`.

See `ReadMetadata` in `DatabaseLoader.cs` for the exact directory offsets and
the bucket-validation rules.

### 3: Data

The actual database content. Crucially, this is **a raw dump of the game's
in-memory representation**: the structs are laid out exactly as they were in
the process's memory, pointers and all.

Because it's a memory dump, the internal layout is defined by the C++ structs
named in the Metadata chunk and by the game version; it is not a fixed layout
and is not described statically here. The typed model lives in
[`ClientResources/Structs`](../ClientResources/Structs), and the (de)serializer
is
[`BinaryStructReader`](../ClientResources/Serialization/Bin/BinaryStructReader.cs).

### 4: Fixes

**This chunk exists precisely because the Data chunk is a memory dump.** A
memory image contains absolute pointers that are only valid at the address the
data originally lived at. Rather than store real pointers, the Data chunk stores
placeholders, and the Fixes chunk is the **pointer relocation table** the game
replays after loading to turn those placeholders back into valid pointers: the
same idea as relocations in an executable.

Each entry is 8 bytes:

| Field   | Type  | Notes                                    |
| ------- | ----- | ---------------------------------------- |
| `data`  | `s32` | Packed, see bit layout                   |
| `value` | `s32` | Meaning depends on `type`, see below     |

`data` bit layout (from the least-significant bit):

| Bits   | Meaning                                                              |
| ------ | -------------------------------------------------------------------- |
| 0–1    | `type`: `0` DbIdRef, `1` Direct, `2` Type, `3` Generic               |
| 2      | `external` flag (only meaningful for DbIdRef)                        |
| 3…     | `address` in 4-byte units; **byte offset into Data = address × 4** |

The address locates the field inside the `Data` chunk that this fix applies
to; `type` says what kind of pointer that field is and how to interpret
`value`:

- **`0` DbIdRef**: a reference to another resource's root object, stored as
  a database id. `value` is the target's **dbid**. The `external` flag
  picks the database it resolves in: `0` the current database, `1` the main
  database (`pack.bin`). Used for `ResourcePointer` and nullable nested
  objects.
- **`1` Direct**: a plain data pointer within the same `Data` chunk:
  `value` is the byte offset of the pointed-to bytes (string characters,
  array elements). The pointer field is followed by an `s32` byte length at
  field offset + 4.
- **`2` Type / `3` Generic**: not a data pointer but a **type tag** for the
  object starting at this position: `value` is an index into the Metadata
  **Structs** list, naming the C++ struct laid out here.

### 5: PakFileRefs (optional)

An array of `s32` offsets pointing at `PakFileRef` values inside the `Data`
chunk. The stored value is encoded: **byte offset = value × 4 − 12**. Present
only in databases that reference pak-packed files.

### 6: Packs (optional)

The list of pak archive names referenced by the database. **This chunk breaks
the usual framing: it has no `size` field.** After the `s32 id` (`6`) comes:

- `s32 count`: number of packs
- `count` ×:
  - `s32 byteLen`: length of the name in bytes
  - `byteLen` bytes of **UTF-16** text (only the file name is kept)

## Using the ImHex pattern

1. Decompress the database (see [Compression](#compression)).
2. Open the decompressed file in [ImHex](https://imhex.werwolv.net/).
3. Load `doc/allods_bin.hexpat` (File → Import → Pattern, or paste it into the
   pattern editor) and run it (it can take time and consume up to 16GB of RAM).