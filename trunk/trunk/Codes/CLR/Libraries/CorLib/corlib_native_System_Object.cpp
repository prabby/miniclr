////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "CorLib.h"


//--//

HRESULT Library_corlib_native_System_Object::Equals___BOOLEAN__OBJECT( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    stack.SetResult_Boolean( CLR_RT_HeapBlock::ObjectsEqual( stack.Arg0(), stack.Arg1(), true ) );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Object::GetHashCode___I4( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    stack.SetResult_I4( CLR_RT_HeapBlock::GetHashCode( stack.This(), true, 0 ) );

    TINYCLR_NOCLEANUP_NOLABEL();
}

HRESULT Library_corlib_native_System_Object::GetType___SystemType( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_TypeDescriptor desc;
    CLR_RT_ReflectionDef_Index idx;

    TINYCLR_CHECK_HRESULT(desc.InitializeFromObject( stack.Arg0() ));
      
    if(stack.Arg0().DataType() == DATATYPE_REFLECTION)
    {
        idx.m_kind               = REFLECTION_TYPE;
        idx.m_levels             = 0;
        idx.m_data.m_type.m_data = desc.m_handlerCls.m_data;
    }
    else
    {
        idx = desc.m_reflex;
    }

    {
        CLR_RT_HeapBlock& top = stack.PushValue();
        
        top.SetDataId     ( CLR_RT_HEAPBLOCK_RAW_ID( DATATYPE_REFLECTION, 0, 1 ) );                
        top.ReflectionData(                                                      ) = idx;
    }

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Object::MemberwiseClone___OBJECT( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(g_CLR_RT_ExecutionEngine.CloneObject( stack.PushValueAndClear(), stack.Arg0() ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Object::ReferenceEquals___STATIC__BOOLEAN__OBJECT__OBJECT( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    stack.SetResult_Boolean( CLR_RT_HeapBlock::ObjectsEqual( stack.Arg0(), stack.Arg1(), true ) );

    TINYCLR_NOCLEANUP_NOLABEL();
}
