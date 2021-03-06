////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include <tinyhal.h>

///////////////////////////////////////////////////////////////////////////////

#pragma arm section code = "SectionForFlashOperations"

AT91_SAM_Driver g_AT91SAM_Driver;

#if defined(ADS_LINKER_BUG__NOT_ALL_UNUSED_VARIABLES_ARE_REMOVED)
#pragma arm section zidata
#endif

//--//

void __section(SectionForFlashOperations) AT91_SAM_Driver::Sleep(void)
{
#if defined(PLATFORM_ARM_SAM9261_ANY) || defined(PLATFORM_ARM_SAM9RL64_ANY)
    UINT32 reg = 0;

    // ARM926EJ-S Wait For Interrupt
#ifdef __GNUC__
    asm("MCR p15, 0, %0, c7, c0, 4" :: "r" (reg));
#else
    __asm
    {
        mcr     p15, 0, reg, c7, c0, 4       
    }
#endif

#elif defined(PLATFORM_ARM_SAM7X_ANY)

    AT91_PMC* pmc = (AT91_PMC*)AT91C_BASE_PMC;

    // disable the processor clock
    pmc->PMC_SCDR = AT91C_PMC_PCK;

#endif
}

BOOL AT91_SAM_Driver::Initialize()
{
    CPU_INTC_Initialize();
    CPU_GPIO_Initialize();
    return TRUE;
}

void AT91_SAM_Driver::Halt(void)      
{
    while(true);
}
void AT91_SAM_Driver::Reset(void)     
{
#if defined(PLATFORM_ARM_SAM9261_ANY) || defined(PLATFORM_ARM_SAM7X_ANY) || defined(PLATFORM_ARM_SAM9RL64_ANY)

    volatile UINT32 *pReset = (volatile UINT32*)AT91C_BASE_RSTC;

    *pReset = (AT91C_RSTC__RESET_KEY | AT91C_RTSC__PROCRST | AT91C_RTSC__PERRST | AT91C_RTSC__EXTRST);

    while(true);
#endif
}
void AT91_SAM_Driver::Pause(void)     
{
    Sleep();
}
void AT91_SAM_Driver::Shutdown(void)  
{
#if defined(PLATFORM_ARM_SAM9261_ANY) || defined(PLATFORM_ARM_SAM9RL64_ANY)
    volatile UINT32 *pShutdown = (volatile UINT32*)AT91C_BASE_SHDWC;

    *pShutdown = (AT91C_SHDWC__SHUTDOWN_KEY | AT91C_SHDWC__SHDW);

    while(true);    
#endif
}
void AT91_SAM_Driver::Hibernate(void) 
{
    Sleep();
}

