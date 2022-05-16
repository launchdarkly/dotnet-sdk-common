﻿using System;
using System.Collections.Immutable;
using LaunchDarkly.Sdk.Json;
using LaunchDarkly.TestHelpers;
using Xunit;

namespace LaunchDarkly.Sdk
{
    public class ContextTest
    {
        [Fact]
        public void InvalidContexts()
        {
            var c = new Context();
            Assert.False(c.Defined);
			Assert.False(c.Valid);
			Assert.Equal(Errors.ContextUninitialized, c.Error);

			ShouldBeInvalid(Context.New(null), Errors.ContextNoKey);
			ShouldBeInvalid(Context.New(""), Errors.ContextNoKey);
			ShouldBeInvalid(Context.NewWithKind("kind", "key"), Errors.ContextKindCannotBeKind);
			ShouldBeInvalid(Context.NewWithKind("ørg", "key"), Errors.ContextKindInvalidChars);
			ShouldBeInvalid(Context.NewWithKind("multi", "key"), Errors.ContextKindMultiForSingle);
			ShouldBeInvalid(Context.NewMulti(), Errors.ContextKindMultiWithNoKinds);
			ShouldBeInvalid(Context.NewMulti(
				Context.New("key1"),
				Context.NewMulti(Context.NewWithKind("kind2", "key2"), Context.NewWithKind("kind3", "key3"))
				), Errors.ContextKindMultiWithinMulti);
			ShouldBeInvalid(Context.NewMulti(Context.NewWithKind("kind1", "key1"), Context.NewWithKind("kind1", "key2")),
				Errors.ContextKindMultiDuplicates);
		}

		private static void ShouldBeInvalid(Context c, string error)
		{
			Assert.True(c.Defined);
			Assert.False(c.Valid);
			Assert.Equal(error, c.Error);

			// we guarantee that Kind and Key are never null even for invalid contexts
			Assert.Equal("", c.Kind);
			Assert.Equal("", c.Key);
		}

        [Fact]
        public void Multiple()
        {
            var sc = Context.New("my-key");
			Assert.False(sc.Multiple);

			var mc = Context.NewMulti(Context.New("my-key"), Context.NewWithKind("org", "my-key"));
            Assert.True(mc.Multiple);
			Assert.True(mc.Defined);
			Assert.True(mc.Valid);
		}

        [Fact]
        public void OptionalAttributeNames()
        {
			Assert.Equal(ImmutableHashSet.Create<string>(),
				Context.New("my-key").OptionalAttributeNames.ToImmutableHashSet());

			Assert.Equal(ImmutableHashSet.Create("name"),
				Context.Builder("my-key").Name("x").Build().
					OptionalAttributeNames.ToImmutableList());

			Assert.Equal(ImmutableHashSet.Create("email", "happy"),
				Context.Builder("my-key").Set("email", "x").Set("happy", true).Build().
					OptionalAttributeNames.ToImmutableHashSet());

			// meta-attributes and required attributes are not included
			Assert.Equal(ImmutableHashSet.Create<string>(),
				Context.Builder("my-key").Secondary("x").Transient(true).Build().
					OptionalAttributeNames.ToImmutableHashSet());

			// none for multi-kind context
			Assert.Equal(ImmutableHashSet.Create<string>(),
				Context.NewMulti(
					Context.NewWithKind("kind1", "key1"),
					Context.Builder("key2").Name("x").Build()
					).
					OptionalAttributeNames.ToImmutableHashSet());
		}

		[Fact]
		public void PrivateAttributes()
        {
			Assert.Equal(ImmutableList.Create<AttributeRef>(),
				Context.New("my-key").PrivateAttributes);

			Assert.Equal(ImmutableList.Create(AttributeRef.FromLiteral("a"), AttributeRef.FromLiteral("b")),
				Context.Builder("my-key").Private("a", "b").Build().PrivateAttributes);

			Assert.Equal(ImmutableList.Create(AttributeRef.FromPath("/a"), AttributeRef.FromPath("/a/b")),
				Context.Builder("my-key").Private(AttributeRef.FromPath("/a"), AttributeRef.FromPath("/a/b")).
					Build().PrivateAttributes);
		}

