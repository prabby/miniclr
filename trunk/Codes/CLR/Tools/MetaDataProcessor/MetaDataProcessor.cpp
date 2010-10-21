////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "keygen.h"

#pragma comment(lib, "Comdlg32")
#include <Commdlg.h>

////////////////////////////////////////////////////////////////////////////////////////////////////

struct Settings : CLR_RT_ParseOptions
{
    PELoader                       m_pe;
    MetaData::Collection           m_col;
    MetaData::Parser*              m_pr;
    bool                           m_fEE;
    CLR_RT_Assembly*               m_assm;
    CLR_RT_ParseOptions::BufferMap m_assemblies;

    bool                           m_fDumpStatistics;

    WatchAssemblyBuilder::Linker   m_lkForStrings;

    bool                           m_patch_fReboot;
    bool                           m_patch_fSign;
    std::wstring                   m_patch_szNative;

    bool                           m_fFromAssembly;
    bool                           m_fFromImage;
    bool                           m_fNoByteCode;

    CLR_RT_StringSet                m_resources;

    //--//

    struct Command_Call : CLR_RT_ParseOptions::Command
    {
        typedef HRESULT (Settings::*FPN)( CLR_RT_ParseOptions::ParameterList* params );

        Settings& m_parent;
        FPN       m_call;

        Command_Call( Settings& parent, FPN call, LPCWSTR szName, LPCWSTR szDescription ) : CLR_RT_ParseOptions::Command( szName, szDescription ), m_parent(parent), m_call(call)
        {
        }

        virtual HRESULT Execute()
        {
            return (m_parent.*m_call)( &m_params );
        }
    };


    Settings()
    {
        m_fEE             = false;

        m_fDumpStatistics = false;

        m_patch_fReboot   = false;
        m_patch_fSign     = false;

        m_fFromAssembly   = false;
        m_fFromImage      = false;
        m_fNoByteCode     = false;

        RevertToDefaults();

        BuildOptions();
    }

    ~Settings()
    {
        Cmd_Reset();
    }

    //--//

    void RevertToDefaults()
    {
        for(CLR_RT_ParseOptions::BufferMapIter it = m_assemblies.begin(); it != m_assemblies.end(); it++)
        {
            delete it->second;
        }

        m_pe .Close();                                   // PELoader                       m_pe;
        m_col.Clear( false );                            // MetaData::Collection           m_col;
        m_pr               = NULL;                       // MetaData::Parser*              m_pr;
                                                         // bool                           m_fEE;
        m_assm             = NULL;                       // CLR_RT_Assembly*               m_assm;
        m_assemblies.clear();                            // CLR_RT_ParseOptions::BufferMap m_assemblies;
                                                         //
        m_fDumpStatistics  = false;                      // bool                           m_fDumpStatistics;
                                                         //
                                                         // WatchAssemblyBuilder::Linker   m_lkForStrings;
                                                         //
                                                         // bool                           m_patch_fReboot;
                                                         // bool                           m_patch_fSign;
                                                         // std::wstring                   m_patch_szNative;
                                                         //
        m_fFromAssembly    = false;                      // bool                           m_fFromAssembly;
        m_fFromImage       = false;                      // bool                           m_fFromImage;
                                                         // bool                           m_fNoByteCode;
    }

    //--//

    HRESULT AllocateSystem()
    {
        TINYCLR_HEADER();

        if(m_fEE == false)
        {
            TINYCLR_CHECK_HRESULT(CLR_RT_ExecutionEngine::CreateInstance());

            m_fEE = true;
        }

        TINYCLR_NOCLEANUP();
    }

    void ReleaseSystem()
    {
        if(m_fEE)
        {
            CLR_RT_ExecutionEngine::DeleteInstance();

            m_fEE = false;
        }
    }

    HRESULT CheckAssemblyFormat( CLR_RECORD_ASSEMBLY* header, LPCWSTR src )
    {
        TINYCLR_HEADER();

        if(header->GoodAssembly() == false)
        {
            wprintf( L"Invalid assembly format for '%s': ", src );
            for(int i=0; i<sizeof(header->marker); i++)
            {
                wprintf( L"%02x", header->marker[i] );
            }
            wprintf( L"\n" );

            TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);
        }

        TINYCLR_NOCLEANUP();
    }

    //--//

#define PARAM_GENERIC(parm1Name,parm1Desc)     param = new CLR_RT_ParseOptions::Parameter_Generic(      parm1Name, parm1Desc ); cmd->m_params.push_back( param )
#define PARAM_STRING(val,parm1Name,parm1Desc)  param = new CLR_RT_ParseOptions::Parameter_String ( val, parm1Name, parm1Desc ); cmd->m_params.push_back( param )
#define PARAM_BOOLEAN(val,parm1Name,parm1Desc) param = new CLR_RT_ParseOptions::Parameter_Boolean( val, parm1Name, parm1Desc ); cmd->m_params.push_back( param )
#define PARAM_INTEGER(val,parm1Name,parm1Desc) param = new CLR_RT_ParseOptions::Parameter_Integer( val, parm1Name, parm1Desc ); cmd->m_params.push_back( param )
#define PARAM_FLOAT(val,parm1Name,parm1Desc)   param = new CLR_RT_ParseOptions::Parameter_Float  ( val, parm1Name, parm1Desc ); cmd->m_params.push_back( param )

#define PARAM_EXTRACT_STRING(lst,idx)    ((CLR_RT_ParseOptions::Parameter_Generic*)(*lst)[idx])->m_data.c_str()
#define PARAM_EXTRACT_BOOLEAN(lst,idx) *(((CLR_RT_ParseOptions::Parameter_Boolean*)(*lst)[idx])->m_dataPtr)


