﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Mash.AppSettings
{
    /// <summary>
    /// Loads application settings into your own custom data class
    /// </summary>
    public sealed class AppSettingsLoader
    {
        /// <summary>
        /// Loads settings for the public properties in the specified class using the specified settings loader
        /// </summary>
        /// <typeparam name="T">The type of settings class being loaded</typeparam>
        /// <param name="settingLoader">The specified setting loader to use</param>
        /// <param name="settingsClass">The settings class to save settings into</param>
        /// <returns>True if successful</returns>
        /// <exception cref="ArgumentNullException">The parameters must be valid</exception>
        /// <exception cref=AggregateException">Any mismatch in setting name or type loading will be reported</exception>
        /// <remarks>Check trace statements for any additional issues encountered during loading</remarks>
        public static bool Load<T>(ISettingLoader settingLoader, ref T settingsClass) where T : class
        {
            if (settingLoader == null)
            {
                throw new ArgumentNullException("settingLoader");
            }

            if (settingsClass == null)
            {
                throw new ArgumentNullException("settingsClass");
            }

            var members = typeof(T).FindMembers(
                MemberTypes.Property,
                BindingFlags.Instance | BindingFlags.Public,
                HasAttribute,
                null);

            var exceptions = new List<Exception>();

            foreach (PropertyInfo member in members)
            {
                string settingName = member.Name;

                var attr = member.GetCustomAttribute<AppSettingAttribute>();
                if (attr != null &&
                    attr.Key != null)
                {
                    settingName = attr.Key;
                }

                Trace.TraceInformation($"Loading class member [{member.Name}] as [{settingName}]");

                if (!member.CanWrite)
                {
                    Trace.TraceWarning($"Property {settingsClass.GetType()}.{member.Name} is not writeable; skipping");
                    continue;
                }

                try
                {
                    if (IsValidConnectionStringProperty(member))
                    {
                        member.SetValue(settingsClass, settingLoader.GetConnectionStrings());
                        continue;
                    }

                    var loadedValue = settingLoader.GetSetting(settingName);
                    if (String.IsNullOrEmpty(loadedValue))
                    {
                        Trace.TraceWarning($"No value found for {settingName}");
                        continue;
                    }

                    var parsedValue = TypeParser.GetTypedValue(member.PropertyType, loadedValue);
                    member.SetValue(settingsClass, parsedValue);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Loading setting {settingName} failed with {ex}");
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(
                    $"{exceptions.Count} errors loading settings",
                    exceptions);
            }

            return true;
        }

        private static bool IsValidConnectionStringProperty(PropertyInfo member)
        {
            var propertyType = member.PropertyType;

            if (member.GetCustomAttribute<AppSettingAttribute>() != null &&
                member.GetCustomAttribute<AppSettingAttribute>().IsConnectionString &&
                propertyType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) &&
                propertyType.GetGenericArguments()[0] == typeof(string) &&
                propertyType.GetGenericArguments()[1] == typeof(string))
            {
                return true;
            }

            return false;
        }

        private static bool HasAttribute(MemberInfo mi, object o)
        {
            if (mi.DeclaringType.GetCustomAttribute<AppSettingAttribute>() != null)
            {
                return true;
            }

            return mi.GetCustomAttribute<AppSettingAttribute>() != null;
        }
    }
}
