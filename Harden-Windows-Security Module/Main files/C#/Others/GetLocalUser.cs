using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.Runtime.InteropServices;

#nullable enable

namespace HardenWindowsSecurity
{
    // Class to represent a local user account
    public sealed class LocalUser
    {
        public string? AccountExpires { get; set; }
        public string? Description { get; set; }
        public bool Enabled { get; set; }
        public string? FullName { get; set; }
        public string? PasswordChangeableDate { get; set; }
        public bool UserMayChangePassword { get; set; }
        public bool PasswordRequired { get; set; }
        public string? PasswordLastSet { get; set; }
        public string? LastLogon { get; set; }
        public string? Name { get; set; }
        public string? SID { get; set; }
        public string? ObjectClass { get; set; }
        public List<string>? Groups { get; set; }
        public List<string>? GroupsSIDs { get; set; }
    }


    /// <summary>
    /// Gets user accounts on the system similar to Get-LocalUser cmdlet
    /// It doesn't contain some properties such as PrincipalSource
    /// It doesn't contain additional properties about each user account such as their group memberships
    /// </summary>
    public static class LocalUserRetriever
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/sddl/nf-sddl-convertsidtostringsida
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);

        /// <summary>
        /// Retrieves local user accounts on the system and returns them as a list of LocalUser objects
        /// </summary>
        /// <returns></returns>
        public static List<LocalUser> Get()
        {
            // List to hold retrieved local users
            List<LocalUser> localUsers = [];

            // Create a context for the local machine
            // https://learn.microsoft.com/en-us/dotnet/api/system.directoryservices.accountmanagement.principalcontext
            using (PrincipalContext context = new(ContextType.Machine))
            {

                // Create a user principal object used as a query filter
                // https://learn.microsoft.com/en-us/dotnet/api/system.directoryservices.accountmanagement.userprincipal
                using UserPrincipal userPrincipal = new(context);

                // Initialize a searcher with the user principal object
                // https://learn.microsoft.com/en-us/dotnet/api/system.directoryservices.accountmanagement.principalsearcher
                using PrincipalSearcher searcher = new(userPrincipal);

                // Iterate over the search results
                foreach (Principal result in searcher.FindAll())
                {
                    // Cast the result to a UserPrincipal object
                    if (result is UserPrincipal user)
                    {
                        // Create a new LocalUser object and populate its properties
                        LocalUser localUser = new()
                        {
                            AccountExpires = user.AccountExpirationDate?.ToString(CultureInfo.InvariantCulture),
                            Description = user.Description,
                            Enabled = user.Enabled ?? false,
                            FullName = user.DisplayName,
                            PasswordChangeableDate = user.LastPasswordSet?.ToString(CultureInfo.InvariantCulture),
                            UserMayChangePassword = !user.UserCannotChangePassword,
                            PasswordRequired = !user.PasswordNotRequired,
                            PasswordLastSet = user.LastPasswordSet?.ToString(CultureInfo.InvariantCulture),
                            LastLogon = user.LastLogon?.ToString(CultureInfo.InvariantCulture),
                            Name = user.SamAccountName,
                            SID = user.Sid?.ToString(),
                            ObjectClass = "User",
                            Groups = GetGroupNames(user), // Populate group names
                            GroupsSIDs = GetGroupSIDs(user) // Populate group SIDs
                        };
                        localUsers.Add(localUser);
                    }
                }
            }

            // Return the list of local users
            return localUsers;
        }

        // Method to retrieve group names for a given user
        private static List<string> GetGroupNames(UserPrincipal user)
        {
            List<string> groupNames = [];

            // Iterate over the groups the user is a member of
            foreach (Principal group in user.GetGroups())
            {
                // Add group name to the list
                groupNames.Add(group.Name);
            }

            return groupNames;
        }

        // Method to retrieve group SIDs for a given user
        private static List<string> GetGroupSIDs(UserPrincipal user)
        {
            List<string> groupSIDs = [];

            // Iterate over the groups the user is a member of
            foreach (Principal group in user.GetGroups())
            {
                // Add group SID to the list
                groupSIDs.Add(group.Sid.ToString());
            }

            return groupSIDs;
        }
    }
}
