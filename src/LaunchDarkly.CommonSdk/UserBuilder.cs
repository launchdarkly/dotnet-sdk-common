using System;
using System.Collections.Immutable;

namespace LaunchDarkly.Sdk
{
    /// <summary>
    /// A mutable object that uses the Builder pattern to specify properties for a <see cref="User"/> object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Obtain an instance of this class by calling <see cref="User.Builder(string)"/>.
    /// </para>
    /// <para>
    /// All of the builder methods for setting a user attribute return a reference to the same builder, so they can be
    /// chained together (see example). Some of them have the return type <see cref="IUserBuilderCanMakeAttributePrivate"/>
    /// rather than <see cref="IUserBuilder"/>; those are the user attributes that can be designated as private.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///     var user = User.Builder("my-key")
    ///         .Name("Bob")
    ///         .Email("test@example.com")
    ///         .Build();
    /// </code>
    /// </example>
    public interface IUserBuilder
    {
        /// <summary>
        /// Creates a <see cref="Context"/> based on the properties that have been set on the builder.
        /// Modifying the builder after this point does not affect the returned <see cref="Context"/>.
        /// </summary>
        /// <returns>the configured <see cref="Context"/> object</returns>
        Context Build();

        /// <summary>
        /// Sets the unique key for a user.
        /// </summary>
        /// <param name="key">the key</param>
        /// <returns>the same builder</returns>
        IUserBuilder Key(string key);

        /// <summary>
        /// Sets the secondary key for a user.
        /// </summary>
        /// <remarks>
        /// This affects <see href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">feature flag targeting</see>
        /// as follows: if you have chosen to bucket users by a specific attribute, the secondary key (if set)
        /// is used to further distinguish between users who are otherwise identical according to that attribute.
        /// </remarks>
        /// <param name="secondaryKey">the secondary key</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Secondary(string secondaryKey);

        /// <summary>
        /// Sets the IP address for a user.
        /// </summary>
        /// <param name="ipAddress">the IP address for the user</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate IPAddress(string ipAddress);

        /// <summary>
        /// Sets the country identifier for a user.
        /// </summary>
        /// <remarks>
        /// This is commonly either a 2- or 3-character standard country code, but LaunchDarkly does not validate
        /// this property or restrict its possible values.
        /// </remarks>
        /// <param name="country">the country for the user</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Country(string country);

        /// <summary>
        /// Sets the first name for a user.
        /// </summary>
        /// <param name="firstName">the first name for the user</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate FirstName(string firstName);

        /// <summary>
        /// Sets the last name for a user.
        /// </summary>
        /// <param name="lastName">the last name for the user</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate LastName(string lastName);

        /// <summary>
        /// Sets the full name for a user.
        /// </summary>
        /// <param name="name">the name for the user</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Name(string name);

        /// <summary>
        /// Sets the avatar URL for a user.
        /// </summary>
        /// <param name="avatar">the avatar URL for the user</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Avatar(string avatar);

        /// <summary>
        /// Sets the email address for a user.
        /// </summary>
        /// <param name="email">the email address for the user</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Email(string email);

        /// <summary>
        /// Sets whether this user is anonymous, meaning that the user key will not appear on your LaunchDarkly dashboard.
        /// </summary>
        /// <param name="anonymous">true if the user is anonymous</param>
        /// <returns>the same builder</returns>
        IUserBuilder Anonymous(bool anonymous);

        /// <summary>
        /// Adds a custom attribute whose value is a JSON value of any kind.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The rules for allowable data types in custom attributes are the same as for flag
        /// variation values. For more details, see our documentation on
        /// <see href="https://docs.launchdarkly.com/sdk/concepts/flag-types">flag value types</see>.
        /// </para>
        /// <para>
        /// When set to one of the <a href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">built-in
        /// user attribute keys</a>, this custom attribute will be ignored.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var arrayOfIntsValue = LdValue.FromValues(new int[] { 1, 2, 3 });
        ///     var user = User.Builder("key").Custom("numbers", arrayOfIntsValue).Build();
        /// </code>
        /// </example>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Custom(string name, LdValue value);

        /// <summary>
        /// Adds a custom attribute with a boolean value.
        /// </summary>
        /// <remarks>
        /// When set to one of the <see href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">built-in
        /// user attribute keys</see>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Custom(string name, bool value);

        /// <summary>
        /// Adds a custom attribute with a string value.
        /// </summary>
        /// <remarks>
        /// When set to one of the <see href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">built-in
        /// user attribute keys</see>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Custom(string name, string value);

        /// <summary>
        /// Adds a custom attribute with an integer value.
        /// </summary>
        /// <remarks>
        /// When set to one of the <see href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">built-in
        /// user attribute keys</see>, this custom attribute will be ignored.
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Custom(string name, int value);

