using System.Collections.Immutable;
using LaunchDarkly.Sdk.Interfaces;

namespace LaunchDarkly.Sdk.Internal.Events
{
    /// <summary>
    /// Used internally to represent user data that is being serialized in an <see cref="Event"/>.
    /// </summary>
    internal struct EventUser
    {
        public string Key { get; internal set; }
        public string Secondary { get; internal set; }
        public string IPAddress { get; internal set; }
        public string Country { get; internal set; }
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string Name { get; internal set; }
        public string Avatar { get; internal set; }
        public string Email { get; internal set; }
        public bool? Anonymous { get; internal set; }
        public IImmutableDictionary<string, LdValue> Custom { get; internal set; }
        public ImmutableSortedSet<string> PrivateAttrs { get; set; }

        internal static EventUser FromUser(User user, IEventProcessorConfiguration config)
        {
            EventUserBuilder eub = new EventUserBuilder(user, config);
            return eub.Build();
        }
    }

    internal struct EventUserBuilder
    {
        private IEventProcessorConfiguration _config;
        private User _user;
        private EventUser _result;
        private ImmutableSortedSet<string>.Builder _privateAttrs;

        internal EventUserBuilder(User user, IEventProcessorConfiguration config)
        {
            _user = user;
            _config = config;
            _result = new EventUser();
            _privateAttrs = null;
        }

        internal EventUser Build()
        {
            _result.Key = _user.Key;
            _result.Secondary = StringAttrIfNotPrivate("secondary", _user.Secondary);
            _result.Anonymous = _user.Anonymous ? (bool?)true : null;
            _result.IPAddress = StringAttrIfNotPrivate("ip", _user.IPAddress);
            _result.Country = StringAttrIfNotPrivate("country", _user.Country);
            _result.FirstName = StringAttrIfNotPrivate("firstName", _user.FirstName);
            _result.LastName = StringAttrIfNotPrivate("lastName", _user.LastName);
            _result.Name = StringAttrIfNotPrivate("name", _user.Name);
            _result.Avatar = StringAttrIfNotPrivate("avatar", _user.Avatar);
            _result.Email = StringAttrIfNotPrivate("email", _user.Email);

            // With the custom attributes, for efficiency's sake we would like to reuse the same ImmutableDictionary
            // whenever possible. So, we'll lazily create a new collection only if it turns out that there are any
            // changes needed (i.e. if one of the custom attributes turns out to be private).
            ImmutableDictionary<string, LdValue>.Builder customAttrsBuilder = null;
            foreach (var kv in _user.Custom)
            {
                if (!CheckPrivateAttr(kv.Key, kv.Value))
                {
                    if (customAttrsBuilder is null)
                    {
                        // This is the first private custom attribute we've found. Lazily create the builder
                        // by first copying all of the ones we've already iterated over. We can rely on the
                        // iteration order being the same because it's immutable.
                        customAttrsBuilder = ImmutableDictionary.CreateBuilder<string, LdValue>();
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
            var custom = customAttrsBuilder is null ? _user.Custom : customAttrsBuilder.ToImmutable();
            _result.Custom = custom.Count == 0 ? null : custom;
            _result.PrivateAttrs = _privateAttrs is null ? null : _privateAttrs.ToImmutable();
            return _result;
        }
        
        private bool CheckPrivateAttr<T>(string name, T value)
        {
            if (_config.AllAttributesPrivate ||
                     (_config.PrivateAttributeNames != null &&_config.PrivateAttributeNames.Contains(name)) ||
                     (_user.PrivateAttributeNames != null && _user.PrivateAttributeNames.Contains(name)))
            {
                if (_privateAttrs is null)
                {
                    _privateAttrs = ImmutableSortedSet.CreateBuilder<string>();
                }
                _privateAttrs.Add(name);
                return false;
            }
            else
            {
                return true;
            }
        }

        private string StringAttrIfNotPrivate(string name, string value)
        {
            return (value is null) ? null : (CheckPrivateAttr(name, value) ? value : null);
        }
    }
}
