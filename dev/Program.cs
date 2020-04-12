using System;
using System.Collections.Generic;

namespace Coflnet.Dev
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupForConsole();
            Console.WriteLine("This is the development project for the Coflnet cloud system");
            //DevCore.Init(new SourceReference(1,0));
            Logger.Log(new List<string>(){"hi"});
            Console.ReadKey();
        }

        static void SetupForConsole()
        {
            Logger.OnLog += Console.WriteLine;
            Logger.OnError += Console.Error.WriteLine;
        }
    }
}
