using System;
using System.Collections.Immutable;
using System.Linq;
using LaunchDarkly.Sdk.Internal.Helpers;
using Newtonsoft.Json;

namespace LaunchDarkly.Sdk
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
    /// Instances of <c>User</c> are immutable once created. They can be created with the factory method
    /// <see cref="User.WithKey(string)"/>, or using a builder pattern with <see cref="User.Builder(string)"/>
    /// or <see cref="User.Builder(User)"/>.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(UserJsonSerializer))]
    public class User : IEquatable<User>
    {
        private readonly string _key;
        private readonly string _secondary;
        private readonly string _ip;
        private readonly string _country;
        private readonly string _firstName;
        private readonly string _lastName;
        private readonly string _name;
        private readonly string _avatar;
        private readonly string _email;
        private readonly bool? _anonymous;
        internal readonly ImmutableDictionary<string, LdValue> _custom;
        internal readonly ImmutableHashSet<string> _privateAttributeNames;

        /// <summary>
        /// Returns the implementation of custom JSON serialization for this type.
        /// </summary>
        public static JsonConverter JsonConverter { get; } = new UserJsonSerializer();

        // Note that the JsonProperty/JsonIgnore attributes here are retained only for historical reasons
        // because developers may expect to still see them; they are not actually used in serialization,
        // since we're now using UserJsonSerializer rather than reflection for that.

        /// <summary>
        /// The unique key for the user.
        /// </summary>
        [JsonProperty(PropertyName = "key", NullValueHandling = NullValueHandling.Ignore)]
        public string Key => _key;

        /// <summary>
        /// The secondary key for a user, which can be used in
        /// <see href="https://docs.launchdarkly.com/docs/targeting-users#section-targeting-rules-based-on-user-attributes">feature flag targeting</see>.
        /// </summary>
        /// <remarks>
        /// The use of the secondary key in targeting is as follows: if you have chosen to bucket users by a
        /// specific attribute, the secondary key (if set) is used to further distinguish between users who are
        /// otherwise identical according to that attribute.
        /// </remarks>
        [JsonProperty(PropertyName = "secondary", NullValueHandling = NullValueHandling.Ignore)]
        public string Secondary => _secondary;

        /// <summary>
        /// Obsolete name for <see cref="Secondary"/>.
        /// </summary>
        [Obsolete("use Secondary")]
        [JsonIgnore]
        public string SecondaryKey => _secondary;
        
        /// <summary>
        /// The IP address of the user.
        /// </summary>
        [JsonProperty(PropertyName = "ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IPAddress => _ip;

        /// <summary>
        /// The country code for the user.
        /// </summary>
        [JsonProperty(PropertyName = "country", NullValueHandling = NullValueHandling.Ignore)]
        public string Country => _country;

        /// <summary>
        /// The user's first name.
        /// </summary>
        [JsonProperty(PropertyName = "firstName", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName => _firstName;

        /// <summary>
        /// The user's last name.
        /// </summary>
        [JsonProperty(PropertyName = "lastName", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName => _lastName;

        /// <summary>
        /// The user's full name.
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name => _name;

        /// <summary>
        /// The user's avatar.
        /// </summary>
        [JsonProperty(PropertyName = "avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string Avatar => _avatar;

        /// <summary>
        /// The user's email address.
        /// </summary>
        [JsonProperty(PropertyName = "email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email => _email;

        /// <summary>
        /// Whether or not the user is anonymous.
        /// </summary>
        [JsonIgnore]
        public bool Anonymous => _anonymous.HasValue && _anonymous.Value;

        /// <summary>
        /// Whether or not the user is anonymous, if that has been specified.
        /// </summary>
        /// <remarks>
        /// Although the <see cref="Anonymous"/> property defaults to <see langword="false"/> in terms
        /// of LaunchDarkly's user indexing behavior, for historical reasons <see langword="null"/>
        /// (the property has not been explicitly set) may behave differently from being explicitly set
        /// to <see langword="false"/>, if this property is referenced in a feature flag rule. This
        /// property getter, and the corresponding setter in <see cref="IUserBuilder"/>, allow you to
        /// treat the property as nullable.
        /// </remarks>
        [JsonProperty(PropertyName = "anonymous", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AnonymousOptional => _anonymous;

        /// <summary>
        /// Custom attributes for the user.
        /// </summary>
        [JsonProperty(PropertyName = "custom", NullValueHandling = NullValueHandling.Ignore)]
        public IImmutableDictionary<string, LdValue> Custom => _custom;

        /// <summary>
        /// Used internally to track which attributes are private.
        /// </summary>
        [JsonIgnore]
        public IImmutableSet<string> PrivateAttributeNames => _privateAttributeNames;

        /// <summary>
        /// Creates an <see cref="IUserBuilder"/> for constructing a user object using a fluent syntax.
        /// </summary>
        /// <remarks>
        /// This is the only method for building a <see cref="User"/> if you are setting properties
        /// besides the <see cref="User.Key"/>. The <see cref="IUserBuilder"/> has methods for setting
        /// any number of properties, after which you call <see cref="IUserBuilder.Build"/> to get the
        /// resulting <see cref="User"/> instance.
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

        private User(string key)
        {
            _key = key;
            _custom = ImmutableDictionary.Create<string, LdValue>();
            _privateAttributeNames = ImmutableHashSet.Create<string>();
        }
        
        /// <summary>
        /// Creates a user by specifying all properties.
        /// </summary>
        [JsonConstructor]
        public User(string key, string secondary, string ip, string country, string firstName,
                    string lastName, string name, string avatar, string email, bool? anonymous,
                    ImmutableDictionary<string, LdValue> custom, ImmutableHashSet<string> privateAttributeNames)
        {
            _key = key;
            _secondary = secondary;
            _ip = ip;
            _country = country;
            _firstName = firstName;
            _lastName = lastName;
            _name = name;
            _avatar = avatar;
            _email = email;
            _anonymous = anonymous;
            _custom = custom ?? ImmutableDictionary.Create<string, LdValue>();
            _privateAttributeNames = privateAttributeNames ?? ImmutableHashSet.Create<string>();
        }

        /// <summary>
        /// Creates a user with the given key.
        /// </summary>
        /// <param name="key">a <see langword="string"/> that uniquely identifies a user</param>
        /// <returns>a <see cref="User"/> instance</returns>
        public static User WithKey(string key)
        {
            return new User(key);
        }

        /// <summary>
        /// Gets the value of a user attribute, if present.
        /// </summary>
        /// <remarks>
        /// This can be either a built-in attribute or a custom one. It returns the value using the
        /// <see cref="LdValue"/> type, which can have any type that is supported in JSON. If the
        /// attribute does not exist, it returns <see cref="LdValue.Null"/>.
        /// </remarks>
        /// <param name="attribute">the attribute to get</param>
        /// <returns>the attribute value or <see cref="LdValue.Null"/></returns>
        public LdValue GetAttribute(UserAttribute attribute)
        {
            if (attribute.BuiltIn)
            {
                return attribute.BuiltInGetter(this);
            }
            if (_custom != null && _custom.TryGetValue(attribute.AttributeName, out var value))
            {
                return value;
            }
            return LdValue.Null;
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
            if (ReferenceEquals(this, u))
            {
                return true;
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
                PrivateAttributeNames.SetEquals(u.PrivateAttributeNames);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashBuilder = new HashCodeBuilder()
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
            foreach (var c in Custom)
            {
                hashBuilder = hashBuilder.With(c.Key).With(c.Value);
            }
            foreach (var p in PrivateAttributeNames)
            {
                hashBuilder = hashBuilder.With(p);
            }
            return hashBuilder.Value;
        }
    }
}