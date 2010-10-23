// MetaParse.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "MetaParse.h"
#include "Filex.h"
#include "image.h"

#define FILE_PATH  L"MFApp.pe"

extern HRESULT Parse_Dasm_IL(IN PBYTE pMtdPtr ,IN DWORD dwSize,IN CLR_RT_Assembly* pAssem);

void Parse_Print(LPCSTR   lpFmt,...)
{	
	CHAR  szBuf[1024];
	va_list  argList;
	memset(szBuf,0,1024);
	va_start(argList,  lpFmt); 
	wvsprintfA(szBuf,lpFmt,  argList);
	va_end(argList); 
	
	printf(szBuf);
}

HRESULT Parse_Record_Assembly(IN PBYTE pPtr,IN OUT PBYTE* ppPtr ,OUT CLR_RECORD_ASSEMBLY* pAssembly)
{
	if(pPtr == NULL || pAssembly == NULL) return RESULT_INVALID_PARAMS;

	ReadByteX(pAssembly,pPtr,sizeof(CLR_RECORD_ASSEMBLY));

	if(pAssembly->numOfPatchedMethods>0)
		return RESULT_UNSUPPORT;

	return RESULT_SUCCESS;
}

HRESULT Parse_GoodAssembly(IN PBYTE pvBase)
{
	PBYTE pPtr = pvBase;
	CLR_RECORD_ASSEMBLY reAssembly = {0};

	ReadByteX(&reAssembly,pPtr,sizeof(CLR_RECORD_ASSEMBLY));

	UINT32 u32HeaderCrc = reAssembly.headerCRC;
	reAssembly.headerCRC = 0;	

	if(u32HeaderCrc != SUPPORT_ComputeCRC(&reAssembly,sizeof(CLR_RECORD_ASSEMBLY),0) ||
		reAssembly.stringTableVersion != c_CLR_StringTable_Version ||
		memcmp(reAssembly.marker,"MSSpot1",sizeof(reAssembly.marker))!=0 ||
		reAssembly.assemblyCRC != SUPPORT_ComputeCRC(pPtr,reAssembly.startOfTables[ TBL_EndOfAssembly ] - sizeof(CLR_RECORD_ASSEMBLY),0))
	{
		return RESULT_INVALID_CHECKSUM;
	}

	return RESULT_SUCCESS;
}

