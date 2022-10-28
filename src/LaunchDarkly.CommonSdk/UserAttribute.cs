using System;
using System.Collections.Generic;
using System.Linq;

namespace LaunchDarkly.Sdk
{
    /// <summary>
    /// Constants for commonly used attribute names in the older LaunchDarkly user model.
    /// </summary>
    /// <remarks>
    /// As described in <see cref="User"/>, earlier versions of the LaunchDarkly SDK had a model
    /// that was less flexible than the current context model. It also had a larger number of
    /// attributes that had predefined names and type constraints. Most of those no longer have
    /// such constraints and are not treated specially by LaunchDarkly, but these constants are
    /// retained for backward compatibility with code that referenced them.
    /// </remarks>
    /// <seealso cref="User"/>
    public static class UserAttribute
    {
#pragma warning disable CS1591
        public const string Key = "key";
        public const string IPAddress = "ip";
        public const string Email = "email";
        public const string Name = "name";
        public const string Avatar = "avatar";
        public const string FirstName = "firstName";
        public const string LastName = "lastName";
        public const string Country = "country";
        public const string Anonymous = "anonymous";
#pragma warning restore CS1591
    }
}
