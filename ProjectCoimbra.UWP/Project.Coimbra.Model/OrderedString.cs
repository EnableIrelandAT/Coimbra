namespace Coimbra.Model
{
    /// <summary>
    /// An ordered string.
    /// </summary>
    public class OrderedString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedString"/> class.
        /// </summary>
        /// <param name="position">Position this string should be ordered at.</param>
        /// <param name="value">String value of item.</param>
        public OrderedString(int position, string value)
        {
            this.Position = position;
            this.Value = value;
        }

        /// <summary>
        /// Gets position.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets value.
        /// </summary>
        public string Value { get; }
    }
}
