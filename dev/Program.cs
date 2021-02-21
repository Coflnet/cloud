using System;
using System.Collections.Generic;
using Core.Extentions.KeyValue;
/// <summary>
/// Example
/// </summary>
namespace Coflnet.Dev
{
    
    class Program
    {
        static void Main(string[] args)
        {
            SetupForConsole();
            Console.WriteLine("This is the development project for the Coflnet cloud system");
            DevCore.Init(new EntityId(1,0));
            var test = new KeyValueStoreTests();
            test.DistribedAddTest().GetAwaiter().GetResult();

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
