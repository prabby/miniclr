////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef _MINICLR_TYPES_H_
#define _MINICLR_TYPES_H_

#include "MiniClr_PlatformDef.h"


///////////////////////////////////////////////////////////////////////////////////////////////////

#if defined(PLATFORM_WINDOWS)
#pragma pack(push, TINYCLR_TYPES_H, 4)
#endif

typedef enum _CLR_OPCODE
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) c,
#include <opcode.def>
#undef OPDEF
CEE_COUNT,        /* number of instructions and macros pre-defined */
}CLR_OPCODE;

//////////////////////////////////////////////////////////////////////////////////////////////////
//The basic data types
typedef unsigned char    CLR_UINT8;
typedef unsigned short   CLR_UINT16;
typedef unsigned int     CLR_UINT32;
typedef unsigned __int64 CLR_UINT64;
typedef signed char      CLR_INT8;
typedef signed short     CLR_INT16;
typedef signed int       CLR_INT32;
typedef signed __int64   CLR_INT64;

typedef CLR_UINT16       CLR_OFFSET;
typedef CLR_UINT32       CLR_OFFSET_LONG;
typedef CLR_UINT16       CLR_IDX;
typedef CLR_UINT16       CLR_STRING;
typedef CLR_UINT16       CLR_SIG;
typedef const CLR_UINT8* CLR_PMETADATA;


//--//
//may need to change later
typedef CLR_INT64        CLR_INT64_TEMP_CAST;
typedef CLR_UINT64       CLR_UINT64_TEMP_CAST;
typedef double           CLR_DOUBLE_TEMP_CAST;

#define CLR_SIG_INVALID  0xFFFF

typedef enum _CLR_LOGICAL_OPCODE
{
    LO_Not                       = 0x00,
    LO_And                       = 0x01,
    LO_Or                        = 0x02,
    LO_Xor                       = 0x03,
    LO_Shl                       = 0x04,
    LO_Shr                       = 0x05,

    LO_Neg                       = 0x06,
    LO_Add                       = 0x07,
    LO_Sub                       = 0x08,
    LO_Mul                       = 0x09,
    LO_Div                       = 0x0A,
    LO_Rem                       = 0x0B,

    LO_Box                       = 0x0C,
    LO_Unbox                     = 0x0D,

    LO_Branch                    = 0x0E,
    LO_Set                       = 0x0F,
    LO_Switch                    = 0x10,

    LO_LoadFunction              = 0x11,
    LO_LoadVirtFunction          = 0x12,

    LO_Call                      = 0x13,
    LO_CallVirt                  = 0x14,
    LO_Ret                       = 0x15,

    LO_NewObject                 = 0x16,
    LO_CastClass                 = 0x17,
    LO_IsInst                    = 0x18,

    LO_Dup                       = 0x19,
    LO_Pop                       = 0x1A,

    LO_Throw                     = 0x1B,
    LO_Rethrow                   = 0x1C,
    LO_Leave                     = 0x1D,
    LO_EndFinally                = 0x1E,

    LO_Convert                   = 0x1F,

    LO_StoreArgument             = 0x20,
    LO_LoadArgument              = 0x21,
    LO_LoadArgumentAddress       = 0x22,

    LO_StoreLocal                = 0x23,
    LO_LoadLocal                 = 0x24,
    LO_LoadLocalAddress          = 0x25,

    LO_LoadConstant_I4           = 0x26,
    LO_LoadConstant_I8           = 0x27,
    LO_LoadConstant_R4           = 0x28,
    LO_LoadConstant_R8           = 0x29,

    LO_LoadNull                  = 0x2A,
    LO_LoadString                = 0x2B,
    LO_LoadToken                 = 0x2C,

    LO_NewArray                  = 0x2D,
    LO_LoadLength                = 0x2E,

    LO_StoreElement              = 0x2F,
    LO_LoadElement               = 0x30,
    LO_LoadElementAddress        = 0x31,

    LO_StoreField                = 0x32,
    LO_LoadField                 = 0x33,
    LO_LoadFieldAddress          = 0x34,

    LO_StoreStaticField          = 0x35,
    LO_LoadStaticField           = 0x36,
    LO_LoadStaticFieldAddress    = 0x37,

    LO_StoreIndirect             = 0x38,
    LO_LoadIndirect              = 0x39,

    LO_InitObject                = 0x3A,
    LO_LoadObject                = 0x3B,
    LO_CopyObject                = 0x3C,
    LO_StoreObject               = 0x3D,

    LO_Nop                       = 0x3E,

    LO_EndFilter                 = 0x3F,

    LO_Unsupported               = 0x40,

    LO_FIRST                     = LO_Not,
    LO_LAST                      = LO_EndFilter,
}CLR_LOGICAL_OPCODE;

///////////////////////////////////////////////////////////////////////////////////////////////////

typedef enum _CLR_OpcodeParam
{
    CLR_OpcodeParam_Field         =  0,
    CLR_OpcodeParam_Method        =  1,
    CLR_OpcodeParam_Type          =  2,
    CLR_OpcodeParam_String        =  3,
    CLR_OpcodeParam_Tok           =  4,
    CLR_OpcodeParam_Sig           =  5,
    CLR_OpcodeParam_BrTarget      =  6,
    CLR_OpcodeParam_ShortBrTarget =  7,
    CLR_OpcodeParam_I             =  8,
    CLR_OpcodeParam_I8            =  9,
    CLR_OpcodeParam_None          = 10,
    CLR_OpcodeParam_R             = 11,
    CLR_OpcodeParam_Switch        = 12,
    CLR_OpcodeParam_Var           = 13,
    CLR_OpcodeParam_ShortI        = 14,
    CLR_OpcodeParam_ShortR        = 15,
    CLR_OpcodeParam_ShortVar      = 16,
}CLR_OpcodeParam;

#define CanCompressOpParamToken(opParam) (opParam >= CLR_OpcodeParam_Field && opParam <= CLR_OpcodeParam_String)
#define IsOpParamToken(opParam) (opParam >= CLR_OpcodeParam_Field && opParam <= CLR_OpcodeParam_Sig)

//--//

