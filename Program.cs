using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MasselGUARD
{
    public static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);

        [STAThread]
        public static int Main(string[] args)
        {
            // ── Early UAC bypass for managed installs ─────────────────────────
            // If a scheduled task 'MasselGUARD' exists and we're NOT already
            // elevated, relaunch via the task (which runs at RunLevel=Highest
            // without a UAC prompt). This only applies to managed installs.
            if (!IsElevated() && ScheduledTaskExists("MasselGUARD"))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("schtasks.exe",
                        "/run /tn MasselGUARD /i")
                    {
                        CreateNoWindow  = true,
                        UseShellExecute = false,
                    });
                    return 0; // exit this non-elevated instance
                }
                catch { /* fall through to normal launch */ }
            }
            // Resolve exe dir via MainModule.FileName — same approach as working
            // WireGuardClient. Environment.ProcessPath is unreliable in some
            // self-contained publish configurations.
            string exeDir;
            try
            {
                exeDir = Path.GetDirectoryName(
                    Process.GetCurrentProcess().MainModule?.FileName
                    ?? AppContext.BaseDirectory)
                    ?? AppContext.BaseDirectory;
            }
            catch { exeDir = AppContext.BaseDirectory; }

            // CRITICAL: set CWD and DLL search path BEFORE everything else.
            // When the SCM launches this process as a service child
            //   (MasselGUARD.exe /service "...")
            // CWD is System32. tunnel.dll calls LoadLibrary("wireguard.dll")
            // using CWD + DLL search order, so both must point at the exe dir.
            try { Directory.SetCurrentDirectory(exeDir); } catch { }
            SetDllDirectory(exeDir);

            // /service dispatch — must happen before any WPF initialisation.
            int svcResult = TunnelDll.HandleServiceArgs(args, exeDir);
            if (svcResult >= 0)
                return svcResult;

            // Normal GUI launch.
            var app = new App();
            app.InitializeComponent();
            app.Run();
            return 0;
        }
        private static bool IsElevated()
        {
            try
            {
                var id = System.Security.Principal.WindowsIdentity.GetCurrent();
                var p  = new System.Security.Principal.WindowsPrincipal(id);
                return p.IsInRole(
                    System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private static bool ScheduledTaskExists(string taskName)
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo(
                    "schtasks.exe", $"/query /tn \"{taskName}\"")
                {
                    CreateNoWindow         = true,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                });
                proc?.WaitForExit(3000);
                return proc?.ExitCode == 0;
            }
            catch { return false; }
        }
    }
}
