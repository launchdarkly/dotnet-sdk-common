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
        /// The IP address of the user (deprecated property name; use <see cref="IPAddress"/>).
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [Obsolete("use IPAddress")]
        [JsonIgnore]
        public string IpAddress
        {
            get
            {
                return IPAddress;
            }
            set
            {
                IPAddress = value;
            }
        }

        /// <summary>
        /// The IP address of the user.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <c>User</c> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <c>User</c>.
        /// </remarks>
        [JsonProperty(PropertyName = "ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IPAddress { get; set; }

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
                    return new JValue(IPAddress);
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
#pragma warning disable 618
            IpAddress = from.IPAddress;
#pragma warning restore 618
            Country = from.Country;
            FirstName = from.FirstName;
            LastName = from.LastName;
            Name = from.Name;
            Avatar = from.Avatar;
            Email = from.Email;
            Anonymous = from.Anonymous;
            Custom = from.Custom == null ? new Dictionary<string, JToken>() : new Dictionary<string, JToken>(from.Custom);
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
#pragma warning disable 618
            IpAddress = ip;
#pragma warning restore 618
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
                Object.Equals(IPAddress, u.IPAddress) &&
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
            var hashBuilder = Util.Hash()
                .With(Key)
                .With(SecondaryKey)
                .With(IPAddress)
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
                    hashBuilder.With(c.Key).With(c.Value);
                }
            }
            if (PrivateAttributeNames != null)
            {
                foreach (var p in PrivateAttributeNames)
                {
                    hashBuilder.With(p);
                }
            }
            return hashBuilder.Value;
        }
    }
}