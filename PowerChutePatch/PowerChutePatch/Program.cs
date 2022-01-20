using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceProcess;

namespace PowerChutePatch
{
    internal class Program
    {
        public static string path = "C:\\Program Files (x86)\\APC\\PowerChute Business Edition\\agent\\lib";

        static void Main(string[] args)
        {
            Console.WriteLine("[i] Log4j Patcher for PowerChute [Version: 1.0.1]");
            Console.WriteLine("[i] by valnoxy (https://valnoxy.dev)");
            Console.WriteLine("\n[i] This tool is open source! See: https://github.com/valnoxy/PowerChute-Log4j-Patch");

            CheckSys();
            RunService(false);

            string log4j = String.Empty;
            if (File.Exists(Path.Combine(path, "log4j-core-2.14.1.jar")))
                log4j = "log4j-core-2.14.1.jar";
            if (File.Exists(Path.Combine(path, "log4j-core-2.11.1.jar")))
                log4j = "log4j-core-2.11.1.jar";
            if (File.Exists(Path.Combine(path, "log4j-core-2.2.jar")))
                log4j = "log4j-core-2.2.jar";

            if (String.IsNullOrEmpty(log4j))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] This Version of PowerChute is too old. Please update it before using this patch.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("[!] Restart service ...");
                RunService(true);
                
                Console.WriteLine("[!] Terminating in 10 sec ...");
                System.Threading.Thread.Sleep(10000);
                Environment.Exit(-1);
            }

            Console.WriteLine("[i] Removing vulnerable classes from jar file ...");
            RemoveClass(Path.Combine(path, log4j));
            RunService(true);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[i] PowerChute was successfully patched! Closing ...");
            Console.ForegroundColor = ConsoleColor.White;
            System.Threading.Thread.Sleep(5000);
        }

        static void CheckSys()
        {
            Console.WriteLine("[i] Searching for APC PowerChute Business Edition ...");
            if (!Directory.Exists(path))
            {
                Console.WriteLine("[!] Error: Cannot find PowerChute Business Edition.");
                Console.WriteLine("[!] Provided path: " + path);
                System.Threading.Thread.Sleep(5000);
                Environment.Exit(-1);
            }
            else
            {
                Console.WriteLine("[i] PowerChute Business Edition found!");
            }
        }

        private static void RunService(bool v)
        {
            // Check whether the apcpbeagent service is started.
            ServiceController sc = new ServiceController();
            sc.ServiceName = "apcpbeagent";
            Console.WriteLine("[i] The apcpbeagent service status is currently set to {0}",
                               sc.Status.ToString());

            if (v == true)
            {
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    // Start the service if the current status is stopped.
                    Console.WriteLine("[i] Starting the apcpbeagent service ...");
                    try
                    {
                        // Start the service, and wait until its status is "Running".
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running);

                        // Display the current service status.
                        Console.WriteLine("[i] The apcpbeagent service status is now set to {0}.",
                                           sc.Status.ToString());
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("[!] Could not start the apcpbeagent service.");
                        System.Threading.Thread.Sleep(5000);
                        Environment.Exit(-1);
                    }
                }
            }

            if (v == false)
            {
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    // Stop the service if the current status is started.
                    Console.WriteLine("[i] Stopping the apcpbeagent service ...");
                    try
                    {
                        // Stop the service, and wait until its status is "Stopped".
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped);

                        // Display the current service status.
                        Console.WriteLine("[i] The apcpbeagent service status is now set to {0}.",
                                           sc.Status.ToString());
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("[!] Could not stop the apcpbeagent service.");
                        System.Threading.Thread.Sleep(5000);
                        Environment.Exit(-1);
                    }
                }
            }
        }

        static void RemoveClass(string file)
        {
            try
            {
                using (ZipArchive zip = ZipFile.Open(@file, ZipArchiveMode.Update))
                {
                    zip.Entries.Where(x => x.FullName.Contains("JndiManager.class")).ToList()
                        .ForEach(y =>
                        {
                            zip.GetEntry(y.FullName).Delete();
                            Console.WriteLine("[i] Removing: JndiManager.class");
                        });
                    zip.Entries.Where(x => x.FullName.Contains("JndiLookup.class")).ToList()
                        .ForEach(y =>
                        {
                            zip.GetEntry(y.FullName).Delete();
                            Console.WriteLine("[i] Removing: JndiLookup.class");
                        });
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] An error has occurred while updating the file:\n\n" + ex);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("[!] Restart service ...");
                RunService(true);

                Console.WriteLine("[!] Terminating in 10 sec ...");
                System.Threading.Thread.Sleep(10000);
                Environment.Exit(-1);
            }
            Console.WriteLine($"[i] File {file} successfully updated.");
        }
    }
}
