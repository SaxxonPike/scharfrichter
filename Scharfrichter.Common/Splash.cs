using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scharfrichter.Common
{
    // each default application should call Scharfrichter.Common.Splash.Show(appname)
    // just for a uniform display anyway.. this is not required for custom projects

    static public class Splash
    {
        static public void Show(string applicationName)
        {
            Console.WriteLine(@"DJSLACKERS - " + applicationName);
            Console.WriteLine(@"Using modified NAudio - http://naudio.codeplex.com/");
        }
    }
}
