using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tunneling.Core
{
    public class Win32ServiceUtils
    {
        // 检查目标Windows服务是否存在的
        public static bool ServiceExists(string serviceName)
        {
            try
            {
                var queryInfo = new ProcessStartInfo("sc.exe", $"query \"{serviceName}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var p = Process.Start(queryInfo))
                {
                    if (p != null)
                    {
                        p.WaitForExit(3000);
                        // Exit code 0 means service exists, 1060 means service does not exist
                        return p.ExitCode == 0;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        // 卸载 Windows 服务的逻辑
        public static void UnInstallWinService(string serviceName)
        {
            try
            {
                Console.WriteLine($"Checking if Windows service '{serviceName}' exists...");

                if (!ServiceExists(serviceName))
                {
                    Console.WriteLine($"Error: Windows service '{serviceName}' does not exist. Nothing to uninstall.");
                    Environment.Exit(1);
                }

                Console.WriteLine($"Attempting to stop Windows service '{serviceName}' (if running)...");

                // Stop the service using sc.exe with administrator privileges
                var stopInfo = new ProcessStartInfo("sc.exe", $"stop \"{serviceName}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas",  // This triggers UAC dialog for sc.exe
                    CreateNoWindow = true
                };

                using (var p = Process.Start(stopInfo))
                {
                    p?.WaitForExit(8000);
                }

                Thread.Sleep(1000);  // Brief delay before delete

                // Delete the service using sc.exe with administrator privileges
                Console.WriteLine($"Deleting Windows service '{serviceName}'...");
                var delInfo = new ProcessStartInfo("sc.exe", $"delete \"{serviceName}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas",  // This triggers UAC dialog for sc.exe
                    CreateNoWindow = true
                };

                using (var p2 = Process.Start(delInfo))
                {
                    p2?.WaitForExit(8000);
                }

                Console.WriteLine("Service uninstall completed successfully.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User clicked "No" on the UAC dialog
                Console.WriteLine("Administrator privileges required for uninstall operation were not granted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to uninstall service '{serviceName}': {ex.Message}");
            }

            Environment.Exit(0);
        }

        // 安装 Windows 服务的逻辑
        public static void InstallWinService(string serviceName, string exePath)
        {
            try
            {
                var exeFileName = Path.GetFileName(exePath);

                Console.WriteLine($"Checking if Windows service '{serviceName}' already exists...");

                if (ServiceExists(serviceName))
                {
                    Console.WriteLine($"Warning: Windows service '{serviceName}' already exists.");
                    Console.WriteLine($"To reinstall, please uninstall it first using: {exeFileName} --uninstall");
                    Environment.Exit(1);
                }

                Console.WriteLine($"Installing Windows service '{serviceName}'...");
                Console.WriteLine($"Executable path: {exePath}");

                // Create the service using sc.exe with administrator privileges
                var createInfo = new ProcessStartInfo("sc.exe", $"create \"{serviceName}\" binPath= \"{exePath} --service\" start= auto")
                {
                    UseShellExecute = true,
                    Verb = "runas",  // This triggers UAC dialog for sc.exe
                    CreateNoWindow = true
                };

                using (var p = Process.Start(createInfo))
                {
                    p?.WaitForExit(8000);
                }

                Console.WriteLine($"Service '{serviceName}' has been installed successfully.");

                // Start the service
                Console.WriteLine($"Starting Windows service '{serviceName}'...");
                var startInfo = new ProcessStartInfo("sc.exe", $"start \"{serviceName}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas",  // This triggers UAC dialog for sc.exe
                    CreateNoWindow = true
                };

                using (var p = Process.Start(startInfo))
                {
                    p?.WaitForExit(1000);                  
                }

                Console.WriteLine($"Service '{serviceName}' has been started successfully.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User clicked "No" on the UAC dialog
                Console.WriteLine("Administrator privileges required for installation were not granted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to install service '{serviceName}': {ex.Message}");
            }

            Environment.Exit(0);
        }

    }
}
