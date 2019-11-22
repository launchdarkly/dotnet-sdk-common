using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{
    /// <summary>
    /// Attributes of a user for whom you are evaluating feature flags.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="User"/> contains any user-specific properties that may be used in feature flag
    /// configurations to produce different flag variations for different users. You may define
    /// these properties however you wish.
    /// </para>
    /// <para>
    /// The only mandatory property is the <see cref="Key"/>, which must uniquely identify each user.
    /// For authenticated users, this may be a username or e-mail address. For anonymous users,
    /// this could be an IP address or session ID.
    /// </para>
    /// <para>
    /// Besides the mandatory key, <see cref="User"/> supports two kinds of optional attributes:
    /// interpreted attributes (e.g. <see cref="IPAddress"/> and <see cref="Country"/>) and custom
    /// attributes. LaunchDarkly can parse interpreted attributes and attach meaning to them. For
    /// example, from an <see cref="IPAddress"/>, LaunchDarkly can do a geo IP lookup and determine
    /// the user's country.
    /// </para>
    /// <para>
    /// Custom attributes are not parsed by LaunchDarkly. They can be used in custom rules-- for example, a
    /// custom attribute such as "customer_ranking" can be used to launch a feature to the top 10% of users
    /// on a site. Custom attributes can have values of any type supported by JSON.
    /// </para>
    /// <para>
    /// Note that the properties of <see cref="User"/> are mutable. In future versions of the SDK, this
    /// class will be immutable. The preferred method of setting user properties is to obtain a builder with
    /// <see cref="User.Builder(string)"/>; avoid using the <see cref="UserExtensions"/> methods or an object
    /// initializer expression such as <c>new User("key") { Name = "name" }</c>, since these will no longer work
    /// once <see cref="User"/> is immutable. Modifying properties after creating a <see cref="User"/> could
    /// result in unexpected inconsistencies in your analytics events, since events that have not yet been
    /// delivered retain a reference to the original <see cref="User"/>.
    /// </para>
    /// </remarks>
    public class User : IEquatable<User>
    {
        /// <summary>
        /// The unique key for the user.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "key", NullValueHandling = NullValueHandling.Ignore)]
        public string Key { get; set; }

        /// <summary>
        /// The secondary key for a user, which can be used in
        /// <see href="https://docs.launchdarkly.com/docs/targeting-users#section-targeting-rules-based-on-user-attributes">feature flag targeting</see>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The use of the secondary key in targeting is as follows: if you have chosen to bucket users by a
        /// specific attribute, the secondary key (if set) is used to further distinguish between users who are
        /// otherwise identical according to that attribute.
        /// </para>
        /// <para>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </para>
        /// </remarks>
        [JsonProperty(PropertyName = "secondary", NullValueHandling = NullValueHandling.Ignore)]
        public string Secondary { get; set; }

        /// <summary>
        /// Obsolete name for <see cref="Secondary"/>.
        /// </summary>
        [Obsolete("use Secondary")]
        [JsonIgnore]
        public string SecondaryKey
        {
            get
            {
                return Secondary;
            }
            set
            {
                Secondary = value;
            }
        }

        /// <summary>
        /// The IP address of the user (deprecated property name; use <see cref="IPAddress"/>).
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
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
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IPAddress { get; set; }

        /// <summary>
        /// The country code for the user.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "country", NullValueHandling = NullValueHandling.Ignore)]
        public string Country { get; set; }

        /// <summary>
        /// The user's first name.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "firstName", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName { get; set; }

        /// <summary>
        /// The user's last name.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "lastName", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName { get; set; }

        /// <summary>
        /// The user's full name.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// The user's avatar.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string Avatar { get; set; }

        /// <summary>
        /// The user's email address.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        /// <summary>
        /// Whether or not the user is anonymous.
        /// </summary>
        /// <remarks>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "anonymous", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Anonymous { get; set; }

        /// <summary>
        /// Custom attributes for the user. These can be more conveniently set via the extension
        /// methods <c>AndCustomAttribute</c> or <c>AndPrivateCustomAttribute</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </para>
        /// <para>
        /// Also, in a future version this will be changed to an immutable dictionary, whose values will be
        /// <see cref="LdValue"/>.
        /// </para>
        /// </remarks>
        [JsonProperty(PropertyName = "custom", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Custom { get; set; }

        /// <summary>
        /// Used internally to track which attributes are private. To set private attributes,
        /// you should use extension methods such as <c>AndPrivateName</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Although there is currently a public setter method for this property, you should avoid modifying
        /// any properties after the <see cref="User"/> has been created. All of the property setters are deprecated
        /// and will be removed in a future version. See remarks on <see cref="User"/>.
        /// </para>
        /// <para>
        /// Also, in a future version this will be changed to an immutable set.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public ISet<string> PrivateAttributeNames { get; set; }

        [Obsolete("This method has been moved to the Operator class in .NET, and is not used in Xamarin")]
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
        /// Creates an <see cref="IUserBuilder"/> for constructing a user object using a fluent syntax.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the preferred method for building a <see cref="User"/> if you are setting properties
        /// besides the <see cref="User.Key"/>. The <see cref="IUserBuilder"/> has methods for setting
        /// any number of properties, after which you call <see cref="IUserBuilder.Build"/> to get the
        /// resulting <see cref="User"/> instance.
        /// </para>
        /// <para>
        /// This is different from using the extension methods such as
        /// <see cref="UserExtensions.AndName(User, string)"/>, which modify the properties of an
        /// existing <see cref="User"/> instance. Those methods are now deprecated, because in a future
        /// version of the SDK, <see cref="User"/> will be an immutable object.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var user = User.Builder("my-key").Name("Bob").Email("test@example.com").Build();
        /// </code>
        /// </example>
        /// <param name="key">a <see langword="string"/> that uniquely identifies a user</param>
        /// <returns>a builder object</returns>
        public static IUserBuilder Builder(string key)
        {
            return new UserBuilder(key);
        }

        /// <summary>
        /// Creates an <see cref="IUserBuilder"/> for constructing a user object, with its initial
        /// properties copied from an existeing user.
        /// </summary>
        /// <remarks>
        /// This is the same as calling <c>User.Builder(fromUser.Key)</c> and then calling the
        /// <see cref="IUserBuilder"/> methods to set each of the individual properties from their current
        /// values in <c>fromUser</c>. Modifying the builder does not affect the original <see cref="User"/>.
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
        public static IUserBuilder Builder(User fromUser)
        {
            return new UserBuilder(fromUser);
        }

        /// <summary>
        /// Creates a user with the given key.
        /// </summary>
        /// <remarks>
        /// In a future version, the <see cref="User"/> constructors will not be used directly; use a
        /// factory method like <see cref="WithKey(string)"/>, or the builder pattern with <see cref="Builder(string)"/>.
        /// </remarks>
        /// <param name="key">a <see langword="string"/> that uniquely identifies a user</param>
        [Obsolete("use User.WithKey")]
        public User(string key)
        {
            Key = key;
            Custom = new Dictionary<string, JToken>();
        }

        /// <summary>
        /// Creates a user by copying all properties from another user.
        /// </summary>
        /// <remarks>
        /// In a future version, <see cref="User"/> will be immutable, so there will be no reason to
        /// make an exact copy of an instance. If you want to make a copy but then change some properties,
        /// use <see cref="User.Builder(User)"/>.
        /// </remarks>
        /// <param name="from">the user to copy</param>
        [Obsolete("use User.Builder(User)")]
        public User(User from)
        {
            Key = from.Key;
            Secondary = from.Secondary;
            IPAddress = from.IPAddress;
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
            Secondary = secondaryKey;
            IPAddress = ip;
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
        /// <param name="key">a <see langword="string"/> that uniquely identifies a user</param>
        /// <returns>a <see cref="User"/> instance</returns>
        public static User WithKey(string key)
        {
#pragma warning disable 618
            return new User(key);
#pragma warning restore 618
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

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is User u)
            {
                return ((IEquatable<User>)this).Equals(u);
            }
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(User u)
        {
            if (u == null)
            {
                return false;
            }
            return Object.Equals(Key, u.Key) &&
                Object.Equals(Secondary, u.Secondary) &&
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

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashBuilder = Util.Hash()
                .With(Key)
                .With(Secondary)
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
                    hashBuilder = hashBuilder.With(c.Key).With(c.Value);
                }
            }
            if (PrivateAttributeNames != null)
            {
                foreach (var p in PrivateAttributeNames)
                {
                    hashBuilder = hashBuilder.With(p);
                }
            }
            return hashBuilder.Value;
        }
    }
}