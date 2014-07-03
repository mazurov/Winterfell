using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Robb
{
    class Program
    {
        static void PressKey()
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var service = new Sync();
            if (service == null)
            {
                Console.WriteLine("Failed to initialize service");
                return;
            }
            HostFactory.Run(x =>                                 //1
            {
                x.Service<Sync>(s =>                        //2
                {
                    s.ConstructUsing(name => service);     //3
                    s.WhenStarted(tc => tc.Start());              //4
                    s.WhenStopped(tc => tc.Stop());               //5
                });
                x.RunAsLocalSystem();                            //6

                x.SetDescription("M-Files to database syncronization tool");        //7
                x.SetDisplayName("Robb");                       //8
                x.SetServiceName("Robb");                       //9
                x.UseNLog();
                x.AfterInstall(PressKey);
                x.AfterUninstall(PressKey);
                
               
            });   
        }
    }
}
