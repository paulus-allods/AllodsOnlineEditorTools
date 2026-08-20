#pragma description Allods Online .bin database, V2 32-bit (decompressed)
#pragma author AllodsOnlineEditorTools
#pragma endian little

// The Metadata hash tables have 65521 buckets each and the Fixes table can hold
// tens of millions of entries, so the default evaluation limits are far too low.
#pragma array_limit 0x2000000
#pragma pattern_limit 0x10000000

// V2 is the layout introduced by Allods 15.0, here in its **32-bit** flavour: the
// version buffer replaces the Header chunk, the TxtFiles chunk is gone, the
// Metadata directory gained an ObjId -> DbId table, and every pointer-sized field
// is 4 bytes. For a 64-bit V2 database use allods_bin_v2_x64.hexpat, for the
// pre-15.0 format allods_bin_v1.hexpat.
//
// A raw `.bin` file is a single zlib stream; this pattern describes the
// *decompressed* payload. Decompress it first:
//
//     EditorCLI utils decompress <file.bin> -o <file.raw>
//
// The Metadata chunk uses relative pointers: a stored offset is added to the
// address of the field it was read from, written here as `addressof(...) + rel`
// placements. The Data chunk is a raw memory dump of hundreds of different
// structs with no single static layout, so it is left as a generic byte block.
//
// See doc/bin-format.md for the full prose description.

import std.mem;

enum ChunkId : s32 {
    Metadata    = 2,
    Data        = 3,
    Fixes       = 4,
    PakFileRefs = 5,
    Packs       = 6,
};

// === Version ================================================================
// Not a chunk: a 12-byte buffer identifying the client build. Its internal
// structure is unknown, so it is left as raw bytes.
struct Version {
    u8 version[12];
};

// === Chunk 2 — Metadata =====================================================
// A directory of (rel, count) entries — one per table — followed by the resource
// system version. Every `rel` is relative to the address of its own field, and
// `count` is the bucket count of its table. Unlike the 64-bit build, no pointer
// to the Data chunk sits between the directory and the version: the ObjId -> DbId
// table starts right after it.
struct MetadataDirEntry {
    s32 rel;
    s32 count;
};

struct MetadataDirectory {
    MetadataDirEntry objId2DbId;
    MetadataDirEntry dbId2File;
    MetadataDirEntry structs;
    MetadataDirEntry resId2DbId;
    MetadataDirEntry dbId2ResId;
    s32              resourceSystemVersion;
};

// -- ObjId -> DbId ------------------------------------------------------------
// Maps the dense object id the client assigns to every non-root object to that
// object's dbid. Same record shape as DbId -> File, but the block carries an
// objId instead of a name, so it is always 9 bytes.
struct ObjId2DbIdBlock {
    s32 delimiter;              // == 1
    u32 objId;                  // objId % 65521 == bucket
    padding[1];
};

struct ObjId2DbIdRecord {
    s32 rel;
    s32 size;                   // == 9
    s32 dbId;
    ObjId2DbIdBlock block @ addressof(this) + rel;
};

struct ObjId2DbIdBucket {
    s32              rel;
    s32              count;
    ObjId2DbIdRecord records[count] @ addressof(this) + rel;
};

// -- DbId -> File -------------------------------------------------------------
// Buckets of records; a record header points at a name block holding the file
// name and its Adler-32 checksum. Within a bucket the headers are contiguous and
// the blocks follow them, each block padded to 4-byte alignment.
struct DbId2FileBlock<auto Size> {
    s32  delimiter;             // == 1
    u32  adler32;               // adler32(name), and adler32 % count == bucket
    char name[Size - 9];
    padding[1];
};

struct DbId2FileRecord {
    s32 rel;
    s32 size;
    s32 dbId;
    DbId2FileBlock<size> block @ addressof(this) + rel;

    // A dbid is the byte offset of the file's root object inside the Data chunk;
    // mark it there (+ 8 skips the Data chunk's id and size fields).
    u32 rootObject @ addressof(parent.parent.blob) + parent.parent.size + 8 + dbId
            [[color("00C000")]];
};

