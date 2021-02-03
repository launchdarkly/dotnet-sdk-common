// This file is not a real class but is used by Sandcastle Help File Builder to provide documentation
// for the namespace. The other way to document a namespace is to add this XML to the .shfbproj file:
//
// <NamespaceSummaries>
//   <NamespaceSummaryItem name="LaunchDarkly.Sdk.Json" isDocumented="True" xmlns="">
//     ...summary here...
//   </NamespaceSummaryItem
// </NamespaceSummaries>
//
// However, currently Sandcastle does not correctly resolve links if you use that method.

namespace LaunchDarkly.Sdk.Json
{
    /// <summary>
    /// Tools for converting LaunchDarkly SDK types to and from JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Any LaunchDarkly SDK type that has the marker interface <see cref="IJsonSerializable"/>
    /// has a canonical JSON encoding that is consistent across all LaunchDarkly SDKs. There are
    /// three ways to convert any such type to or from JSON:
    /// </para>
    /// <list type="bullet">
    /// <item> On platforms that support the <c>System.Text.Json</c> API, these types already
    /// have the necessary attributes to behave correctly with that API. </item>
    /// <item> You may use the <see cref="LdJsonSerialization.SerializeObject{T}(T)"/> and
    /// <see cref="LdJsonSerialization.DeserializeObject{T}(string)"/> methods in
    /// <see cref="LdJsonSerialization"/> to convert to or from a JSON-encoded string. </item>
    /// <item> You may use the lower-level <c>LaunchDarkly.JsonStream</c> API
    /// (https://github.com/launchdarkly/dotnet-jsonstream) in conjunction with the converters
    /// in <see cref="LdJsonConverters"/>.
    /// </item>
    /// </list>
    /// <para>
    /// Earlier versions of the LaunchDarkly SDKs used <c>Newtonsoft.Json</c> for JSON
    /// serialization, but current versions have no such third-party dependency. Therefore,
    /// these types will not work correctly with the reflection-based <c>JsonConvert</c> methods
    /// in <c>Newtonsoft.Json</c>; instead, you should convert them separately as described above.
    /// For instance, if you are using <c>Newtonsoft.Json</c> to serialize some values that will
    /// be passed to JavaScript code, and those values include a <see cref="User"/>, instead of
    /// doing this--
    /// </para>
    /// <code>
    ///     // won't work:
    ///     public class MySerializableValues
    ///     {
    ///         public User UserData { get; set; }
    ///     }
    ///
    ///     var values = new MySerializableValues();
    ///     values.User = User.WithKey("some-user-key");
    ///     var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(values);
    /// </code>
    /// <para>
    /// --do this:
    /// </para>
    /// <code>
    ///     // will work:
    ///     public class MySerializableValues
    ///     {
    ///         public Newtonsoft.Json.Linq.JRaw UserData { get; set; }
    ///     }
    ///
    ///     var values = new MySerializableValues();
    ///     values.User = new Newtonsoft.Json.Linq.JRaw(
    ///         LdJsonSerialization.SerializeObject(User.WithKey("some-user-key")));
    ///     var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(values);
    /// </code>
    /// </remarks>
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