typedef enum _CLR_FlowControl
{
    CLR_FlowControl_NEXT        = 0,
    CLR_FlowControl_CALL        = 1,
    CLR_FlowControl_RETURN      = 2,
    CLR_FlowControl_BRANCH      = 3,
    CLR_FlowControl_COND_BRANCH = 4,
    CLR_FlowControl_THROW       = 5,
    CLR_FlowControl_BREAK       = 6,
    CLR_FlowControl_META        = 7,
}CLR_FlowControl;

//--//

#define c_CLR_StringTable_Version 1

///////////////////////////////////////////////////////////////////////////////////////////////////

typedef enum _CLR_TABLESENUM
{
    TBL_AssemblyRef    = 0x00000000,
    TBL_TypeRef        = 0x00000001,
    TBL_FieldRef       = 0x00000002,
    TBL_MethodRef      = 0x00000003,
    TBL_TypeDef        = 0x00000004,
    TBL_FieldDef       = 0x00000005,
    TBL_MethodDef      = 0x00000006,
    TBL_Attributes     = 0x00000007,
    TBL_TypeSpec       = 0x00000008,
    TBL_Resources      = 0x00000009,
    TBL_ResourcesData  = 0x0000000A,
    TBL_Strings        = 0x0000000B,
    TBL_Signatures     = 0x0000000C,
    TBL_ByteCode       = 0x0000000D,
    TBL_ResourcesFiles = 0x0000000E,
    TBL_EndOfAssembly  = 0x0000000F,
    TBL_Max            = 0x00000010,        
}CLR_TABLESENUM;

typedef enum _CLR_CorCallingConvention
{
    /////////////////////////////////////////////////////////////////////////////////////////////
    //
    // This is based on CorCallingConvention.
    //
    PIMAGE_CEE_CS_CALLCONV_DEFAULT       = 0x0,

    PIMAGE_CEE_CS_CALLCONV_VARARG        = 0x5,
    PIMAGE_CEE_CS_CALLCONV_FIELD         = 0x6,
    PIMAGE_CEE_CS_CALLCONV_LOCAL_SIG     = 0x7,
    PIMAGE_CEE_CS_CALLCONV_PROPERTY      = 0x8,
    PIMAGE_CEE_CS_CALLCONV_UNMGD         = 0x9,
    PIMAGE_CEE_CS_CALLCONV_GENERICINST   = 0xa,  // generic method instantiation
    PIMAGE_CEE_CS_CALLCONV_NATIVEVARARG  = 0xb,  // used ONLY for 64bit vararg PInvoke calls
    PIMAGE_CEE_CS_CALLCONV_MAX           = 0xc,  // first invalid calling convention
        
    // The high bits of the calling convention convey additional info
    PIMAGE_CEE_CS_CALLCONV_MASK          = 0x0f, // Calling convention is bottom 4 bits
    PIMAGE_CEE_CS_CALLCONV_HASTHIS       = 0x20, // Top bit indicates a 'this' parameter
    PIMAGE_CEE_CS_CALLCONV_EXPLICITTHIS  = 0x40, // This parameter is explicitly in the signature
    PIMAGE_CEE_CS_CALLCONV_GENERIC       = 0x10, // Generic method sig with explicit number of type arguments (precedes ordinary parameter count)
    //
    // End of overlap with CorCallingConvention.
    //
    /////////////////////////////////////////////////////////////////////////////////////////////
}CLR_CorCallingConvention;

typedef enum _CLR_DataType // KEEP IN SYNC WITH Microsoft.SPOT.DataType!!
{
    DATATYPE_VOID                       , // 0 bytes

    DATATYPE_BOOLEAN                    , // 1 byte
    DATATYPE_I1                         , // 1 byte
    DATATYPE_U1                         , // 1 byte

    DATATYPE_CHAR                       , // 2 bytes
    DATATYPE_I2                         , // 2 bytes
    DATATYPE_U2                         , // 2 bytes

    DATATYPE_I4                         , // 4 bytes
    DATATYPE_U4                         , // 4 bytes
    DATATYPE_R4                         , // 4 bytes

    DATATYPE_I8                         , // 8 bytes
    DATATYPE_U8                         , // 8 bytes
    DATATYPE_R8                         , // 8 bytes
    DATATYPE_DATETIME                   , // 8 bytes     // Shortcut for System.DateTime
    DATATYPE_TIMESPAN                   , // 8 bytes     // Shortcut for System.TimeSpan
    DATATYPE_STRING                     ,

    DATATYPE_LAST_NONPOINTER            = DATATYPE_TIMESPAN, // This is the last type that doesn't need to be relocated.
    DATATYPE_LAST_PRIMITIVE_TO_PRESERVE = DATATYPE_R8      , // All the above types don't need fix-up on assignment.
#if defined(TINYCLR_NO_ASSEMBLY_STRINGS)    
    DATATYPE_LAST_PRIMITIVE_TO_MARSHAL  = DATATYPE_STRING,   // All the above types can be marshaled by assignment.
#else    
    DATATYPE_LAST_PRIMITIVE_TO_MARSHAL  = DATATYPE_TIMESPAN, // All the above types can be marshaled by assignment.
#endif
    DATATYPE_LAST_PRIMITIVE             = DATATYPE_STRING  , // All the above types don't need fix-up on assignment.

    DATATYPE_OBJECT                     , // Shortcut for System.Object
    DATATYPE_CLASS                      , // CLASS <class Token>
    DATATYPE_VALUETYPE                  , // VALUETYPE <class Token>
    DATATYPE_SZARRAY                    , // Shortcut for single dimension zero lower bound array SZARRAY <type>
    DATATYPE_BYREF                      , // BYREF <type>

    ////////////////////////////////////////

    DATATYPE_FREEBLOCK                  ,
    DATATYPE_CACHEDBLOCK                ,
    DATATYPE_ASSEMBLY                   ,
    DATATYPE_WEAKCLASS                  ,
    DATATYPE_REFLECTION                 ,
    DATATYPE_ARRAY_BYREF                ,
    DATATYPE_DELEGATE_HEAD              ,
    DATATYPE_DELEGATELIST_HEAD          ,
    DATATYPE_OBJECT_TO_EVENT            ,
    DATATYPE_BINARY_BLOB_HEAD           ,

    DATATYPE_THREAD                     ,
    DATATYPE_SUBTHREAD                  ,
    DATATYPE_STACK_FRAME                ,
    DATATYPE_TIMER_HEAD                 ,
    DATATYPE_LOCK_HEAD                  ,
    DATATYPE_LOCK_OWNER_HEAD            ,
    DATATYPE_LOCK_REQUEST_HEAD          ,
    DATATYPE_WAIT_FOR_OBJECT_HEAD       ,
    DATATYPE_FINALIZER_HEAD             ,
    DATATYPE_MEMORY_STREAM_HEAD         , // SubDataType?
    DATATYPE_MEMORY_STREAM_DATA         , // SubDataType?

    DATATYPE_SERIALIZER_HEAD            , // SubDataType?
    DATATYPE_SERIALIZER_DUPLICATE       , // SubDataType?
    DATATYPE_SERIALIZER_STATE           , // SubDataType?

    DATATYPE_ENDPOINT_HEAD              ,

    //These constants are shared by Debugger.dll, and cannot be conditionally compiled away.
    //This adds a couple extra bytes to the lookup table.  But frankly, the lookup table should probably 
    //be shrunk to begin with.  Most of the datatypes are used just to tag memory.
    //For those datatypes, perhaps we should use a subDataType instead (probably what the comments above are about).

    DATATYPE_RADIO_LAST                 = DATATYPE_ENDPOINT_HEAD + 3,

    DATATYPE_IO_PORT                    ,
    DATATYPE_IO_PORT_LAST               = DATATYPE_RADIO_LAST + 1,

    DATATYPE_VTU_PORT_LAST              = DATATYPE_IO_PORT_LAST + 1,

    DATATYPE_I2C_XACTION                ,
    DATATYPE_I2C_XACTION_LAST           = DATATYPE_VTU_PORT_LAST + 1,

#if defined(TINYCLR_APPDOMAINS)
    DATATYPE_APPDOMAIN_HEAD             ,
    DATATYPE_TRANSPARENT_PROXY          ,
    DATATYPE_APPDOMAIN_ASSEMBLY         ,
#endif
    DATATYPE_APPDOMAIN_LAST             = DATATYPE_I2C_XACTION_LAST + 3,

    DATATYPE_FIRST_INVALID              ,

    // Type modifies. This is exact copy of VALUES ELEMENT_TYPE_* from CorHdr.h
    // 
    
    DATATYPE_TYPE_MODIFIER       = 0x40,
    DATATYPE_TYPE_SENTINEL       = 0x01 | DATATYPE_TYPE_MODIFIER, // sentinel for varargs
    DATATYPE_TYPE_PINNED         = 0x05 | DATATYPE_TYPE_MODIFIER,
    DATATYPE_TYPE_R4_HFA         = 0x06 | DATATYPE_TYPE_MODIFIER, // used only internally for R4 HFA types
    DATATYPE_TYPE_R8_HFA         = 0x07 | DATATYPE_TYPE_MODIFIER, // used only internally for R8 HFA types
}CLR_DataType;

