using System;
using System.Collections.Generic;
using System.Linq;

namespace LaunchDarkly.Sdk
{
    /// <summary>
    /// Represents a built-in or custom attribute name supported by <see cref="User"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstraction helps to distinguish attribute names from other string values, and also
    /// improves efficiency in feature flag data structures and evaluations because built-in
    /// attributes always reuse the same instances.
    /// </para>
    /// <para>
    /// For a fuller description of user attributes and how they can be referenced in feature
    /// flag rules, read the reference guides on
    /// <a href="https://docs.launchdarkly.com/home/users/attributes">Setting user attributes</a>
    /// and <a href="https://docs.launchdarkly.com/home/flags/targeting-users">Targeting users</a>.
    /// </para>
    /// </remarks>
    /// <seealso cref="User"/>
    public struct UserAttribute : IEquatable<UserAttribute>
    {
        /// <summary>
        /// The case-sensitive attribute name.
        /// </summary>
        public string AttributeName { get; }

        private UserAttribute(string name)
        {
            AttributeName = name;
        }

        /// <summary>
        /// Represents the user key attribute.
        /// </summary>
        public static readonly UserAttribute Key =
            new UserAttribute("key");

        /// <summary>
        /// Represents the secondary key attribute.
        /// </summary>
        public static readonly UserAttribute Secondary =
            new UserAttribute("secondary");

        /// <summary>
        /// Represents the IP address attribute.
        /// </summary>
        public static readonly UserAttribute IPAddress =
            new UserAttribute("ip");

        /// <summary>
        /// Represents the user email attribute.
        /// </summary>
        public static readonly UserAttribute Email =
            new UserAttribute("email");

        /// <summary>
        /// Represents the full name attribute.
        /// </summary>
        public static readonly UserAttribute Name =
            new UserAttribute("name");

        /// <summary>
        /// Represents the avatar URL attribute.
        /// </summary>
        public static readonly UserAttribute Avatar =
            new UserAttribute("avatar");

        /// <summary>
        /// Represents the first name attribute.
        /// </summary>
        public static readonly UserAttribute FirstName =
            new UserAttribute("firstName");

        /// <summary>
        /// Represents the last name attribute.
        /// </summary>
        public static readonly UserAttribute LastName =
            new UserAttribute("lastName");

        /// <summary>
        /// Represents the country attribute.
        /// </summary>
        public static readonly UserAttribute Country =
            new UserAttribute("country");

        /// <summary>
        /// Represents the anonymous attribute.
        /// </summary>
        public static readonly UserAttribute Anonymous =
            new UserAttribute("anonymous");

        private static Dictionary<string, UserAttribute> _builtins =
            new UserAttribute[]
            {
                Key, Secondary, IPAddress, Email, Name, Avatar, FirstName, LastName, Country, Anonymous
            }.ToDictionary(a => a.AttributeName);

        /// <summary>
        /// Returns a UserAttribute instance for the specified attribute name.
        /// </summary>
        /// <param name="name">the attribute name</param>
        /// <returns>a <see cref="UserAttribute"/></returns>
        public static UserAttribute ForName(string name)
        {
            if (_builtins.TryGetValue(name, out var a))
            {
                return a;
            }
            return new UserAttribute(name);
        }

#pragma warning disable CS1591  // don't need XML comments for these standard methods
        public override bool Equals(object obj) =>
            obj is UserAttribute a && Equals(a);

        public bool Equals(UserAttribute a) => AttributeName == a.AttributeName;

        public static bool operator ==(UserAttribute a, UserAttribute b) =>
            a.AttributeName == b.AttributeName;

        public static bool operator !=(UserAttribute a, UserAttribute b) =>
            a.AttributeName != b.AttributeName;

        public override int GetHashCode() => AttributeName.GetHashCode();

        public override string ToString() => AttributeName;
#pragma warning restore CS1591
    }
}
