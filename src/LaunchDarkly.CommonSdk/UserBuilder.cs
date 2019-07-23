using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{

    /// <summary>
    /// A mutable object that uses the Builder pattern to specify properties for a
    /// <see cref="User"/> object.
    /// </summary>
    /// <remarks>
    /// Obtain an instance of this class by calling <see cref="User.Builder(string)"/>.
    /// </remarks>
    /// <example>
    /// <code>
    ///     var user = User.Build("my-key").Name("Bob").Email("test@example.com").Build();
    /// </code>
    /// </example>
    public class UserBuilder
    {
        private readonly string _key;
        private string _secondaryKey;
        private string _ipAddress;
        private string _country;
        private string _firstName;
        private string _lastName;
        private string _name;
        private string _avatar;
        private string _email;
        private bool _anonymous;
        private HashSet<string> _privateAttributeNames;
        private Dictionary<string, JToken> _custom;

        internal UserBuilder(string key)
        {
            _key = key;
        }

        internal UserBuilder(User fromUser)
        {
            _key = fromUser.Key;
            _secondaryKey = fromUser.SecondaryKey;
            _ipAddress = fromUser.IPAddress;
            _country = fromUser.Country;
            _firstName = fromUser.FirstName;
            _lastName = fromUser.LastName;
            _name = fromUser.Name;
            _avatar = fromUser.Avatar;
            _email = fromUser.Email;
            _anonymous = fromUser.Anonymous.HasValue && fromUser.Anonymous.Value;
            _privateAttributeNames = fromUser.PrivateAttributeNames == null ? null :
                new HashSet<string>(fromUser.PrivateAttributeNames);
            _custom = fromUser.Custom == null ? null :
                new Dictionary<string, JToken>(fromUser.Custom);
        }

        /// <summary>
        /// Creates a <see cref="User"/> based on the properties that have been set on the builder.
        /// Modifying the builder after this point does not affect the returned <c>User</c>.
        /// </summary>
        /// <returns>the configured <c>User</c> object</returns>
        public User Build()
        {
            return new User(_key)
            {
                SecondaryKey = _secondaryKey,
#pragma warning disable 618
                IpAddress = _ipAddress,
#pragma warning restore 618
                Country = _country,
                FirstName = _firstName,
                LastName = _lastName,
                Name = _name,
                Avatar = _avatar,
                Email = _email,
                Anonymous = _anonymous ? (bool?)true : null,
                PrivateAttributeNames = _privateAttributeNames == null ? null :
                    new HashSet<string>(_privateAttributeNames),
                Custom = _custom == null ? new Dictionary<string, JToken>() :
                    new Dictionary<string, JToken>(_custom)
            };
        }

        /// <summary>
        /// Sets the secondary key for a user.
        /// </summary>
        /// <remarks>
        /// This affects <a href="https://docs.launchdarkly.com/docs/targeting-users#section-targeting-rules-based-on-user-attributes">feature flag targeting</a>
        /// as follows: if you have chosen to bucket users by a specific attribute, the secondary key (if set)
        /// is used to further distinguish between users who are otherwise identical according to that attribute.
        /// </remarks>
        /// <param name="secondaryKey">the secondary key</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder SecondaryKey(string secondaryKey)
        {
            _secondaryKey = secondaryKey;
            return this;
        }

        /// <summary>
        /// Sets the IP address for a user.
        /// </summary>
        /// <param name="ipAddress">the IP address for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder IPAddress(string ipAddress)
        {
            _ipAddress = ipAddress;
            return this;
        }

        /// <summary>
        /// same as <see cref="IPAddress(string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="ipAddress">the IP address for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateIPAddress(string ipAddress)
        {
            AddPrivateAttribute("ip");
            return IPAddress(ipAddress);
        }

        /// <summary>
        /// Sets the country identifier for a user.
        /// </summary>
        /// <remarks>
        /// This is commonly either a 2- or 3-character standard country code, but LaunchDarkly does not validate
        /// this property or restrict its possible values.
        /// </remarks>
        /// <param name="country">the country for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Country(string country)
        {
            _country = country;
            return this;
        }

        /// <summary>
        /// same as <see cref="Country(string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="country">the country for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateCountry(string country)
        {
            AddPrivateAttribute("country");
            return Country(country);
        }

        /// <summary>
        /// Sets the first name for a user.
        /// </summary>
        /// <param name="firstName">the first name for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder FirstName(string firstName)
        {
            _firstName = firstName;
            return this;
        }

        /// <summary>
        /// same as <see cref="FirstName(string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="firstName">the first n ame for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateFirstName(string firstName)
        {
            AddPrivateAttribute("firstName");
            return FirstName(firstName);
        }

        /// <summary>
        /// Sets the last name for a user.
        /// </summary>
        /// <param name="lastName">the last name for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder LastName(string lastName)
        {
            _lastName = lastName;
            return this;
        }

        /// <summary>
        /// same as <see cref="LastName(string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="lastName">the last name for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateLastName(string lastName)
        {
            AddPrivateAttribute("lastName");
            return LastName(lastName);
        }

        /// <summary>
        /// Sets the full name for a user.
        /// </summary>
        /// <param name="name">the name for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Name(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>
        /// same as <see cref="Name(string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="name">the name for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateName(string name)
        {
            AddPrivateAttribute("name");
            return Name(name);
        }

        /// <summary>
        /// Sets the avatar URL for a user.
        /// </summary>
        /// <param name="avatar">the avatar URL for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Avatar(string avatar)
        {
            _avatar = avatar;
            return this;
        }

        /// <summary>
        /// same as <see cref="Avatar(string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="avatar">the avatar URL for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateAvatar(string avatar)
        {
            AddPrivateAttribute("avatar");
            return Avatar(avatar);
        }

        /// <summary>
        /// Sets the email address for a user.
        /// </summary>
        /// <param name="email">the email address for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Email(string email)
        {
            _email = email;
            return this;
        }

        /// <summary>
        /// same as <see cref="Email(string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="email">the email address for the user</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateEmail(string email)
        {
            AddPrivateAttribute("email");
            return Email(email);
        }

        /// <summary>
        /// Sets whether this user is anonymous, meaning that the user key will not appear on your LaunchDarkly dashboard.
        /// </summary>
        /// <param name="anonymous">true if the user is anonymous</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Anonymous(bool anonymous)
        {
            _anonymous = anonymous;
            return this;
        }

        /// <summary>
        /// Adds a custom attribute whose value is a JSON value of any kind.
        /// </summary>
        /// <remarks>
        /// When set to one of the <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Custom(string name, JToken value)
        {
            if (_custom == null)
            {
                _custom = new Dictionary<string, JToken>();
            }
            _custom[name] = value;
            return this;
        }

        /// <summary>
        /// same as <see cref="Custom(string, JToken)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateCustom(string name, JToken value)
        {
            AddPrivateAttribute(name);
            return Custom(name, value);
        }

        /// <summary>
        /// Adds a custom attribute with a boolean value.
        /// </summary>
        /// <remarks>
        /// When set to one of the <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Custom(string name, bool value)
        {
            return Custom(name, new JValue(value));
        }

        /// <summary>
        /// same as <see cref="Custom(string, bool)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateCustom(string name, bool value)
        {
            return PrivateCustom(name, new JValue(value));
        }

        /// <summary>
        /// Adds a custom attribute with a string value.
        /// </summary>
        /// <remarks>
        /// When set to one of the <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Custom(string name, string value)
        {
            return Custom(name, new JValue(value));
        }

        /// <summary>
        /// same as <see cref="Custom(string, string)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateCustom(string name, string value)
        {
            return PrivateCustom(name, new JValue(value));
        }

        /// <summary>
        /// Adds a custom attribute with an integer value.
        /// </summary>
        /// <remarks>
        /// When set to one of the <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Custom(string name, int value)
        {
            return Custom(name, new JValue(value));
        }

        /// <summary>
        /// same as <see cref="Custom(string, int)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateCustom(string name, int value)
        {
            return PrivateCustom(name, new JValue(value));
        }

        /// <summary>
        /// Adds a custom attribute with a floating-point value.
        /// </summary>
        /// <remarks>
        /// When set to one of the <a href="http://docs.launchdarkly.com/docs/targeting-users#targeting-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder Custom(string name, float value)
        {
            return Custom(name, new JValue(value));
        }

        /// <summary>
        /// same as <see cref="Custom(string, float)"/>, but also specifies that this attribute should not be sent to LaunchDarkly.
        /// </summary>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder instance</returns>
        public UserBuilder PrivateCustom(string name, float value)
        {
            return PrivateCustom(name, new JValue(value));
        }

        private void AddPrivateAttribute(string attrName)
        {
            if (_privateAttributeNames == null)
            {
                _privateAttributeNames = new HashSet<string>();
            }
            _privateAttributeNames.Add(attrName);
        }
    }
}