		[Fact]
		public void GetValue()
		{
			// equivalent to GetValue(AttributeRef) for simple attribute name
			var c = Context.Builder("my-key").Kind("org").Name("x").Set("my-attr", "y").Set("/starts-with-slash", "z").Build();

			ExpectAttributeFoundForName(LdValue.Of("org"), c, "kind");
			ExpectAttributeFoundForName(LdValue.Of("my-key"), c, "key");
			ExpectAttributeFoundForName(LdValue.Of("x"), c, "name");
			ExpectAttributeFoundForName(LdValue.Of("y"), c, "my-attr");
			ExpectAttributeFoundForName(LdValue.Of("z"), c, "/starts-with-slash");

			ExpectAttributeNotFoundForName(c, "/kind");
			ExpectAttributeNotFoundForName(c, "/key");
			ExpectAttributeNotFoundForName(c, "/name");
			ExpectAttributeNotFoundForName(c, "/my-attr");
			ExpectAttributeNotFoundForName(c, "other");
			ExpectAttributeNotFoundForName(c, "");
			ExpectAttributeNotFoundForName(c, "/");

			var mc = Context.NewMulti(c, Context.NewWithKind("otherkind", "otherkey"));

			ExpectAttributeFoundForName(LdValue.Of("multi"), mc, "kind");

			ExpectAttributeNotFoundForName(mc, "/kind");
			ExpectAttributeNotFoundForName(mc, "key");

			// does not allow querying of subpath/element
			var objValue = LdValue.BuildObject().Set("a", 1).Build();
			var arrayValue = LdValue.ArrayOf(LdValue.Of(1));
			var c1 = Context.Builder("key").Set("obj-attr", objValue).Set("array-attr", arrayValue).Build();
			ExpectAttributeFoundForName(objValue, c1, "obj-attr");
			ExpectAttributeFoundForName(arrayValue, c1, "array-attr");
			ExpectAttributeNotFoundForName(c1, "/obj-attr/a");
			ExpectAttributeNotFoundForName(c1, "/array-attr/0");
		}

		private static void ExpectAttributeFoundForName(LdValue expectedValue, Context c, string name)
        {
			var value = c.GetValue(name);
			Assert.False(value.IsNull, string.Format(@"attribute ""{0}"" should have been found, but was not", name));
			Assert.Equal(expectedValue, value);
		}

		private static void ExpectAttributeNotFoundForName(Context c, string name)
		{
			var value = c.GetValue(name);
			Assert.True(value.IsNull, string.Format(@"attribute ""{0}"" should not have been found, but was", name));
		}

		[Fact]
		public void GetValueForRefSpecialTopLevelAttributes()
        {
			var multi = Context.NewMulti(Context.New("my-key"), Context.NewWithKind("otherkind", "otherkey"));

			ExpectAttributeFoundForRef(LdValue.Of("org"), Context.NewWithKind("org", "my-key"), "kind");
			ExpectAttributeFoundForRef(LdValue.Of("multi"), multi, "kind");

			ExpectAttributeFoundForRef(LdValue.Of("my-key"), Context.New("my-key"), "key");
			ExpectAttributeNotFoundForRef(multi, "key");

			ExpectAttributeFoundForRef(LdValue.Of("my-name"), Context.Builder("key").Name("my-name").Build(), "name");
			ExpectAttributeNotFoundForRef(Context.New("key"), "name");
			ExpectAttributeNotFoundForRef(multi, "name");

			ExpectAttributeFoundForRef(LdValue.Of(false), Context.New("key"), "transient");
			ExpectAttributeFoundForRef(LdValue.Of(true), Context.Builder("key").Transient(true).Build(), "transient");
			ExpectAttributeNotFoundForRef(multi, "transient");
		}

