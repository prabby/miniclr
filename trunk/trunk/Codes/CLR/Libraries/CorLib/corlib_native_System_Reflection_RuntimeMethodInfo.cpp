////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "CorLib.h"

HRESULT Library_corlib_native_System_Reflection_RuntimeMethodInfo::get_ReturnType___SystemType( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_MethodDef_Instance md;
    CLR_RT_SignatureParser    parser;
    CLR_RT_TypeDescriptor     desc;

    TINYCLR_CHECK_HRESULT(Library_corlib_native_System_Reflection_MethodBase::GetMethodDescriptor( stack, stack.Arg0(), md ));

    parser.Initialize_MethodSignature( md.m_assm, md.m_target );

    TINYCLR_CHECK_HRESULT(desc.InitializeFromSignatureParser( parser ));

    stack.PushValue().SetReflection( desc.m_reflex );

    TINYCLR_NOCLEANUP();
}

//--//

