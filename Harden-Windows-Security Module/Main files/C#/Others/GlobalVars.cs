using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Security.Principal;

#nullable enable

namespace HardenWindowsSecurity
{
    public static class GlobalVars
    {
        // Minimum required OS build number
        internal const decimal Requiredbuild = 22621.4169M;

        // Current OS build version
        internal static readonly decimal OSBuildNumber = Environment.OSVersion.Version.Build;

        // Update Build Revision (UBR) number
        internal static int UBR;

        // Create full OS build number as seen in Windows Settings
        internal static string? FullOSBuild;

        // Stores the value of $PSScriptRoot in a global variable to allow the internal functions to use it when navigating the module structure
        public static string? path;

        // Stores the output of Get-MpComputerStatus which happens early on in the root module .psm1 file
        public static dynamic? MDAVConfigCurrent;

        // Stores the output of Get-MpPreference which happens early on in the root module .psm1 file
        public static dynamic? MDAVPreferencesCurrent;

        //
        // The following variables are only used by the Confirm-SystemCompliance cmdlet
        //

        // Total number of Compliant values
        public static int TotalNumberOfTrueCompliantValues;

        //
        // The following variables are only used by the Protect-WindowsSecurity cmdlet
        //

        // The working directory used by the Protect-WindowsSecurity cmdlet
        internal static string WorkingDir = Path.Combine(Path.GetTempPath(), "HardeningXStuff");

        // Defining a boolean variable to determine whether optional diagnostic data should be enabled for Smart App Control or not
        public static bool ShouldEnableOptionalDiagnosticData;

        // Variable indicating whether user launched the module with Offline parameter or not
        public static bool Offline;

        // To track whether the header has been added to the log file
        internal static bool LogHeaderHasBeenWritten;

        // Path to the Microsoft Security Baselines directory after extraction
        internal static string? MicrosoftSecurityBaselinePath;

        // The path to the Microsoft 365 Security Baseline directory after extraction
        internal static string? Microsoft365SecurityBaselinePath;

        // The path to the LGPO.exe utility
        internal static string? LGPOExe;

        // A flag to determine whether the new notifications experience should be used or not
        // It won't be used if there is an interferences detected with DLL load due to other addons being loaded in the PowerShell session
        // Such as PowerToys' CommandNotFound or WinGet's PowerShell module
        public static bool UseNewNotificationsExp = true;

        // To store the registry data CSV parse output - Registry.csv
        internal static List<HardeningRegistryKeys.CsvRecord>? RegistryCSVItems;

        // To store the Process mitigations CSV parse output used by all cmdlets - ProcessMitigations.csv
        internal static List<ProcessMitigationsParser.ProcessMitigationsRecords>? ProcessMitigations;

        // a global variable to save the output of the [HardenWindowsSecurity.ProtectionCategoriex]::New().GetValidValues() in
        public static string[]? HardeningCategorieX;

        // the explicit path to save the security_policy.inf file
        internal static string securityPolicyInfPath = Path.Combine(WorkingDir, "security_policy.inf");

        // Backup of the current Controlled Folder Access List
        // Used to be restored at the end of the operation
        internal static string[]? CFABackup;

        // The value of the automatic variable $HOST from the PowerShell session
        // Stored from the module root .psm1 file
        public static PSHost? Host;

        // The value of the VerbosePreference variable of the PowerShell session
        // stored at the beginning of each cmdlet in the begin block through the Initialize() method
        public static string? VerbosePreference;

        // An object to store the final results of Confirm-SystemCompliance cmdlet
        public static ConcurrentDictionary<String, List<IndividualResult>>? FinalMegaObject;

        // Storing the output of the ini file parsing function
        internal static Dictionary<string, Dictionary<string, string>>? SystemSecurityPoliciesIniObject;

        // a variable to store the security policies CSV file parse output
        internal static List<SecurityPolicyRecord>? SecurityPolicyRecords;

        // the explicit path to save the CurrentlyAppliedMitigations.xml file
        internal static string CurrentlyAppliedMitigations = Path.Combine(WorkingDir, "CurrentlyAppliedMitigations.xml");

        // variable that contains the results of all of the related MDM CimInstances that can be interacted with using Administrator privilege
        internal static List<MDMClassProcessor>? MDMResults;

        // To store the Firewall Domain MDM profile parsed JSON output
        internal static Hashtable? MDM_Firewall_DomainProfile02;

        // To store the Firewall Private MDM profile parsed JSON output
        internal static Hashtable? MDM_Firewall_PrivateProfile02;

        // To store the Firewall Public MDM profile parsed JSON output
        internal static Hashtable? MDM_Firewall_PublicProfile02;

        // To store the Windows Update MDM parsed JSON output
        internal static Hashtable? MDM_Policy_Result01_Update02;

        // To store the System MDM parsed JSON output
        internal static Hashtable? MDM_Policy_Result01_System02;


        internal readonly static string userName;
        internal readonly static string userSID;
        internal readonly static string? userFullName;

        static GlobalVars()
        {
            // Save the valid values of the Protect-WindowsSecurity categories to a variable since the process can be time consuming and shouldn't happen every time the categories are fetched
            HardeningCategorieX = ProtectionCategoriex.GetValidValues();

            // Save the username in the class variable
            WindowsIdentity CurrentUserResult = WindowsIdentity.GetCurrent();
            userSID = CurrentUserResult.User!.Value.ToString();

            // The LocalUserRetriever.Get() method doesn't return SYSTEM so we have to handle it separately
            // In case the Harden Windows Security is running as SYSTEM
            if (CurrentUserResult.IsSystem)
            {
                userName = "SYSTEM";
                userFullName = "SYSTEM";
            }

            else
            {

                try
                {

                    LocalUser CurrentLocalUser = LocalUserRetriever.Get()
        .First(Lu => string.Equals(Lu.SID, userSID, StringComparison.OrdinalIgnoreCase));

                    userName = CurrentLocalUser.Name!;
                    userFullName = CurrentLocalUser.FullName;

                }
                // We don't fail it if username or full name can't be detected
                // Any parts of the Harden Windows Security relying on their values must gracefully handle empty or inaccurate values.
                catch
                {
                    userName = string.Empty;
                    Logger.LogMessage($"Could not find UserName or FullName of the current user with the SID {userSID}", LogTypeIntel.Warning);
                }
            }
        }

    }
}