typedef enum _CLR_ReflectionType
{
    REFLECTION_INVALID      = 0x00,
    REFLECTION_ASSEMBLY     = 0x01,
    REFLECTION_TYPE         = 0x02,
    REFLECTION_TYPE_DELAYED = 0x03,
    REFLECTION_CONSTRUCTOR  = 0x04,
    REFLECTION_METHOD       = 0x05,
    REFLECTION_FIELD        = 0x06,
}CLR_ReflectionType;

////////////////////////////////////////////////////////////////////////////////////////////////////

//INLINE CLR_TABLESENUM CLR_TypeFromTk( CLR_UINT32 tk ) { return (CLR_TABLESENUM)(tk >> 24);       }
//INLINE CLR_UINT32     CLR_DataFromTk( CLR_UINT32 tk ) { return                  tk & 0x00FFFFFF; }
//
//INLINE CLR_UINT32 CLR_TkFromType( CLR_TABLESENUM tbl, CLR_UINT32 data ) { return ((((CLR_UINT32)tbl) << 24) & 0xFF000000) | (data & 0x00FFFFFF); }
//
////--//
//
//INLINE CLR_UINT32 CLR_UncompressStringToken( CLR_UINT32 tk )
//{
//    return CLR_TkFromType( TBL_Strings, tk );
//}
//
//INLINE CLR_UINT32 CLR_UncompressTypeToken( CLR_UINT32 tk )
//{
//    static const CLR_TABLESENUM c_lookup[ 3 ] = { TBL_TypeDef, TBL_TypeRef, TBL_TypeSpec };
//
//    return CLR_TkFromType( c_lookup[ (tk >> 14) & 3 ], 0x3fff & tk );
//}
//
//INLINE CLR_UINT32 CLR_UncompressFieldToken( CLR_UINT32 tk )
//{
//    static const CLR_TABLESENUM c_lookup[ 2 ] = { TBL_FieldDef, TBL_FieldRef };
//
//    return CLR_TkFromType( c_lookup[ (tk >> 15) & 1 ], 0x7fff & tk );
//}
//
//INLINE CLR_UINT32 CLR_UncompressMethodToken( CLR_UINT32 tk )
//{
//    static const CLR_TABLESENUM c_lookup[ 2 ] = { TBL_MethodDef, TBL_MethodRef };
//    return CLR_TkFromType( c_lookup[ (tk >> 15) & 1 ], 0x7fff & tk );
//}
//
//#if defined(PLATFORM_WINDOWS)
//
//CLR_UINT32 CLR_ReadTokenCompressed( CLR_PMETADATA ip, CLR_OPCODE opcode );
//
//#endif

//--//

//HRESULT CLR_CompressTokenHelper( const CLR_TABLESENUM *tables, CLR_UINT16 cTables, CLR_UINT32& tk );
//
//INLINE HRESULT CLR_CompressStringToken( CLR_UINT32& tk )
//{
//   static const CLR_TABLESENUM c_lookup[ 1 ] = { TBL_Strings };
//
//   return CLR_CompressTokenHelper( c_lookup, ARRAYSIZE(c_lookup), tk );
//}
//
//INLINE HRESULT CLR_CompressTypeToken( CLR_UINT32& tk )
//{
//    static const CLR_TABLESENUM c_lookup[ 3 ] = { TBL_TypeDef, TBL_TypeRef, TBL_TypeSpec };
//
//    return CLR_CompressTokenHelper( c_lookup, ARRAYSIZE(c_lookup), tk );
//}
//
//INLINE HRESULT CLR_CompressFieldToken( CLR_UINT32& tk )
//{
//    static const CLR_TABLESENUM c_lookup[ 2 ] = { TBL_FieldDef, TBL_FieldRef };
//
//    return CLR_CompressTokenHelper( c_lookup, ARRAYSIZE(c_lookup), tk );
//}
//
//INLINE HRESULT CLR_CompressMethodToken( CLR_UINT32& tk )
//{
//    static const CLR_TABLESENUM c_lookup[ 2 ] = { TBL_MethodDef, TBL_MethodRef };
//
//    return CLR_CompressTokenHelper( c_lookup, ARRAYSIZE(c_lookup), tk );
//}

