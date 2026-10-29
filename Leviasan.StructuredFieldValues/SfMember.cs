namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents a member of an RFC 9651 top-level <see cref="SfList"/> or a value in a <see cref="SfDictionary"/>: either an <see cref="SfItem"/> or an <see cref="SfInnerList"/>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="SfMember"/> class with the specified parameters.
    /// </remarks>
    /// <param name="parameters">The member's parameters, or <see langword="null"/> for none.</param>
    public abstract class SfMember(SfParameters? parameters)
    {
        /// <summary>
        /// Gets the parameters associated with this member.
        /// </summary>
        public SfParameters Parameters { get; } = parameters ?? new SfParameters([]);
    }
}