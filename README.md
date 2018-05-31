LaunchDarkly SDK .NET Common Code
=================================
[![CircleCI](https://circleci.com/gh/launchdarkly/dotnet-client-common/tree/master.svg?style=svg)](https://circleci.com/gh/launchdarkly/dotnet-client-common/tree/master)

This project contains .NET classes and interfaces that are shared between the LaunchDarkly .NET and Xamarin SDKs. Code that is specific to one or the other is in [.net-client-private](https://github.com/launchdarkly/.net-client) or [xamarin-client-private](https://github.com/launchdarkly/xamarin-client).

Contributing
------------

See [Contributing](https://github.com/launchdarkly/.net-client/blob/master/CONTRIBUTING.md).

Signing
-------
The artifacts generated from this repo are signed by LaunchDarkly. The public key file is in this repo at `LaunchDarkly.Common.pk` as well as here:

```
Public Key:
0024000004800000940000000602000000240000525341310004000001000100
250509411af6d31f2abfc9b33d02b01c6ad14fd5c7f83cc6135f499ebb0ec8f3
4e05c59e49232f5a7d75d5761281610219d323043936d55c19bb26f1dd86bdc7
6ab178015e78b54aef9cbdc824db2afcf7250292ae3d8d9c4522bcc3a4fc4831
d4b4320e820f32e024ad50a786f86d37ea45e0c25ec431a7a0f3e93575a0d2ad

Public Key Token: 
```

About LaunchDarkly
-----------

* LaunchDarkly is a continuous delivery platform that provides feature flags as a service and allows developers to iterate quickly and safely. We allow you to easily flag your features and manage them from the LaunchDarkly dashboard.  With LaunchDarkly, you can:
    * Roll out a new feature to a subset of your users (like a group of users who opt-in to a beta tester group), gathering feedback and bug reports from real-world use cases.
    * Gradually roll out a feature to an increasing percentage of users, and track the effect that the feature has on key metrics (for instance, how likely is a user to complete a purchase if they have feature A versus feature B?).
    * Turn off a feature that you realize is causing performance problems in production, without needing to re-deploy, or even restart the application with a changed configuration file.
    * Grant access to certain features based on user attributes, like payment plan (eg: users on the ‘gold’ plan get access to more features than users in the ‘silver’ plan). Disable parts of your application to facilitate maintenance, without taking everything offline.
* LaunchDarkly provides feature flag SDKs for
    * [Java](http://docs.launchdarkly.com/docs/java-sdk-reference "Java SDK")
    * [JavaScript](http://docs.launchdarkly.com/docs/js-sdk-reference "LaunchDarkly JavaScript SDK")
    * [PHP](http://docs.launchdarkly.com/docs/php-sdk-reference "LaunchDarkly PHP SDK")
    * [Python](http://docs.launchdarkly.com/docs/python-sdk-reference "LaunchDarkly Python SDK")
    * [Python Twisted](http://docs.launchdarkly.com/docs/python-twisted-sdk-reference "LaunchDarkly Python Twisted SDK")
    * [Go](http://docs.launchdarkly.com/docs/go-sdk-reference "LaunchDarkly Go SDK")
    * [Node.JS](http://docs.launchdarkly.com/docs/node-sdk-reference "LaunchDarkly Node SDK")
    * [.NET](http://docs.launchdarkly.com/docs/dotnet-sdk-reference "LaunchDarkly .Net SDK")
    * [Ruby](http://docs.launchdarkly.com/docs/ruby-sdk-reference "LaunchDarkly Ruby SDK")
    * [iOS](http://docs.launchdarkly.com/docs/ios-sdk-reference "LaunchDarkly iOS SDK")
    * [Android](http://docs.launchdarkly.com/docs/android-sdk-reference "LaunchDarkly Android SDK")
* Explore LaunchDarkly
    * [launchdarkly.com](http://www.launchdarkly.com/ "LaunchDarkly Main Website") for more information
    * [docs.launchdarkly.com](http://docs.launchdarkly.com/  "LaunchDarkly Documentation") for our documentation and SDKs
    * [apidocs.launchdarkly.com](http://apidocs.launchdarkly.com/  "LaunchDarkly API Documentation") for our API documentation
    * [blog.launchdarkly.com](http://blog.launchdarkly.com/  "LaunchDarkly Blog Documentation") for the latest product updates
    * [Feature Flagging Guide](https://github.com/launchdarkly/featureflags/  "Feature Flagging Guide") for best practices and strategies