//--//

//INLINE bool CLR_CompressData( CLR_UINT32 val, CLR_UINT8* p )
//{
//    CLR_UINT8* ptr = p;
//
//    if(val <= 0x7F)
//    {
//        *ptr++ = (CLR_UINT8)(val);
//    }
//    else if(val <= 0x3FFF)
//    {
//        *ptr++ = (CLR_UINT8)((val >> 8) | 0x80);
//        *ptr++ = (CLR_UINT8)((val     )       );
//    }
//    else if(val <= 0x1FFFFFFF)
//    {
//        *ptr++ = (CLR_UINT8)((val >> 24) | 0xC0);
//        *ptr++ = (CLR_UINT8)((val >> 16)       );
//        *ptr++ = (CLR_UINT8)((val >>  8)       );
//        *ptr++ = (CLR_UINT8)((val      )       );
//    }
//    else
//    {
//        return false;
//    }
//
//    p = ptr;
//
//    return true;
//}
//
//INLINE CLR_UINT32 CLR_UncompressData( const CLR_UINT8* p )
//{
//    CLR_PMETADATA ptr = p;
//    CLR_UINT32    val = *ptr++;
//
//    // Handle smallest data inline.
//    if((val & 0x80) == 0x00)        // 0??? ????
//    {
//    }
//    else if((val & 0xC0) == 0x80)  // 10?? ????
//    {
//        val  =             (val & 0x3F) << 8;
//        val |= (CLR_UINT32)*ptr++           ;
//    }
//    else // 110? ????
//    {
//        val  =             (val & 0x1F) << 24;
//        val |= (CLR_UINT32)*ptr++       << 16;
//        val |= (CLR_UINT32)*ptr++       <<  8;
//        val |= (CLR_UINT32)*ptr++       <<  0;
//    }
//
//    p = ptr;
//
//    return val;
//}
//
//INLINE CLR_DataType CLR_UncompressElementType( const CLR_UINT8* p )
//{
//    return (CLR_DataType)*p++;
//}
//
//INLINE CLR_UINT32 CLR_TkFromStream( const CLR_UINT8* p )
//{
//    static const CLR_TABLESENUM c_lookup[ 4 ] = { TBL_TypeDef, TBL_TypeRef, TBL_TypeSpec, TBL_Max };
//
//    CLR_UINT32 data = CLR_UncompressData( p );
//
//    return CLR_TkFromType( c_lookup[ data & 3 ], data >> 2 );
//}

//--//--//--//
#if defined(PLATFORM_WINDOWS)

#define MINICLR_READ_UNALIGNED_UINT8(arg,ip)  arg = *(__declspec(align(1)) const CLR_UINT8 *)ip; ip += sizeof(CLR_UINT8 )
#define MINICLR_READ_UNALIGNED_UINT16(arg,ip) arg = *(__declspec(align(1)) const CLR_UINT16*)ip; ip += sizeof(CLR_UINT16)
#define MINICLR_READ_UNALIGNED_UINT32(arg,ip) arg = *(__declspec(align(1)) const CLR_UINT32*)ip; ip += sizeof(CLR_UINT32)
#define MINICLR_READ_UNALIGNED_UINT64(arg,ip) arg = *(__declspec(align(1)) const CLR_UINT64*)ip; ip += sizeof(CLR_UINT64)

#define MINICLR_READ_UNALIGNED_INT8(arg,ip)   arg = *(__declspec(align(1)) const CLR_INT8 * )ip; ip += sizeof(CLR_INT8  )
#define MINICLR_READ_UNALIGNED_INT16(arg,ip)  arg = *(__declspec(align(1)) const CLR_INT16* )ip; ip += sizeof(CLR_INT16 )
#define MINICLR_READ_UNALIGNED_INT32(arg,ip)  arg = *(__declspec(align(1)) const CLR_INT32* )ip; ip += sizeof(CLR_INT32 )
#define MINICLR_READ_UNALIGNED_INT64(arg,ip)  arg = *(__declspec(align(1)) const CLR_INT64* )ip; ip += sizeof(CLR_INT64 )

//--//

#define MINICLR_WRITE_UNALIGNED_UINT8(ip,arg)  *(__declspec(align(1)) CLR_UINT8 *)ip = arg; ip += sizeof(CLR_UINT8 )
#define MINICLR_WRITE_UNALIGNED_UINT16(ip,arg) *(__declspec(align(1)) CLR_UINT16*)ip = arg; ip += sizeof(CLR_UINT16)
#define MINICLR_WRITE_UNALIGNED_UINT32(ip,arg) *(__declspec(align(1)) CLR_UINT32*)ip = arg; ip += sizeof(CLR_UINT32)
#define MINICLR_WRITE_UNALIGNED_UINT64(ip,arg) *(__declspec(align(1)) CLR_UINT64*)ip = arg; ip += sizeof(CLR_UINT64)

#define MINICLR_WRITE_UNALIGNED_INT8(ip,arg)   *(__declspec(align(1)) CLR_INT8 * )ip = arg; ip += sizeof(CLR_INT8  )
#define MINICLR_WRITE_UNALIGNED_INT16(ip,arg)  *(__declspec(align(1)) CLR_INT16* )ip = arg; ip += sizeof(CLR_INT16 )
#define MINICLR_WRITE_UNALIGNED_INT32(ip,arg)  *(__declspec(align(1)) CLR_INT32* )ip = arg; ip += sizeof(CLR_INT32 )
#define MINICLR_WRITE_UNALIGNED_INT64(ip,arg)  *(__declspec(align(1)) CLR_INT64* )ip = arg; ip += sizeof(CLR_INT64 )

#else

