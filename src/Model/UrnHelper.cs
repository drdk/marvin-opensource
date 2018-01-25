using System.Text.RegularExpressions;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Urn validation helper
    /// </summary>
    public static class UrnHelper
    {
        /// <summary>
        /// Marvin urn prefix
        /// </summary>
        public static string UrnBase => "urn:dr:marvin:";

        /// <summary>
        /// validate a given urn
        /// </summary>
        /// <param name="urn">input to validate</param>
        /// <param name="subType">optional sub type, will be appended to UrnBase before validation</param>
        /// <returns>true, if valid</returns>
        public static bool ValidateUrn(this string urn, string subType = null)
        {
            return 
                (subType?.EndsWith(":")).GetValueOrDefault(true) && 
                urn.StartsWith(UrnBase + subType) &&
                Regex.Match(urn, @"^urn:[a-z0-9][a-z0-9-]{0,31}:[a-z0-9()+,\-.:=@;$_!*'%/?#]+$",RegexOptions.Compiled).Success;
        }

        /// <summary>
        /// Returns the plugin type for a given plugin-urn. 
        /// </summary>
        /// <param name="urn">A valid plugin urn</param>
        /// <returns>the type part of the urn</returns>
        public static string GetPluginTypeFromUrn(this string urn)
        {
            return Regex.Match(urn, $"{UrnBase}plugin:(?<type>[^:]+):.+",RegexOptions.Compiled).Groups["type"].Value;
        }
    }
}
