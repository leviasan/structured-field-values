using System.Collections.Generic;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents an RFC 9651 top-level <c>sf-dictionary</c>: an ordered map from unique keys to either an <see cref="SfItem"/> or an <see cref="SfInnerList"/>.
    /// </summary>
    /// <remarks>See <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-3.2">RFC 9651 section 3.2</see>.</remarks>
    /// <param name="members">The ordered key/value members.</param>
    public sealed class SfDictionary(IEnumerable<KeyValuePair<string, SfMember>> members) : SfOrderedMap<SfMember>(members)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SfDictionary"/> class with an empty dictionary.
        /// </summary>
        public SfDictionary() : this([]) { }
    }
}