HRESULT ParsePE(IN PBYTE pPtr)
{
	PBYTE pvBase = pPtr;
	HRESULT hResult = RESULT_UNKNOWN;
	CLR_RT_Assembly rtAssembly = {0};

	if(RESULT_SUCCESS != Parse_GoodAssembly(pvBase))
		return RESULT_INVALID_CHECKSUM;
	pvBase = pPtr;

	hResult = Parse_Record_Assembly(pvBase,&pvBase,&rtAssembly.m_header);
	if(RESULT_SUCCESS != hResult) return hResult;
	
	pvBase = pPtr;
	for (int i=0;i<TBL_Max-1;++i){
		rtAssembly.m_pTablesSize[i] = rtAssembly.m_header.SizeOfTable((CLR_TABLESENUM)i);
	}

	rtAssembly.m_pStringsPtr = pvBase + rtAssembly.m_header.startOfTables[TBL_Strings];
	rtAssembly.m_pSignaturesPtr = pvBase + rtAssembly.m_header.startOfTables[TBL_Signatures];
	rtAssembly.m_pByteCodePtr = pvBase + rtAssembly.m_header.startOfTables[TBL_ByteCode];

	rtAssembly.m_pResourcesDataPtr = pvBase + rtAssembly.m_header.startOfTables[TBL_ResourcesData];

	rtAssembly.m_pTablesSize[TBL_AssemblyRef] /= sizeof(CLR_RECORD_ASSEMBLYREF);
	rtAssembly.m_pAssemblyRefPtr = (CLR_RECORD_ASSEMBLYREF*)(pvBase + rtAssembly.m_header.startOfTables[TBL_AssemblyRef]);

	rtAssembly.m_pTablesSize[TBL_TypeRef] /= sizeof(CLR_RECORD_TYPEREF);
	rtAssembly.m_pTypeRefPtr = (CLR_RECORD_TYPEREF*)(pvBase + rtAssembly.m_header.startOfTables[TBL_TypeRef]);

	rtAssembly.m_pTablesSize[TBL_FieldRef] /= sizeof(CLR_RECORD_FIELDREF);
	rtAssembly.m_pFieldRefPtr = (CLR_RECORD_FIELDREF*)(pvBase + rtAssembly.m_header.startOfTables[TBL_FieldRef]);

	rtAssembly.m_pTablesSize[TBL_MethodRef] /= sizeof(CLR_RECORD_METHODREF);
	rtAssembly.m_pMethodRefPtr = (CLR_RECORD_METHODREF*)(pvBase + rtAssembly.m_header.startOfTables[TBL_MethodRef]);

	rtAssembly.m_pTablesSize[TBL_TypeDef] /= sizeof(CLR_RECORD_TYPEDEF);
	rtAssembly.m_pTypeDefPtr = (CLR_RECORD_TYPEDEF*)(pvBase + rtAssembly.m_header.startOfTables[TBL_TypeDef]);

	rtAssembly.m_pTablesSize[TBL_FieldDef] /= sizeof(CLR_RECORD_FIELDDEF);
	rtAssembly.m_pFieldDefPtr = (CLR_RECORD_FIELDDEF*)(pvBase + rtAssembly.m_header.startOfTables[TBL_FieldDef]);

	rtAssembly.m_pTablesSize[TBL_MethodDef] /= sizeof(CLR_RECORD_METHODDEF);
	rtAssembly.m_pMethodDefPtr = (CLR_RECORD_METHODDEF*)(pvBase + rtAssembly.m_header.startOfTables[TBL_MethodDef]);

	rtAssembly.m_pTablesSize[TBL_Attributes] /= sizeof(CLR_RECORD_ATTRIBUTE);
	rtAssembly.m_pAttributePtr = (CLR_RECORD_ATTRIBUTE*)(pvBase + rtAssembly.m_header.startOfTables[TBL_Attributes]);

	rtAssembly.m_pTablesSize[TBL_TypeSpec] /= sizeof(CLR_RECORD_TYPESPEC);
	rtAssembly.m_pTypeSpecPtr = (CLR_RECORD_TYPESPEC*)(pvBase + rtAssembly.m_header.startOfTables[TBL_TypeSpec]);

	rtAssembly.m_pTablesSize[TBL_Resources] /= sizeof(CLR_RECORD_RESOURCE);
	rtAssembly.m_pResourcePtr = (CLR_RECORD_RESOURCE*)(pvBase + rtAssembly.m_header.startOfTables[TBL_Resources]);

	rtAssembly.m_pTablesSize[TBL_ResourcesFiles] /= sizeof(CLR_RECORD_RESOURCE_FILE);
	rtAssembly.m_pResourceFilePtr = (CLR_RECORD_RESOURCE_FILE*)(pvBase + rtAssembly.m_header.startOfTables[TBL_ResourcesFiles]);
	

	//for (int i = 0; i<rtAssembly.m_pTablesSize[TBL_MethodDef];++i)
	{
		PBYTE pMtd = rtAssembly.m_pByteCodePtr + rtAssembly.m_pMethodDefPtr[0].RVA;

		Parse_Dasm_IL(pMtd,8,&rtAssembly);

	}

	return RESULT_SUCCESS;
}

int _tmain(int argc, _TCHAR* argv[])
{
	HANDLE hFile = NULL;
	CFilex Filex;
	CImage peImage;

	hFile = Filex.Open(FILE_PATH,OPEN_EXISTING,GENERIC_READ);
	if(hFile == NULL)
		return -1;

	if(peImage.Load(hFile,MAP_READ,0,Filex.GetSize()) == FALSE)
		return -1;

	//Parse .NET MF pe Files
	ParsePE(peImage.GetBuff());

	peImage.UnLoad();
	Filex.Close(hFile);

	return 0;
}

