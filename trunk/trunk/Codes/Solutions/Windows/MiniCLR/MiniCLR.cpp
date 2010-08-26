
#include <tinyhal.h>
#include "CLRStartup.h"
#include "EmulatorNative.h"

#define  CmdLineArgs L"-load ..\\..\\..\\pe\\Microsoft.SPOT.Native.pe -load ..\\..\\..\\pe\\mscorlib.pe -load ..\\..\\..\\pe\\MFApp.pe"

int main(int argc, CHAR* argv[])
{	

	EmulatorStart(CmdLineArgs);

	return 0;
}