using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents an ordered map from unique RFC 9651 keys to values.
    /// </summary>
    /// <typeparam name="TValue">The type of values.</typeparam>
    public abstract class SfOrderedMap<TValue> : IReadOnlyList<KeyValuePair<string, TValue>>, IReadOnlyDictionary<string, TValue>
    {
        /// <summary>
        /// The underlying ordered members.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OrderedDictionary<string, TValue> _items = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of <see cref="SfOrderedMap{TValue}"/> from the specified members.
        /// If a key appears more than once, the later occurrence's value wins and its position is kept,
        /// per <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.3.2">member overwrite semantics</see>.
        /// </summary>
        /// <param name="members">The ordered key/value members.</param>
        /// <exception cref="ArgumentException">A key is not a valid <see cref="StructuredFieldSyntax.IsValidKey(ReadOnlySpan{char})"/> value.</exception>
        protected SfOrderedMap(IEnumerable<KeyValuePair<string, TValue>> members)
        {
            ArgumentNullException.ThrowIfNull(members);
            foreach (KeyValuePair<string, TValue> member in members)
                SetItem(member.Key, member.Value);
        }

        /// <summary>
        /// Sets <paramref name="key"/> to <paramref name="value"/>,
        /// overwriting an existing member of the same key in place (keeping its position), or appending it if not already present.
        /// </summary>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is not a valid <see cref="StructuredFieldSyntax.IsValidKey(ReadOnlySpan{char})"/> value.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
        public void SetItem(string key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            if (!StructuredFieldSyntax.IsValidKey(key))
                throw new ArgumentException($"The '{key}' is not a valid RFC 9651 key", nameof(key));
            _items[key] = value;
        }

        #region Interface: IReadOnlyDictionary
        /// <inheritdoc/>
        public IEnumerable<string> Keys => _items.Keys;
        /// <inheritdoc/>
        public IEnumerable<TValue> Values => _items.Values;
        /// <inheritdoc/>
        public TValue this[string key] => _items[key];
        /// <inheritdoc/>
        public bool ContainsKey(string key) => _items.ContainsKey(key);
        /// <inheritdoc/>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _items.TryGetValue(key, out value);
        }
        #endregion

        #region Interface: IReadOnlyList
        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in source.</exception>
        public KeyValuePair<string, TValue> this[int index] => _items.GetAt(index);
        #endregion

        #region Interface: IReadOnlyCollection
        /// <inheritdoc/>
        public int Count => _items.Count;
        #endregion

        #region Interface: IEnumerable
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => _items.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}