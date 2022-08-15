namespace LaunchDarkly.Sdk
{
    /// <summary>
    /// Contains legacy methods for constructing simple evaluation contexts, using the older LaunchDarkly
    /// SDK model for user properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SDK now uses the type <see cref="Context"/> to represent an evaluation context that might
    /// represent a user, or some other kind of entity, or multiple kinds. But in older SDK versions,
    /// this was limited to one kind and was represented by the type lduser.User. This differed from
    /// Context in several ways:
    /// </para>
    /// <list type="bullet">
    /// <item><description>There was always a single implicit context kind of "user".</description></item>
    /// <item><description>Unlike Context where only a few attributes such as <see cref="Context.Key"/>
    /// and <see cref="Context.Name"/> have special behavior, the user model defined many other built-in
    /// attributes such as <c>email</c> which, like <c>name</c>, were constrained to only allow string
    /// values. These had specific setter methods in <see cref="UserBuilder"/>.</description></item>
    /// </list>
    /// <para>
    /// The User class now exists only as a container for static methods; the SDK now operates only on
    /// Contexts. <see cref="User.WithKey(string)"/> has been changed to return a Context.
    /// <see cref="User.Builder(string)"/> and the <see cref="UserBuilder"/> type have been retained,
    /// and modified to be a wrapper for <see cref="ContextBuilder"/>. This allows code that used the
    /// older model to still work with minor adjustments.
    /// </para>
    /// <para>
    /// For any code that still uses these methods, the significant differences from older SDK versions are:
    /// </para>
    /// <list type="bullet">
    /// <item><description>They now return a <see cref="Context"/>, so you will need to update any part of
    /// your code that referred to the User type by name.</description></item>
    /// <item><description>The SDK no longer supports setting the key to an empty string. If you do this,
    /// the returned Context will be invalid (as indicated by a non-nil <see cref="Context.Error"/>) and
    /// the SDK will refuse to use it for evaluations or events.</description></item>
    /// <item><description>Previously, the Anonymous property had three states: true, false, or
    /// undefined/null. Undefined/null and false were functionally the same in terms of the LaunchDarkly
    /// dashboard/indexing behavior, but they were represented differently in JSON and could behave
    /// differently if referenced in a flag rule (an undefined/null value would not match "anonymous is
    /// false"). Now, the property is a simple boolean defaulting to false, and the undefined state is
    /// the same as false.</description></item>
    /// </list>
    /// </remarks>
    public static class User
    {
        /// <summary>
        /// Creates an <see cref="IUserBuilder"/> for constructing a <see cref="Context"/> object using a
        /// fluent syntax. The resulting Context will have a <see cref="Context.Kind"/> of "user". 
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var user = User.Builder("my-key").Name("Bob").Email("test@example.com").Build();
        /// </code>
        /// </example>
        /// <param name="key">a <see langword="string"/> that uniquely identifies a user</param>
        /// <returns>a builder object</returns>
        public static IUserBuilder Builder(string key) =>
            new UserBuilder(key);

        /// <summary>
        /// Creates an <see cref="IUserBuilder"/> for constructing a <see cref="Context"/>, with its initial
        /// properties copied from an existing Context.
        /// </summary>
        /// <remarks>
        /// This is the same as calling <c>User.Builder(fromContext.Key)</c> and then calling the
        /// <see cref="IUserBuilder"/> methods to set each of the individual properties from their current
        /// values in <c>fromUser</c>. Modifying the builder does not affect the original <see cref="Context"/>.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var user1 = User.Builder("my-key").FirstName("Joe").LastName("Schmoe").Build();
        ///     var user2 = User.Builder(user1).FirstName("Jane").Build();
        ///     // this is equvalent to: user2 = User.Builder("my-key").FirstName("Jane").LastName("Schmoe").Build();
        /// </code>
        /// </example>
        /// <param name="fromContext">the context to copy</param>
        /// <returns>a builder object</returns>
        public static IUserBuilder Builder(Context fromContext) =>
            new UserBuilder(fromContext);

        /// <summary>
        /// Creates a simple <see cref="Context"/> whose <see cref="Context.Kind"/> is "user", with
        /// the given key and no other attributes.
        /// </summary>
        /// <param name="key">a <see langword="string"/> that uniquely identifies a context</param>
        /// <returns>a <see cref="Context"/> instance</returns>
        public static Context WithKey(string key) =>
            Context.New(key);
    }
}
