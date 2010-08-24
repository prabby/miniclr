using Microsoft.SPOT;
using System.Diagnostics;

namespace System.Ext
{
    public static class Console
    {
        public static bool Verbose = false;

        //--//

        [Conditional("DEBUG")]
        public static void Write(string message)
        {
            if (Verbose)
            {
                Debug.Print(message);
            }
        }
    }
}


