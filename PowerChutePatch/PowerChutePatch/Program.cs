using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceProcess;

namespace PowerChutePatch
{
    internal class Program
    {
        public static string business_path = "C:\\Program Files (x86)\\APC\\PowerChute Business Edition\\agent\\lib";
        public static string network_path = "C:\\Program Files\\APC\\PowerChute\\group1\\lib";

        static void Main(string[] args)
        {
            Console.WriteLine("[i] Log4j Patcher for PowerChute [Version: 1.1.0]");
            Console.WriteLine("[i] by valnoxy (https://valnoxy.dev)");
            Console.WriteLine("\n[i] This tool is open source! See: https://github.com/valnoxy/PowerChute-Log4j-Patch");

            string service = CheckSys();

            RunService(service, false);

            string log4j = String.Empty;
            if (service == "pcns1")
            {
                if (File.Exists(Path.Combine(network_path, "log4j-core-2.13.3.jar")))
                    log4j = "log4j-core-2.13.3.jar";
                if (File.Exists(Path.Combine(network_path, "log4j-core-2.10.0.jar")))
                    log4j = "log4j-core-2.10.0.jar";
                if (File.Exists(Path.Combine(network_path, "log4j-core-2.2.jar")))
                    log4j = "log4j-core-2.2.jar";
            }

            if (service == "apcpbeagent")
            {
                if (File.Exists(Path.Combine(business_path, "log4j-core-2.14.1.jar")))
                    log4j = "log4j-core-2.14.1.jar";
                if (File.Exists(Path.Combine(business_path, "log4j-core-2.11.1.jar")))
                    log4j = "log4j-core-2.11.1.jar";
                if (File.Exists(Path.Combine(business_path, "log4j-core-2.2.jar")))
                    log4j = "log4j-core-2.2.jar";
            }

            if (String.IsNullOrEmpty(log4j))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] This Version of PowerChute is too old. Please update it before using this patch.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("[!] Restart service ...");
                RunService(service, true);
                
                Console.WriteLine("[!] Terminating in 10 sec ...");
                System.Threading.Thread.Sleep(10000);
                Environment.Exit(-1);
            }

            Console.WriteLine("[i] Removing vulnerable classes from jar file ...");
            if (service == "pcns1")
                RemoveClass(service, Path.Combine(network_path, log4j));
            if (service == "apcpbeagent")
                RemoveClass(service, Path.Combine(business_path, log4j));
            RunService(service, true);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[i] PowerChute was successfully patched! Closing ...");
            Console.ForegroundColor = ConsoleColor.White;
            System.Threading.Thread.Sleep(5000);
        }

        static string CheckSys()
        {
            Console.WriteLine("[i] Searching for APC PowerChute Business Edition ...");
            if (Directory.Exists(business_path))
            {
                Console.WriteLine("[i] PowerChute Business Edition found!");
                return "apcpbeagent";
            }
            if (Directory.Exists(network_path))
            {
                Console.WriteLine("[i] PowerChute Network Shutdown found!");
                return "pcns1";
            }
            else
            {
                Console.WriteLine("[!] Error: Cannot find PowerChute Business Edition or Network Shutdown.");
                System.Threading.Thread.Sleep(5000);
                Environment.Exit(-1);
                return "";
            }
        }

        private static void RunService(string servicename, bool v)
        {
            // Check whether the service is started.
            ServiceController sc = new ServiceController();
            sc.ServiceName = servicename;
            Console.WriteLine($"[i] The {servicename} service status is currently set to {sc.Status}");

            if (v == true)
            {
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    // Start the service if the current status is stopped.
                    Console.WriteLine($"[i] Starting the {servicename} service ...");
                    try
                    {
                        // Start the service, and wait until its status is "Running".
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running);

                        // Display the current service status.
                        Console.WriteLine($"[i] The {servicename} service status is now set to {sc.Status}.");
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine($"[!] Could not start the {servicename} service.");
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
                    Console.WriteLine($"[i] Stopping the {servicename} service ...");
                    try
                    {
                        // Stop the service, and wait until its status is "Stopped".
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped);

                        // Display the current service status.
                        Console.WriteLine($"[i] The {servicename} service status is now set to {sc.Status}.");
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine($"[!] Could not stop the {servicename} service.");
                        System.Threading.Thread.Sleep(5000);
                        Environment.Exit(-1);
                    }
                }
            }
        }

        static void RemoveClass(string servicename, string file)
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
                RunService(servicename, true);

                Console.WriteLine("[!] Terminating in 10 sec ...");
                System.Threading.Thread.Sleep(10000);
                Environment.Exit(-1);
            }
            Console.WriteLine($"[i] File {file} successfully updated.");
        }
    }
}
