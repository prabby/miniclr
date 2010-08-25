////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////#include <tinyhal.h>
#include <tinyhal.h>
 
/////////////////////////////////////////////////////////////////////

void InitCRuntime()
{
}

/////////////////////////////////////////////////////////////////////

void SystemState_Set  ( SYSTEM_STATE State )
{
}
void SystemState_Clear( SYSTEM_STATE State )
{
}
void SystemState_SetNoLock  ( SYSTEM_STATE State )
{
}
void SystemState_ClearNoLock( SYSTEM_STATE State )
{
}
BOOL SystemState_QueryNoLock( SYSTEM_STATE State )
{
    return TRUE;
}

void HAL_EnterBooterMode()
{
}

void HAL_Initialize()
{
    HAL_CONTINUATION::InitializeList();
    HAL_COMPLETION  ::InitializeList();

    Events_Initialize();

    ENABLE_INTERRUPTS();

    BlockStorageList::Initialize();

    BlockStorage_AddDevices();

    BlockStorageList::InitializeDevices();

    FS_Initialize();

    FileSystemVolumeList::Initialize();

    FS_AddVolumes();

    FileSystemVolumeList::InitializeVolumes();



    CPU_InitializeCommunication();

    LCD_Initialize();

    I2C_Initialize();

    PalEvent_Initialize();

    Gesture_Initialize();

    Ink_Initialize();

    TimeService_Initialize();
    

/*
    other drivers init
*/
}


void HAL_Uninitialize()
{
/* 
    other driver uninit
*/

    LCD_Uninitialize();
    
    I2C_Uninitialize();


    TimeService_UnInitialize();

    Ink_Uninitialize();
    Gesture_Uninitialize();
    PalEvent_Uninitialize();


//    SOCKETS_CloseConnections();


    CPU_UninitializeCommunication();

    FileSystemVolumeList::UninitializeVolumes();

    BlockStorageList::UnInitializeDevices();

    DISABLE_INTERRUPTS();
    
    Events_Uninitialize();

    HAL_CONTINUATION::Uninitialize();
    HAL_COMPLETION  ::Uninitialize();
}

////////////////////////////////////////////////////////////////////////////////////////////////////

void HARD_Breakpoint()
{
    return;
}
/////////////////////////////////////////////////////////////////////




