#pragma once

#include <windows.h>
#include "TinyCLR_Types.h"
#include "MetaParse.h"

CHAR * CLR_OPCODE_NAME[] = 
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) s,
#include <opcode.def>
#undef OPDEF
	NULL
};

extern void Parse_Print(LPCSTR   lpFmt,...);

HRESULT Parse_Dasm_IL(IN PBYTE pMtdPtr ,IN DWORD dwSize,IN CLR_RT_Assembly* pAssem)
{
	if(!pMtdPtr || !pAssem) return RESULT_INVALID_PARAMS;
	
	PBYTE ip = pMtdPtr;

	while(true)
	{

		if(ip >= (pMtdPtr + dwSize))
			break;

		CLR_OPCODE op = CLR_OPCODE(*ip++);

Execute_RestartDecoding:
		{
			//
			//
			////////////////////////

			//--//
			Parse_Print("IL_%04x:  %-10s",ip - pMtdPtr , CLR_OPCODE_NAME[op]);

			switch(op)
			{
#define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) case name:
				OPDEF(CEE_PREFIX1,                    "prefix1",          Pop0,               Push0,       InlineNone,         IInternal,   1,  0xFF,    0xFE,    META)
				{
					op = CLR_OPCODE(*ip++ + 256);
					goto Execute_RestartDecoding;
				}

				OPDEF(CEE_BREAK,                      "break",            Pop0,               Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x01,    BREAK)

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDARG_0,                    "ldarg.0",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x02,    NEXT)
				OPDEF(CEE_LDARG_1,                    "ldarg.1",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x03,    NEXT)
				OPDEF(CEE_LDARG_2,                    "ldarg.2",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x04,    NEXT)
				OPDEF(CEE_LDARG_3,                    "ldarg.3",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x05,    NEXT)
				// Stack: ... ... -> <value> ...
				{
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDARG_S,                    "ldarg.s",          Pop0,               Push1,       ShortInlineVar,     IMacro,      1,  0xFF,    0x0E,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT8(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDARG,                      "ldarg",            Pop0,               Push1,       InlineVar,          IPrimitive,  2,  0xFE,    0x09,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT16(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDLOC_0,                    "ldloc.0",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x06,    NEXT)
				OPDEF(CEE_LDLOC_1,                    "ldloc.1",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x07,    NEXT)
				OPDEF(CEE_LDLOC_2,                    "ldloc.2",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x08,    NEXT)
				OPDEF(CEE_LDLOC_3,                    "ldloc.3",          Pop0,               Push1,       InlineNone,         IMacro,      1,  0xFF,    0x09,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDLOC_S,                    "ldloc.s",          Pop0,               Push1,       ShortInlineVar,     IMacro,      1,  0xFF,    0x11,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT8(arg,ip);
					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDLOC,                      "ldloc",            Pop0,               Push1,       InlineVar,          IPrimitive,  2,  0xFE,    0x0C,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT16(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDLOCA_S,                   "ldloca.s",         Pop0,               PushI,       ShortInlineVar,     IMacro,      1,  0xFF,    0x12,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT8(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDLOCA,                     "ldloca",           Pop0,               PushI,       InlineVar,          IPrimitive,  2,  0xFE,    0x0D,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT16(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDARGA_S,                   "ldarga.s",         Pop0,               PushI,       ShortInlineVar,     IMacro,      1,  0xFF,    0x0F,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT8(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDARGA,                     "ldarga",           Pop0,               PushI,       InlineVar,          IPrimitive,  2,  0xFE,    0x0A,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT16(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_STLOC_0,                    "stloc.0",          Pop1,               Push0,       InlineNone,         IMacro,      1,  0xFF,    0x0A,    NEXT)
				OPDEF(CEE_STLOC_1,                    "stloc.1",          Pop1,               Push0,       InlineNone,         IMacro,      1,  0xFF,    0x0B,    NEXT)
				OPDEF(CEE_STLOC_2,                    "stloc.2",          Pop1,               Push0,       InlineNone,         IMacro,      1,  0xFF,    0x0C,    NEXT)
				OPDEF(CEE_STLOC_3,                    "stloc.3",          Pop1,               Push0,       InlineNone,         IMacro,      1,  0xFF,    0x0D,    NEXT)
					// Stack: ... ... <value> -> ...
				{
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_STLOC_S,                    "stloc.s",          Pop1,               Push0,       ShortInlineVar,     IMacro,      1,  0xFF,    0x13,    NEXT)
					// Stack: ... ... <value> -> ...
				{
					FETCH_ARG_UINT8(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_STLOC,                      "stloc",            Pop1,               Push0,       InlineVar,          IPrimitive,  2,  0xFE,    0x0E,    NEXT)
					// Stack: ... ... <value> -> ...
				{
					FETCH_ARG_UINT16(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_STARG_S,                    "starg.s",          Pop1,               Push0,       ShortInlineVar,     IMacro,      1,  0xFF,    0x10,    NEXT)
					// Stack: ... ... <value> -> ...
				{
					FETCH_ARG_UINT8(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_STARG,                      "starg",            Pop1,               Push0,       InlineVar,          IPrimitive,  2,  0xFE,    0x0B,    NEXT)
					// Stack: ... ... <value> -> ...
				{
					FETCH_ARG_UINT16(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDNULL,                     "ldnull",           Pop0,               PushRef,     InlineNone,         IPrimitive,  1,  0xFF,    0x14,    NEXT)
				// Stack: ... ... -> <value> ...
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDC_I4_M1,                  "ldc.i4.m1",        Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x15,    NEXT)
				OPDEF(CEE_LDC_I4_0,                   "ldc.i4.0",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x16,    NEXT)
				OPDEF(CEE_LDC_I4_1,                   "ldc.i4.1",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x17,    NEXT)
				OPDEF(CEE_LDC_I4_2,                   "ldc.i4.2",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x18,    NEXT)
				OPDEF(CEE_LDC_I4_3,                   "ldc.i4.3",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x19,    NEXT)
				OPDEF(CEE_LDC_I4_4,                   "ldc.i4.4",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x1A,    NEXT)
				OPDEF(CEE_LDC_I4_5,                   "ldc.i4.5",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x1B,    NEXT)
				OPDEF(CEE_LDC_I4_6,                   "ldc.i4.6",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x1C,    NEXT)
				OPDEF(CEE_LDC_I4_7,                   "ldc.i4.7",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x1D,    NEXT)
				OPDEF(CEE_LDC_I4_8,                   "ldc.i4.8",         Pop0,               PushI,       InlineNone,         IMacro,      1,  0xFF,    0x1E,    NEXT)
				// Stack: ... ... -> <value> ...
				{
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDC_I4_S,                   "ldc.i4.s",         Pop0,               PushI,       ShortInlineI,       IMacro,      1,  0xFF,    0x1F,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_INT8(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				OPDEF(CEE_LDC_I4,                     "ldc.i4",           Pop0,               PushI,       InlineI,            IPrimitive,  1,  0xFF,    0x20,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_INT32(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDC_I8,                     "ldc.i8",           Pop0,               PushI8,      InlineI8,           IPrimitive,  1,  0xFF,    0x21,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_INT64(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDC_R4,                     "ldc.r4",           Pop0,               PushR4,      ShortInlineR,       IPrimitive,  1,  0xFF,    0x22,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT32(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDC_R8,                     "ldc.r8",           Pop0,               PushR8,      InlineR,            IPrimitive,  1,  0xFF,    0x23,    NEXT)
					// Stack: ... ... -> <value> ...
				{
					FETCH_ARG_UINT64(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_DUP,                        "dup",              Pop1,               Push1+Push1, InlineNone,         IPrimitive,  1,  0xFF,    0x25,    NEXT)
				// Stack: ... ... <value> -> <value> <value> ...
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_POP,                        "pop",              Pop1,               Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x26,    NEXT)
				// Stack: ... ... <value> -> ...

				//----------------------------------------------------------------------------------------------------------//
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BR_S,                       "br.s",             Pop0,               Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x2B,    BRANCH)
				OPDEF(CEE_BR,                         "br",               Pop0,               Push0,       InlineBrTarget,     IPrimitive,  1,  0xFF,    0x38,    BRANCH)

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BRFALSE_S,                  "brfalse.s",        PopI,               Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x2C,    COND_BRANCH)
				OPDEF(CEE_BRFALSE,                    "brfalse",          PopI,               Push0,       InlineBrTarget,     IPrimitive,  1,  0xFF,    0x39,    COND_BRANCH)
				// Stack: ... ... <value> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BRTRUE_S,                   "brtrue.s",         PopI,               Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x2D,    COND_BRANCH)
				OPDEF(CEE_BRTRUE,                     "brtrue",           PopI,               Push0,       InlineBrTarget,     IPrimitive,  1,  0xFF,    0x3A,    COND_BRANCH)
				// Stack: ... ... <value> -> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BEQ_S,                      "beq.s",            Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x2E,    COND_BRANCH)
				OPDEF(CEE_BEQ,                        "beq",              Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x3B,    COND_BRANCH)
				// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BGE_S,                      "bge.s",            Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x2F,    COND_BRANCH)
				OPDEF(CEE_BGE,                        "bge",              Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x3C,    COND_BRANCH)
				// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BGT_S,                      "bgt.s",            Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x30,    COND_BRANCH)
				OPDEF(CEE_BGT,                        "bgt",              Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x3D,    COND_BRANCH)
				// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BLE_S,                      "ble.s",            Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x31,    COND_BRANCH)
				OPDEF(CEE_BLE,                        "ble",              Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x3E,    COND_BRANCH)
				// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BLT_S,                      "blt.s",            Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x32,    COND_BRANCH)
				OPDEF(CEE_BLT,                        "blt",              Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x3F,    COND_BRANCH)
				// Stack: ... ... <value1> <value2> -> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BNE_UN_S,                   "bne.un.s",         Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x33,    COND_BRANCH)
				OPDEF(CEE_BNE_UN,                     "bne.un",           Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x40,    COND_BRANCH)
				// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BGE_UN_S,                   "bge.un.s",         Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x34,    COND_BRANCH)
					OPDEF(CEE_BGE_UN,                     "bge.un",           Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x41,    COND_BRANCH)
					// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BGT_UN_S,                   "bgt.un.s",         Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x35,    COND_BRANCH)
					OPDEF(CEE_BGT_UN,                     "bgt.un",           Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x42,    COND_BRANCH)
					// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BLE_UN_S,                   "ble.un.s",         Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x36,    COND_BRANCH)
					OPDEF(CEE_BLE_UN,                     "ble.un",           Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x43,    COND_BRANCH)
					// Stack: ... ... <value1> <value2> -> ...


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BLT_UN_S,                   "blt.un.s",         Pop1+Pop1,          Push0,       ShortInlineBrTarget,IMacro,      1,  0xFF,    0x37,    COND_BRANCH)
					OPDEF(CEE_BLT_UN,                     "blt.un",           Pop1+Pop1,          Push0,       InlineBrTarget,     IMacro,      1,  0xFF,    0x44,    COND_BRANCH)
					// Stack: ... ... <value1> <value2> -> ...
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_SWITCH,                     "switch",           PopI,               Push0,       InlineSwitch,       IPrimitive,  1,  0xFF,    0x45,    COND_BRANCH)
					// Stack: ... ... <value> -> ...
				{
					//evalPos--; CHECKSTACK(stack,evalPos);

					FETCH_ARG_UINT8(arg,ip);

					CLR_UINT32 numCases     = arg;
					//CLR_UINT32 caseSelected = evalPos[ 1 ].NumericByRef().u4;

					//if(caseSelected < numCases)
					//{
					//	CLR_PMETADATA ipsub = ip + (CLR_INT32)caseSelected * sizeof(CLR_INT16);

					//	FETCH_ARG_INT16(offset,ipsub);

					//	ip += offset;
					//}

					ip += (CLR_INT32)numCases * sizeof(CLR_INT16);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDIND_I1,                   "ldind.i1",         PopI,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x46,    NEXT)
					OPDEF(CEE_LDIND_U1,                   "ldind.u1",         PopI,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x47,    NEXT)
					OPDEF(CEE_LDIND_I2,                   "ldind.i2",         PopI,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x48,    NEXT)
					OPDEF(CEE_LDIND_U2,                   "ldind.u2",         PopI,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x49,    NEXT)
					OPDEF(CEE_LDIND_I4,                   "ldind.i4",         PopI,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x4A,    NEXT)
					OPDEF(CEE_LDIND_U4,                   "ldind.u4",         PopI,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x4B,    NEXT)
					OPDEF(CEE_LDIND_I8,                   "ldind.i8",         PopI,               PushI8,      InlineNone,         IPrimitive,  1,  0xFF,    0x4C,    NEXT)
					OPDEF(CEE_LDIND_I,                    "ldind.i",          PopI,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x4D,    NEXT)
					OPDEF(CEE_LDIND_R4,                   "ldind.r4",         PopI,               PushR4,      InlineNone,         IPrimitive,  1,  0xFF,    0x4E,    NEXT)
					OPDEF(CEE_LDIND_R8,                   "ldind.r8",         PopI,               PushR8,      InlineNone,         IPrimitive,  1,  0xFF,    0x4F,    NEXT)
					OPDEF(CEE_LDIND_REF,                  "ldind.ref",        PopI,               PushRef,     InlineNone,         IPrimitive,  1,  0xFF,    0x50,    NEXT)
					// Stack: ... ... <address> -> <value> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_STIND_REF,                  "stind.ref",        PopI+PopI,          Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x51,    NEXT)
					OPDEF(CEE_STIND_I1,                   "stind.i1",         PopI+PopI,          Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x52,    NEXT)
					OPDEF(CEE_STIND_I2,                   "stind.i2",         PopI+PopI,          Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x53,    NEXT)
					OPDEF(CEE_STIND_I4,                   "stind.i4",         PopI+PopI,          Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x54,    NEXT)
					OPDEF(CEE_STIND_I8,                   "stind.i8",         PopI+PopI8,         Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x55,    NEXT)
					OPDEF(CEE_STIND_R4,                   "stind.r4",         PopI+PopR4,         Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x56,    NEXT)
					OPDEF(CEE_STIND_R8,                   "stind.r8",         PopI+PopR8,         Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x57,    NEXT)
					OPDEF(CEE_STIND_I,                    "stind.i",          PopI+PopI,          Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0xDF,    NEXT)
					// Stack: ... ... <address> <value> -> ...
				{
					//int size = 0;

					//evalPos -= 2; CHECKSTACK(stack,evalPos);

					//switch(op)
					//{
					//case CEE_STIND_I  : size = 4; break;
					//case CEE_STIND_I1 : size = 1; break;
					//case CEE_STIND_I2 : size = 2; break;
					//case CEE_STIND_I4 : size = 4; break;
					//case CEE_STIND_I8 : size = 8; break;
					//case CEE_STIND_R4 : size = 4; break;
					//case CEE_STIND_R8 : size = 8; break;
					//case CEE_STIND_REF: size = 0; break;
					//}

					//evalPos[ 2 ].Promote();

					//TINYCLR_CHECK_HRESULT(evalPos[ 2 ].StoreToReference( evalPos[ 1 ], size ));
					//break;
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_ADD,                        "add",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x58,    NEXT)
					OPDEF(CEE_ADD_OVF,                    "add.ovf",          Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0xD6,    NEXT)
					OPDEF(CEE_ADD_OVF_UN,                 "add.ovf.un",       Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0xD7,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_SUB,                        "sub",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x59,    NEXT)
					OPDEF(CEE_SUB_OVF,                    "sub.ovf",          Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0xDA,    NEXT)
					OPDEF(CEE_SUB_OVF_UN,                 "sub.ovf.un",       Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0xDB,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_MUL,                        "mul",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x5A,    NEXT)
					OPDEF(CEE_MUL_OVF,                    "mul.ovf",          Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0xD8,    NEXT)
					OPDEF(CEE_MUL_OVF_UN,                 "mul.ovf.un",       Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0xD9,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_DIV,                        "div",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x5B,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_DIV_UN,                     "div.un",           Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x5C,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_REM,                        "rem",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x5D,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_REM_UN,                     "rem.un",           Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x5E,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_AND,                        "and",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x5F,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_OR,                         "or",               Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x60,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_XOR,                        "xor",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x61,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_SHL,                        "shl",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x62,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_SHR,                        "shr",              Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x63,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_SHR_UN,                     "shr.un",           Pop1+Pop1,          Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x64,    NEXT)
					// Stack: ... ... <value1> <value2> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_NEG,                        "neg",              Pop1,               Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x65,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_NOT,                        "not",              Pop1,               Push1,       InlineNone,         IPrimitive,  1,  0xFF,    0x66,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_I1,                    "conv.i1",          Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x67,    NEXT)
					OPDEF(CEE_CONV_OVF_I1,                "conv.ovf.i1",      Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xB3,    NEXT)
					OPDEF(CEE_CONV_OVF_I1_UN,             "conv.ovf.i1.un",   Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x82,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_I2,                    "conv.i2",          Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x68,    NEXT)
					OPDEF(CEE_CONV_OVF_I2,                "conv.ovf.i2",      Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xB5,    NEXT)
					OPDEF(CEE_CONV_OVF_I2_UN,             "conv.ovf.i2.un",   Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x83,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_I4,                    "conv.i4",          Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x69,    NEXT)
					OPDEF(CEE_CONV_OVF_I4,                "conv.ovf.i4",      Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xB7,    NEXT)
					OPDEF(CEE_CONV_OVF_I4_UN,             "conv.ovf.i4.un",   Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x84,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				OPDEF(CEE_CONV_I,                     "conv.i",           Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xD3,    NEXT)
					OPDEF(CEE_CONV_OVF_I,                 "conv.ovf.i",       Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xD4,    NEXT)
					OPDEF(CEE_CONV_OVF_I_UN,              "conv.ovf.i.un",    Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x8A,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_I8,                    "conv.i8",          Pop1,               PushI8,      InlineNone,         IPrimitive,  1,  0xFF,    0x6A,    NEXT)
					OPDEF(CEE_CONV_OVF_I8,                "conv.ovf.i8",      Pop1,               PushI8,      InlineNone,         IPrimitive,  1,  0xFF,    0xB9,    NEXT)
					OPDEF(CEE_CONV_OVF_I8_UN,             "conv.ovf.i8.un",   Pop1,               PushI8,      InlineNone,         IPrimitive,  1,  0xFF,    0x85,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_R4,                    "conv.r4",          Pop1,               PushR4,      InlineNone,         IPrimitive,  1,  0xFF,    0x6B,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_R_UN,                  "conv.r.un",        Pop1,               PushR8,      InlineNone,         IPrimitive,  1,  0xFF,    0x76,    NEXT)
					OPDEF(CEE_CONV_R8,                    "conv.r8",          Pop1,               PushR8,      InlineNone,         IPrimitive,  1,  0xFF,    0x6C,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_U1,                    "conv.u1",          Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xD2,    NEXT)
					OPDEF(CEE_CONV_OVF_U1,                "conv.ovf.u1",      Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xB4,    NEXT)
					OPDEF(CEE_CONV_OVF_U1_UN,             "conv.ovf.u1.un",   Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x86,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_U2,                    "conv.u2",          Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xD1,    NEXT)
					OPDEF(CEE_CONV_OVF_U2,                "conv.ovf.u2",      Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xB6,    NEXT)
					OPDEF(CEE_CONV_OVF_U2_UN,             "conv.ovf.u2.un",   Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x87,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_U4,                    "conv.u4",          Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x6D,    NEXT)
					OPDEF(CEE_CONV_OVF_U4,                "conv.ovf.u4",      Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xB8,    NEXT)
					OPDEF(CEE_CONV_OVF_U4_UN,             "conv.ovf.u4.un",   Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x88,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...


				OPDEF(CEE_CONV_U,                     "conv.u",           Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xE0,    NEXT)
					OPDEF(CEE_CONV_OVF_U,                 "conv.ovf.u",       Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0xD5,    NEXT)
					OPDEF(CEE_CONV_OVF_U_UN,              "conv.ovf.u.un",    Pop1,               PushI,       InlineNone,         IPrimitive,  1,  0xFF,    0x8B,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONV_U8,                    "conv.u8",          Pop1,               PushI8,      InlineNone,         IPrimitive,  1,  0xFF,    0x6E,    NEXT)
					OPDEF(CEE_CONV_OVF_U8,                "conv.ovf.u8",      Pop1,               PushI8,      InlineNone,         IPrimitive,  1,  0xFF,    0xBA,    NEXT)
					OPDEF(CEE_CONV_OVF_U8_UN,             "conv.ovf.u8.un",   Pop1,               PushI8,      InlineNone,         IPrimitive,  1,  0xFF,    0x89,    NEXT)
					// Stack: ... ... <value> -> <valueR> ...
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//
				//----------------------------------------------------------------------------------------------------------//
				OPDEF(CEE_CALL,                       "call",             VarPop,             VarPush,     InlineMethod,       IPrimitive,  1,  0xFF,    0x28,    CALL)
					OPDEF(CEE_CALLVIRT,                   "callvirt",         VarPop,             VarPush,     InlineMethod,       IObjModel,   1,  0xFF,    0x6F,    CALL)

				{
					FETCH_ARG_COMPRESSED_METHODTOKEN(arg,ip);
					
					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;

//					CLR_RT_MethodDef_Instance calleeInst; if(calleeInst.ResolveToken( arg, assm ) == false) TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);
//					CLR_RT_TypeDef_Index      cls;
//					CLR_RT_HeapBlock*         pThis;
//#if defined(TINYCLR_APPDOMAINS)                
//					bool                      fAppDomainTransition = false;
//#endif
//
//					pThis = &evalPos[1-calleeInst.m_target->numArgs]; // Point to the first arg, 'this' if an instance method
//
//					if(calleeInst.m_target->flags & CLR_RECORD_METHODDEF::MD_DelegateInvoke)
//					{
//						CLR_RT_HeapBlock_Delegate* dlg = pThis->DereferenceDelegate(); FAULT_ON_NULL(dlg);
//
//						if(dlg->DataType() == DATATYPE_DELEGATE_HEAD)
//						{
//							calleeInst.InitializeFromIndex( dlg->DelegateFtn() );
//
//							if((calleeInst.m_target->flags & CLR_RECORD_METHODDEF::MD_Static) == 0)
//							{
//								pThis->Assign( dlg->m_object );
//
//#if defined(TINYCLR_APPDOMAINS)
//								fAppDomainTransition = pThis[ 0 ].IsTransparentProxy();
//#endif
//							}
//							else
//							{                            
//								memmove( &pThis[ 0 ], &pThis[ 1 ], calleeInst.m_target->numArgs * sizeof(CLR_RT_HeapBlock) );
//
//								evalPos--;
//							}
//						}
//						else
//						{
//							//
//							// The lookup for multicast delegates is done at a later stage...
//							//
//						}
//					}
//					else //Non delegate
//					{                                        
//						CLR_RT_MethodDef_Index calleeReal;
//
//						if((calleeInst.m_target->flags & CLR_RECORD_METHODDEF::MD_Static) == 0)
//						{                        
//							//Instance method, pThis[ 0 ] is valid
//
//							if(op == CEE_CALL && pThis[ 0 ].Dereference() == NULL)
//							{                                                    
//								//CALL on a null instance is allowed, and should not throw a NullReferenceException on the call
//								//although a NullReferenceException is likely to be thrown soon thereafter if the call tries to access
//								//any member variables.       
//							}
//							else
//							{
//								TINYCLR_CHECK_HRESULT(CLR_RT_TypeDescriptor::ExtractTypeIndexFromObject( pThis[ 0 ], cls ));
//
//								//This test is for performance reasons.  c# emits a callvirt on all instance methods to make sure that 
//								//a NullReferenceException is thrown if 'this' is NULL.  However, if the instance method isn't virtual
//								//we don't need to do the more expensive virtual method lookup.
//								if(op == CEE_CALLVIRT && (calleeInst.m_target->flags & (CLR_RECORD_METHODDEF::MD_Abstract | CLR_RECORD_METHODDEF::MD_Virtual)))
//								{
//									if(g_CLR_RT_EventCache.FindVirtualMethod( cls, calleeInst, calleeReal ) == false)
//									{
//										TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);
//									}
//
//									calleeInst.InitializeFromIndex( calleeReal );
//								}
//
//#if defined(TINYCLR_APPDOMAINS)
//								fAppDomainTransition = pThis[ 0 ].IsTransparentProxy();
//#endif
//							}
//						}
//					}
//
//					WRITEBACK(stack,evalPos,ip,fDirty);
//
//#if defined(TINYCLR_APPDOMAINS)
//					if(fAppDomainTransition)
//					{
//						_ASSERTE(FIMPLIES(pThis->DataType() == DATATYPE_OBJECT, pThis->Dereference() != NULL));
//						TINYCLR_CHECK_HRESULT(CLR_RT_StackFrame::PushAppDomainTransition( th, calleeInst, &pThis[ 0 ], &pThis[ 1 ]));
//					}
//					else
//#endif //TINYCLR_APPDOMAINS
//					{
//						TINYCLR_CHECK_HRESULT(CLR_RT_StackFrame::Push( th, calleeInst, -1 ));
//					}
//
//					goto Execute_Restart;
				}
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_RET,                        "ret",              VarPop,             Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x2A,    RETURN)
				{
					Parse_Print("\n");
					break;
				}
				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CPOBJ,                      "cpobj",            PopI+PopI,          Push0,       InlineType,         IObjModel,   1,  0xFF,    0x70,    NEXT)
					OPDEF(CEE_STOBJ,                      "stobj",            PopI+Pop1,          Push0,       InlineType,         IPrimitive,  1,  0xFF,    0x81,    NEXT)
					// Stack: ... ... <dstValObj> <srcValObj> -> ...
				{
					ip += 2; // Skip argument, not used...

					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDOBJ,                      "ldobj",            PopI,               Push1,       InlineType,         IObjModel,   1,  0xFF,    0x71,    NEXT)
					// Stack: ... ... <srcValObj> -> <valObj> ...
				{
					FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDSTR,                      "ldstr",            Pop0,               PushRef,     InlineString,       IObjModel,   1,  0xFF,    0x72,    NEXT)
				{
					FETCH_ARG_COMPRESSED_STRINGTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_NEWOBJ,                     "newobj",           VarPop,             PushRef,     InlineMethod,       IObjModel,   1,  0xFF,    0x73,    CALL)
					// Stack: ... <arg1> <arg2> ... <argN> -> ...
				{
					FETCH_ARG_COMPRESSED_METHODTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CASTCLASS,                  "castclass",        PopRef,             PushRef,     InlineType,         IObjModel,   1,  0xFF,    0x74,    NEXT)
					OPDEF(CEE_ISINST,                     "isinst",           PopRef,             PushI,       InlineType,         IObjModel,   1,  0xFF,    0x75,    NEXT)
				{
					FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_THROW,                      "throw",            PopRef,             Push0,       InlineNone,         IObjModel,   1,  0xFF,    0x7A,    THROW)
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDFLD,                      "ldfld",            PopRef,             Push1,       InlineField,        IObjModel,   1,  0xFF,    0x7B,    NEXT)
					// Stack: ... <obj> -> <value> ...
				{
					FETCH_ARG_COMPRESSED_FIELDTOKEN(arg,ip);
					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDFLDA,                     "ldflda",           PopRef,             PushI,       InlineField,        IObjModel,   1,  0xFF,    0x7C,    NEXT)
					// Stack: ... <obj> -> <address>...
				{
					FETCH_ARG_COMPRESSED_FIELDTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_STFLD,                      "stfld",            PopRef+Pop1,        Push0,       InlineField,        IObjModel,   1,  0xFF,    0x7D,    NEXT)
					// Stack: ... ... <obj> <value> -> ...
				{
					FETCH_ARG_COMPRESSED_FIELDTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDSFLD,                     "ldsfld",           Pop0,               Push1,       InlineField,        IObjModel,   1,  0xFF,    0x7E,    NEXT)
					// Stack: ... -> <value> ...
				{
					FETCH_ARG_COMPRESSED_FIELDTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDSFLDA,                    "ldsflda",          Pop0,               PushI,       InlineField,        IObjModel,   1,  0xFF,    0x7F,    NEXT)
					// Stack: ... -> <address> ...
				{
					FETCH_ARG_COMPRESSED_FIELDTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_STSFLD,                     "stsfld",           Pop1,               Push0,       InlineField,        IObjModel,   1,  0xFF,    0x80,    NEXT)
					// Stack: ... ... <value> -> ...
				{
					FETCH_ARG_COMPRESSED_FIELDTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_BOX,                        "box",              Pop1,               PushRef,     InlineType,         IPrimitive,  1,  0xFF,    0x8C,    NEXT)
					OPDEF(CEE_UNBOX,                      "unbox",            PopRef,             PushI,       InlineType,         IPrimitive,  1,  0xFF,    0x79,    NEXT)
				{
					FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_UNBOX_ANY,                  "unbox.any",        PopRef,             Push1,       InlineType,         IObjModel,   1,  0xFF,    0xA5,    NEXT)
				{
					//Stack: ... <value> -> ..., value or obj
					FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_NEWARR,                     "newarr",           PopI,               PushRef,     InlineType,         IObjModel,   1,  0xFF,    0x8D,    NEXT)
				{
					FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDLEN,                      "ldlen",            PopRef,             PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x8E,    NEXT)
					// Stack: ... <obj> -> ...
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDELEMA,                    "ldelema",          PopRef+PopI,        PushI,       InlineType,         IObjModel,   1,  0xFF,    0x8F,    NEXT)
					// Stack: ... <obj> <index> -> <address> ...
				{
					ip += 2; // Skip argument, not used...

					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDELEM_I1,                  "ldelem.i1",        PopRef+PopI,        PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x90,    NEXT)
					OPDEF(CEE_LDELEM_U1,                  "ldelem.u1",        PopRef+PopI,        PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x91,    NEXT)
					OPDEF(CEE_LDELEM_I2,                  "ldelem.i2",        PopRef+PopI,        PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x92,    NEXT)
					OPDEF(CEE_LDELEM_U2,                  "ldelem.u2",        PopRef+PopI,        PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x93,    NEXT)
					OPDEF(CEE_LDELEM_I4,                  "ldelem.i4",        PopRef+PopI,        PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x94,    NEXT)
					OPDEF(CEE_LDELEM_U4,                  "ldelem.u4",        PopRef+PopI,        PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x95,    NEXT)
					OPDEF(CEE_LDELEM_I8,                  "ldelem.i8",        PopRef+PopI,        PushI8,      InlineNone,         IObjModel,   1,  0xFF,    0x96,    NEXT)
					OPDEF(CEE_LDELEM_I,                   "ldelem.i",         PopRef+PopI,        PushI,       InlineNone,         IObjModel,   1,  0xFF,    0x97,    NEXT)
					OPDEF(CEE_LDELEM_R4,                  "ldelem.r4",        PopRef+PopI,        PushR4,      InlineNone,         IObjModel,   1,  0xFF,    0x98,    NEXT)
					OPDEF(CEE_LDELEM_R8,                  "ldelem.r8",        PopRef+PopI,        PushR8,      InlineNone,         IObjModel,   1,  0xFF,    0x99,    NEXT)
					OPDEF(CEE_LDELEM_REF,                 "ldelem.ref",       PopRef+PopI,        PushRef,     InlineNone,         IObjModel,   1,  0xFF,    0x9A,    NEXT)
					// Stack: ... <obj> <index> -> <value> ...
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_STELEM_I,                   "stelem.i",         PopRef+PopI+PopI,   Push0,       InlineNone,         IObjModel,   1,  0xFF,    0x9B,    NEXT)
					OPDEF(CEE_STELEM_I1,                  "stelem.i1",        PopRef+PopI+PopI,   Push0,       InlineNone,         IObjModel,   1,  0xFF,    0x9C,    NEXT)
					OPDEF(CEE_STELEM_I2,                  "stelem.i2",        PopRef+PopI+PopI,   Push0,       InlineNone,         IObjModel,   1,  0xFF,    0x9D,    NEXT)
					OPDEF(CEE_STELEM_I4,                  "stelem.i4",        PopRef+PopI+PopI,   Push0,       InlineNone,         IObjModel,   1,  0xFF,    0x9E,    NEXT)
					OPDEF(CEE_STELEM_I8,                  "stelem.i8",        PopRef+PopI+PopI8,  Push0,       InlineNone,         IObjModel,   1,  0xFF,    0x9F,    NEXT)
					OPDEF(CEE_STELEM_R4,                  "stelem.r4",        PopRef+PopI+PopR4,  Push0,       InlineNone,         IObjModel,   1,  0xFF,    0xA0,    NEXT)
					OPDEF(CEE_STELEM_R8,                  "stelem.r8",        PopRef+PopI+PopR8,  Push0,       InlineNone,         IObjModel,   1,  0xFF,    0xA1,    NEXT)
					OPDEF(CEE_STELEM_REF,                 "stelem.ref",       PopRef+PopI+PopRef, Push0,       InlineNone,         IObjModel,   1,  0xFF,    0xA2,    NEXT)
					// Stack: ... ... <obj> <index> <value> -> ...
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDTOKEN,                    "ldtoken",          Pop0,               PushI,       InlineTok,          IPrimitive,  1,  0xFF,    0xD0,    NEXT)
				{
					FETCH_ARG_UINT32(arg,ip);

					//evalPos++; CHECKSTACK(stack,evalPos);

					//evalPos[ 0 ].SetObjectReference( NULL );

					//switch(CLR_TypeFromTk( arg ))
					//{
					//case TBL_TypeSpec:
					//	{
					//		CLR_RT_TypeSpec_Instance sig; if(sig.ResolveToken( arg, assm ) == false) TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);

					//		evalPos[ 0 ].SetReflection( sig );
					//	}
					//	break;

					//case TBL_TypeRef:
					//case TBL_TypeDef:
					//	{
					//		CLR_RT_TypeDef_Instance cls; if(cls.ResolveToken( arg, assm ) == false) TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);

					//		evalPos[ 0 ].SetReflection( cls );
					//	}
					//	break;

					//case TBL_FieldRef:
					//case TBL_FieldDef:
					//	{
					//		CLR_RT_FieldDef_Instance field; if(field.ResolveToken( arg, assm ) == false) TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);

					//		evalPos[ 0 ].SetReflection( field );
					//	}
					//	break;

					//case TBL_MethodRef:
					//case TBL_MethodDef:
					//	{
					//		CLR_RT_MethodDef_Instance method; if(method.ResolveToken( arg, assm ) == false) TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);

					//		evalPos[ 0 ].SetReflection( method );
					//	}
					//	break;

					//default:
					//	TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);
					//	break;
					//}
					//break;
					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_ENDFINALLY,                 "endfinally",       Pop0,               Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0xDC,    RETURN)
				{                                                
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LEAVE,                      "leave",            Pop0,               Push0,       InlineBrTarget,     IPrimitive,  1,  0xFF,    0xDD,    BRANCH)
					OPDEF(CEE_LEAVE_S,                    "leave.s",          Pop0,               Push0,       ShortInlineBrTarget,IPrimitive,  1,  0xFF,    0xDE,    BRANCH)
				{
					CLR_INT32 arg;

					if(op == CEE_LEAVE)
					{
						TINYCLR_READ_UNALIGNED_INT16( arg, ip );
					}
					else
					{
						TINYCLR_READ_UNALIGNED_INT8( arg, ip );
					}

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CEQ,                        "ceq",              Pop1+Pop1,          PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x01,    NEXT)

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CGT,                        "cgt",              Pop1+Pop1,          PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x02,    NEXT)

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CGT_UN,                     "cgt.un",           Pop1+Pop1,          PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x03,    NEXT)


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CLT,                        "clt",              Pop1+Pop1,          PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x04,    NEXT)


				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CLT_UN,                     "clt.un",           Pop1+Pop1,          PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x05,    NEXT)
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDFTN,                      "ldftn",            Pop0,               PushI,       InlineMethod,       IPrimitive,  2,  0xFE,    0x06,    NEXT)
				{
					FETCH_ARG_COMPRESSED_METHODTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_LDVIRTFTN,                  "ldvirtftn",        PopRef,             PushI,       InlineMethod,       IPrimitive,  2,  0xFE,    0x07,    NEXT)
				{
					FETCH_ARG_COMPRESSED_METHODTOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_INITOBJ,                    "initobj",          PopI,               Push0,       InlineType,         IObjModel,   2,  0xFE,    0x15,    NEXT)
				{
					ip += 2; // Skip argument, not used...

					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_RETHROW,                    "rethrow",          Pop0,               Push0,       InlineNone,         IObjModel,   2,  0xFE,    0x1A,    THROW)
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_SIZEOF,                     "sizeof",           Pop0,               PushI,       InlineType,         IPrimitive,  2,  0xFE,    0x1C,    NEXT)
				{
					FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip);

					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_CONSTRAINED,                "constrained.",     Pop0,               Push0,       InlineType,         IPrefix,     2,  0xFE,    0x16,    META)
				{
					FETCH_ARG_COMPRESSED_TYPETOKEN(arg,ip);
					
					Parse_Print(" 0x%x",arg);
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				OPDEF(CEE_ENDFILTER,                  "endfilter",        PopI,               Push0,       InlineNone,         IPrimitive,  2,  0xFE,    0x11,    RETURN)
				{
					Parse_Print("\n");
					break;
				}

				//----------------------------------------------------------------------------------------------------------//

				//////////////////////////////////////////////////////////////////////////////////////////
				//
				// These opcodes do nothing...
				//
				OPDEF(CEE_NOP,                        "nop",              Pop0,               Push0,       InlineNone,         IPrimitive,  1,  0xFF,    0x00,    NEXT)
				OPDEF(CEE_UNALIGNED,                  "unaligned.",       Pop0,               Push0,       ShortInlineI,       IPrefix,     2,  0xFE,    0x12,    META)
				OPDEF(CEE_VOLATILE,                   "volatile.",        Pop0,               Push0,       InlineNone,         IPrefix,     2,  0xFE,    0x13,    META)
				OPDEF(CEE_TAILCALL,                   "tail.",            Pop0,               Push0,       InlineNone,         IPrefix,     2,  0xFE,    0x14,    META)
				{
					Parse_Print("\n");
					break;
				}

				//////////////////////////////////////////////////////////////////////////////////////////
				//
				// Unsupported opcodes...
				//
				OPDEF(CEE_ARGLIST,                    "arglist",          Pop0,               PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x00,    NEXT)
				OPDEF(CEE_CPBLK,                      "cpblk",            PopI+PopI+PopI,     Push0,       InlineNone,         IPrimitive,  2,  0xFE,    0x17,    NEXT)
				OPDEF(CEE_JMP,                        "jmp",              Pop0,               Push0,       InlineMethod,       IPrimitive,  1,  0xFF,    0x27,    CALL)
				OPDEF(CEE_INITBLK,                    "initblk",          PopI+PopI+PopI,     Push0,       InlineNone,         IPrimitive,  2,  0xFE,    0x18,    NEXT)
				OPDEF(CEE_CALLI,                      "calli",            VarPop,             VarPush,     InlineSig,          IPrimitive,  1,  0xFF,    0x29,    CALL)
				OPDEF(CEE_CKFINITE,                   "ckfinite",         Pop1,               PushR8,      InlineNone,         IPrimitive,  1,  0xFF,    0xC3,    NEXT)
				OPDEF(CEE_LOCALLOC,                   "localloc",         PopI,               PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x0F,    NEXT)
				OPDEF(CEE_MKREFANY,                   "mkrefany",         PopI,               Push1,       InlineType,         IPrimitive,  1,  0xFF,    0xC6,    NEXT)
				OPDEF(CEE_REFANYTYPE,                 "refanytype",       Pop1,               PushI,       InlineNone,         IPrimitive,  2,  0xFE,    0x1D,    NEXT)
				OPDEF(CEE_REFANYVAL,                  "refanyval",        Pop1,               PushI,       InlineType,         IPrimitive,  1,  0xFF,    0xC2,    NEXT)
				OPDEF(CEE_LDELEM,                     "ldelem",           PopRef+PopI,        Push1,       InlineType,         IObjModel,   1,  0xFF,    0xA3,    NEXT)
				OPDEF(CEE_STELEM,                     "stelem",           PopRef+PopI+Pop1,   Push0,       InlineType,         IObjModel,   1,  0xFF,    0xA4,    NEXT)            
				OPDEF(CEE_READONLY,                   "readonly.",        Pop0,               Push0,       InlineNone,         IPrefix,     2,  0xFE,    0x1E,    META)
				{
					Parse_Print("\n");
					return RESULT_UNSUPPORT_INSRUCTION;
				}
				//////////////////////////////////////////////////////////////////////////////////////////

			default:
				return RESULT_UNKNOWN_INSRUCTION;
#undef OPDEF
			}//end switch
		}//end while
	}
	
	return RESULT_SUCCESS;
}