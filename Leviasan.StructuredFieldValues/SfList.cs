using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents an RFC 9651 top-level <c>sf-list</c>: an array of zero or more members, each of which is either an <see cref="SfItem"/> or an <see cref="SfInnerList"/>.
    /// </summary>
    /// <remarks>See <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-3.1">RFC 9651 section 3.1</see>.</remarks>
    public sealed class SfList : IReadOnlyList<SfMember>
    {
        /// <summary>
        /// Gets an empty list.
        /// </summary>
        public static SfList Empty { get; } = new([]);

        /// <summary>
        /// The underlying members of the list.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<SfMember> _members;

        /// <summary>
        /// Initializes a new instance of <see cref="SfList"/> class from the specified members.
        /// </summary>
        /// <param name="members">The list's members.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="members"/> parameter is <see langword="null"/>.</exception>
        public SfList(IEnumerable<SfMember> members)
        {
            ArgumentNullException.ThrowIfNull(members);
            _members = [.. members];
        }

        #region Interface: IReadOnlyList
        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> parameter is less than 0 or greater than or equal to <see cref="Count"/>.</exception>
        public SfMember this[int index] => _members[index];
        #endregion

        #region Interface: IReadOnlyCollection
        /// <inheritdoc/>
        public int Count => _members.Count;
        #endregion

        #region Interface: IEnumerable
        /// <inheritdoc/>
        public IEnumerator<SfMember> GetEnumerator() => _members.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}