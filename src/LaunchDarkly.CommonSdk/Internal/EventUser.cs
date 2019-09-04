using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Used internally to represent user data that is being serialized in an <see cref="Event"/>.
    /// </summary>
    internal sealed class EventUser
    {
        /// <see cref="User.Key"/>
        [JsonProperty(PropertyName = "key", NullValueHandling = NullValueHandling.Ignore)]
        public string Key { get; internal set; }

        /// <see cref="User.SecondaryKey"/>
        [JsonProperty(PropertyName = "secondary", NullValueHandling = NullValueHandling.Ignore)]
        public string SecondaryKey { get; internal set; }

        /// <see cref="User.IPAddress"/>
        [JsonProperty(PropertyName = "ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IPAddress { get; internal set; }

        /// <see cref="User.Country"/>
        [JsonProperty(PropertyName = "country", NullValueHandling = NullValueHandling.Ignore)]
        public string Country { get; internal set; }

        /// <see cref="User.FirstName"/>
        [JsonProperty(PropertyName = "firstName", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName { get; internal set; }

        /// <see cref="User.LastName"/>
        [JsonProperty(PropertyName = "lastName", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName { get; internal set; }

        /// <see cref="User.Name"/>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; internal set; }

        /// <see cref="User.Avatar"/>
        [JsonProperty(PropertyName = "avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string Avatar { get; internal set; }

        /// <see cref="User.Email"/>
        [JsonProperty(PropertyName = "email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; internal set; }

        /// <see cref="User.Anonymous"/>
        [JsonProperty(PropertyName = "anonymous", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Anonymous { get; internal set; }

        /// <see cref="User.Custom"/>
        [JsonProperty(PropertyName = "custom", NullValueHandling = NullValueHandling.Ignore)]
        public IImmutableDictionary<string, ImmutableJsonValue> Custom { get; internal set; }

        /// <summary>
        /// A list of attribute names that have been omitted from the event.
        /// </summary>
        // Note that this is a sorted set - LaunchDarkly doesn't care about the ordering, but
        // having a defined order makes our test logic much simpler.
        [JsonProperty(PropertyName = "privateAttrs", NullValueHandling = NullValueHandling.Ignore)]
        public ImmutableSortedSet<string> PrivateAttrs { get; set; }

        internal static EventUser FromUser(User user, IEventProcessorConfiguration config)
        {
            EventUserBuilder eub = new EventUserBuilder(user, config);
            return eub.Build();
        }
    }

    internal sealed class EventUserBuilder
    {
        private IEventProcessorConfiguration _config;
        private User _user;
        private EventUser _result = new EventUser();
        private ImmutableSortedSet<string>.Builder _privateAttrs = null;

        internal EventUserBuilder(User user, IEventProcessorConfiguration config)
        {
            _user = user;
            _config = config;
        }

        internal EventUser Build()
        {
            _result.Key = _user.Key;
            _result.SecondaryKey = _user.SecondaryKey;
            _result.Anonymous = _user.Anonymous ? (bool?)true : null;
            _result.IPAddress = CheckPrivateAttr("ip", _user.IPAddress);
            _result.Country = CheckPrivateAttr("country", _user.Country);
            _result.FirstName = CheckPrivateAttr("firstName", _user.FirstName);
            _result.LastName = CheckPrivateAttr("lastName", _user.LastName);
            _result.Name = CheckPrivateAttr("name", _user.Name);
            _result.Avatar = CheckPrivateAttr("avatar", _user.Avatar);
            _result.Email = CheckPrivateAttr("email", _user.Email);

            // With the custom attributes, for efficiency's sake we would like to reuse the same ImmutableDictionary
            // whenever possible. So, we'll lazily create a new collection only if it turns out that there are any
            // changes needed (i.e. if one of the custom attributes turns out to be private).
            ImmutableDictionary<string, ImmutableJsonValue>.Builder customAttrsBuilder = null;
            foreach (var kv in _user.Custom)
            {
                JToken value = CheckPrivateAttr(kv.Key, kv.Value.InnerValue);
                if (value is null)
                {
                    if (customAttrsBuilder is null)
                    {
                        // This is the first private custom attribute we've found. Lazily create the builder
                        // by first copying all of the ones we've already iterated over. We can rely on the
                        // iteration order being the same because it's immutable.
                        customAttrsBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableJsonValue>();
                        foreach (var kv1 in _user.Custom)
                        {
                            if (kv1.Key == kv.Key)
                            {
                                break;
                            }
                            customAttrsBuilder[kv1.Key] = kv1.Value;
                        }
                    }
                }
                else
                {
                    // It's not a private attribute.
                    if (customAttrsBuilder != null)
                    {
                        customAttrsBuilder[kv.Key] = kv.Value;
                    }
                }
            }
            var custom = customAttrsBuilder is null ? _user.Custom : customAttrsBuilder.ToImmutableDictionary();
            _result.Custom = custom.Count == 0 ? null : custom;
            _result.PrivateAttrs = _privateAttrs is null ? null : _privateAttrs.ToImmutableSortedSet();

            return _result;
        }
        
        private T CheckPrivateAttr<T>(string name, T value) where T: class
        {
            if (value is null)
            {
                return null;
            }
            else if (_config.AllAttributesPrivate ||
                     (_config.PrivateAttributeNames != null &&_config.PrivateAttributeNames.Contains(name)) ||
                     (_user.PrivateAttributeNames != null && _user.PrivateAttributeNames.Contains(name)))
            {
                if (_privateAttrs is null)
                {
                    _privateAttrs = ImmutableSortedSet.CreateBuilder<string>();
                }
                _privateAttrs.Add(name);
                return null;
            }
            else
            {
                return value;
            }
        }
    }
}