#define MINICLR_READ_UNALIGNED_UINT8(arg,ip)  arg = *(__packed CLR_UINT8 *)ip; ip += sizeof(CLR_UINT8 )
#define MINICLR_READ_UNALIGNED_UINT16(arg,ip) arg = *(__packed CLR_UINT16*)ip; ip += sizeof(CLR_UINT16)
#define MINICLR_READ_UNALIGNED_UINT32(arg,ip) arg = *(__packed CLR_UINT32*)ip; ip += sizeof(CLR_UINT32)
#define MINICLR_READ_UNALIGNED_UINT64(arg,ip) arg = *(__packed CLR_UINT64*)ip; ip += sizeof(CLR_UINT64)

#define MINICLR_READ_UNALIGNED_INT8(arg,ip)   arg = *(__packed CLR_INT8 * )ip; ip += sizeof(CLR_INT8  )
#define MINICLR_READ_UNALIGNED_INT16(arg,ip)  arg = *(__packed CLR_INT16* )ip; ip += sizeof(CLR_INT16 )
#define MINICLR_READ_UNALIGNED_INT32(arg,ip)  arg = *(__packed CLR_INT32* )ip; ip += sizeof(CLR_INT32 )
#define MINICLR_READ_UNALIGNED_INT64(arg,ip)  arg = *(__packed CLR_INT64* )ip; ip += sizeof(CLR_INT64 )

//--//

#define MINICLR_WRITE_UNALIGNED_UINT8(ip,arg)  *(__packed CLR_UINT8 *)ip = arg; ip += sizeof(CLR_UINT8 )
#define MINICLR_WRITE_UNALIGNED_UINT16(ip,arg) *(__packed CLR_UINT16*)ip = arg; ip += sizeof(CLR_UINT16)
#define MINICLR_WRITE_UNALIGNED_UINT32(ip,arg) *(__packed CLR_UINT32*)ip = arg; ip += sizeof(CLR_UINT32)
#define MINICLR_WRITE_UNALIGNED_UINT64(ip,arg) *(__packed CLR_UINT64*)ip = arg; ip += sizeof(CLR_UINT64)

#define MINICLR_WRITE_UNALIGNED_INT8(ip,arg)   *(__packed CLR_INT8 * )ip = arg; ip += sizeof(CLR_INT8  )
#define MINICLR_WRITE_UNALIGNED_INT16(ip,arg)  *(__packed CLR_INT16* )ip = arg; ip += sizeof(CLR_INT16 )
#define MINICLR_WRITE_UNALIGNED_INT32(ip,arg)  *(__packed CLR_INT32* )ip = arg; ip += sizeof(CLR_INT32 )
#define MINICLR_WRITE_UNALIGNED_INT64(ip,arg)  *(__packed CLR_INT64* )ip = arg; ip += sizeof(CLR_INT64 )

#endif

//--//
#define MINICLR_READ_UNALIGNED_OPCODE(op,ip) op = CLR_OPCODE(*ip++); if(op == CEE_PREFIX1) { opcode = CLR_OPCODE(*ip++ + 256); }

#define MINICLR_READ_UNALIGNED_COMPRESSED_FIELDTOKEN(arg,ip)  MINICLR_READ_UNALIGNED_UINT16( arg, ip ); arg = CLR_UncompressFieldToken ( arg )
#define MINICLR_READ_UNALIGNED_COMPRESSED_METHODTOKEN(arg,ip) MINICLR_READ_UNALIGNED_UINT16( arg, ip ); arg = CLR_UncompressMethodToken( arg )
#define MINICLR_READ_UNALIGNED_COMPRESSED_TYPETOKEN(arg,ip)   MINICLR_READ_UNALIGNED_UINT16( arg, ip ); arg = CLR_UncompressTypeToken  ( arg )
#define MINICLR_READ_UNALIGNED_COMPRESSED_STRINGTOKEN(arg,ip) MINICLR_READ_UNALIGNED_UINT16( arg, ip ); arg = CLR_UncompressStringToken( arg )


//--//

//INLINE CLR_OPCODE CLR_ReadNextOpcode( CLR_PMETADATA ip )
//{
//    CLR_PMETADATA ptr    = ip;
//    CLR_OPCODE       opcode = (CLR_OPCODE)(*ptr++);
//
//    if(opcode == CEE_PREFIX1)
//    {
//        opcode = (CLR_OPCODE)(*ptr++ + 256);
//    }
//
//    ip = ptr;
//
//    return opcode;
//}
//
//INLINE CLR_OPCODE CLR_ReadNextOpcodeCompressed( CLR_PMETADATA ip )
//{
//    CLR_PMETADATA ptr    = ip;
//    CLR_OPCODE       opcode = (CLR_OPCODE)(*ptr++);
//
//    if(opcode == CEE_PREFIX1)
//    {
//        opcode = (CLR_OPCODE)(*ptr++ + 256);
//    }
//
//    ip = ptr;
//
//    return opcode;
//}

//--//

#define FETCH_ARG_UINT8(arg,ip)                   CLR_UINT32 arg; MINICLR_READ_UNALIGNED_UINT8 ( arg, ip )
#define FETCH_ARG_UINT16(arg,ip)                  CLR_UINT32 arg; MINICLR_READ_UNALIGNED_UINT16( arg, ip )
#define FETCH_ARG_UINT32(arg,ip)                  CLR_UINT32 arg; MINICLR_READ_UNALIGNED_UINT32( arg, ip )
#define FETCH_ARG_UINT64(arg,ip)                  CLR_UINT64 arg; MINICLR_READ_UNALIGNED_UINT64( arg, ip )

#define FETCH_ARG_INT8(arg,ip)                    CLR_INT32  arg; MINICLR_READ_UNALIGNED_INT8 ( arg, ip )
#define FETCH_ARG_INT16(arg,ip)                   CLR_INT32  arg; MINICLR_READ_UNALIGNED_INT16( arg, ip )
#define FETCH_ARG_INT32(arg,ip)                   CLR_INT32  arg; MINICLR_READ_UNALIGNED_INT32( arg, ip )
#define FETCH_ARG_INT64(arg,ip)                   CLR_INT64  arg; MINICLR_READ_UNALIGNED_INT64( arg, ip )