        /// <summary>
        /// Adds a custom attribute with a <see langword="long"/> value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Numeric values in custom attributes have some precision limitations, the same as for
        /// numeric values in flag variations. For more details, see our documentation on
        /// <see href="https://docs.launchdarkly.com/sdk/concepts/flag-types">flag value types</see>.
        /// </para>
        /// <para>
        /// When set to one of the <see href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">built-in
        /// user attribute keys</see>, this custom attribute will be ignored.
        /// </para>
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Custom(string name, long value);

        /// <summary>
        /// Adds a custom attribute with a floating-point value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Numeric values in custom attributes have some precision limitations, the same as for
        /// numeric values in flag variations. For more details, see our documentation on
        /// <see href="https://docs.launchdarkly.com/sdk/concepts/flag-types">flag value types</see>.
        /// </para>
        /// <para>
        /// When set to one of the <see href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">built-in
        /// user attribute keys</see>, this custom attribute will be ignored.
        /// </para>
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Custom(string name, float value);

        /// <summary>
        /// Adds a custom attribute with a <see langword="double"/> value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Numeric values in custom attributes have some precision limitations, the same as for
        /// numeric values in flag variations. For more details, see our documentation on
        /// <see href="https://docs.launchdarkly.com/sdk/concepts/flag-types">flag value types</see>.
        /// </para>
        /// <para>
        /// When set to one of the <see href="https://docs.launchdarkly.com/home/flags/targeting-users#targeting-rules-based-on-user-attributes">built-in
        /// user attribute keys</see>, this custom attribute will be ignored.
        /// </para>
        /// </remarks>
        /// <param name="name">the key for the custom attribute</param>
        /// <param name="value">the value for the custom attribute</param>
        /// <returns>the same builder</returns>
        IUserBuilderCanMakeAttributePrivate Custom(string name, double value);
    }

    /// <summary>
    /// An extension of <see cref="IUserBuilder"/> that allows attributes to be made private via
    /// the <see cref="AsPrivateAttribute"/> method.
    /// </summary>
    /// <remarks>
    /// <see cref="IUserBuilder"/> setter methods for attribute that can be made private always
    /// return this interface, rather than returning <see cref="IUserBuilder"/>. See
    /// <see cref="AsPrivateAttribute"/> for more details.
    /// </remarks>
    public interface IUserBuilderCanMakeAttributePrivate : IUserBuilder
    {
        /// <summary>
        /// Marks the last attribute that was set on this builder as being a private attribute: that is, its value will not be
        /// sent to LaunchDarkly.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This action only affects analytics events that are generated by this particular user object. To mark some (or all)
        /// user attributes as private for <i>all</i> users, use the configuration properties <c>PrivateAttributeName</c>
        /// and <c>AllAttributesPrivate</c>.
        /// </para>
        /// <para>
        /// Not all attributes can be made private: <see cref="IUserBuilder.Key(string)"/>, <see cref="IUserBuilder.Secondary(string)"/>,
        /// and <see cref="IUserBuilder.Anonymous(bool)"/> cannot be private. This is enforced by the compiler, since the builder
        /// methods for attributes that can be made private are the only ones that return <see cref="IUserBuilderCanMakeAttributePrivate"/>;
        /// therefore, you cannot write an expression like <c>User.Builder("user-key").AsPrivateAttribute()</c> or
        /// <c>User.Builder("user-key").Secondary("secondary").AsPrivateAttribute()</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>
        /// In this example, <c>FirstName</c> and <c>LastName</c> are marked as private, but <c>Country</c> is not.
        /// </para>
        /// <code>
        ///     var user = User.Builder("user-key")
        ///         .FirstName("Pierre").AsPrivateAttribute()
        ///         .LastName("Menard").AsPrivateAttribute()
        ///         .Country("ES")
        ///         .Build();
        /// </code>
        /// </example>
        /// <returns>the same builder</returns>
        IUserBuilder AsPrivateAttribute();
    }

    internal class UserBuilder : IUserBuilder
    {
        private readonly ContextBuilder _builder;

        internal UserBuilder(string key)
        {
            _builder = Context.Builder(key);
        }

        internal UserBuilder(Context fromContext)
        {
            _builder = Context.BuilderFromContext(fromContext);
        }

        public Context Build() => _builder.Build();

        public IUserBuilder Key(string key)
        {
            _builder.Key(key);
            return this;
        }

        public IUserBuilderCanMakeAttributePrivate Secondary(string secondary)
        {
            _builder.Secondary(secondary);
            return CanMakeAttributePrivate("secondary");
        }

        public IUserBuilderCanMakeAttributePrivate IPAddress(string ipAddress)
        {
            _builder.Set("ip", ipAddress);
            return CanMakeAttributePrivate("ip");
        }

        public IUserBuilderCanMakeAttributePrivate Country(string country)
        {
            _builder.Set("country", country);
            return CanMakeAttributePrivate("country");
        }

