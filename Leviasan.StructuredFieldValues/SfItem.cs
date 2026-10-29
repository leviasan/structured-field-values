namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents an RFC 9651 top-level <c>sf-item</c>: a <see cref="SfBareItem"/> value together with its <see cref="SfParameters"/>.
    /// </summary>
    /// <remarks>See <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-3.3">RFC 9651 section 3.3</see>.</remarks>
    /// <remarks>
    /// Initializes a new instance of <see cref="SfItem"/> class with the specified value and parameters.
    /// </remarks>
    /// <param name="value">The bare item value.</param>
    /// <param name="parameters">The item's parameters, or <see langword="null"/> for none.</param>
    public sealed class SfItem(SfBareItem value, SfParameters? parameters = null) : SfMember(parameters)
    {
        /// <summary>
        /// Gets the bare item value.
        /// </summary>
        public SfBareItem Value { get; } = value;
    }
}