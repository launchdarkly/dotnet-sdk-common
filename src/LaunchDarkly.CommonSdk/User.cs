using System;
using System.Collections.Immutable;
using System.Linq;
using LaunchDarkly.Common;
using Newtonsoft.Json;

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
    /// Instances of <c>User</c> are immutable once created. They can be created with the factory method
    /// <see cref="User.WithKey(string)"/>, or using a builder pattern with <see cref="User.Builder(string)"/>
    /// or <see cref="User.Builder(User)"/>.
    /// </remarks>
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
        private readonly bool _anonymous;
        internal readonly ImmutableDictionary<string, ImmutableJsonValue> _custom;
        internal readonly ImmutableHashSet<string> _privateAttributeNames;

        /// <summary>
        /// The unique key for the user.
        /// </summary>
        [JsonProperty(PropertyName = "key", NullValueHandling = NullValueHandling.Ignore)]
        public string Key => _key;

        /// <summary>
        /// The secondary key for a user. This affects
        /// <a href="https://docs.launchdarkly.com/docs/targeting-users#section-targeting-rules-based-on-user-attributes">feature flag targeting</a>
        /// as follows: if you have chosen to bucket users by a specific attribute, the secondary key (if set)
        /// is used to further distinguish between users who are otherwise identical according to that attribute.
        /// </summary>
        [JsonProperty(PropertyName = "secondary", NullValueHandling = NullValueHandling.Ignore)]
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
        [JsonProperty(PropertyName = "anonymous", NullValueHandling = NullValueHandling.Ignore)]
        public bool Anonymous => _anonymous;

        /// <summary>
        /// Custom attributes for the user.
        /// </summary>
        [JsonProperty(PropertyName = "custom", NullValueHandling = NullValueHandling.Ignore)]
        public IImmutableDictionary<string, ImmutableJsonValue> Custom => _custom;

        /// <summary>
        /// Used internally to track which attributes are private.
        /// </summary>
        [JsonIgnore]
        public IImmutableSet<string> PrivateAttributeNames => _privateAttributeNames;
        
        /// <summary>
        /// Creates a <see cref="UserBuilder"/> for constructing a user object using a fluent syntax.
        /// </summary>
        /// <remarks>
        /// This is the only method for building a <c>User</c> if you are setting properties
        /// besides the <c>Key</c>. The <c>UserBuilder</c> has methods for setting any number of
        /// properties, after which you call <see cref="UserBuilder.Build"/> to get the resulting
        /// <c>User</c> instance.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var user = User.Builder("my-key").Name("Bob").Email("test@example.com").Build();
        /// </code>
        /// </example>
        /// <param name="key">a <c>string</c> that uniquely identifies a user</param>
        /// <returns>a builder object</returns>
        public static IUserBuilder Builder(string key)
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
        public static IUserBuilder Builder(User fromUser)
        {
            return new UserBuilder(fromUser);
        }

        private User(string key)
        {
            _key = key;
            _custom = ImmutableDictionary.Create<string, ImmutableJsonValue>();
            _privateAttributeNames = ImmutableHashSet.Create<string>();
        }
        
        /// <summary>
        /// Creates a user by specifying all properties.
        /// </summary>
        [JsonConstructor]
        public User(string key, string secondaryKey, string ip, string country, string firstName,
                    string lastName, string name, string avatar, string email, bool? anonymous,
                    ImmutableDictionary<string, ImmutableJsonValue> custom, ImmutableHashSet<string> privateAttributeNames)
        {
            _key = key;
            _secondary = secondaryKey;
            _ip = ip;
            _country = country;
            _firstName = firstName;
            _lastName = lastName;
            _name = name;
            _avatar = avatar;
            _email = email;
            _anonymous = anonymous.HasValue && anonymous.Value;
            _custom = custom ?? ImmutableDictionary.Create<string, ImmutableJsonValue>();
            _privateAttributeNames = privateAttributeNames ?? ImmutableHashSet.Create<string>();
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
            if (ReferenceEquals(this, u))
            {
                return true;
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
                PrivateAttributeNames.SetEquals(u.PrivateAttributeNames);
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
            foreach (var c in Custom)
            {
                hashBuilder.With(c.Key).With(c.Value);
            }
            foreach (var p in PrivateAttributeNames)
            {
                hashBuilder.With(p);
            }
            return hashBuilder.Value;
        }
    }
}