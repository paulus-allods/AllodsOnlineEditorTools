#pragma description Allods Online .bin database (decompressed)
#pragma author AllodsOnlineEditorTools
#pragma endian little

// The Metadata hash tables have 65521 buckets each and the Fixes table can
// hold tens of millions of entries, so the default evaluation limits are far
// too low.
#pragma array_limit 0x2000000
#pragma pattern_limit 0x10000000

// A raw `.bin` file is a single zlib stream; this pattern describes the
// *decompressed* payload. Decompress it first:
//
//     EditorCLI utils decompress <file.bin> -o <file.raw>
//
// The TxtFiles and Metadata chunks use relative pointers: a stored offset is
// added to the address of the field it was read from, reproduced here with
// `addressof(...) + offset` placements. The Data chunk is a raw memory dump
// of hundreds of different structs with no single static layout, so it is
// left as a generic byte block.
//
// See doc/bin-format.md for the full prose description.

import std.mem;

enum ChunkId : s32 {
    Header       = 0,
    TxtFiles     = 1,
    Metadata     = 2,
    Data         = 3,
    Fixes        = 4,
    PakFileRefs  = 5, // optional
    Packs        = 6, // optional
};

// === Chunk 0 — Header =======================================================
struct HeaderChunk {
    ChunkId id;
    s32     size;        // == 8
    u8      body[size];
    // Version hash — likely a hash of the game's struct/enum definitions:
    // constant across client versions that share the same database structs.
    // An opaque 8-byte value; shown as a big-endian u64 so the hex digits
    // match the byte order in the file.
    be u64  version @ addressof(body);
};

// === Chunk 1 — TxtFiles =====================================================
// Ids of TextFileRef occurrences mapped to the referenced .txt path.
struct TxtFileEntry {
    s32  nameRel;
    s32  nameSize;
    s32  id;
    char name[nameSize] @ addressof(this) + nameRel;
};

struct TxtFilesTable {
    s32          arrayOffset;
    s32          count;
    TxtFileEntry entries[count] @ addressof(this) + arrayOffset;
};

struct TxtFilesChunk {
    ChunkId       id;
    s32           size;
    u8            blob[size];
    TxtFilesTable table @ addressof(blob);
};

// === Chunk 2 — Metadata =====================================================
struct MetadataDirectory {
    s32 dbId2FileRel;
    s32 dbId2FileCount;         // 65521 or 0
    s32 structsRel;
    s32 structsCount;
    s32 resId2DbIdRel;
    s32 resId2DbIdCount;        // 65521 or 0
    s32 dbId2ResIdRel;
    s32 dbId2ResIdCount;        // 65521 or 0
    s32 resourceSystemVersion;
};

struct StructNameEntry {
    s32  nameRel;
    s32  nameSize;
    s32  delimiter;             // == 0
    char name[nameSize] @ addressof(this) + nameRel;  // e.g. "struct NDb::World"
};

struct DbId2FileItem {
    s32  dataRel;
    s32  dataSize;
    s32  dbId;
    s32  blockDelimiter @ addressof(this) + dataRel;        // == 1
    u32  blockAdler32   @ addressof(this) + dataRel + 4;    // == adler32(blockName)
    char blockName[dataSize] @ addressof(this) + dataRel + 8;

    // A dbid is the byte offset of the resource's root object inside the Data
    // chunk; mark it there (+ 8 skips the Data chunk's id/size fields).
    u32  resourceRoot @ addressof(parent.parent.blob) + parent.parent.size + 8 + dbId
             [[color("00C000")]];
};

struct DbId2FileBucket {
    s32           itemsRel;
    s32           itemCount;
    DbId2FileItem items[itemCount] @ addressof(this) + itemsRel;
};

struct ResIdDbIdPair { s32 resId; s32 dbId; };
struct ResId2DbIdBucket {
    s32           pairsRel;
    s32           pairCount;
    ResIdDbIdPair pairs[pairCount] @ addressof(this) + pairsRel;
};

struct DbIdResIdPair { s32 dbId; s32 resId; };
struct DbId2ResIdBucket {
    s32           pairsRel;
    s32           pairCount;
    DbIdResIdPair pairs[pairCount] @ addressof(this) + pairsRel;
};

struct MetadataChunk {
    ChunkId           id;
    s32               size;
    u8                blob[size];
    MetadataDirectory dir @ addressof(blob);

    // Absent tables have a count of 0.
    if (dir.structsCount > 0)
        StructNameEntry structs[dir.structsCount]
            @ addressof(dir.structsRel) + dir.structsRel;
    if (dir.dbId2FileCount > 0)
        DbId2FileBucket dbId2File[dir.dbId2FileCount]
            @ addressof(dir.dbId2FileRel) + dir.dbId2FileRel;
    if (dir.resId2DbIdCount > 0)
        ResId2DbIdBucket resId2DbId[dir.resId2DbIdCount]
            @ addressof(dir.resId2DbIdRel) + dir.resId2DbIdRel;
    if (dir.dbId2ResIdCount > 0)
        DbId2ResIdBucket dbId2ResId[dir.dbId2ResIdCount]
            @ addressof(dir.dbId2ResIdRel) + dir.dbId2ResIdRel;
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
// Pointer relocation table for the Data chunk.
// Byte offset into Data = address * 4; `value` depends on `type`:
//   0 DbIdRef — dbid of the target object (external: resolve in pack.bin)
//   1 Direct  — byte offset of the pointed-to bytes in Data
//   2 Type / 3 Generic — index into the Metadata struct name list
bitfield FixData {
    type     : 2;
    external : 1;
    address  : 29;   // in 4-byte units
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

// === Chunk 5 — PakFileRefs (optional) =======================================
// Offsets of PakFileRef values in Data; byte offset = value * 4 - 12.
struct PakFileRefsChunk {
    ChunkId id;
    s32     count;
    s32     offsets[count];
};

// === Chunk 6 — Packs (optional) =============================================
// Referenced pak archive names. No `size` field: `id` is followed directly
// by the pack count.
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
    HeaderChunk   header;
    TxtFilesChunk txtFiles;
    MetadataChunk metadata;
    DataChunk     data;
    FixesChunk    fixes;

    if (!std::mem::eof()) {
        PakFileRefsChunk pakFileRefs;
        PacksChunk       packs;
    }
};

Database database @ 0x00;