		[Fact]
		public void GetValueForRefCannotGetMetaProperties()
        {
			ExpectAttributeNotFoundForRef(Context.Builder("key").Private("attr").Build(), "privateAttributes");

			ExpectAttributeNotFoundForRef(
				Context.Builder("key").Secondary("my-value").Build(),
				"secondary");
		}

		[Fact]
		public void GetValueForRefCustomAttributeSingleKind()
        {
			// simple attribute name
			ExpectAttributeFoundForRef(LdValue.Of("abc"),
				Context.Builder("key").Set("my-attr", "abc").Build(), "my-attr");

			// simple attribute name not found
			ExpectAttributeNotFoundForRef(Context.New("key"), "my-attr");
			ExpectAttributeNotFoundForRef(Context.Builder("key").Set("other-attr", "abc").Build(), "my-attr");

			// property in object
			ExpectAttributeFoundForRef(LdValue.Of("abc"),
				Context.Builder("key").Set("my-attr", LdValue.Parse(@"{""my-prop"":""abc""}")).Build(),
				"/my-attr/my-prop");

			// property in object not found
			ExpectAttributeNotFoundForRef(
				Context.Builder("key").Set("my-attr", LdValue.Parse(@"{""my-prop"":""abc""}")).Build(),
				"/my-attr/other-prop");

			// property in nested object
			ExpectAttributeFoundForRef(LdValue.Of("abc"),
				Context.Builder("key").Set("my-attr", LdValue.Parse(@"{""my-prop"":{""sub-prop"":""abc""}}")).Build(),
				"/my-attr/my-prop/sub-prop");

			// property in value that is not an object
			ExpectAttributeNotFoundForRef(
				Context.Builder("key").Set("my-attr", "xyz").Build(),
				"/my-attr/my-prop");

			// element in array
			ExpectAttributeFoundForRef(LdValue.Of("good"),
				Context.Builder("key").Set("my-attr", LdValue.Parse(@"[""bad"", ""good"", ""worse""]")).Build(),
				"/my-attr/1");

			// element in nested array in object
			ExpectAttributeFoundForRef(LdValue.Of("good"),
				Context.Builder("key").Set("my-attr", LdValue.Parse(@"{""my-prop"": [""bad"", ""good"", ""worse""]}")).Build(),
				"/my-attr/my-prop/1");

			// index too low in array
			ExpectAttributeNotFoundForRef(
				Context.Builder("key").Set("my-attr", LdValue.Parse(@"[""bad"", ""good"", ""worse""]")).Build(),
				"/my-attr/-1");

			// index too high in array
			ExpectAttributeNotFoundForRef(
				Context.Builder("key").Set("my-attr", LdValue.Parse(@"[""bad"", ""good"", ""worse""]")).Build(),
				"/my-attr/3");

			// index in value that is not an array
			ExpectAttributeNotFoundForRef(
				Context.Builder("key").Set("my-attr", "xyz").Build(),
				"/my-attr/0");
		}

		private static void ExpectAttributeFoundForRef(LdValue expectedValue, Context c, string attrRef)
		{
			var value = c.GetValue(AttributeRef.FromPath(attrRef));
			Assert.False(value.IsNull, string.Format(@"attribute ""{0}"" should have been found, but was not", attrRef));
			Assert.Equal(expectedValue, value);
		}

		private static void ExpectAttributeNotFoundForRef(Context c, string attrRef)
		{
			var value = c.GetValue(AttributeRef.FromPath(attrRef));
			Assert.True(value.IsNull, string.Format(@"attribute ""{0}"" should not have been found, but was", attrRef));
		}

		[Fact]
		public void GetValueForInvalidRef()
        {
			ExpectAttributeNotFoundForRef(Context.New("key"), "/");
        }