#define FETCH_ARG_COMPRESSED_STRINGTOKEN(arg,ip) CLR_UINT32 arg; MINICLR_READ_UNALIGNED_COMPRESSED_STRINGTOKEN( arg, ip )
#define FETCH_ARG_COMPRESSED_FIELDTOKEN(arg,ip)  CLR_UINT32 arg; MINICLR_READ_UNALIGNED_COMPRESSED_FIELDTOKEN ( arg, ip )
#define FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip)   CLR_UINT32 arg; MINICLR_READ_UNALIGNED_COMPRESSED_TYPETOKEN  ( arg, ip )
#define FETCH_ARG_COMPRESSED_METHODTOKEN(arg,ip) CLR_UINT32 arg; MINICLR_READ_UNALIGNED_COMPRESSED_METHODTOKEN( arg, ip )
#define FETCH_ARG_TOKEN(arg,ip)                  CLR_UINT32 arg; MINICLR_READ_UNALIGNED_UINT32                ( arg, ip )

//--//

#if defined(PLATFORM_WINDOWS)

CLR_PMETADATA CLR_SkipBodyOfOpcode          ( CLR_PMETADATA ip, CLR_OPCODE opcode );
CLR_PMETADATA CLR_SkipBodyOfOpcodeCompressed( CLR_PMETADATA ip, CLR_OPCODE opcode );

#endif

////////////////////////////////////////////////////////////////////////////////////////////////////

typedef struct _CLR_RECORD_VERSION
{
    CLR_UINT16 iMajorVersion;
    CLR_UINT16 iMinorVersion;
    CLR_UINT16 iBuildNumber;
    CLR_UINT16 iRevisionNumber;
}CLR_RECORD_VERSION;



typedef struct _CLR_RECORD_ASSEMBLY
{
#define  CLR_ASSEMBLY_FLAGS_NEEDREBOOT	1
#define  CLR_ASSEMBLY_FLAGS_PATCH				2

    CLR_UINT8          marker[ 8 ];
    //
    CLR_UINT32         headerCRC;
    CLR_UINT32         assemblyCRC;
    CLR_UINT32         flags;
    //
    CLR_UINT32         nativeMethodsChecksum;
    CLR_UINT32         patchEntryOffset;
    //
    CLR_RECORD_VERSION version;
    //
    CLR_STRING         assemblyName;    // TBL_Strings
    CLR_UINT16         stringTableVersion;
    //
    CLR_OFFSET_LONG    startOfTables[ TBL_Max ];
    CLR_UINT32         numOfPatchedMethods;
    //             
    //For every table, a number of bytes that were padded to the end of the table
    //to align to DWORD.  Each table starts at a DWORD boundary, and ends 
    //at a DWORD boundary.  Some of these tables will, by construction, have
    //no padding, and all will have values in the range [0-3].  This isn't the most
    //compact form to hold this information, but it only costs 16 bytes/assembly.
    //Trying to only align some of the tables is just much more hassle than it's worth.
    //And, of course, this field also has to be DWORD-aligned.
    CLR_UINT8          paddingOfTables[ ((TBL_Max-1)+3)/4*4 ];
    //--//

//    bool GoodHeader  () ;
//    bool GoodAssembly() ;
//
//#if defined(PLATFORM_WINDOWS)
//    void ComputeCRC();
//#endif
//
//    CLR_OFFSET_LONG SizeOfTable( CLR_TABLESENUM tbl ) { return startOfTables[ tbl+1 ] - startOfTables[ tbl ] - paddingOfTables[ tbl ]; }
//
//    CLR_OFFSET_LONG TotalSize() { return startOfTables[ TBL_EndOfAssembly ]; }
//
//    //--//
//
//    static CLR_UINT32 ComputeAssemblyHash( LPCSTR name, const CLR_RECORD_VERSION* ver );
}CLR_RECORD_ASSEMBLY;

typedef struct _CLR_RECORD_ASSEMBLYREF
{
    CLR_STRING         name;            // TBL_Strings
    CLR_UINT16         pad;
    //
    CLR_RECORD_VERSION version;
}CLR_RECORD_ASSEMBLYREF;

typedef struct _CLR_RECORD_TYPEREF
{
    CLR_STRING name;            // TBL_Strings
    CLR_STRING nameSpace;       // TBL_Strings
    //
    CLR_IDX    scope;           // TBL_AssemblyRef | TBL_TypeRef // 0x8000
    CLR_UINT16 pad;
}CLR_RECORD_TYPEREF;

typedef struct _CLR_RECORD_FIELDREF
{
    CLR_STRING name;            // TBL_Strings
    CLR_IDX    container;       // TBL_TypeRef
    //
    CLR_SIG    sig;             // TBL_Signatures
    CLR_UINT16 pad;
}CLR_RECORD_FIELDREF;

typedef struct _CLR_RECORD_METHODREF
{
    CLR_STRING name;            // TBL_Strings
    CLR_IDX    container;       // TBL_TypeRef
    //
    CLR_SIG    sig;             // TBL_Signatures
    CLR_UINT16 pad;
}CLR_RECORD_METHODREF;

