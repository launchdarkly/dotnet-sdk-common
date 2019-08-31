using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

// This file should be removed in the next major version.

namespace LaunchDarkly.Client
{
    /// <summary>
    /// Extension methods that can be called on a <see cref="User"/> to add to its properties.
    /// The preferred method is to use <see cref="User.Builder(string)"/> instead.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static class UserExtensions
    {
        private const string ObsoleteMessage =
            "Use User.Build() and the UserBuilder methods instead; UserExtensions modifies User properties, which will eventually become immutable";
        /// <summary>
        /// Sets the secondary key for a user. This affects
        /// <see href="https://docs.launchdarkly.com/docs/targeting-users#section-targeting-rules-based-on-user-attributes">feature flag targeting</see>
        /// as follows: if you have chosen to bucket users by a specific attribute, the secondary key (if set)
        /// is used to further distinguish between users who are otherwise identical according to that attribute.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="secondaryKey"></param>
        /// <returns></returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndSecondaryKey(this User user, string secondaryKey)
        {
            user.SecondaryKey = secondaryKey;
            return user;
        }

        /// <summary>
        /// Sets the IP for a user.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="ipAddress">the IP address for the user</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndIpAddress(this User user, string ipAddress)
        {
            user.IpAddress = ipAddress;
            return user;
        }

        /// <summary>
        /// Sets the IP for a user, and ensures that the IP attribute is not sent back to LaunchDarkly.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="ipAddress">the IP address for the user</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateIpAddress(this User user, string ipAddress)
        {
            return user.AndIpAddress(ipAddress).AddPrivate("ip");
        }

        /// <summary>
        /// Sets the country for a user to a two-character country code.
        /// </summary>
        /// <remarks>
        /// This method requires non-null values to be two characters long; otherwise it will throw an
        /// <c>ArgumentException</c>. However, it does not validate that the value  is actually a valid
        /// <a href="http://en.wikipedia.org/wiki/ISO_3166-1">ISO 3166-1</a> alpha-2 code.
        /// 
        /// This requirement is obsolete and is maintained for backward compatibility; the LaunchDarkly
        /// service does not require that the country value be a two-character code. There is no such
        /// requirement in the newer method <see cref="IUserBuilder.Country(string)"/>.
        /// </remarks>
        /// <param name="user">the user</param>
        /// <param name="country">the country code for the user</param>
        /// <returns>the same user</returns>
        /// <exception cref="ArgumentException">if the value is not a 2-character string and is not null</exception>
        [Obsolete(ObsoleteMessage)]
        public static User AndCountry(this User user, string country)
        {
            if (country != null && country.Length != 2)
                throw new ArgumentException("Country should be a 2 character ISO 3166-1 alpha-2 code. e.g. 'US'");

            user.Country = country;
            return user;
        }

        /// <summary>
        /// Sets the country for a user, and ensures that the country attribute will not be sent back
        /// to LaunchDarkly.
        /// </summary>
        /// <remarks>
        /// This method requires non-null values to be two characters long; otherwise it will throw an
        /// <c>ArgumentException</c>. However, it does not validate that the value  is actually a valid
        /// <a href="http://en.wikipedia.org/wiki/ISO_3166-1">ISO 3166-1</a> alpha-2 code.
        /// 
        /// This requirement is obsolete and is maintained for backward compatibility; the LaunchDarkly
        /// service does not require that the country value be a two-character code. There is no such
        /// requirement in the newer method <see cref="IUserBuilder.Country(string)"/>.
        /// </remarks>
        /// <param name="user">the user</param>
        /// <param name="country">the country code for the user</param>
        /// <exception cref="ArgumentException">if the value is not a 2-character string and is not null</exception>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCountry(this User user, string country)
        {
            return user.AndCountry(country).AddPrivate("country");
        }

        /// <summary>
        /// Sets the user's first name.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="firstName">the user's first name</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndFirstName(this User user, string firstName)
        {
            user.FirstName = firstName;
            return user;
        }

        /// <summary>
        /// Sets the user's first name, and ensures that the first name attribute will not be sent back
        /// to LaunchDarkly.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="firstName">the user's first name</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateFirstName(this User user, string firstName)
        {
            return user.AndFirstName(firstName).AddPrivate("firstName");
        }

        /// <summary>
        /// Sets the user's last name.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="lastName">the user's last name</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndLastName(this User user, string lastName)
        {
            user.LastName = lastName;
            return user;
        }

        /// <summary>
        /// Sets the user's last name, and ensures that the last name attribute will not be sent back
        /// to LaunchDarkly.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="lastName">the user's last name</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateLastName(this User user, string lastName)
        {
            return user.AndLastName(lastName).AddPrivate("lastName");
        }

        /// <summary>
        /// Sets the user's full name.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="name">the user's name</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndName(this User user, string name)
        {
            user.Name = name;
            return user;
        }

        /// <summary>
        /// Sets the user's full name, and ensures that the name attribute will not be sent back
        /// to LaunchDarkly.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="name">the user's name</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateName(this User user, string name)
        {
            return user.AndName(name).AddPrivate("name");
        }

        /// <summary>
        /// Sets the user's email address.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="email">the user's email</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndEmail(this User user, string email)
        {
            user.Email = email;
            return user;
        }

        /// <summary>
        /// Sets the user's email address, and ensures that the email attribute will not be sent back
        /// to LaunchDarkly.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="email">the user's email</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateEmail(this User user, string email)
        {
            return user.AndEmail(email).AddPrivate("email");
        }

        /// <summary>
        /// Sets whether this user is anonymous.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="anonymous">true if the user is anonymous</param>
        /// <returns></returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndAnonymous(this User user, bool anonymous)
        {
            user.Anonymous = anonymous;
            return user;
        }

        /// <summary>
        /// Sets the user's avatar.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="avatar">the user's avatar</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndAvatar(this User user, string avatar)
        {
            user.Avatar = avatar;
            return user;
        }

        /// <summary>
        /// Sets the user's avatar, and ensures that the avatar attribute will not be sent back
        /// to LaunchDarkly.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="avatar">the user's avatar</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateAvatar(this User user, string avatar)
        {
            return user.AndAvatar(avatar).AddPrivate("avatar");
        }

        /// <summary>
        /// Adds a <see langword="string"/>-valued custom attribute. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, string value)
        {
            return user.AddCustom(attribute, new JValue(value));
        }

        /// <summary>
        /// Adds a <see langword="bool"/>-valued custom attribute. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, bool value)
        {
            return user.AddCustom(attribute, new JValue(value));
        }

        /// <summary>
        /// Adds an <see langword="int"/>-valued custom attribute. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, int value)
        {
            return user.AddCustom(attribute, new JValue(value));
        }

        /// <summary>
        /// Adds a <see langword="float"/>-valued custom attribute. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, float value)
        {
            return user.AddCustom(attribute, new JValue(value));
        }

        /// <summary>
        /// Adds a <see langword="long"/>-valued custom attribute. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, long value)
        {
            return user.AddCustom(attribute, new JValue(value));
        }

        /// <summary>
        /// Adds a custom attribute whose value is a list of strings. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, List<string> value)
        {
            return user.AddCustom(attribute, new JArray(value.ToArray()));
        }

        /// <summary>
        /// Adds a custom attribute whose value is a list of ints. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, List<int> value)
        {
            return user.AddCustom(attribute, new JArray(value.ToArray()));
        }

        /// <summary>
        /// Adds a custom attribute whose value is a JSON value of any kind. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndCustomAttribute(this User user, string attribute, JToken value)
        {
            return user.AddCustom(attribute, value);
        }

        /// <summary>
        /// Adds a <see langword="string"/>-valued custom attribute, and ensures that the attribute will not
        /// be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, string value)
        {
            return user.AddCustom(attribute, new JValue(value)).AddPrivate(attribute);
        }

        /// <summary>
        /// Adds a <see langword="bool"/>-valued custom attribute, and ensures that the attribute will not
        /// be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, bool value)
        {
            return user.AddCustom(attribute, new JValue(value)).AddPrivate(attribute);
        }

        /// <summary>
        /// Adds an <see langword="int"/>-valued custom attribute, and ensures that the attribute will not
        /// be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, int value)
        {
            return user.AddCustom(attribute, new JValue(value)).AddPrivate(attribute);
        }

        /// <summary>
        /// Adds a <see langword="float"/>-valued custom attribute, and ensures that the attribute will not
        /// be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, float value)
        {
            return user.AddCustom(attribute, new JValue(value)).AddPrivate(attribute);
        }

        /// <summary>
        /// Adds a <see langword="long"/>-valued custom attribute, and ensures that the attribute will not
        /// be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, long value)
        {
            return user.AddCustom(attribute, new JValue(value)).AddPrivate(attribute);
        }

        /// <summary>
        /// Adds a custom attribute who value is a list of strings, and ensures that the attribute will not
        /// be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, List<string> value)
        {
            return user.AddCustom(attribute, new JArray(value.ToArray())).AddPrivate(attribute);
        }

        /// <summary>
        /// Adds a custom attribute who value is a list of ints, and ensures that the attribute will not
        /// be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, List<int> value)
        {
            return user.AddCustom(attribute, new JArray(value.ToArray())).AddPrivate(attribute);
        }

        /// <summary>
        /// Adds a custom attribute whose value is a JSON value of any kind, and ensures that the
        /// attribute will not be sent back to LaunchDarkly. When set to one of the
        /// <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </summary>
        /// <param name="user">the user</param>
        /// <param name="attribute">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same user</returns>
        [Obsolete(ObsoleteMessage)]
        public static User AndPrivateCustomAttribute(this User user, string attribute, JToken value)
        {
            return user.AddCustom(attribute, value).AddPrivate(attribute);
        }
    }
}
