using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents an RFC 9651 <c>inner-list</c>: an array of zero or more <see cref="SfItem"/> together with its <see cref="SfParameters"/>.
    /// </summary>
    /// <remarks>See <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-3.1.1">RFC 9651 section 3.1.1</see>.</remarks>
    public sealed class SfInnerList : SfMember, IReadOnlyList<SfItem>
    {
        /// <summary>
        /// The underlying items of the inner list.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<SfItem> _items;

        /// <summary>
        /// Initializes a new instance of <see cref="SfInnerList"/> class from the specified items and parameters.
        /// </summary>
        /// <param name="items">The list's items.</param>
        /// <param name="parameters">The inner list's parameters, or <see langword="null"/> for none.</param>
        public SfInnerList(IEnumerable<SfItem> items, SfParameters? parameters = null) : base(parameters)
        {
            ArgumentNullException.ThrowIfNull(items);
            _items = [.. items];
        }

        #region Interface: IReadOnlyList
        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="index"/> is less than 0.
        /// -or- index is equal to or greater than <see cref="Count"/>.
        /// </exception>
        public SfItem this[int index] => _items[index];
        #endregion

        #region Interface: IReadOnlyCollection
        /// <inheritdoc/>
        public int Count => _items.Count;
        #endregion

        #region Interface: IEnumerable
        /// <inheritdoc/>
        public IEnumerator<SfItem> GetEnumerator() => _items.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}