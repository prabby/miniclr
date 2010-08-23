////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "CorLib.h"


HRESULT Library_corlib_native_System_Reflection_MethodBase::get_IsPublic___BOOLEAN( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(CheckFlags( stack, CLR_RECORD_METHODDEF::MD_Scope_Mask, CLR_RECORD_METHODDEF::MD_Scope_Public ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::get_IsStatic___BOOLEAN( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(CheckFlags( stack, CLR_RECORD_METHODDEF::MD_Static, CLR_RECORD_METHODDEF::MD_Static ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::get_IsFinal___BOOLEAN( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(CheckFlags( stack, CLR_RECORD_METHODDEF::MD_Final, CLR_RECORD_METHODDEF::MD_Final ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::get_IsVirtual___BOOLEAN( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(CheckFlags( stack, CLR_RECORD_METHODDEF::MD_Virtual, CLR_RECORD_METHODDEF::MD_Virtual ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::get_IsAbstract___BOOLEAN( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    TINYCLR_SET_AND_LEAVE(CheckFlags( stack, CLR_RECORD_METHODDEF::MD_Abstract, CLR_RECORD_METHODDEF::MD_Abstract ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::Invoke___OBJECT__OBJECT__SZARRAY_OBJECT( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_HeapBlock&           obj     = stack.Arg1();
    CLR_RT_MethodDef_Instance   md;
    const CLR_RECORD_METHODDEF* mdR;
    CLR_RT_HeapBlock_Array*     pArray  = stack.Arg2().DereferenceArray();
    CLR_RT_HeapBlock*           args    = NULL;
    int                         numArgs = 0;

    TINYCLR_CHECK_HRESULT(GetMethodDescriptor( stack, stack.Arg0(), md ));

    mdR = md.m_target;

    if(stack.m_customState == 0)
    {
        stack.m_customState = 1;

        if(pArray)
        {
            args    = (CLR_RT_HeapBlock*)pArray->GetFirstElement();
            numArgs =                    pArray->m_numOfElements;
        }

        TINYCLR_CHECK_HRESULT(stack.MakeCall( md, &obj, args, numArgs ));
    }
    else
    {                
        if(mdR->retVal != DATATYPE_VOID)
        {                    
            if(mdR->retVal < DATATYPE_I4)
            {
                stack.TopValue().ChangeDataType( mdR->retVal );
            }

            TINYCLR_CHECK_HRESULT(stack.TopValue().PerformBoxingIfNeeded());
        }
        else
        {               
            stack.SetResult_Object( NULL );
        }
    }

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::get_Name___STRING( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_MethodDef_Instance md;

    TINYCLR_CHECK_HRESULT(GetMethodDescriptor( stack, stack.Arg0(), md ));

    TINYCLR_SET_AND_LEAVE(CLR_RT_HeapBlock_String::CreateInstance( stack.PushValue(), md.m_target->name, md.m_assm ));

    TINYCLR_NOCLEANUP();
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::get_DeclaringType___SystemType( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_MethodDef_Instance md;
    CLR_RT_TypeDef_Instance   cls;

    TINYCLR_CHECK_HRESULT(GetMethodDescriptor( stack, stack.Arg0(), md ));

    if(cls.InitializeFromMethod( md ) == false) TINYCLR_SET_AND_LEAVE(CLR_E_WRONG_TYPE);

    {
        CLR_RT_HeapBlock& top = stack.PushValue();

        top.SetReflection( cls );
    }

    TINYCLR_NOCLEANUP();
}

//--//

HRESULT Library_corlib_native_System_Reflection_MethodBase::GetMethodDescriptor( CLR_RT_StackFrame& stack, CLR_RT_HeapBlock& arg, CLR_RT_MethodDef_Instance& inst )
{
    NATIVE_PROFILE_CLR_CORE();
    return CLR_RT_ReflectionDef_Index::Convert( arg, inst ) ? S_OK : CLR_E_NULL_REFERENCE;
}

HRESULT Library_corlib_native_System_Reflection_MethodBase::CheckFlags( CLR_RT_StackFrame& stack, CLR_UINT32 mask, CLR_UINT32 flag )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_MethodDef_Instance md;
    bool                      fRes;

    TINYCLR_CHECK_HRESULT(GetMethodDescriptor( stack, stack.Arg0(), md ));

    if((md.m_target->flags & mask) == flag)
    {
        fRes = true;
    }
    else
    {
        fRes = false;
    }

    stack.SetResult_Boolean( fRes );

    TINYCLR_NOCLEANUP();
}
