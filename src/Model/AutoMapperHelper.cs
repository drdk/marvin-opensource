using System;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Helper class to load every registreted automapper profiles.
    /// </summary>
    public static class AutoMapperHelper
    {
        static AutoMapperHelper()
        {
            //Get all Profiles
            var profiles = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                            from type in assembly.GetTypes()
                            where !Regex.IsMatch(assembly.FullName, @"^(?:System\.|Microsoft\.|AutoMapper,)")
                                && typeof(Profile).IsAssignableFrom(type)
                            select type).ToList();
            Mapper.Initialize(cfg =>
            {
                cfg.AllowNullCollections = true;
                foreach (var profile in profiles)
                {
                    cfg.AddProfile((Profile)Activator.CreateInstance(profile));
                }
            });
            Mapper.Configuration.AssertConfigurationIsValid();
        }
        /// <summary>
        /// Call static contructor to ensure one time initalation.
        /// </summary>
        public static void EnsureInitialization() { }
    }
}
