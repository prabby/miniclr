////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////#include <tinyhal.h> 
#include <tinyhal.h> 

#ifndef _DRIVERS_BATTERYMEASUREMENT_AD7466_H_
#define _DRIVERS_BATTERYMEASUREMENT_AD7466_H_ 1



#define ZEROC_KELVINS_X100                          27315

//--//

struct AD7466_CONFIG
{
    //
    // for MAX4704 analog mux
    //
    static const int c_ADMUX_UNUSED_CHANNEL  = 0;
    static const int c_ADMUX_TEMP_CHANNEL    = 1;
    static const int c_ADMUX_LIGHT_CHANNEL   = 2;
    static const int c_ADMUX_BATTERY_CHANNEL = 3;

    struct MUX_ADDRESS
    {
        BOOL A0;
        BOOL A1;
    };

    //--//

    BATTERY_COMMON_CONFIG CommonConfig;

    //--//

    GPIO_PIN          ADMUX_EN_L_GPIO_PIN;
    GPIO_PIN          ADMUX_A0_GPIO_PIN;
    GPIO_PIN          ADMUX_A1_GPIO_PIN;
    MUX_ADDRESS       ADMUX_Address[4];
    SPI_CONFIGURATION SPI_Config;
    UINT32            Thermistor_Rb;
    UINT32            Thermistor_R_Nominal;
    INT32             Thermistor_T_Nominal_x100;
    INT32             Thermistor_B_VAL;
    UINT32            Battery_Low_Divider_Resistor;
    UINT32            Battery_High_Divider_Resistor;
    UINT16            Battery_ADC_VMAX;
    INT16             Battery_Offset_mV;

    //--//

    static LPCSTR GetDriverName() { return "AD7466"; }
};

extern AD7466_CONFIG g_AD7466_Config;

//--//

struct AD7466_Driver
{
    VALUE_FILTER m_VoltageFilter;
    VALUE_FILTER m_ResistanceFilter;

    INT32        m_MostRecent_DegreesCelcius_x10;
    INT32        m_MostRecent_Millivolts;

    UINT32       m_Rb;

    //--//

    static BOOL Initialize  ();
    static BOOL Uninitialize();

    static BOOL Voltage    ( INT32& Millivolts         );
    static BOOL Temperature( INT32& DegreesCelcius_x10 );

    static void VoltageFilter_Reset();

private:
    static UINT16 Read( INT32 Channel );
};

extern AD7466_Driver g_AD7466_Driver;

//--//

#endif  // _DRIVERS_BATTERYMEASUREMENT_AD7466_H_
