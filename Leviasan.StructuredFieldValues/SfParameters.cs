using System.Collections.Generic;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents an ordered, unique-keyed set of RFC 9651 <c>parameters</c> attached to an <see cref="SfItem"/> or <see cref="SfInnerList"/>.
    /// </summary>
    /// <remarks>See <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-3.1.2">RFC 9651 section 3.1.2</see>.</remarks>
    /// <param name="members">The key/value members.</param>
    public sealed class SfParameters(IEnumerable<KeyValuePair<string, SfBareItem>> members) : SfOrderedMap<SfBareItem>(members) { }
}