        public IUserBuilderCanMakeAttributePrivate FirstName(string firstName)
        {
            _builder.Set("firstName", firstName);
            return CanMakeAttributePrivate("firstName");
        }

        public IUserBuilderCanMakeAttributePrivate LastName(string lastName)
        {
            _builder.Set("lastName", lastName);
            return CanMakeAttributePrivate("lastName");
        }

        public IUserBuilderCanMakeAttributePrivate Name(string name)
        {
            _builder.Set("name", name);
            return CanMakeAttributePrivate("name");
        }

        public IUserBuilderCanMakeAttributePrivate Avatar(string avatar)
        {
            _builder.Set("avatar", avatar);
            return CanMakeAttributePrivate("avatar");
        }

        public IUserBuilderCanMakeAttributePrivate Email(string email)
        {
            _builder.Set("email", email);
            return CanMakeAttributePrivate("email");
        }

        public IUserBuilder Anonymous(bool anonymous)
        {
            _builder.Anonymous(anonymous);
            return this;
        }

        public IUserBuilderCanMakeAttributePrivate Custom(string name, LdValue value)
        {
            _builder.Set(name, value);
            return CanMakeAttributePrivate(name);
        }

        public IUserBuilderCanMakeAttributePrivate Custom(string name, bool value) =>
            Custom(name, LdValue.Of(value));

        public IUserBuilderCanMakeAttributePrivate Custom(string name, string value) =>
            Custom(name, LdValue.Of(value));

        public IUserBuilderCanMakeAttributePrivate Custom(string name, int value) =>
            Custom(name, LdValue.Of(value));

        public IUserBuilderCanMakeAttributePrivate Custom(string name, long value) =>
            Custom(name, LdValue.Of(value));

        public IUserBuilderCanMakeAttributePrivate Custom(string name, float value) =>
            Custom(name, LdValue.Of(value));

        public IUserBuilderCanMakeAttributePrivate Custom(string name, double value) =>
            Custom(name, LdValue.Of(value));

        private IUserBuilderCanMakeAttributePrivate CanMakeAttributePrivate(string attrName) =>
            new UserBuilderCanMakeAttributePrivate(this, attrName);

        internal IUserBuilder AddPrivateAttribute(string attrName)
        {
            _builder.Private(AttributeRef.FromLiteral(attrName));
            return this;
        }
    }

    internal class UserBuilderCanMakeAttributePrivate : IUserBuilderCanMakeAttributePrivate
    {
        private readonly UserBuilder _builder;
        private readonly string _attrName;

        internal UserBuilderCanMakeAttributePrivate(UserBuilder builder, string attrName)
        {
            _builder = builder;
            _attrName = attrName;
        }

        public Context Build() =>
            _builder.Build();

        public IUserBuilder Key(string key) =>
            _builder.Key(key);

        public IUserBuilderCanMakeAttributePrivate Secondary(string secondary) =>
            _builder.Secondary(secondary);

        public IUserBuilderCanMakeAttributePrivate IPAddress(string ipAddress) =>
            _builder.IPAddress(ipAddress);

        public IUserBuilderCanMakeAttributePrivate Country(string country) =>
            _builder.Country(country);

        public IUserBuilderCanMakeAttributePrivate FirstName(string firstName) =>
            _builder.FirstName(firstName);

        public IUserBuilderCanMakeAttributePrivate LastName(string lastName) =>
            _builder.LastName(lastName);

        public IUserBuilderCanMakeAttributePrivate Name(string name) =>
            _builder.Name(name);

        public IUserBuilderCanMakeAttributePrivate Avatar(string avatar) =>
            _builder.Avatar(avatar);

        public IUserBuilderCanMakeAttributePrivate Email(string email) =>
            _builder.Email(email);

        public IUserBuilder Anonymous(bool anonymous) =>
            _builder.Anonymous(anonymous);

        public IUserBuilderCanMakeAttributePrivate Custom(string name, LdValue value) =>
            _builder.Custom(name, value);

        public IUserBuilderCanMakeAttributePrivate Custom(string name, bool value) =>
            _builder.Custom(name, value);

        public IUserBuilderCanMakeAttributePrivate Custom(string name, string value) =>
            _builder.Custom(name, value);
        
        public IUserBuilderCanMakeAttributePrivate Custom(string name, int value) =>
            _builder.Custom(name, value);

        public IUserBuilderCanMakeAttributePrivate Custom(string name, long value) =>
            _builder.Custom(name, value);

        public IUserBuilderCanMakeAttributePrivate Custom(string name, float value) =>
            _builder.Custom(name, value);

        public IUserBuilderCanMakeAttributePrivate Custom(string name, double value) =>
            _builder.Custom(name, value);

        public IUserBuilder AsPrivateAttribute() =>
            _builder.AddPrivateAttribute(_attrName);
    }
}
