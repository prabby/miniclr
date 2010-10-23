#pragma once

#include "TinyCLR_Types.h"

extern UINT32 SUPPORT_ComputeCRC( const void* rgBlock ,
																 int         nLength ,
																 UINT32      crc     );

//
//error define
//
#define  RESULT_SUCCESS							0L
#define  RESULT_INVALID_PARAMS			0x80000001L
#define  RESULT_INVALID_CHECKSUM		0x80000002L
#define  RESULT_UNKNOWN_INSRUCTION	0x80000003L	
#define  RESULT_UNSUPPORT_INSRUCTION	0x80000004L	

#define  RESULT_UNSUPPORT						0x80000010L
#define  RESULT_UNKNOWN							0x80000020L

//
//common 
//
#define  ReadByteX(dst,src,len) memcpy((void*)dst,(void*)src,(size_t)len); ((BYTE*)src) += len
#define  ReadByte(src) *((BYTE*)src); ((BYTE*)src) += 1
#define  ReadWord(src) *((WORD*)src); ((BYTE*)src) += 2
#define  ReadDWord(src) *((DWORD*)src); ((BYTE*)src) += 4
#define  ZeroByteX(dst,len) memset((void*)dst,0,(size_t)len)