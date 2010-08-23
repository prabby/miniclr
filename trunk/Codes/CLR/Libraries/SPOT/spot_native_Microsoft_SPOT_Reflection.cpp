////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "SPOT.h"


HRESULT Library_spot_native_Microsoft_SPOT_Reflection::GetTypesImplementingInterface___STATIC__SZARRAY_mscorlibSystemType__mscorlibSystemType( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_HeapBlock&       top    = stack.PushValueAndClear();
    int                     tot    = 0;
    CLR_RT_HeapBlock*       pArray = NULL;
    CLR_RT_TypeDef_Instance tdMatch;

    TINYCLR_CHECK_HRESULT(Library_corlib_native_System_RuntimeType::GetTypeDescriptor( stack.Arg0(), tdMatch ));

    if((tdMatch.m_target->flags & CLR_RECORD_TYPEDEF::TD_Semantics_Mask) != CLR_RECORD_TYPEDEF::TD_Semantics_Interface)
    {
        TINYCLR_SET_AND_LEAVE(CLR_E_INVALID_PARAMETER);
    }

    for(int pass=0; pass<2; pass++)
    {
        int              count = 0;

        TINYCLR_FOREACH_ASSEMBLY_IN_CURRENT_APPDOMAIN(g_CLR_RT_TypeSystem)
        {
            const CLR_RECORD_TYPEDEF* td      = pASSM->GetTypeDef( 0 );
            int                       tblSize = pASSM->m_pTablesSize[ TBL_TypeDef ];

            for(int i=0; i<tblSize; i++, td++)
            {
                if(td->flags & CLR_RECORD_TYPEDEF::TD_Abstract) continue;

                CLR_RT_TypeDef_Index idx; idx.Set( pASSM->m_idx, i );

                if(CLR_RT_ExecutionEngine::IsInstanceOf( idx, tdMatch ))
                {
                    if(pass == 0)
                    {
                        tot++;
                    }
                    else
                    {
                        pArray[ count ].SetReflection( idx );
                    }

                    count++;
                }
            }
        }
        TINYCLR_FOREACH_ASSEMBLY_IN_CURRENT_APPDOMAIN_END();

        if(pass == 0)
        {
            if(tot == 0) break;

            TINYCLR_CHECK_HRESULT(CLR_RT_HeapBlock_Array::CreateInstance( top, tot, g_CLR_RT_WellKnownTypes.m_Type ));

            CLR_RT_HeapBlock_Array* array = top.DereferenceArray();

            pArray = (CLR_RT_HeapBlock*)array->GetFirstElement();
        }
    }


    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_Reflection::IsTypeLoaded___STATIC__BOOLEAN__mscorlibSystemType( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_TypeDef_Instance inst;

    stack.SetResult_Boolean( CLR_RT_ReflectionDef_Index::Convert( stack.Arg0(), inst, NULL ) );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_spot_native_Microsoft_SPOT_Reflection::GetTypeHash___STATIC__U4__mscorlibSystemType( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_UINT32 hash;

    if(CLR_RT_ReflectionDef_Index::Convert( stack.Arg0(), hash ) == false)
    {
        hash = 0;
    }

    stack.SetResult( hash, DATATYPE_U4 );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_spot_native_Microsoft_SPOT_Reflection::GetAssemblyHash___STATIC__U4__mscorlibSystemReflectionAssembly( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_Assembly_Instance inst;
    CLR_UINT32               hash;

    if(CLR_RT_ReflectionDef_Index::Convert( stack.Arg0(), inst ))
    {
        hash = inst.m_assm->ComputeAssemblyHash();
    }
    else
    {
        hash = 0;
    }

    stack.SetResult( hash, DATATYPE_U4 );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_spot_native_Microsoft_SPOT_Reflection::GetAssemblies___STATIC__SZARRAY_mscorlibSystemReflectionAssembly( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_HeapBlock& top    = stack.PushValueAndClear();

#if defined(TINYCLR_APPDOMAINS)
    TINYCLR_CHECK_HRESULT(g_CLR_RT_ExecutionEngine.GetCurrentAppDomain()->GetAssemblies( top ));
#else
    int               count  = 0;
    CLR_RT_HeapBlock* pArray = NULL;

    for(int pass=0; pass<2; pass++)
    {
        TINYCLR_FOREACH_ASSEMBLY(g_CLR_RT_TypeSystem)
        {
            if(pASSM->m_header->flags & CLR_RECORD_ASSEMBLY::c_Flags_Patch) continue;

            if(pass == 0)
            {
                count++;
            }
            else
            {
                CLR_RT_Assembly_Index idx; idx.Set( pASSM->m_idx );

                pArray->SetReflection( idx ); pArray++;
            }
        }
        TINYCLR_FOREACH_ASSEMBLY_END();

        if(pass == 0)
        {
            TINYCLR_CHECK_HRESULT(CLR_RT_HeapBlock_Array::CreateInstance( top, count, g_CLR_RT_WellKnownTypes.m_Assembly ));

            pArray = (CLR_RT_HeapBlock*)top.DereferenceArray()->GetFirstElement();
        }
    }
#endif //TINYCLR_APPDOMAINS

    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_native_Microsoft_SPOT_Reflection::GetAssemblyInfo___STATIC__BOOLEAN__SZARRAY_U1__MicrosoftSPOTReflectionAssemblyInfo( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_HeapBlock_Array* array = NULL;
    CLR_RT_Assembly*        assm  = NULL;
    CLR_RECORD_ASSEMBLY*    header;

    array = stack.Arg0().DereferenceArray(); FAULT_ON_NULL(array);

    header = (CLR_RECORD_ASSEMBLY*)array->GetFirstElement();

    if(header->GoodAssembly())
    {
        CLR_RT_HeapBlock* dst = stack.Arg1().Dereference(); FAULT_ON_NULL(dst);

        TINYCLR_CHECK_HRESULT(CLR_RT_Assembly::CreateInstance( header, assm ));

        TINYCLR_CHECK_HRESULT(CLR_RT_HeapBlock_String::CreateInstance( dst[ Library_spot_native_Microsoft_SPOT_Reflection__AssemblyInfo::FIELD__m_name ], assm->m_szName ));

        {
            CLR_RT_HeapBlock& refs   = dst[ Library_spot_native_Microsoft_SPOT_Reflection__AssemblyInfo::FIELD__m_refs ];
            CLR_UINT32        numRef = assm->m_pTablesSize[ TBL_AssemblyRef ];


            TINYCLR_CHECK_HRESULT(CLR_RT_HeapBlock_Array::CreateInstance( refs, numRef, g_CLR_RT_WellKnownTypes.m_UInt32 ));

            {
                const CLR_RECORD_ASSEMBLYREF* ar  =              assm->GetAssemblyRef( 0 );
                CLR_UINT32*                   dst = (CLR_UINT32*)refs.DereferenceArray()->GetFirstElement();

                while(numRef--)
                {
                    *dst++ = assm->ComputeAssemblyHash( ar++ );
                }
            }
        }

        dst[ Library_spot_native_Microsoft_SPOT_Reflection__AssemblyInfo::FIELD__m_flags ].SetInteger(                 assm->m_header->flags                  , DATATYPE_U4 );
        dst[ Library_spot_native_Microsoft_SPOT_Reflection__AssemblyInfo::FIELD__m_size  ].SetInteger( ROUNDTOMULTIPLE(assm->m_header->TotalSize(), CLR_INT32), DATATYPE_I4 );
        dst[ Library_spot_native_Microsoft_SPOT_Reflection__AssemblyInfo::FIELD__m_hash  ].SetInteger(                 assm->ComputeAssemblyHash()            , DATATYPE_U4 );

        stack.SetResult_Boolean( true );
    }
    else
    {
        stack.SetResult_Boolean( false );
    }

    TINYCLR_CLEANUP();

    if(assm)
    {
        assm->DestroyInstance();
    }

    TINYCLR_CLEANUP_END();
}

HRESULT Library_spot_native_Microsoft_SPOT_Reflection::GetTypeFromHash___STATIC__mscorlibSystemType__U4( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_HeapBlock&    top = stack.PushValueAndClear();
    CLR_RT_TypeDef_Index res;

    if(g_CLR_RT_TypeSystem.FindTypeDef( stack.Arg0().NumericByRefConst().u4, res ))
    {
        top.SetReflection( res );
    }

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_spot_native_Microsoft_SPOT_Reflection::GetAssemblyFromHash___STATIC__mscorlibSystemReflectionAssembly__U4( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_HeapBlock& top  = stack.PushValueAndClear();
    CLR_UINT32        hash = stack.Arg0().NumericByRefConst().u4;

    TINYCLR_FOREACH_ASSEMBLY_IN_CURRENT_APPDOMAIN(g_CLR_RT_TypeSystem)
    {
        if(pASSM->ComputeAssemblyHash() == hash)
        {
            CLR_RT_Assembly_Index idx; idx.Set( pASSM->m_idx );

            top.SetReflection( idx );

            TINYCLR_SET_AND_LEAVE(S_OK);
        }
    }
    TINYCLR_FOREACH_ASSEMBLY_IN_CURRENT_APPDOMAIN_END();

    TINYCLR_NOCLEANUP();
}
