////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "CorLib.h"


HRESULT Library_corlib_native_System_Globalization_DateTimeFormat::FormatDigits___STATIC__STRING__I4__I4( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_CORE();
    TINYCLR_HEADER();

    CLR_RT_HeapBlock* pArgs = &(stack.Arg0());

    int value = pArgs[ 0 ].NumericByRef().s4;
    int len   = pArgs[ 1 ].NumericByRef().s4;

    char buffer[ 12 ]; // Enough to accommodate max int

    hal_snprintf( buffer, ARRAYSIZE(buffer), (len >= 2) ? "%02d" : "%d", value );

    TINYCLR_SET_AND_LEAVE(stack.SetResult_String( buffer ));

    TINYCLR_NOCLEANUP();
}
