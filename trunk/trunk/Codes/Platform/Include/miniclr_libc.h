#ifndef  __MINICLR_CRT_H
#define  __MINICLR_CRT_H

#include "MiniCLR_PlatformDef.h"

#if defined(PLATFORM_WINDOWS)

#define crt_memset memset
#define crt_memcpy memcpy
#define crt_memcmp memcmp

#else


#endif //defined(PLATFORM_WINDOWS)

#endif