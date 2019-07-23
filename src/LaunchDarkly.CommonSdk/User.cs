using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{
    /// <summary>
    /// A <c>User</c> object contains specific attributes of a user browsing your site. These attributes may
    /// affect the values of feature flags evaluated for that user.
    /// </summary>
    /// <remarks>
    /// The only mandatory property is the <c>Key</c>, which must uniquely identify each user. For authenticated
    /// users, this may be a username or e-mail address. For anonymous users, this could be an IP address or session ID.
    ///
    /// Besides the mandatory <c>Key</c>, <c>User</c> supports two kinds of optional attributes: interpreted
    /// attributes (e.g. <c>IpAddress</c> and <c>Country</c>) and custom attributes. LaunchDarkly can parse
    /// interpreted attributes and attach meaning to them. For example, from an <c>IpAddress</c>,
    /// LaunchDarkly can do a geo IP lookup and determine the user's country.
    ///
    /// Custom attributes are not parsed by LaunchDarkly. They can be used in custom rules-- for example, a
    /// custom attribute such as "customer_ranking" can be used to launch a feature to the top 10% of users
    /// on a site.
    /// 
    /// Note that the properties of <c>User</c> are mutable. In future versions of the SDK, this class may be
    /// changed to be immutable. The preferred method of setting user properties is to obtain a builder with
    /// <see cref="User.Builder(string)"/>; avoid using the <see cref="UserExtensions"/> methods or an object
    /// initializer expression such as <c>new User("key") { Name = "name" }</c>, since these will no longer work
    /// once <c>User</c> is immutable. Modifying properties after creating a <c>User</c> could result in
    /// unexpected inconsistencies in your analytics events, since events that have not yet been delivered
    /// retain a reference to the original <c>User</c>.
    /// </remarks>
    public class User : IEquatable<User>
    {
        /// <summary>
        /// The unique key for the user.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "key", NullValueHandling = NullValueHandling.Ignore)]
        public string Key { get; set; }

        /// <summary>
        /// The secondary key for a user. This affects
        /// <a href="https://docs.launchdarkly.com/docs/targeting-users#section-targeting-rules-based-on-user-attributes">feature flag targeting</a>
        /// as follows: if you have chosen to bucket users by a specific attribute, the secondary key (if set)
        /// is used to further distinguish between users who are otherwise identical according to that attribute.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "secondary", NullValueHandling = NullValueHandling.Ignore)]
        public string SecondaryKey { get; set; }

        /// <summary>
        /// The IP address of the user.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IpAddress { get; set; }

        /// <summary>
        /// The country code for the user.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "country", NullValueHandling = NullValueHandling.Ignore)]
        public string Country { get; set; }

        /// <summary>
        /// The user's first name.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "firstName", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName { get; set; }

        /// <summary>
        /// The user's last name.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "lastName", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName { get; set; }

        /// <summary>
        /// The user's full name.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// The user's avatar.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string Avatar { get; set; }

        /// <summary>
        /// The user's email address.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        /// <summary>
        /// Whether or not the user is anonymous.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "anonymous", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Anonymous { get; set; }

        /// <summary>
        /// Custom attributes for the user. These can be more conveniently set via the extension
        /// methods <c>AndCustomAttribute</c> or <c>AndPrivateCustomAttribute</c>.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "custom", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Custom { get; set; }

        /// <summary>
        /// Used internally to track which attributes are private. To set private attributes,
        /// you should use extension methods such as <c>AndPrivateName</c>.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonIgnore]
        public ISet<string> PrivateAttributeNames { get; set; }

        internal JToken GetValueForEvaluation(string attribute)
        {
            switch (attribute)
            {
                case "key":
                    return new JValue(Key);
                case "secondary":
                    return null;
                case "ip":
                    return new JValue(IpAddress);
                case "email":
                    return new JValue(Email);
                case "avatar":
                    return new JValue(Avatar);
                case "firstName":
                    return new JValue(FirstName);
                case "lastName":
                    return new JValue(LastName);
                case "name":
                    return new JValue(Name);
                case "country":
                    return new JValue(Country);
                case "anonymous":
                    return new JValue(Anonymous);
                default:
                    JToken customValue;
                    Custom.TryGetValue(attribute, out customValue);
                    return customValue;
            }
        }

        /// <summary>
        /// Creates a <see cref="UserBuilder"/> for constructing a user object using a fluent syntax.
        /// </summary>
        /// <remarks>
        /// This is the preferred method for building a <c>User</c> if you are setting properties
        /// besides the <c>Key</c>. The <c>UserBuilder</c> has methods for setting any number of
        /// properties, after which you call <see cref="UserBuilder.Build"/> to get the resulting
        /// <c>User</c> instance.
        /// 
        /// This is different from using the extension methods such as
        /// <see cref="UserExtensions.AndName(User, string)"/>, which modify the properties of an
        /// existing <c>User</c> instance. Those methods are now deprecated, because in a future
        /// version of the SDK, <c>User</c> will be an immutable object.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var user = User.Builder("my-key").Name("Bob").Email("test@example.com").Build();
        /// </code>
        /// </example>
        /// <param name="key">a <c>string</c> that uniquely identifies a user</param>
        /// <returns>a builder object</returns>
        public static UserBuilder Builder(string key)
        {
            return new UserBuilder(key);
        }

        /// <summary>
        /// Creates a <see cref="UserBuilder"/> for constructing a user object, with its initial
        /// properties copied from an existeing user.
        /// </summary>
        /// <remarks>
        /// This is the same as calling <c>User.Build(fromUser.Key)</c> and then calling the
        /// <c>UserBuilder</c> methods to set each of the individual properties from their current
        /// values in <c>fromUser</c>. Modifying the builder does not affect the original <c>User</c>.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var user1 = User.Builder("my-key").FirstName("Joe").LastName("Schmoe").Build();
        ///     var user2 = User.Builder(user1).FirstName("Jane").Build();
        ///     // this is equvalent to: user2 = User.Builder("my-key").FirstName("Jane").LastName("Schmoe").Build();
        /// </code>
        /// </example>
        /// <param name="fromUser">the user to copy</param>
        /// <returns>a builder object</returns>
        public static UserBuilder Builder(User fromUser)
        {
            return new UserBuilder(fromUser);
        }

        /// <summary>
        /// Creates a user with the given key.
        /// </summary>
        /// <param name="key">a <c>string</c> that uniquely identifies a user</param>
        public User(string key)
        {
            Key = key;
            Custom = new Dictionary<string, JToken>();
        }

        /// <summary>
        /// Creates a user by copying all properties from another user.
        /// </summary>
        /// <param name="from">the user to copy</param>
        public User(User from)
        {
            Key = from.Key;
            SecondaryKey = from.SecondaryKey;
            IpAddress = from.IpAddress;
            Country = from.Country;
            FirstName = from.FirstName;
            LastName = from.LastName;
            Name = from.Name;
            Avatar = from.Avatar;
            Email = from.Email;
            Anonymous = from.Anonymous;
            Custom = from.Custom == null ? null : new Dictionary<string, JToken>(from.Custom);
            PrivateAttributeNames = from.PrivateAttributeNames == null ? null : new HashSet<string>(from.PrivateAttributeNames);
        }

        /// <summary>
        /// Creates a user by specifying all properties.
        /// </summary>
        [JsonConstructor]
        public User(string key, string secondaryKey, string ip, string country, string firstName,
                    string lastName, string name, string avatar, string email, bool? anonymous,
                    IDictionary<string, JToken> custom, ISet<string> privateAttributeNames)
        {
            Key = key;
            SecondaryKey = secondaryKey;
            IpAddress = ip;
            Country = country;
            FirstName = firstName;
            LastName = lastName;
            Name = name;
            Avatar = avatar;
            Email = email;
            Anonymous = anonymous;
            Custom = custom == null ? null : new Dictionary<string, JToken>(custom);
            PrivateAttributeNames = privateAttributeNames == null ? null : new HashSet<string>(privateAttributeNames);
        }

        /// <summary>
        /// Creates a user with the given key.
        /// </summary>
        /// <param name="key">a <c>string</c> that uniquely identifies a user</param>
        /// <returns>a <c>User</c> instance</returns>
        public static User WithKey(string key)
        {
            return new User(key);
        }

        internal User AddCustom(string attribute, JToken value)
        {
            if (attribute == string.Empty)
            {
                throw new ArgumentException("Attribute Name cannot be empty");
            }
            if (Custom is null)
            {
                Custom = new Dictionary<string, JToken>();
            }
            Custom.Add(attribute, value);
            return this;
        }

        internal User AddPrivate(string name)
        {
            if (PrivateAttributeNames is null)
            {
                PrivateAttributeNames = new HashSet<string>();
            }
            PrivateAttributeNames.Add(name);
            return this;
        }

        /// <summary>
        /// Tests for equality with another object by comparing all fields of the User.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true if the object is a User and all fields are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is User u)
            {
                return ((IEquatable<User>)this).Equals(u);
            }
            return false;
        }

        /// <summary>
        /// Tests for equality with another User by comparing all fields of the User.
        /// </summary>
        /// <param name="u"></param>
        /// <returns>true if all fields are equal</returns>
        public bool Equals(User u)
        {
            if (u == null)
            {
                return false;
            }
            return Object.Equals(Key, u.Key) &&
                Object.Equals(SecondaryKey, u.SecondaryKey) &&
                Object.Equals(IpAddress, u.IpAddress) &&
                Object.Equals(Country, u.Country) &&
                Object.Equals(FirstName, u.FirstName) &&
                Object.Equals(LastName, u.LastName) &&
                Object.Equals(Name, u.Name) &&
                Object.Equals(Avatar, u.Avatar) &&
                Object.Equals(Email, u.Email) &&
                Object.Equals(Anonymous, u.Anonymous) &&
                Custom.Count == u.Custom.Count &&
                Custom.Keys.All(k => u.Custom.ContainsKey(k) && Object.Equals(Custom[k], u.Custom[k])) &&
                (PrivateAttributeNames ?? new HashSet<string>()).SetEquals(
                    u.PrivateAttributeNames ?? new HashSet<string>());
        }

        /// <summary>
        /// Computes a hash code for a User. Note that for performance reasons, the Custom and
        /// PrivateAttributeNames properties are not used in this computation, even though they
        /// are used in Equals.
        /// </summary>
        /// <returns>a hash code</returns>
        public override int GetHashCode()
        {
            var hb = Util.Hash()
                .With(Key)
                .With(SecondaryKey)
                .With(IpAddress)
                .With(Country)
                .With(FirstName)
                .With(LastName)
                .With(Name)
                .With(Avatar)
                .With(Email)
                .With(Anonymous);
            if (Custom != null)
            {
                foreach (var c in Custom)
                {
                    hb.With(c.Key).With(c.Value);
                }
            }
            if (PrivateAttributeNames != null)
            {
                foreach (var p in PrivateAttributeNames)
                {
                    hb.With(p);
                }
            }
            return hb.Value;
        }
    }

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
            _ipAddress = fromUser.IpAddress;
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
                IpAddress = _ipAddress,
                Country = _country,
                FirstName = _firstName,
                LastName = _lastName,
                Name = _name,
                Avatar = _avatar,
                Email = _email,
                Anonymous = _anonymous ? (bool?)true : null,
                PrivateAttributeNames = _privateAttributeNames == null ? null :
                    new HashSet<string>(_privateAttributeNames),
                Custom = _custom == null ? null :
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

    /// <summary>
    /// Extension methods that can be called on a <see cref="User"/> to add to its properties.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static class UserExtensions
    {
        private const string ObsoleteMessage =
            "Use User.Build() and the UserBuilder methods instead; UserExtensions modifies User properties, which will eventually become immutable";
        /// <summary>
        /// Sets the secondary key for a user. This affects
        /// <a href="https://docs.launchdarkly.com/docs/targeting-users#section-targeting-rules-based-on-user-attributes">feature flag targeting</a>
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
        /// requirement in the newer method <see cref="UserBuilder.Country(string)"/>.
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
        /// requirement in the newer method <see cref="UserBuilder.PrivateCountry(string)"/>.
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
        /// Adds a <c>string</c>-valued custom attribute. When set to one of the
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
        /// Adds a <c>bool</c>-valued custom attribute. When set to one of the
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
        /// Adds an <c>int</c>-valued custom attribute. When set to one of the
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
        /// Adds a <c>float</c>-valued custom attribute. When set to one of the
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
        /// Adds a <c>long</c>-valued custom attribute. When set to one of the
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
        /// Adds a <c>string</c>-valued custom attribute, and ensures that the attribute will not
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
        /// Adds a <c>bool</c>-valued custom attribute, and ensures that the attribute will not
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
        /// Adds an <c>int</c>-valued custom attribute, and ensures that the attribute will not
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
        /// Adds a <c>float</c>-valued custom attribute, and ensures that the attribute will not
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
        /// Adds a <c>long</c>-valued custom attribute, and ensures that the attribute will not
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