////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"

inline __declspec(naked) __int64 RDTSC()
{
	_asm
	{
		RDTSC
			ret
	}
}

__int64 GetRDTSC()
{
	return RDTSC();
}

INT64 rdtscStart, rdtscEnd;

//using namespace Microsoft::SPOT::Emulator;

UINT64 HAL_Time_CurrentTicks()
{
	return GetTickCount();
    //return EmulatorNative::GetITimeDriver()->CurrentTicks();
}

INT64 HAL_Time_TicksToTime( UINT64 Ticks )
{
    _ASSERTE(Ticks <= 0x7FFFFFFFFFFFFFFF);
    
    //No need to go to managed code just to return Time.  

    return Ticks;
}
       
INT64 HAL_Time_CurrentTime()
{
	return GetTickCount();
    //return EmulatorNative::GetITimeDriver()->CurrentTime();
}

void HAL_Time_GetDriftParameters  ( INT32* a, INT32* b, INT64* c )
{
    *a = 1;
    *b = 1;
    *c = 0;
}

UINT32 CPU_SystemClock()
{
	return 0;
    //return EmulatorNative::GetITimeDriver()->SystemClock;    
}

UINT32 CPU_TicksPerSecond()
{
	return 0;
    //return EmulatorNative::GetITimeDriver()->TicksPerSecond;
}

//Completions

void HAL_COMPLETION::EnqueueDelta( UINT32 uSecFromNow )
{
	//return 0;
    //EmulatorNative::GetITimeDriver()->EnqueueCompletion( (IntPtr)this, uSecFromNow ); 
}

void HAL_COMPLETION::EnqueueTicks( UINT64 EventTimeTicks )
{
    _ASSERTE(FALSE);
}

void HAL_COMPLETION::Abort()
{
    //EmulatorNative::GetITimeDriver()->AbortCompletion( (IntPtr)this );
}

void HAL_COMPLETION::Execute()
{
    //if(this->ExecuteInISR)
    //{
    //    HAL_CONTINUATION* cont = this;

    //    cont->Execute();
    //}
    //else
    //{
    //    this->Enqueue();
    //}
}

//Continuations

BOOL HAL_CONTINUATION::Dequeue_And_Execute()
{
	return 0;
    //return EmulatorNative::GetITimeDriver()->DequeueAndExecuteContinuation();
}

void HAL_CONTINUATION::InitializeCallback( HAL_CALLBACK_FPN EntryPoint, void* Argument )
{
//    Initialize();
//
//    Callback.Initialize( EntryPoint, Argument );
}

void HAL_CONTINUATION::Enqueue()
{    
/*    _ASSERTE(this->GetEntryPoint() != NULL);
    
    EmulatorNative::GetITimeDriver()->EnqueueContinuation( (IntPtr) this );*/        
}

void HAL_CONTINUATION::Abort()
{
    //EmulatorNative::GetITimeDriver()->AbortContinuation( (IntPtr) this );
}

//various

void CLR_RT_EmulatorHooks::Notify_ExecutionStateChanged()
{    
    //EmulatorNative::GetITimeDriver()->IsExecutionPaused = ClrIsDebuggerStopped();
}

