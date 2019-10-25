using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Used internally to represent user data that is being serialized in an <see cref="Event"/>.
    /// </summary>
    internal struct EventUser
    {
        public string Key { get; internal set; }
        public string SecondaryKey { get; internal set; }
        public string IpAddress { get; internal set; }
        public string Country { get; internal set; }
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string Name { get; internal set; }
        public string Avatar { get; internal set; }
        public string Email { get; internal set; }
        public bool? Anonymous { get; internal set; }
        public Dictionary<string, JToken> Custom { get; internal set; }
        public List<string> PrivateAttrs { get; set; }

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

        internal EventUserBuilder(User user, IEventProcessorConfiguration config)
        {
            _user = user;
            _config = config;
            _result = new EventUser();
        }

        internal EventUser Build()
        {
            _result.Key = _user.Key;
            _result.SecondaryKey = _user.SecondaryKey;
            _result.Anonymous = _user.Anonymous;
            _result.IpAddress = CheckPrivateAttr("ip", _user.IPAddress);
            _result.Country = CheckPrivateAttr("country", _user.Country);
            _result.FirstName = CheckPrivateAttr("firstName", _user.FirstName);
            _result.LastName = CheckPrivateAttr("lastName", _user.LastName);
            _result.Name = CheckPrivateAttr("name", _user.Name);
            _result.Avatar = CheckPrivateAttr("avatar", _user.Avatar);
            _result.Email = CheckPrivateAttr("email", _user.Email);
            if (_user.Custom != null && _user.Custom.Count > 0)
            {
                Dictionary<string, JToken> filteredCustom = null;
                foreach (KeyValuePair<string, JToken> kv in _user.Custom)
                {
                    JToken value = CheckPrivateAttr(kv.Key, kv.Value);
                    if (value is null && kv.Value != null)
                    {
                        if (filteredCustom is null)
                        {
                            filteredCustom = new Dictionary<string, JToken>(_user.Custom);
                        }
                        filteredCustom.Remove(kv.Key);
                    }
                }
                if (filteredCustom != null)
                {
                    _result.Custom = filteredCustom.Count == 0 ? null : filteredCustom;
                }
                else
                {
                    _result.Custom = _user.Custom;
                }
            }
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
                if (_result.PrivateAttrs is null)
                {
                    _result.PrivateAttrs = new List<string>();
                }
                _result.PrivateAttrs.Add(name);
                return null;
            }
            else
            {
                return value;
            }
        }
    }
}