struct DbId2FileBucket {
    s32             rel;
    s32             count;
    DbId2FileRecord records[count] @ addressof(this) + rel;
};

// -- Structs ------------------------------------------------------------------
struct StructNameEntry {
    s32  rel;
    s32  size;
    s32  delimiter;             // == 0
    char name[size] @ addressof(this) + rel;    // e.g. "struct NDb::World"
};

// -- ResId <-> DbId -----------------------------------------------------------
struct ResIdDbIdPair { s32 resId; s32 dbId; };
struct ResId2DbIdBucket {
    s32           rel;
    s32           count;
    ResIdDbIdPair pairs[count] @ addressof(this) + rel;    // resId % 65521 == bucket
};

struct DbIdResIdPair { s32 dbId; s32 resId; };
struct DbId2ResIdBucket {
    s32           rel;
    s32           count;
    DbIdResIdPair pairs[count] @ addressof(this) + rel;    // dbId % 65521 == bucket
};

struct MetadataChunk {
    ChunkId           id;
    s32               size;
    u8                blob[size];
    MetadataDirectory dir @ addressof(blob);

    // Absent tables have a count of 0.
    if (dir.objId2DbId.count > 0)
        ObjId2DbIdBucket objId2DbId[dir.objId2DbId.count]
            @ addressof(dir.objId2DbId) + dir.objId2DbId.rel;
    if (dir.dbId2File.count > 0)
        DbId2FileBucket dbId2File[dir.dbId2File.count]
            @ addressof(dir.dbId2File) + dir.dbId2File.rel;
    if (dir.structs.count > 0)
        StructNameEntry structs[dir.structs.count]
            @ addressof(dir.structs) + dir.structs.rel;
    if (dir.resId2DbId.count > 0)
        ResId2DbIdBucket resId2DbId[dir.resId2DbId.count]
            @ addressof(dir.resId2DbId) + dir.resId2DbId.rel;
    if (dir.dbId2ResId.count > 0)
        DbId2ResIdBucket dbId2ResId[dir.dbId2ResId.count]
            @ addressof(dir.dbId2ResId) + dir.dbId2ResId.rel;
};

// === Chunk 3 — Data =========================================================
// Raw dump of the game's in-memory structs; pointers inside are placeholders
// relocated through the Fixes chunk.
struct DataChunk {
    ChunkId id;
    s32     size;
    u8      data[size];
};

// === Chunk 4 — Fixes ========================================================
// Pointer relocation table for the Data chunk. `value` depends on `type`:
//   0 DbIdRef — dbid of the target object (external: resolve in pack.bin)
//   1 Direct  — byte offset of the pointed-to bytes in Data
//   2 Type / 3 Generic — index into the Metadata struct name list
bitfield FixData {
    type     : 2;
    external : 1;
    address  : 29;              // in 4-byte units: byte offset into Data = address * 4
};

struct FixEntry {
    FixData data;
    s32     value;
};

struct FixesChunk {
    ChunkId  id;
    s32      count;
    FixEntry entries[count];
};

// === Chunk 5 — PakFileRefs =======================================
// Offsets of PakFileRef values in Data; byte offset = value * 4 - 12.
struct PakFileRefsChunk {
    ChunkId id;
    s32     count;
    s32     offsets[count];
};

// === Chunk 6 — Packs =============================================
// Referenced pak archive names. This chunk carries a count, not a byte size.
struct Pack {
    s32    byteLen;
    char16 name[byteLen / 2];
};

struct PacksChunk {
    ChunkId id;
    s32     count;
    Pack    packs[count];
};

// === Top-level layout =======================================================
struct Database {
    Version          version;
    MetadataChunk    metadata;
    DataChunk        data;
    FixesChunk       fixes;
    PakFileRefsChunk pakFileRefs;
    PacksChunk       packs;
};

Database database @ 0x00;
