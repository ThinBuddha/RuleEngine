namespace Probabilities
{
    // A probability is basically a double type, where 0 <= value <= 1
    // Integers are taken as the percentage counterpart, e.g. double value = 0.5 -> integer value = 50,
    // by means of explicit conversion
    //
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
    public class Probability
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    {
        private readonly double _value;

        public double Value { get { return _value; } }

        public Probability(double value)
        {
            _value = value;
        }

        public static implicit operator Probability(double value)
        {
            return new Probability(value);
        }

        public static explicit operator Probability(int value)
        {
            return new Probability((double)value / 100);
        }

        public static explicit operator int(Probability p)
        {
            return (int)(p.Value * 100);
        }

        public static Probability operator +(Probability p1, Probability p2)
        {
            return new Probability(p1.Value + p2.Value);
        }

        public static Probability operator -(Probability p1, Probability p2)
        {
            return new Probability(p1.Value - p2.Value);
        }

        public static Probability operator *(Probability p1, Probability p2)
        {
            return new Probability(p1.Value * p2.Value);
        }

        public static Probability operator /(Probability p1, Probability p2)
        {
            return new Probability(p1.Value / p2.Value);
        }

        public static bool operator ==(Probability p1, Probability p2)
        {
            return (p1.Value == p2.Value);
        }

        public static bool operator !=(Probability p1, Probability p2)
        {
            return (p1.Value != p2.Value);
        }

        public static bool operator >=(Probability p1, Probability p2)
        {
            return (p1.Value >= p2.Value);
        }

        public static bool operator <=(Probability p1, Probability p2)
        {
            return (p1.Value <= p2.Value);
        }

        public static bool operator >(Probability p1, Probability p2)
        {
            return (p1.Value > p2.Value);
        }

        public static bool operator <(Probability p1, Probability p2)
        {
            return (p1.Value < p2.Value);
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public static Probability Min { get { return 0; } }

        public static Probability Max { get { return 100; } }
    }
}
