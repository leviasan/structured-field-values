using System;
using System.Text;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Serializes RFC 9651 Structured Field Values (Lists, Dictionaries, and Items) to their ASCII string representation,
    /// following <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1">RFC 9651 section 4.1</see>.
    /// </summary>
    /// <remarks>
    /// The serialization of individual bare items is implemented by the bare item types themselves, see <see cref="SfBareItem.ToString"/>.
    /// </remarks>
    public static class StructuredFieldSerializer
    {
        /// <summary>
        /// Serializes <paramref name="dictionary"/> per <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.2">section 4.1.2</see>.
        /// </summary>
        /// <param name="dictionary">The dictionary to serialize.</param>
        /// <returns>The serialized dictionary as a string, or <see langword="null"/> if the dictionary is empty.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="dictionary"/> is <see langword="null"/>.</exception>
        public static string? SerializeDictionary(SfDictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            if (dictionary.Count == 0)
            {
                return null;
            }
            StringBuilder builder = new();
            for (int index = 0; index < dictionary.Count; index++)
            {
                if (index > 0)
                {
                    _ = builder.Append(", ");
                }
                _ = builder.Append(dictionary[index].Key);
                SfMember member = dictionary[index].Value;
                if (member is SfItem { Value: SfBareBoolean { Value: true } })
                {
                    AppendParameters(builder, member.Parameters);
                }
                else
                {
                    _ = builder.Append('=');
                    AppendMember(builder, member);
                }
            }
            return builder.ToString();
        }
        /// <summary>
        /// Serializes <paramref name="item"/> per <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.3">section 4.1.3</see>.
        /// </summary>
        /// <param name="item">The item to serialize.</param>
        /// <returns>The serialized item as a string.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="item"/> is <see langword="null"/>.</exception>
        public static string SerializeItem(SfItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            StringBuilder builder = new();
            AppendItem(builder, item);
            return builder.ToString();
        }
        /// <summary>
        /// Serializes <paramref name="list"/> per <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.1">section 4.1.1</see>.
        /// </summary>
        /// <param name="list">The list to serialize.</param>
        /// <returns>The serialized list as a string, or <see langword="null"/> if the list is empty.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="list"/> is <see langword="null"/>.</exception>
        public static string? SerializeList(SfList list)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Count == 0)
            {
                return null;
            }
            StringBuilder builder = new();
            for (int index = 0; index < list.Count; index++)
            {
                if (index > 0)
                {
                    _ = builder.Append(", ");
                }
                AppendMember(builder, list[index]);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Appends a member (either an item or an inner list) to the provided <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="member">The member to append.</param>
        private static void AppendMember(StringBuilder builder, SfMember member)
        {
            switch (member)
            {
                case SfItem item:
                    AppendItem(builder, item);
                    break;
                case SfInnerList innerList:
                    AppendInnerList(builder, innerList);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported member type '{member.GetType().Name}'");
            }
        }
        /// <summary>
        /// Appends an item to the provided <paramref name="builder"/>, see <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.3">section 4.1.3</see>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="item">The item to append.</param>
        private static void AppendItem(StringBuilder builder, SfItem item)
        {
            _ = builder.Append(item.Value.ToString());
            AppendParameters(builder, item.Parameters);
        }
        /// <summary>
        /// Appends an inner list to the provided <paramref name="builder"/>, see <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.1.1">section 4.1.1.1</see>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="innerList">The inner list to append.</param>
        private static void AppendInnerList(StringBuilder builder, SfInnerList innerList)
        {
            _ = builder.Append('(');
            for (int index = 0; index < innerList.Count; index++)
            {
                if (index > 0)
                {
                    _ = builder.Append(' ');
                }
                AppendItem(builder, innerList[index]);
            }
            _ = builder.Append(')');
            AppendParameters(builder, innerList.Parameters);
        }
        /// <summary>
        /// Appends parameters to the provided <paramref name="builder"/>, see <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.1.2">section 4.1.1.2</see>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="parameters">The parameters to append.</param>
        private static void AppendParameters(StringBuilder builder, SfParameters parameters)
        {
            foreach ((string? key, SfBareItem? value) in parameters)
            {
                _ = builder.Append(';');
                _ = builder.Append(key);
                if (value is not SfBareBoolean { Value: true })
                {
                    _ = builder.Append('=');
                    _ = builder.Append(value.ToString());
                }
            }
        }
    }
}