typedef struct _CLR_RECORD_TYPEDEF
{
#define CLR_TYPEDEF_FLAGS_SCOPE_MASK												0x0007
#define CLR_TYPEDEF_FLAGS_SCOPE_NOT_PUBLIC									0x0000 // Class is not public scope.
#define CLR_TYPEDEF_FLAGS_SCOPE_PUBLIC											0x0001 // Class is public scope.
#define CLR_TYPEDEF_FLAGS_SCOPE_NESTED_PUBLIC								0x0002 // Class is nested with public visibility.
#define CLR_TYPEDEF_FLAGS_SCOPE_NESTED_PRIVATE							0x0003 // Class is nested with private visibility.
#define CLR_TYPEDEF_FLAGS_SCOPE_NESTED_FAMILY								0x0004 // Class is nested with family visibility.
#define CLR_TYPEDEF_FLAGS_SCOPE_NESTED_ASSEMBLY							0x0005 // Class is nested with assembly visibility.
#define CLR_TYPEDEF_FLAGS_SCOPE_NESTED_FAMILY_AND_ASSEMBLY	0x0006 // Class is nested with family and assembly visibility.
#define CLR_TYPEDEF_FLAGS_SCOPE_NESTED_FAMILY_OR_ASSEMBLY		0x0007 // Class is nested with family or assembly visibility.

#define CLR_TYPEDEF_FLAGS_SERIALIZABLE											0x0008

#define CLR_TYPEDEF_FLAGS_SEMANTICS_MASK										0x0030
#define CLR_TYPEDEF_FLAGS_SEMANTICS_CLASS										0x0000
#define CLR_TYPEDEF_FLAGS_SEMANTICS_VALUETYPE								0x0010
#define CLR_TYPEDEF_FLAGS_SEMANTICS_INTERFACE								0x0020
#define CLR_TYPEDEF_FLAGS_SEMANTICS_ENUM										0x0030

#define CLR_TYPEDEF_FLAGS_ABSTRACT													0x0040
#define CLR_TYPEDEF_FLAGS_SEALED														0x0080

#define CLR_TYPEDEF_FLAGS_SPECIALNAME												0x0100
#define CLR_TYPEDEF_FLAGS_DELEGATE													0x0200
#define CLR_TYPEDEF_FLAGS_MULTICAST_DELEGATE								0x0400

#define CLR_TYPEDEF_FLAGS_PATCHED														0x0800

#define CLR_TYPEDEF_FLAGS_BEFORE_FIELDINIT									0x1000
#define CLR_TYPEDEF_FLAGS_HAS_SECURITY											0x2000
#define CLR_TYPEDEF_FLAGS_HAS_FINALIZER											0x4000
#define CLR_TYPEDEF_FLAGS_HAS_ATTRIBUTES										0x8000


    CLR_STRING name;            // TBL_Strings
    CLR_STRING nameSpace;       // TBL_Strings
    //
    CLR_IDX    extends;         // TBL_TypeDef | TBL_TypeRef // 0x8000
    CLR_IDX    enclosingType;   // TBL_TypeDef
    //
    CLR_SIG    interfaces;      // TBL_Signatures
    CLR_IDX    methods_First;   // TBL_MethodDef
    //
    CLR_UINT8  vMethods_Num;
    CLR_UINT8  iMethods_Num;
    CLR_UINT8  sMethods_Num;
    CLR_UINT8  dataType;
    //
    CLR_IDX    sFields_First;   // TBL_FieldDef
    CLR_IDX    iFields_First;   // TBL_FieldDef
    //
    CLR_UINT8  sFields_Num;
    CLR_UINT8  iFields_Num;
    CLR_UINT16 flags;

    //--//

    //bool IsEnum    () { return (flags & (TD_Semantics_Mask                 )) == TD_Semantics_Enum; }
    //bool IsDelegate() { return (flags & (TD_Delegate | TD_MulticastDelegate)) != 0                ; }
}CLR_RECORD_TYPEDEF;

typedef struct _CLR_RECORD_FIELDDEF
{

#define  CLR_FIELDDEF_FLAGS_SCOPE_MASK									0x0007
#define  CLR_FIELDDEF_FLAGS_SCOPE_PRIVATE_SCOPE					0x0000     // Member not referenceable.
#define  CLR_FIELDDEF_FLAGS_SCOPE_PRIVATE								0x0001     // Accessible only by the parent type.
#define  CLR_FIELDDEF_FLAGS_SCOPE_FAMILY_AND_ASSEMBLY		0x0002     // Accessible by sub-types only in this Assembly.
#define  CLR_FIELDDEF_FLAGS_SCOPE_ASSEMBLY							0x0003     // Accessibly by anyone in the Assembly.
#define  CLR_FIELDDEF_FLAGS_SCOPE_FAMILY								0x0004     // Accessible only by type and sub-types.
#define  CLR_FIELDDEF_FLAGS_SCOPE_FAMILY_OR_ASSEMBLY		0x0005     // Accessibly by sub-types anywhere, plus anyone in assembly.
#define  CLR_FIELDDEF_FLAGS_SCOPE_PUBLIC								0x0006     // Accessibly by anyone who has visibility to this scope.

#define  CLR_FIELDDEF_FLAGS_NOT_SERIALIZED							0x0008     // Field does not have to be serialized when type is remoted.

#define  CLR_FIELDDEF_FLAGS_STATIC											0x0010     // Defined on type, else per instance.
#define  CLR_FIELDDEF_FLAGS_INITONLY										0x0020     // Field may only be initialized, not written to after init.
#define  CLR_FIELDDEF_FLAGS_LITERAL											0x0040     // Value is compile time constant.

#define  CLR_FIELDDEF_FLAGS_SPECIAL_NAME								0x0100     // field is special.  Name describes how.
#define  CLR_FIELDDEF_FLAGS_HAS_DEFAULT									0x0200     // Field has default.
#define  CLR_FIELDDEF_FLAGS_HAS_FIELD_RVA								0x0400     // Field has RVA.

#define  CLR_FIELDDEF_FLAGS_NO_REFLECTION								0x0800     // field does not allow reflection

#define  CLR_FIELDDEF_FLAGS_HAS_ATTRIBUTES							0x8000


    CLR_STRING name;            // TBL_Strings
    CLR_SIG    sig;             // TBL_Signatures
    //
    CLR_SIG    defaultValue;    // TBL_Signatures
    CLR_UINT16 flags;
}CLR_RECORD_FIELDDEF;

