namespace SharedKernel
{
    /// <summary>
    /// Represents a value object, which is an immutable object defined by its properties.
    /// Two value objects are equal if all their attributes are equal.
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        /// <summary>
        /// When overridden in a derived class, this method provides the components
        /// that are used to determine the equality of the value object.
        /// </summary>
        /// <returns>An enumerable of the value object's equality components.</returns>
        public abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj is null || obj.GetType() != GetType())
            {
                return false;
            }

            var valueObject = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
        }

        public bool Equals(ValueObject? other)
        {
            return Equals((object?)other);
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (left is null && right is null)
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var component in GetEqualityComponents())
            {
                hash.Add(component);
            }
            return hash.ToHashCode();
        }
    }
}