#define OPTION_GENERIC(optName,optDesc) cmd = new CLR_RT_ParseOptions::Command        (      optName, optDesc ); m_commands.push_back( cmd )
#define OPTION_SET(val,optName,optDesc) cmd = new CLR_RT_ParseOptions::Command_SetFlag( val, optName, optDesc ); m_commands.push_back( cmd )
#define OPTION_CALL(fpn,optName,optDesc) cmd = new Command_Call( *this, &Settings::fpn, optName, optDesc ); m_commands.push_back( cmd )

#define OPTION_STRING(val,optName,optDesc,parm1Name,parm1Desc)  OPTION_GENERIC(optName,optDesc); PARAM_STRING(val,parm1Name,parm1Desc)
#define OPTION_BOOLEAN(val,optName,optDesc,parm1Name,parm1Desc) OPTION_GENERIC(optName,optDesc); PARAM_BOOLEAN(val,parm1Name,parm1Desc)
#define OPTION_INTEGER(val,optName,optDesc,parm1Name,parm1Desc) OPTION_GENERIC(optName,optDesc); PARAM_INTEGER(val,parm1Name,parm1Desc)
#define OPTION_FLOAT(val,optName,optDesc,parm1Name,parm1Desc)   OPTION_GENERIC(optName,optDesc); PARAM_FLOAT(val,parm1Name,parm1Desc)

    //--//

    HRESULT Cmd_Cfg( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        TINYCLR_CHECK_HRESULT(ExtractOptionsFromFile( PARAM_EXTRACT_STRING( params, 0 ) ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_Reset( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        RevertToDefaults();

        TINYCLR_NOCLEANUP_NOLABEL();
    }

    HRESULT Cmd_ResetHints( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        m_col.Clear( true );

        TINYCLR_NOCLEANUP_NOLABEL();
    }

    HRESULT Cmd_LoadHints( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        TINYCLR_CHECK_HRESULT(m_col.LoadHints( PARAM_EXTRACT_STRING( params, 0 ), PARAM_EXTRACT_STRING( params, 1 ) ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_IgnoreAssembly( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        TINYCLR_CHECK_HRESULT(m_col.IgnoreAssembly( PARAM_EXTRACT_STRING( params, 0 ) ));

        TINYCLR_NOCLEANUP();
    }

    //--//

    HRESULT Cmd_Parse( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        m_fFromAssembly = true ;
        m_fFromImage    = false;

        if(!m_pr) TINYCLR_CHECK_HRESULT(m_col.CreateAssembly( m_pr ));

        TINYCLR_CHECK_HRESULT(m_pr->Analyze( PARAM_EXTRACT_STRING( params, 0 ) ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_VerboseMinimize( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        if(!m_pr) TINYCLR_CHECK_HRESULT(m_col.CreateAssembly( m_pr ));

        m_pr->m_fVerboseMinimize = true;

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_NoByteCode( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        if(!m_pr) TINYCLR_CHECK_HRESULT(m_col.CreateAssembly( m_pr ));

        m_pr->m_fNoByteCode = true;
        m_fNoByteCode       = true;

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_NoAttributes( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        if(!m_pr) TINYCLR_CHECK_HRESULT(m_col.CreateAssembly( m_pr ));

        m_pr->m_fNoAttributes = true;

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_ExcludeClassByName( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        if(!m_pr) TINYCLR_CHECK_HRESULT(m_col.CreateAssembly( m_pr ));

        m_pr->m_setFilter_ExcludeClassByName.insert( PARAM_EXTRACT_STRING( params, 0 ) );

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_Minimize( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        if(!m_pr) TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);

        TINYCLR_CHECK_HRESULT(m_pr->RemoveUnused());

        TINYCLR_CHECK_HRESULT(m_pr->VerifyConsistency());

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_SaveStrings( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        if(!m_pr) TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);

        {
            MetaData::Parser prCopy = *m_pr;

            TINYCLR_CHECK_HRESULT(m_lkForStrings.Process( prCopy ));

            TINYCLR_CHECK_HRESULT(m_lkForStrings.SaveUniqueStrings( PARAM_EXTRACT_STRING( params, 0 ) ));
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_LoadStrings( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        TINYCLR_CHECK_HRESULT(m_lkForStrings.LoadUniqueStrings( PARAM_EXTRACT_STRING( params, 0 ) ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_GenerateStringsTable( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        TINYCLR_CHECK_HRESULT(m_lkForStrings.DumpUniqueStrings( PARAM_EXTRACT_STRING( params, 0 ) ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_ImportResource( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        m_resources.insert( PARAM_EXTRACT_STRING( params, 0 ) );

        TINYCLR_NOCLEANUP_NOLABEL();
    }

    HRESULT Cmd_Compile( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        if(!m_pr) TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);

        m_pr->m_resources = m_resources; m_resources.clear();

        {
            WatchAssemblyBuilder::Linker             lk;
            WatchAssemblyBuilder::CQuickRecord<BYTE> buf;
            MetaData::Parser                         prCopy = *m_pr;
            LPCWSTR                                  szFile = PARAM_EXTRACT_STRING( params, 0 );

            lk.LoadGlobalStrings();

            TINYCLR_CHECK_HRESULT(lk.Process( prCopy ));

            TINYCLR_CHECK_HRESULT(lk.Generate( buf, m_patch_fReboot, m_patch_fSign, m_patch_szNative.size() ? &m_patch_szNative : NULL ));

            if(m_fDumpStatistics)
            {
                MetaData::ByteCode::DumpDistributionStats();
            }

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::SaveFile( szFile, (CLR_UINT8*)buf.Ptr(), (DWORD)buf.Size() ));

            TINYCLR_CHECK_HRESULT(lk.DumpPdbx     ( szFile ));
            //TINYCLR_CHECK_HRESULT(lk.DumpDownloads( szFile ));
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_Diff( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        MetaData::Reparser::Assembly rprOld;
        MetaData::Reparser::Assembly rprNew;
        MetaData::Reparser::Assembly rprDiff;
        LPCWSTR                      szFileOld  = PARAM_EXTRACT_STRING( params, 0 );
        LPCWSTR                      szFileNew  = PARAM_EXTRACT_STRING( params, 1 );
        LPCWSTR                      szFileDiff = PARAM_EXTRACT_STRING( params, 2 );

        bool                         fNative = m_patch_szNative.size() > 0;

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        TINYCLR_CHECK_HRESULT(rprOld.Load( szFileOld ));
        TINYCLR_CHECK_HRESULT(rprNew.Load( szFileNew ));

        TINYCLR_CHECK_HRESULT(rprDiff.CreateDiff( &rprOld, &rprNew, fNative ));

        {
            WatchAssemblyBuilder::Linker             lk;
            WatchAssemblyBuilder::CQuickRecord<BYTE> buf;

            lk.LoadGlobalStrings();

            TINYCLR_CHECK_HRESULT(lk.Reprocess( rprDiff ));

            TINYCLR_CHECK_HRESULT(lk.Generate( buf, m_patch_fReboot, m_patch_fSign, fNative ? &m_patch_szNative : NULL ));

            TINYCLR_CHECK_HRESULT(lk.DumpDownloads( szFileDiff ));

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::SaveFile( szFileDiff, (CLR_UINT8*)buf.Ptr(), (DWORD)buf.Size() ));
        }

        TINYCLR_CLEANUP();

        if(::IsDebuggerPresent())
        {
            getchar();
        }

        TINYCLR_CLEANUP_END();
    }

    void AppendString( std::string& str, LPCSTR format, ... )
    {
        char    rgBuffer[512];
        LPSTR   szBuffer =           rgBuffer;
        size_t  iBuffer  = MAXSTRLEN(rgBuffer);
        va_list arg;

        va_start( arg, format );

        CLR_SafeSprintfV( szBuffer, iBuffer, format, arg );

        str.append( rgBuffer );
    }

    void AppendString( std::string& str, LPCSTR text, MetaData::Reparser::Assembly& rpr )
    {
        AppendString( str, "%s%s (%d.%d.%d.%d)\n", text, rpr.m_name.c_str(), rpr.m_version.iMajorVersion, rpr.m_version.iMinorVersion, rpr.m_version.iBuildNumber, rpr.m_version.iRevisionNumber );
    }

    HRESULT Cmd_DiffHex( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        MetaData::Reparser::Assembly rprOld;
        MetaData::Reparser::Assembly rprNew;
        MetaData::Reparser::Assembly rprDiff;
        bool                         fNative = m_patch_szNative.size() > 0;

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        TINYCLR_CHECK_HRESULT(rprOld.Load( PARAM_EXTRACT_STRING( params, 0 ) ));
        TINYCLR_CHECK_HRESULT(rprNew.Load( PARAM_EXTRACT_STRING( params, 1 ) ));

        TINYCLR_CHECK_HRESULT(rprDiff.CreateDiff( &rprOld, &rprNew, fNative ));

        {
            WatchAssemblyBuilder::Linker             lk;
            WatchAssemblyBuilder::CQuickRecord<BYTE> buf;
            std::string                              str;

            lk.LoadGlobalStrings();

            TINYCLR_CHECK_HRESULT(lk.Reprocess( rprDiff ));

            TINYCLR_CHECK_HRESULT(lk.Generate( buf, m_patch_fReboot, m_patch_fSign, fNative ? &m_patch_szNative : NULL ));

            //--//

            BYTE*  ptr = (BYTE*)buf.Ptr();
            size_t len =        buf.Size();

            AppendString( str, "//\n" );
            AppendString( str, "// Original assembly: ", rprOld );
            AppendString( str, "// Modified assembly: ", rprNew );
            AppendString( str, "//\n" );

            AppendString( str, "const UINT8 patch[] = \n{" );
            for(size_t i=0; i<len; i++)
            {
                if((i % 16) == 0)
                {
                    AppendString( str, "\n   " );
                }

                AppendString( str, " 0x%02X,", *ptr++ );
            }
            AppendString( str, "\n};\n" );

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::SaveFile( PARAM_EXTRACT_STRING( params, 2 ), (CLR_UINT8*)str.c_str(), (DWORD)str.size() ));
        }

        TINYCLR_NOCLEANUP();
    }


    HRESULT Cmd_Load( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        m_fFromAssembly = false;
        m_fFromImage    = true ;

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        {
            LPCWSTR              szName = PARAM_EXTRACT_STRING( params, 0 );
            CLR_RT_Buffer*       buffer = new CLR_RT_Buffer(); m_assemblies[szName] = buffer;
            CLR_RECORD_ASSEMBLY* header;
            CLR_RT_Assembly*     assm;

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::LoadFile( szName, *buffer ));

            header = (CLR_RECORD_ASSEMBLY*)&(*buffer)[0]; TINYCLR_CHECK_HRESULT(CheckAssemblyFormat( header, szName ));

            TINYCLR_CHECK_HRESULT(CLR_RT_Assembly::CreateInstance( header, assm ));

            g_CLR_RT_TypeSystem.Link( assm );
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_LoadDatabase( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        m_fFromAssembly = false;
        m_fFromImage    = true ;

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        {
            LPCWSTR              szFile = PARAM_EXTRACT_STRING( params, 0 );
            CLR_RT_Buffer        buffer;
            CLR_RECORD_ASSEMBLY* header;
            CLR_RECORD_ASSEMBLY* headerEnd;
            std::wstring         strName;

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::LoadFile( szFile, buffer ));

            header    = (CLR_RECORD_ASSEMBLY*)&buffer[0              ];
            headerEnd = (CLR_RECORD_ASSEMBLY*)&buffer[buffer.size()-1];

            while(header + 1 <= headerEnd && header->GoodAssembly())
            {
                CLR_RT_Buffer*       bufferSub = new CLR_RT_Buffer();
                CLR_RECORD_ASSEMBLY* headerSub;
                CLR_RT_Assembly*     assm;

                bufferSub->resize( header->TotalSize() );

                headerSub = (CLR_RECORD_ASSEMBLY*)&(*bufferSub)[0];

                if((CLR_UINT8*)header + header->TotalSize() > (CLR_UINT8*)headerEnd)
                {
                    //checksum passed, but not enough data in assembly
                    _ASSERTE(FALSE);
                    break;
                }
                memcpy( headerSub, header, header->TotalSize() );

                TINYCLR_CHECK_HRESULT(CLR_RT_Assembly::CreateInstance( headerSub, assm ));

                g_CLR_RT_TypeSystem.Link( assm );

                CLR_RT_UnicodeHelper::ConvertFromUTF8( assm->m_szName, strName ); m_assemblies[strName] = bufferSub;

                header = (CLR_RECORD_ASSEMBLY*)ROUNDTOMULTIPLE( (size_t)header + header->TotalSize(), CLR_UINT32 );
            }
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_DumpAll( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        LPCWSTR szName = PARAM_EXTRACT_STRING( params, 0 );

        if(szName[0] == 0) szName = NULL;

        if(m_fFromAssembly && m_pr)
        {
            m_pr->DumpSchema( szName, m_fNoByteCode );
        }
        else
        {
            TINYCLR_CHECK_HRESULT(AllocateSystem());

            g_CLR_RT_TypeSystem.Dump( szName, m_fNoByteCode );
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_DumpDat( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        m_fFromAssembly = false;
        m_fFromImage    = true ;

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        {
            LPCWSTR              szFile = PARAM_EXTRACT_STRING( params, 0 );
            CLR_RT_Buffer        buffer;
            CLR_RECORD_ASSEMBLY* header;
            CLR_RECORD_ASSEMBLY* headerEnd;
            std::wstring         strName;

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::LoadFile( szFile, buffer ));

            header    = (CLR_RECORD_ASSEMBLY*)&buffer[0              ];
            headerEnd = (CLR_RECORD_ASSEMBLY*)&buffer[buffer.size()-1];

            int number = 0;

            while(header + 1 <= headerEnd && header->GoodAssembly())
            {
                CLR_RT_Buffer*       bufferSub = new CLR_RT_Buffer();
                CLR_RECORD_ASSEMBLY* headerSub;
                CLR_RT_Assembly*     assm;

                bufferSub->resize( header->TotalSize() );

                headerSub = (CLR_RECORD_ASSEMBLY*)&(*bufferSub)[0];

                if((CLR_UINT8*)header + header->TotalSize() > (CLR_UINT8*)headerEnd)
                {
                    //checksum passed, but not enough data in assembly
                    _ASSERTE(FALSE);
                    break;
                }
                memcpy( headerSub, header, header->TotalSize() );

                TINYCLR_CHECK_HRESULT(CLR_RT_Assembly::CreateInstance( headerSub, assm ));

                //CLR_RT_UnicodeHelper::ConvertFromUTF8( assm->m_szName, strName ); m_assemblies[strName] = bufferSub;

                printf( "Assembly %d: %s (%d.%d.%d.%d), size: %d\n", ++number, assm->m_szName, header->version.iMajorVersion, header->version.iMinorVersion, header->version.iBuildNumber, header->version.iRevisionNumber, header->TotalSize() );

                // jump to next assembly
                header = (CLR_RECORD_ASSEMBLY*)ROUNDTOMULTIPLE( (size_t)header + header->TotalSize(), CLR_UINT32 );
            }
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_DumpExports( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        LPCWSTR szName = PARAM_EXTRACT_STRING( params, 0 );

        if(szName[0] == 0) szName = NULL;

        if(m_fFromAssembly && m_pr)
        {
            m_pr->DumpCompact( szName );
        }
        else
        {
            TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_GenerateSkeleton( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        LPCWSTR     szFile = PARAM_EXTRACT_STRING( params, 0 );
        LPCWSTR     szName = PARAM_EXTRACT_STRING( params, 1 );
        LPCWSTR     szProj = PARAM_EXTRACT_STRING( params, 2 );
        std::string name;

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        if(szFile[0] == 0) szFile = NULL;

        CLR_RT_UnicodeHelper::ConvertToUTF8( szName, name );

        m_assm = g_CLR_RT_TypeSystem.FindAssembly( name.c_str(), NULL, false );
        if(m_assm)
        {
            m_assm->GenerateSkeleton( szFile, szProj );
        }

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_RefreshAssembly( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        LPCWSTR     szName = PARAM_EXTRACT_STRING( params, 0 );
        LPCWSTR     szFile = PARAM_EXTRACT_STRING( params, 1 );
        std::string name;

        CLR_RT_UnicodeHelper::ConvertToUTF8( szName, name );

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        m_assm = g_CLR_RT_TypeSystem.FindAssembly( name.c_str(), NULL, false );
        if(m_assm)
        {
            CLR_UINT32 len = m_assm->m_header->TotalSize();

            if(len % sizeof(CLR_UINT32))
            {
                len += sizeof(CLR_UINT32) - (len % sizeof(CLR_UINT32));
            }

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::SaveFile( szFile, (CLR_UINT8*)m_assm->m_header, (DWORD)len ));
        }

        TINYCLR_NOCLEANUP();
    }

#if defined(TINYCLR_JITTER)
    HRESULT Cmd_Jit( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        LPCWSTR     szName = PARAM_EXTRACT_STRING( params, 0 );
        std::string name;

        CLR_RT_UnicodeHelper::ConvertToUTF8( szName, name );

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        m_assm = g_CLR_RT_TypeSystem.FindAssembly( name.c_str(), NULL, false );
        if(m_assm)
        {
            for(int i=0; i<m_assm->m_pTablesSize[TBL_MethodDef]; i++)
            {
                CLR_RT_MethodDef_Index md;

                md.Set( m_assm->m_idx, i );

                TINYCLR_CHECK_HRESULT(g_CLR_RT_ExecutionEngine.Compile( md, CLR_RT_ExecutionEngine::c_Compile_CPP ));
            }
        }

        TINYCLR_NOCLEANUP();
    }
#endif

    HRESULT Cmd_Resolve( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        bool fError = false;

        TINYCLR_CHECK_HRESULT(AllocateSystem());

        TINYCLR_FOREACH_ASSEMBLY(g_CLR_RT_TypeSystem)
        {
            const CLR_RECORD_ASSEMBLYREF* src = (const CLR_RECORD_ASSEMBLYREF*)pASSM->GetTable( TBL_AssemblyRef );
            for(int i=0; i<pASSM->m_pTablesSize[TBL_AssemblyRef]; i++, src++)
            {
                LPCSTR szName = pASSM->GetString( src->name );

                if(g_CLR_RT_TypeSystem.FindAssembly( szName, &src->version, true ) == NULL)
                {
                    printf( "Missing assembly: %s (%d.%d.%d.%d)\n", szName, src->version.iMajorVersion, src->version.iMinorVersion, src->version.iBuildNumber, src->version.iRevisionNumber );

                    fError = true;
                }
            }
        }
        TINYCLR_FOREACH_ASSEMBLY_END();

        if(fError) TINYCLR_SET_AND_LEAVE(CLR_E_ENTRY_NOT_FOUND);

        TINYCLR_CHECK_HRESULT(g_CLR_RT_TypeSystem.ResolveAll());

        TINYCLR_NOCLEANUP();
    }

    //--//

    HRESULT Cmd_GenerateDependency__OutputAssembly( CLR_XmlUtil& xml, IXMLDOMNode* node, IXMLDOMNodePtr& assmNode, LPCWSTR szTag, CLR_RT_Assembly* assm )
    {
        TINYCLR_HEADER();

        std::wstring name;
        WCHAR        rgBuffer[1024];
        bool         fFound;

        CLR_RT_UnicodeHelper::ConvertFromUTF8( assm->m_szName, name );
        swprintf( rgBuffer, ARRAYSIZE(rgBuffer), L"%d.%d.%d.%d", assm->m_header->version.iMajorVersion, assm->m_header->version.iMinorVersion, assm->m_header->version.iBuildNumber, assm->m_header->version.iRevisionNumber );

        TINYCLR_CHECK_HRESULT(xml.CreateNode( szTag, &assmNode, node ));

        TINYCLR_CHECK_HRESULT(xml.PutAttribute( NULL, L"Name"   , name                                                      , fFound, assmNode ));
        TINYCLR_CHECK_HRESULT(xml.PutAttribute( NULL, L"Version", rgBuffer                                                  , fFound, assmNode ));
        TINYCLR_CHECK_HRESULT(xml.PutAttribute( NULL, L"Hash"   , WatchAssemblyBuilder::ToHex( assm->ComputeAssemblyHash() ), fFound, assmNode ));
        TINYCLR_CHECK_HRESULT(xml.PutAttribute( NULL, L"Flags"  , WatchAssemblyBuilder::ToHex( assm->m_header->flags       ), fFound, assmNode ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_GenerateDependency( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        LPCWSTR     szFile = PARAM_EXTRACT_STRING( params, 0 );
        CLR_XmlUtil xml;

        TINYCLR_CHECK_HRESULT(xml.New( L"AssemblyGraph" ));

        TINYCLR_FOREACH_ASSEMBLY(g_CLR_RT_TypeSystem)
        {
            IXMLDOMNodePtr assmNode;

            TINYCLR_CHECK_HRESULT(Cmd_GenerateDependency__OutputAssembly( xml, NULL, assmNode, L"Assembly", pASSM ));

            {
                const CLR_RECORD_ASSEMBLYREF* src = (const CLR_RECORD_ASSEMBLYREF*)pASSM->GetTable( TBL_AssemblyRef );
                for(int i=0; i<pASSM->m_pTablesSize[TBL_AssemblyRef]; i++, src++)
                {
                    IXMLDOMNodePtr   assmRefNode;
                    CLR_RT_Assembly* assmRef = g_CLR_RT_TypeSystem.FindAssembly( pASSM->GetString( src->name ), &src->version, true ); if(!assmRef) TINYCLR_SET_AND_LEAVE(CLR_E_NULL_REFERENCE);

                    TINYCLR_CHECK_HRESULT(Cmd_GenerateDependency__OutputAssembly( xml, assmNode, assmRefNode, L"AssemblyRef", assmRef ));
                }
            }

            {
                const CLR_RECORD_TYPEDEF*      src = pASSM->GetTypeDef( 0 );
                CLR_RT_TypeDef_CrossReference* dst = pASSM->m_pCrossReference_TypeDef;

                for(int i=0; i<pASSM->m_pTablesSize[TBL_TypeDef]; i++, src++, dst++)
                {
                    IXMLDOMNodePtr       typeNode;
                    CLR_RT_TypeDef_Index td; td.Set( pASSM->m_idx, i );
                    char                 rgBuffer[512];
                    LPSTR                szBuffer = rgBuffer;
                    size_t               iBuffer  = MAXSTRLEN(rgBuffer);
                    std::wstring         name;
                    bool                 fFound;

                    g_CLR_RT_TypeSystem.BuildTypeName( td, szBuffer, iBuffer );

                    //
                    // Skip types used by the runtime.
                    //
                    if(strchr( rgBuffer, '<' )) continue;
                    if(strchr( rgBuffer, '>' )) continue;
                    if(strchr( rgBuffer, '$' )) continue;

                    CLR_RT_UnicodeHelper::ConvertFromUTF8( rgBuffer, name );

                    TINYCLR_CHECK_HRESULT(xml.CreateNode( L"Type", &typeNode, assmNode ));

                    TINYCLR_CHECK_HRESULT(xml.PutAttribute( NULL, L"Name", name                                      , fFound, typeNode ));
                    TINYCLR_CHECK_HRESULT(xml.PutAttribute( NULL, L"Hash", WatchAssemblyBuilder::ToHex( dst->m_hash ), fFound, typeNode ));
                }
            }
        }
        TINYCLR_FOREACH_ASSEMBLY_END();

        TINYCLR_CHECK_HRESULT(xml.Save( szFile ));

        TINYCLR_NOCLEANUP();
    }

    //--//

    HRESULT Cmd_CreateDatabase( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        CLR_RT_StringVector vec;
        CLR_RT_Buffer       database;
        size_t              pos;

        TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::ExtractTokensFromFile( PARAM_EXTRACT_STRING( params, 0 ), vec ));

        for(size_t j=0; j<vec.size(); j++)
        {
            CLR_RT_Buffer buffer;

            TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::LoadFile( vec[j].c_str(), buffer ));

            pos = ROUNDTOMULTIPLE(database.size(),CLR_UINT32);

            database.resize( pos + buffer.size() );

            memcpy( &database[pos], &buffer[0], buffer.size() );
        }

        //
        // Add a group of zeros at the end, the device will stop at that point.
        //
        pos = ROUNDTOMULTIPLE(database.size(),CLR_UINT32);
        database.resize( pos + sizeof(CLR_UINT32) );

        TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::SaveFile( PARAM_EXTRACT_STRING( params, 1 ), database ));

        TINYCLR_NOCLEANUP();
    }

    //--//

    HRESULT Cmd_GenerateKeyPair( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        std::wstring privateKeyFile = PARAM_EXTRACT_STRING( params, 0 );
        std::wstring publicKeyFile  = PARAM_EXTRACT_STRING( params, 1 );

        RSAKey privateKey, publicKey;

        int retries = 100;
        // this call can fail becasuse of crypto API
        // try 100 times
        while(--retries)
        {
            if(GenerateKeyPair( privateKey, publicKey )) break;
        }

        TINYCLR_CHECK_HRESULT(SaveKeyToFile( privateKeyFile, privateKey ));
        TINYCLR_CHECK_HRESULT(SaveKeyToFile( publicKeyFile , publicKey  ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_DumpKey( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        unsigned int i;

        std::wstring keyFile = PARAM_EXTRACT_STRING( params, 0 );

        RSAKey key;

        TINYCLR_CHECK_HRESULT(LoadKeyFromFile( keyFile, key ));

        printf( "//typedef struct tagRSAKey\r\n" );
        printf( "//{\r\n" );
        printf( "//   DWORD exponent_len;\r\n" );
        printf( "//   RSABuffer module;\r\n" );
        printf( "//   RSABuffer exponent;\r\n" );
        printf( "//} RSAKey, *PRSAKey;\r\n" );

        printf( "\r\n" );
        printf( "\r\n" );


        printf( "RSAKey myKey =\r\n" );
        printf( "{\r\n" );
        // exponenent length
        printf( "   0x%08x,\r\n", key.exponent_len );

        // module
        printf( "{\r\n" );
        for(i = 0; i < RSA_BLOCK_SIZE_BYTES; ++i)
        {
            printf( "   0x%02x,", key.module[i] );
        }
        printf( "\r\n},\r\n" );

        // exponenent
        printf( "{\r\n" );
        for(i = 0; i < RSA_BLOCK_SIZE_BYTES; ++i)
        {
            printf( "   0x%02x,", key.exponent[i] );
        }
        printf( "\r\n},\r\n" );


        printf( "};\r\n" );

        TINYCLR_NOCLEANUP();
    }


    HRESULT Cmd_SignFile( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        CLR_RT_Buffer buf;
        RSAKey        privateKey;
        CLR_RT_Buffer signature(RSA_BLOCK_SIZE_BYTES);

        std::wstring dataFile       = PARAM_EXTRACT_STRING( params, 0 );
        std::wstring privateKeyFile = PARAM_EXTRACT_STRING( params, 1 );
        std::wstring signatureFile  = PARAM_EXTRACT_STRING( params, 2 );

        TINYCLR_CHECK_HRESULT(LoadKeyFromFile( privateKeyFile, privateKey ));

        TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::LoadFile( dataFile.c_str(), buf ));

        if(!SignData( &buf[0], buf.size(), privateKey, &signature[0], signature.size() ))
        {
            TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);
        }

        TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::SaveFile( signatureFile.c_str(), signature ));

        TINYCLR_NOCLEANUP();
    }

    HRESULT Cmd_VerifySignature( CLR_RT_ParseOptions::ParameterList* params = NULL )
    {
        TINYCLR_HEADER();

        CLR_RT_Buffer buf;
        RSAKey        publicKey;
        CLR_RT_Buffer signature;

        std::wstring dataFile      = PARAM_EXTRACT_STRING( params, 0 );
        std::wstring publicKeyFile = PARAM_EXTRACT_STRING( params, 1 );
        std::wstring signatureFile = PARAM_EXTRACT_STRING( params, 2 );

        TINYCLR_CHECK_HRESULT(LoadKeyFromFile( publicKeyFile, publicKey ));

        TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::LoadFile( dataFile.c_str(), buf ));

        TINYCLR_CHECK_HRESULT(CLR_RT_FileStore::LoadFile( signatureFile.c_str(), signature ));

        if(!VerifySignature( &buf[0], buf.size(), publicKey, &signature[0], signature.size() ))
        {
            TINYCLR_SET_AND_LEAVE(CLR_E_FAIL);
        }

        TINYCLR_NOCLEANUP();
    }

    //--//

    void Usage()
    {
        wprintf( L"\nMetaDataProcessor.exe\n"            );
        wprintf( L"Available command line switches:\n\n" );

        CLR_RT_ParseOptions::Usage();
    }

    void BuildOptions()
    {
        CLR_RT_ParseOptions::Command*   cmd;
        CLR_RT_ParseOptions::Parameter* param;

        OPTION_SET( &m_fVerbose, L"-verbose", L"Outputs each command before executing it" );

        OPTION_INTEGER( &s_CLR_RT_fTrace_RedirectLinesPerFile, L"-Trace_RedirectLinesPerFile", L"", L"<lines>", L"Lines per File" );
        OPTION_STRING ( &s_CLR_RT_fTrace_RedirectOutput      , L"-Trace_RedirectOutput"      , L"", L"<file>" , L"Output file"    );

        OPTION_SET( &m_fDumpStatistics, L"-ILstats", L"Dumps statistics about IL code" );

        //--//

        OPTION_CALL( Cmd_Reset, L"-reset", L"Clears all previous configuration" );

        //--//

        OPTION_CALL( Cmd_ResetHints, L"-resetHints", L"Clears all previous DLL hints" );

        OPTION_CALL( Cmd_LoadHints, L"-loadHints", L"Loads a specific file as a dependency" );
        PARAM_GENERIC( L"<assembly>", L"Name of the assembly to process"                    );
        PARAM_GENERIC( L"<file>"    , L"File for the assembly"                              );

        //--//

        OPTION_CALL( Cmd_IgnoreAssembly, L"-ignoreAssembly", L"Doesn't include an assembly in the dependencies" );
        PARAM_GENERIC( L"<assembly>", L"Assembly to ignore"                                                     );

        //--//

        OPTION_CALL( Cmd_Parse, L"-parse", L"Analyzes .NET assembly" );
        PARAM_GENERIC( L"<file>", L"File to analyze"                 );

        //--//

        OPTION_SET(    &m_patch_fReboot , L"-patchReboot", L"Marks the patch as needing a reboot"                               );
        OPTION_SET(    &m_patch_fSign   , L"-patchSign"  , L"Sign the patch"                                                    );
        OPTION_STRING( &m_patch_szNative, L"-patchNative", L"ARM code to include in the patch", L"<file>" , L"Native code file" );

        OPTION_CALL( Cmd_Diff, L"-diff", L"Create a patch from two assemblies" );
        PARAM_GENERIC( L"<assembly Old>", L"Old version of the assembly"       );
        PARAM_GENERIC( L"<assembly New>", L"New version of the assembly"       );
        PARAM_GENERIC( L"<patch>"       , L"File to generate"                  );

        OPTION_CALL( Cmd_DiffHex, L"-diffHex", L"Create a patch from two assemblies, as a C file" );
        PARAM_GENERIC( L"<assembly Old>", L"Old version of the assembly"                          );
        PARAM_GENERIC( L"<assembly New>", L"New version of the assembly"                          );
        PARAM_GENERIC( L"<patch>"       , L"File to generate"                                     );

        //--//

        OPTION_CALL( Cmd_Cfg, L"-cfg", L"Loads configuration from a file" );
        PARAM_GENERIC( L"<file>", L"Config file to load"                  );

        OPTION_CALL( Cmd_VerboseMinimize, L"-verboseMinimize", L"Turns on verbose level for the minimization phase" );

        OPTION_CALL( Cmd_NoByteCode, L"-noByteCode", L"Skips any ByteCode present in the assembly" );

        OPTION_CALL( Cmd_NoAttributes, L"-noAttributes", L"Skips any attribute present in the assembly" );

        OPTION_CALL( Cmd_ExcludeClassByName, L"-excludeClassByName", L"Removes a class from an assembly" );
        PARAM_GENERIC( L"<class>", L"Class to exclude"                                                   );

        OPTION_CALL( Cmd_Minimize, L"-minimize", L"Minimizes the assembly, removing unwanted elements" );

        OPTION_CALL( Cmd_SaveStrings, L"-saveStrings", L"Saves strings table to a file" );
        PARAM_GENERIC( L"<file>", L"Output file"                                        );

        OPTION_CALL( Cmd_LoadStrings, L"-loadStrings", L"Loads strings table from file" );
        PARAM_GENERIC( L"<file>", L"Input file"                                         );

        OPTION_CALL( Cmd_GenerateStringsTable, L"-generateStringsTable", L"Outputs the collected database of strings" );
        PARAM_GENERIC( L"<file>", L"Output file"                                                                      );

        OPTION_CALL( Cmd_ImportResource, L"-importResource", L"Imports .tinyresources file"   );
        PARAM_GENERIC( L"<file>", L"File to load"                                             );

        OPTION_CALL( Cmd_Compile, L"-compile", L"Compiles an assembly into the TinyCLR format" );
        PARAM_GENERIC( L"<file>", L"Generated file"                                            );

        OPTION_CALL( Cmd_Load, L"-load", L"Loads an assembly formatted for TinyCLR" );
        PARAM_GENERIC( L"<file>", L"File to load"                                   );

        OPTION_CALL( Cmd_LoadDatabase, L"-loadDatabase", L"Loads a set of assemblies" );
        PARAM_GENERIC( L"<file>", L"Image to load"                                    );

        OPTION_CALL( Cmd_DumpAll, L"-dump_all", L"Generates a report of an assembly's metadata" );
        PARAM_GENERIC( L"<file>", L"Report file"                                                );

        OPTION_CALL( Cmd_DumpDat, L"-dump_dat", L"dumps the pe files in a dat file together with their size" );
        PARAM_GENERIC( L"<file>", L"Dat file"                                                                );

        OPTION_CALL( Cmd_DumpExports, L"-dump_exports", L"Generates a report of an assembly's metadata, more readable format" );
        PARAM_GENERIC( L"<file>", L"Report file"                                                                          );

        OPTION_CALL( Cmd_GenerateSkeleton, L"-generate_skeleton", L"Generates a skeleton for the methods implemented in native code" );
        PARAM_GENERIC( L"<file>"   , L"Prefix name for the files"                                                                    );
        PARAM_GENERIC( L"<name>"   , L"Name of the assembly"                                                                         );
        PARAM_GENERIC( L"<project>", L"Identifier for the library"                                                                   );

        OPTION_CALL( Cmd_RefreshAssembly, L"-refresh_assembly", L"Recomputes CRCs for an assembly" );
        PARAM_GENERIC( L"<name>"  , L"Name of the assembly"                                        );
        PARAM_GENERIC( L"<output>", L"Output file"                                                 );

        OPTION_CALL( Cmd_Resolve, L"-resolve", L"Tries to resolve cross-assembly references" );

#if defined(TINYCLR_JITTER)
        OPTION_CALL( Cmd_Jit, L"-jit", L"Generate JIT code" );
        PARAM_GENERIC( L"<name>", L"Name of the assembly"   );

        OPTION_INTEGER( &s_CLR_RT_fJitter_Trace_Statistics, L"-Jitter_Trace_Statistics", L"", L"<level>", L"Level of verbosity" );
        OPTION_INTEGER( &s_CLR_RT_fJitter_Trace_Compile   , L"-Jitter_Trace_Compile"   , L"", L"<level>", L"Level of verbosity" );
#endif

        OPTION_CALL( Cmd_GenerateDependency, L"-generate_dependency", L"Generate an XML file with the relationship between assemblies" );
        PARAM_GENERIC( L"<file>", L"Output file"                                                                                       );

        //--//

        OPTION_CALL( Cmd_CreateDatabase, L"-create_database", L"Creates file database for a device" );
        PARAM_GENERIC( L"<config>", L"File containing the Bill of Materials"                        );
        PARAM_GENERIC( L"<file>"  , L"Output file"                                                  );

        //--//

        OPTION_CALL( Cmd_GenerateKeyPair, L"-create_key_pair", L"Creates a pair of private and public RSA keys" );
        PARAM_GENERIC( L"<private key file>", L"Output file containing the private key"                         );
        PARAM_GENERIC( L"<public key file>" , L"Output file containing the public key"                          );

        OPTION_CALL( Cmd_DumpKey, L"-dump_key", L"Dumps the key in the input file in readable format" );
        PARAM_GENERIC( L"<key file>", L"Input file containing the key"                                );

        OPTION_CALL( Cmd_SignFile, L"-sign_file", L"Signs a file with a rivate RSA key" );
        PARAM_GENERIC( L"<file to sign>"    , L"Input File to be signed"                );
        PARAM_GENERIC( L"<private key file>", L"Input file containing the private key"  );
        PARAM_GENERIC( L"<signature file>"  , L"Output file containing the signature"   );

        OPTION_CALL( Cmd_VerifySignature, L"-verify_signature", L"Verifies the signature of a file"   );
        PARAM_GENERIC( L"<signed file>"    , L"Input file for which the signature has been generated" );
        PARAM_GENERIC( L"<public key file>", L"Input file containing the public key"                  );
        PARAM_GENERIC( L"<signature file>" , L"Input file containing the signature"                   );
    }
};

//--//

const CLR_RT_NativeAssemblyData *g_CLR_InteropAssembliesNativeData[1];

int _tmain(int argc, _TCHAR* argv[])
{
    TINYCLR_HEADER();

    CLR_RT_Assembly::InitString();

    CLR_RT_StringVector vec;
    Settings            st;

    ::CoInitialize( 0 );

    TINYCLR_CHECK_HRESULT(HAL_Windows::Memory_Resize( 4 * 1024 * 1024 ));
    HAL_Init_Custom_Heap();

    CLR_RT_Memory::Reset         ();    

    st.PushArguments( argc-1, argv+1, vec );

    TINYCLR_CHECK_HRESULT(st.ProcessOptions( vec ));

    TINYCLR_CLEANUP();

    if(FAILED(hr))
    {
        ErrorReporting::Print( NULL, NULL, TRUE, 0, L"%S", CLR_RT_DUMP::GETERRORMESSAGE( hr ) );
        fflush( stdout );
    }

    ::CoUninitialize();

    return FAILED(hr) ? 10 : 0;
}