typedef struct _CLR_RECORD_METHODDEF
{

#define  CLR_METHODDEF_FLAGS_SCOPE_MASK									0x00000007
#define  CLR_METHODDEF_FLAGS_SCOPE_PRIVATE_SCOPE				0x00000000     // Member not referenceable.
#define  CLR_METHODDEF_FLAGS_SCOPE_PRIVATE							0x00000001     // Accessible only by the parent type.
#define  CLR_METHODDEF_FLAGS_SCOPE_FAMILY_AND_ASSEMBLY	0x00000002     // Accessible by sub-types only in this Assembly.
#define  CLR_METHODDEF_FLAGS_SCOPE_ASSEMBLY							0x00000003     // Accessibly by anyone in the Assembly.
#define  CLR_METHODDEF_FLAGS_SCOPE_FAMILY								0x00000004     // Accessible only by type and sub-types.
#define  CLR_METHODDEF_FLAGS_SCOPE_FAMILY_OR_ASSEMBLY		0x00000005     // Accessibly by sub-types anywhere, plus anyone in assembly.
#define  CLR_METHODDEF_FLAGS_SCOPE_PUBLIC								0x00000006     // Accessibly by anyone who has visibility to this scope.

#define  CLR_METHODDEF_FLAGS_STATIC											0x00000010     // Defined on type, else per instance.
#define  CLR_METHODDEF_FLAGS_FINAL											0x00000020     // Method may not be overridden.
#define  CLR_METHODDEF_FLAGS_VIRTUAL										0x00000040     // Method virtual.
#define  CLR_METHODDEF_FLAGS_HIDE_BY_SIG								0x00000080     // Method hides by name+sig, else just by name.

#define  CLR_METHODDEF_FLAGS_VTABLELAYOUT_MASK					0x00000100
#define  CLR_METHODDEF_FLAGS_REUSE_SLOT									0x00000000     // The default.
#define  CLR_METHODDEF_FLAGS_NEW_SLOT										0x00000100     // Method always gets a new slot in the vtable.
#define  CLR_METHODDEF_FLAGS_ABSTRACT										0x00000200     // Method does not provide an implementation.
#define  CLR_METHODDEF_FLAGS_SPECIAL_NAME								0x00000400     // Method is special.  Name describes how.
#define  CLR_METHODDEF_FLAGS_NATIVE_PROFILED						0x00000800

#define  CLR_METHODDEF_FLAGS_CONSTRUCTOR								0x00001000
#define  CLR_METHODDEF_FLAGS_STATIC_CONSTRUCTOR					0x00002000
#define  CLR_METHODDEF_FLAGS_FINALIZER									0x00004000    

#define  CLR_METHODDEF_FLAGS_DELEGATE_CONSTRUCTOR				0x00010000
#define  CLR_METHODDEF_FLAGS_DELEGATE_INVOKE						0x00020000
#define  CLR_METHODDEF_FLAGS_DELEGATE_BEGIN_INVOKE			0x00040000
#define  CLR_METHODDEF_FLAGS_DELEGATE_END_INVOKE				0x00080000

#define  CLR_METHODDEF_FLAGS_SYNCHRONIZED								0x01000000
#define  CLR_METHODDEF_FLAGS_GLOBALLY_SYNCHRONIZED			0x02000000
#define  CLR_METHODDEF_FLAGS_PATCHED										0x04000000
#define  CLR_METHODDEF_FLAGS_ENTRYPOINT									0x08000000
#define  CLR_METHODDEF_FLAGS_REQUIRE_SECOBJECT					0x10000000     // Method calls another method containing security code.
#define  CLR_METHODDEF_FLAGS_HAS_SECURITY								0x20000000     // Method has security associate with it.
#define  CLR_METHODDEF_FLAGS_HAS_EXCEPTION_HANDLERS			0x40000000
#define  CLR_METHODDEF_FLAGS_HAS_ATTRIBUTES							0x80000000


    CLR_STRING name;            // TBL_Strings
    CLR_OFFSET RVA;
    //
    CLR_UINT32 flags;
    //
    CLR_UINT8  retVal;
    CLR_UINT8  numArgs;
    CLR_UINT8  numLocals;
    CLR_UINT8  lengthEvalStack;
    //
    CLR_SIG    locals;          // TBL_Signatures
    CLR_SIG    sig;             // TBL_Signatures
}CLR_RECORD_METHODDEF;

typedef struct _CLR_RECORD_ATTRIBUTE
{
    CLR_UINT16 ownerType;       // one of TBL_TypeDef, TBL_MethodDef, or TBL_FieldDef.
    CLR_UINT16 ownerIdx;        // TBL_TypeDef | TBL_MethodDef | TBL_FielfDef
    CLR_UINT16 constructor;
    CLR_SIG    data;            // TBL_Signatures

    //CLR_UINT32 Key() { return *(CLR_UINT32*)&ownerType; }
}CLR_RECORD_ATTRIBUTE;

typedef struct _CLR_RECORD_TYPESPEC
{
    CLR_SIG    sig;             // TBL_Signatures
    CLR_UINT16 pad;
}CLR_RECORD_TYPESPEC;

typedef struct _CLR_RECORD_EH
{
#define  CLR_EH_FLAGS_CATCH		0x0000
#define  CLR_EH_FLAGS_CATCHALL 0x0001
#define  CLR_EH_FLAGS_FINALLY	0x0002
#define  CLR_EH_FLAGS_FILTER		0x0003

    //--//

    CLR_UINT16 mode;
    union {
      CLR_IDX    classToken;      // TBL_TypeDef | TBL_TypeRef
      CLR_OFFSET filterStart;
    };
    CLR_OFFSET tryStart;
    CLR_OFFSET tryEnd;
    CLR_OFFSET handlerStart;
    CLR_OFFSET handlerEnd;

    //--//

    //static CLR_PMETADATA ExtractEhFromByteCode( CLR_PMETADATA ipEnd, const CLR_RECORD_EH* ptrEh, CLR_UINT32* numEh );

    //CLR_UINT32 GetToken() ;
}CLR_RECORD_EH;

typedef struct _CLR_RECORD_RESOURCE_FILE
{
#define  CLR_RESOURCE_FILE_FLAGS_CURREN_VERSION 2

    CLR_UINT32 version;
    CLR_UINT32 sizeOfHeader;
    CLR_UINT32 sizeOfResourceHeader;
    CLR_UINT32 numberOfResources;
    CLR_STRING name;            // TBL_Strings
    CLR_UINT16 pad;
    CLR_UINT32 offset;          // TBL_Resource
}CLR_RECORD_RESOURCE_FILE;

typedef struct _CLR_RECORD_RESOURCE
{
#define  CLR_RESOURCE_FLAGS_INVALID				0x00
#define  CLR_RESOURCE_FLAGS_BITMAP				0x01
#define  CLR_RESOURCE_FLAGS_FONT					0x02
#define  CLR_RESOURCE_FLAGS_STRING				0x03
#define  CLR_RESOURCE_FLAGS_BINARY				0x04
#define  CLR_RESOURCE_FLAGS_PADDING_MASK	0x03

#define  CLR_RESOURCE_FLAGS_SENTINEL_ID		0x7FFF

    //
    // Sorted on id
    //
    CLR_INT16  id;
    CLR_UINT8  kind;
    CLR_UINT8  flags;
    CLR_UINT32 offset;
}CLR_RECORD_RESOURCE;


#if defined(PLATFORM_WINDOWS)
#pragma pack(pop, TINYCLR_TYPES_H)
#endif

#endif // _MINICLR_TYPES_H_
