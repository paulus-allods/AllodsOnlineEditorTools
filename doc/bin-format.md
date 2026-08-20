# Allods Online `.bin` database format

This document describes the on-disk layout of the game's `.bin` databases (e.g. `pack.bin`). It pairs
with the [ImHex](https://imhex.werwolv.net/) patterns in this folder, which you
can apply to a decompressed database to see the structure interactively:
[`allods_bin_v1.hexpat`](allods_bin_v1.hexpat) for the original format,
[`allods_bin_v2_x86.hexpat`](allods_bin_v2_x86.hexpat) and
[`allods_bin_v2_x64.hexpat`](allods_bin_v2_x64.hexpat) for V2.

The original layout is described in [V1 format](#v1-format), which is always a
32-bit build. Allods 15.0 reworked it into [V2 format](#v2-format), which comes
in 32-bit and 64-bit builds; each has its own pattern.

## Compression

> [!IMPORTANT]
> A `.bin` file is a single **zlib** stream (RFC 1950). Everything below describes the **decompressed** payload.

Decompress a database with this repo's own CLI:

```sh
EditorCLI utils decompress <file.bin> -o <file.raw>
```

Then open `<file.raw>` in ImHex and apply the pattern (see
[Using the ImHex patterns](#using-the-imhex-patterns)).

## V1 format

The original layout, used by every client before 15.0. It is always a 32-bit
build, so every field below is fixed-width.

### Chunk framing

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
endianness, so the reader keeps the raw buffer; it is identified by the hex string
of its bytes in file order. The known values are mapped to client versions in
[`GameVersion.cs`](../ClientResources/Structs/GameVersion.cs).

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

## V2 format

Allods 15.0 reworked the format. Where V1 was always a 32-bit build, a V2
database is built for either a 32-bit **or** a 64-bit client, so the changes
split into **structural** changes (shared by both builds) and the **pointer
width** of certain fields (which follows the client architecture). Field widths
below are given for the 64-bit build; each build has its own pattern,
[`allods_bin_v2_x86.hexpat`](allods_bin_v2_x86.hexpat) and
[`allods_bin_v2_x64.hexpat`](allods_bin_v2_x64.hexpat).
What did *not* change from V1: the file is still a single zlib stream; the
payload is still an ordered sequence of chunks; the Metadata tables still use
the same field-relative offset encoding and Adler-32 bucketing; and the Data
chunk is still a raw memory image relocated by the Fixes chunk.

### Structural changes (both builds)

- **The Header is no longer a chunk.** The file opens with a bare **12-byte
  version buffer** (no `id`/`size` framing). It takes V1's place for identifying
  the build ([`GameVersion.cs`](../ClientResources/Structs/GameVersion.cs)
  matches databases on its hex string), but whether it is a hash of the struct
  definitions, as V1's 8 bytes are believed to be, is **not** established. Its
  internal structure is unknown and the reader keeps it as raw bytes. One hint
  that it is not a flat hash: across two consecutive 64-bit builds, bytes 4–7
  changed from `0x0472` to `0x0473` while the rest changed completely. That
  window behaves like a build counter, not like hash bytes.
- **The TxtFiles chunk (id 1) was removed.** The remaining chunks keep their
  ids (`2` Metadata, `3` Data, `4` Fixes, `5` PakFileRefs, `6` Packs). The
  framing is still `s32 id` + `size` + body (the `size` width is architecture-
  dependent, see below), and Packs is still the special count-prefixed chunk.
- **The Metadata directory grew from 4 to 5 entries**, still `(s32 rel, s32
  count)` pairs, and these stay 32-bit in both builds: a new **ObjId → DbId**
  table leads the directory. As in V1 the directory is still followed by the
  `resourceSystemVersion`; on the 64-bit build a pointer-sized offset to the Data
  chunk (relative to its own field) sits between the two, which the 32-bit build
  does not have.

### Pointer width (32- vs 64-bit)

Pointer-sized fields match the client architecture, so where V1 (32-bit only)
always had them at 4 bytes, V2 has them at 4 or 8. The 32-bit build is a
transitional one: the client dropped 32-bit entirely in **17.0**, so a V2
database from 17.0 onwards is always 64-bit. A 32-bit V2 database is
byte-for-byte identical to a 64-bit one except each of these fields is
half-width:

| Field | 64-bit build | 32-bit build |
| ----- | ------------ | ------------ |
| chunk `size` | `s64` | `s32` |
| `resourceSystemVersion` | `s64` | `s32` |
| dbid / resid / ObjId → DbId value | `s64` | `s32` |
| Data chunk pointer | `s64` | absent |
| Fixes entry | 16 B (`s64 data`, `s64 value`) | 8 B (`s32`, `s32`) |
| Fixes address scale | `× 8` (`address : 61`) | `× 4` (`address : 29`) |
| PakFileRefs offset | `s64`, `value × 8 − 24` | `s32`, `value × 4 − 12` |
| Packs `count` / `byteLen` | `s64` | `s32` |

Metadata entries are padded to the pointer size (8-byte alignment on the 64-bit
build). The Metadata directory's `(rel, count)` entries stay `s32` in both.

### Metadata tables (V2)

The 5-entry directory is followed by the `resourceSystemVersion` and then the
tables. Each `rel` is relative to the address of its own field; the widths shown
are the 64-bit build's (halve them for 32-bit, per [Pointer
width](#pointer-width-32--vs-64-bit)). Order and shape:

0. **ObjId → DbId** (65521 buckets): maps a **dense object id** `objId`
   (`0 … N−1`) to a **dbid**, bucketed by `objId % 65521`. Each entry is `s32
   rel, s32 size (== 9), s64 dbId`, and its 9-byte block is `s32 delimiter
   (== 1), u32 objId, u8 pad`. The client assigns an `objId` to every object
   that is **not** a root of the dependency tree; the roots live in table 1
   (**DbId → File**), keyed by their `.xdb` name, and have no `objId` (they are
   absent here). The `dbId` is the object's byte offset in the Data chunk: its
   memory image starts there, tagged by a **Type** pointer-fix naming its
   struct.
1. **DbId → File** (`count` buckets, no longer V1's fixed `65521`; `4081` in
   the databases inspected): the V1 table, unchanged in shape. Each record is
   `s32 rel, s32 size, s64 dbId` and its block `s32 delimiter (== 1), u32
   adler32, char name[size − 9], u8 pad` still carries the **filename and its
   Adler-32**, bucketed by `adler32 % count`. Within a bucket the record headers
   are contiguous and their blocks follow them, each block padded to the pointer
   size.
2. **Structs**: `struct NDb::…` names; entry `s32 rel, s32 size, s32 delimiter
   (== 0), s32 pad`.
3. **ResId → DbId** (65521 buckets): pairs `s64 resId, s64 dbId`, bucketed by
   `resId % 65521`.
4. **DbId → ResId** (65521 buckets): pairs `s64 dbId, s64 resId`, bucketed by
   `dbId % 65521`.

The `File → DbId`, `DbId → File` and both resource-id directions carry the same
information as V1; only the widths and the added ObjId → DbId table differ.

## Using the ImHex patterns

1. Decompress the database (see [Compression](#compression)).
2. Open the decompressed file in [ImHex](https://imhex.werwolv.net/).
3. Load the pattern for the file's format (File → Import → Pattern, or paste it
   into the pattern editor) and run it (it can take time and consume up to 16GB
   of RAM):
   - `doc/allods_bin_v1.hexpat`: original pre-15.0 format;
   - `doc/allods_bin_v2_x86.hexpat`: V2, 32-bit client;
   - `doc/allods_bin_v2_x64.hexpat`: V2, 64-bit client.

   A V1 file starts with the `s32` chunk id `0`; a V2 file starts with the
   12-byte version buffer, so its first `s32` is not `0`.
