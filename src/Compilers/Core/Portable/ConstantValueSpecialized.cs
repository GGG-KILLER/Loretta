// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis
{
    internal partial class ConstantValue
    {
        /// <summary>
        /// The IEEE floating-point spec doesn't specify which bit pattern an implementation
        /// is required to use when producing NaN values.  Indeed, the spec does recommend
        /// "diagnostic" information "left to the implementer’s discretion" be placed in the
        /// undefined bits. It is therefore likely that NaNs produced on different platforms
        /// will differ even for the same arithmetic such as 0.0 / 0.0.  To ensure that the
        /// compiler behaves in a deterministic way, we force NaN values to use the
        /// IEEE "canonical" form with the diagnostic bits set to zero and the sign bit set
        /// to one.  Conversion of this value to float produces the corresponding
        /// canonical NaN of the float type (IEEE Std 754-2008 section 6.2.3).
        /// </summary>
        private static readonly double s_IEEE_canonical_NaN = BitConverter.Int64BitsToDouble(unchecked((long) 0xFFF8000000000000UL));

        private sealed class ConstantValueBad : ConstantValue
        {
            private ConstantValueBad() { }

            public static readonly ConstantValueBad Instance = new ConstantValueBad();

            public override ConstantValueTypeDiscriminator Discriminator =>
                ConstantValueTypeDiscriminator.Bad;

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other) => ReferenceEquals(this, other);

            public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

            internal override string GetValueToDisplay() => "bad";
        }

        private sealed class ConstantValueNil : ConstantValue
        {
            private ConstantValueNil() { }

            public static readonly ConstantValueNil Instance = new ConstantValueNil();
            public static readonly ConstantValueNil Uninitialized = new ConstantValueNil();

            public override ConstantValueTypeDiscriminator Discriminator =>
                ConstantValueTypeDiscriminator.Nil;

            public override string? StringValue => null;
            internal override Rope? RopeValue => null;

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other) => ReferenceEquals(this, other);

            public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

            public override bool IsDefaultValue => true;

            internal override string GetValueToDisplay() =>
                ReferenceEquals(this, Uninitialized) ? "unset" : "null";
        }

        private sealed class ConstantValueString : ConstantValue
        {
            private readonly Rope _value;
            /// <summary>
            /// Some string constant values can have large costs to realize. To compensate, we realize
            /// constant values lazily, and hold onto a weak reference. If the next time we're asked for the constant
            /// value the previous one still exists, we can avoid rerealizing it. But we don't want to root the constant
            /// value if it's not being used.
            /// </summary>
            private WeakReference<string>? _constantValueReference;

            public ConstantValueString(string value)
            {
                // we should have just one Null regardless string or object.
                RoslynDebug.Assert(value != null, "null strings should be represented as Null constant.");
                _value = Rope.ForString(value);
                _constantValueReference = new WeakReference<string>(value);
            }

            public ConstantValueString(Rope value)
            {
                // we should have just one Null regardless string or object.
                RoslynDebug.Assert(value != null, "null strings should be represented as Null constant.");
                _value = value;
            }

            public override ConstantValueTypeDiscriminator Discriminator =>
                ConstantValueTypeDiscriminator.String;

            public override string StringValue
            {
                get
                {
                    string? constantValue = null;
                    if (_constantValueReference?.TryGetTarget(out constantValue) != true)
                    {
                        // Note: we could end up realizing the constant value multiple times if there's
                        // a race here. Currently, this isn't believed to be an issue, as the assignment
                        // to _constantValueReference is atomic so the worst that will happen is we return
                        // different instances of a string constant.
                        constantValue = _value.ToString();
                        _constantValueReference = new WeakReference<string>(constantValue);
                    }

                    RoslynDebug.Assert(constantValue != null);
                    return constantValue;
                }
            }

            internal override Rope RopeValue => _value;

            public override int GetHashCode() =>
                Hash.Combine(base.GetHashCode(), _value.GetHashCode());

            public override bool Equals(ConstantValue? other) =>
                base.Equals(other) && _value.Equals(other.RopeValue);

            internal override string GetValueToDisplay() =>
                _value is null ? "null" : string.Format("\"{0}\"", _value);
        }

        // base for constant classes that may represent more than one 
        // constant type
        private abstract class ConstantValueDiscriminated : ConstantValue
        {
            private readonly ConstantValueTypeDiscriminator _discriminator;

            public ConstantValueDiscriminated(ConstantValueTypeDiscriminator discriminator)
            {
                _discriminator = discriminator;
            }

            public override ConstantValueTypeDiscriminator Discriminator => _discriminator;
        }

        // default value of a value type constant. (reference type constants use Null as default)
        private class ConstantValueDefault : ConstantValueDiscriminated
        {
            public static readonly ConstantValueDefault Int64 = new ConstantValueDefault(ConstantValueTypeDiscriminator.Int64);
            public static readonly ConstantValueDefault Double = new ConstantValueDoubleZero();
            public static readonly ConstantValueDefault Boolean = new ConstantValueDefault(ConstantValueTypeDiscriminator.Boolean);

            protected ConstantValueDefault(ConstantValueTypeDiscriminator discriminator)
                : base(discriminator)
            {
            }

            public override bool BooleanValue => false;

            public override long Int64Value => 0L;

            public override double DoubleValue => 0;

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other) => ReferenceEquals(this, other);

            public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

            public override bool IsDefaultValue => true;
        }

        private sealed class ConstantValueDoubleZero : ConstantValueDefault
        {
            internal ConstantValueDoubleZero()
                : base(ConstantValueTypeDiscriminator.Double)
            {
            }

            public override bool Equals(ConstantValue? other)
            {
                if (ReferenceEquals(other, this))
                {
                    return true;
                }

                if (other is null)
                {
                    return false;
                }

                return Discriminator == other.Discriminator && other.DoubleValue == 0;
            }
        }

        private class ConstantValueOne : ConstantValueDiscriminated
        {
            public static readonly ConstantValueOne Int64 = new ConstantValueOne(ConstantValueTypeDiscriminator.Int64);
            public static readonly ConstantValueOne Double = new ConstantValueOne(ConstantValueTypeDiscriminator.Double);
            public static readonly ConstantValueOne Boolean = new ConstantValueOne(ConstantValueTypeDiscriminator.Boolean);

            protected ConstantValueOne(ConstantValueTypeDiscriminator discriminator)
                : base(discriminator)
            {
            }

            public override bool BooleanValue => true;

            public override long Int64Value => 1L;

            public override double DoubleValue => 1;

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other) => ReferenceEquals(this, other);

            public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
        }

        private sealed class ConstantValueI64 : ConstantValueDiscriminated
        {
            private readonly long _value;

            public ConstantValueI64(long value)
                : base(ConstantValueTypeDiscriminator.Int64)
            {
                _value = value;
            }

            public override long Int64Value => _value;

            public override int GetHashCode() =>
                Hash.Combine(base.GetHashCode(), _value.GetHashCode());

            public override bool Equals(ConstantValue? other) =>
                base.Equals(other) && _value == other.Int64Value;
        }

        private sealed class ConstantValueDouble : ConstantValueDiscriminated
        {
            private readonly double _value;

            public ConstantValueDouble(double value)
                : base(ConstantValueTypeDiscriminator.Double)
            {
                if (double.IsNaN(value))
                {
                    value = s_IEEE_canonical_NaN;
                }

                _value = value;
            }

            public override double DoubleValue => _value;

            public override int GetHashCode() =>
                Hash.Combine(base.GetHashCode(), _value.GetHashCode());

            public override bool Equals(ConstantValue? other) =>
                base.Equals(other) && _value.Equals(other.DoubleValue);
        }
    }
}
