////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "SPOT_Hardware.h"

HRESULT Library_spot_hardware_native_Microsoft_SPOT_Hardware_SPI::InternalWriteRead___VOID__SZARRAY_U2__I4__SZARRAY_U2__I4( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_HARDWARE();
    TINYCLR_HEADER();
    {
        CLR_RT_HeapBlock*       pThis           = stack.This();                    FAULT_ON_NULL(pThis);
        CLR_RT_HeapBlock_Array* writeBuffer     = stack.Arg1().DereferenceArray(); FAULT_ON_NULL(writeBuffer);
        CLR_INT32               writeElemCount  = stack.Arg2().NumericByRef().s4;
        CLR_RT_HeapBlock_Array* readBuffer      = stack.Arg3().DereferenceArray(); 
        CLR_UINT32              readOffset      = stack.Arg4().NumericByRef().s4;

        
        // If writeElemCount is -1, use count of elements in the array.
        writeElemCount = writeElemCount == -1 ? writeBuffer->m_numOfElements : writeElemCount; 
        
        // Check that we do not write past the buffer.
        if(writeElemCount > (CLR_INT32)writeBuffer->m_numOfElements)
        {
            TINYCLR_SET_AND_LEAVE(CLR_E_INVALID_PARAMETER);
        }

        SPI_CONFIGURATION       config;
        TINYCLR_CHECK_HRESULT(Library_spot_hardware_native_Microsoft_SPOT_Hardware_SPI__Configuration::GetInitialConfig( pThis[ FIELD__m_config ], config ));

        config.MD_16bits = TRUE;

        CPU_SPI_Initialize();
        
        if(!::CPU_SPI_nWrite16_nRead16(
                                    config,
                                    (CLR_UINT16*)writeBuffer->GetFirstElement(), 
                                    writeBuffer->m_numOfElements,
                                    readBuffer == NULL ? NULL : (CLR_UINT16*)readBuffer ->GetFirstElement(), 
                                    readBuffer == NULL ? 0    : readBuffer ->m_numOfElements,
                                    readOffset
                                  ))
        {
            TINYCLR_SET_AND_LEAVE(CLR_E_INVALID_OPERATION);
        }
    }
    TINYCLR_NOCLEANUP();
}

HRESULT Library_spot_hardware_native_Microsoft_SPOT_Hardware_SPI::InternalWriteRead___VOID__SZARRAY_U1__I4__SZARRAY_U1__I4( CLR_RT_StackFrame& stack )
{
    NATIVE_PROFILE_CLR_HARDWARE();
    TINYCLR_HEADER();
    {
        CLR_RT_HeapBlock*       pThis           = stack.This();                    FAULT_ON_NULL(pThis);
        CLR_RT_HeapBlock_Array* writeBuffer     = stack.Arg1().DereferenceArray(); FAULT_ON_NULL(writeBuffer);
        CLR_INT32               writeElemCount  = stack.Arg2().NumericByRef().s4;
        CLR_RT_HeapBlock_Array* readBuffer      = stack.Arg3().DereferenceArray();
        CLR_UINT32              readOffset      = stack.Arg4().NumericByRef().s4;

        // If writeElemCount is -1, use count of elements in the array.
        writeElemCount = writeElemCount == -1 ? writeBuffer->m_numOfElements : writeElemCount; 

        // Check that we do not write past the buffer.
        if (writeElemCount > (CLR_INT32)writeBuffer->m_numOfElements)
        {
            TINYCLR_SET_AND_LEAVE(CLR_E_INVALID_PARAMETER);
        }
        
        SPI_CONFIGURATION       config;
        TINYCLR_CHECK_HRESULT(Library_spot_hardware_native_Microsoft_SPOT_Hardware_SPI__Configuration::GetInitialConfig( pThis[ FIELD__m_config ], config ));

        config.MD_16bits = FALSE;

        CPU_SPI_Initialize();

        if(!::CPU_SPI_nWrite8_nRead8(
                                  config,
                                  writeBuffer->GetFirstElement(), 
                                  writeBuffer->m_numOfElements,
                                  readBuffer == NULL ? NULL : readBuffer ->GetFirstElement(), 
                                  readBuffer == NULL ? 0    : readBuffer ->m_numOfElements,
                                  readOffset
                                ))
        {
            TINYCLR_SET_AND_LEAVE(CLR_E_INVALID_OPERATION);
        }
    }
    TINYCLR_NOCLEANUP();
}

