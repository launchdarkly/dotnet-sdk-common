﻿using System.Collections.Immutable;

namespace LaunchDarkly.Sdk
{
    /// <summary>
    /// A mutable object that uses the builder pattern to specify properties for a <see cref="Context"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this type if you need to construct a Context that has multiple Kind values, each with its
    /// own nested Context. To define a single-kind context, use <see cref="Context.Builder(string)"/>.
    /// </para>
    /// <para>
    /// Obtain an instance of ContextMultiBuilder by calling <see cref="Context.MultiBuilder()"/>; then,
    /// call <see cref="Add(Context)"/> to specify the nested Context for each kind. Add returns a
    /// reference to the same builder, so calls can be chained:
    /// </para>
    /// <code>
    ///     var context = Context.MultiBuilder().
    ///         Add(Context.New("my-user-key")).
    ///         Add(Context.Builder("my-org-key").Kind("organization").Build()).
    ///         Build();
    /// </code>
    /// <para>
    /// A ContextMultiBuilder should not be accessed by multiple threads at once. Once you have called
    /// <see cref="Build"/>, the resulting Context is immutable and is safe to use from multiple threads.
    /// Instances created with <see cref="Build"/> are not affected by subsequent actions taken on the builder.
    /// </para>
    /// </remarks>
    /// <seealso cref="Context.NewMulti(Context[])"/>
    public sealed class ContextMultiBuilder
    {
        private readonly ImmutableList<Context>.Builder _contexts = ImmutableList.CreateBuilder<Context>();

        /// <summary>
        /// Creates a <see cref="Context"/> from the current Builder properties.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Context is immutable and will not be affected by any subsequent actions on the ContextBuilder.
        /// </para>
        /// <para>
        /// It is possible for a ContextMultiBuilder to represent an invalid state. Instead of throwing an
        /// exception, the ContextMultiBuilder always returns a Context and you can check <see cref="Context.Error"/>
        /// to see if it has an error. See <see cref="Context.Error"/> for more information about invalid Context
        /// conditions. If you pass an invalid Context to an SDK method, the SDK will detect this and will generally
        /// log a description of the error.
        /// </para>
        /// <para>
        /// If only one context kind was added to the builder, Build returns a single-kind Context rather
        /// than a multi-kind Context.
        /// </para>
        /// </remarks>
        /// <returns>a new <see cref="Context"/></returns>
        public Context Build()
        {
            var list = _contexts.ToImmutableList();
            if (list.IsEmpty)
            {
                return new Context(Context.ErrKindMultiWithNoKinds);
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            return new Context(list);
        }

        /// <summary>
        /// Adds a nested Context for a specific kind to a MultiBuilder.
        /// </summary>
        /// <remarks>
        /// It is invalid to add more than one Context with the same kind, or to add a nested multi-kind Context.
        /// This error is detected when you call <see cref="Build"/>/
        /// </remarks>
        /// <param name="context">the context to add</param>
        /// <returns>the builder</returns>
        public ContextMultiBuilder Add(Context context)
        {
            _contexts.Add(context);
            return this;
        }
    }
}