		[Fact]
		public void MultiKindContexts()
		{
			var c1 = Context.NewWithKind("kind1", "key1");
			var c2 = Context.NewWithKind("kind2", "key2");
			var multi = Context.NewMulti(c1, c2);

			Assert.Equal(ImmutableList.Create<Context>(), c1.MultiKindContexts);
			Assert.True(c1.TryGetContextByKind("kind1", out var c1a));
			Assert.Equal(c1a, c1);
			Assert.False(c1.TryGetContextByKind("kind2", out var _));

			Assert.Equal(ImmutableList.Create(c1, c2), multi.MultiKindContexts);
			Assert.True(multi.TryGetContextByKind("kind1", out var m1));
			Assert.Equal(c1, m1);
			Assert.True(multi.TryGetContextByKind("kind2", out var m2));
			Assert.Equal(c2, m2);
			Assert.False(multi.TryGetContextByKind("kind3", out var _));

			Assert.Equal(multi, Context.MultiBuilder().Add(c1).Add(c2).Build());
			Assert.Equal(c1, Context.NewMulti(c1));
			Assert.Equal(c1, Context.MultiBuilder().Add(c1).Build());

			var uc1 = Context.NewWithKind(Context.DefaultKind, "key1");
			var multi2 = Context.NewMulti(uc1, c2);
			Assert.True(multi2.TryGetContextByKind("", out var uc1a));
			Assert.Equal(uc1, uc1a);
			Assert.True(multi2.TryGetContextByKind(null, out var uc1b));
			Assert.Equal(uc1, uc1b);
		}

		[Fact]
		public void Equality()
		{
			TypeBehavior.CheckEqualsAndHashCode(MakeContextFactories());
		}

		[Fact]
		public void BuilderFromContext()
        {
			foreach (var cf in MakeContextFactories())
            {
				var c = cf();
				if (!c.Defined || c.Multiple)
                {
					continue;
                }
				var c1 = Context.BuilderFromContext(c).Build();
				Assert.Equal(c, c1);
            }
        }

		private static Func<Context>[] MakeContextFactories() =>
			new Func<Context>[]
			{
				() => new Context(),
				() => Context.New("a"),
				() => Context.New("b"),
				() => Context.NewWithKind("k1", "a"),
				() => Context.NewWithKind("k1", "b"),
				() => Context.Builder("a").Name("b").Build(),
				() => Context.Builder("a").Name("c").Build(),
				() => Context.Builder("a").Secondary("b").Build(),
				() => Context.Builder("a").Secondary("").Build(), // "" is not the same as undefined
				() => Context.Builder("a").Transient(true).Build(),
				() => Context.Builder("a").Set("b", true).Build(),
				() => Context.Builder("a").Set("b", false).Build(),
				() => Context.Builder("a").Set("b", 0).Build(),
				() => Context.Builder("a").Set("b", 1).Build(),
				() => Context.Builder("a").Set("b", "").Build(),
				() => Context.Builder("a").Set("b", "c").Build(),
				TypeBehavior.ValueFactoryFromInstances(
					Context.Builder("a").Set("b", true).Set("c", false).Build(),
					Context.Builder("a").Set("c", false).Set("b", true).Build()
					),
				() => Context.Builder("a").Name("b").Private("name").Build(),
				() => Context.Builder("a").Name("b").Set("c", true).Private("name").Build(),
				TypeBehavior.ValueFactoryFromInstances(
					Context.Builder("a").Name("b").Set("c", true).Private("name", "c").Build(),
					Context.Builder("a").Name("b").Set("c", true).Private("c", "name").Build()
					),
				() => Context.Builder("a").Name("b").Set("c", true).Private("name", "d").Build(),
				TypeBehavior.ValueFactoryFromInstances(
					Context.NewMulti(Context.NewWithKind("k1", "a"), Context.NewWithKind("k2", "b")),
					Context.NewMulti(Context.NewWithKind("k2", "b"), Context.NewWithKind("k1", "a"))
					),
				() => Context.NewMulti(Context.NewWithKind("k1", "a"), Context.NewWithKind("k2", "c")),
				() => Context.NewMulti(Context.NewWithKind("k1", "a"), Context.NewWithKind("k3", "b")),
				() => Context.NewMulti(Context.NewWithKind("k1", "a"), Context.NewWithKind("k2", "b"),
					Context.NewWithKind("k3", "c"))
			};
	}
}
