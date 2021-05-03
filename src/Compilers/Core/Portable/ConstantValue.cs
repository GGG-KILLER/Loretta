// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis
{
    internal enum ConstantValueTypeDiscriminator : byte
    {
        Nil,
        Bad,
        Int64,
        Boolean,
        Double,
        String,
    }

    internal abstract partial class ConstantValue : IEquatable<ConstantValue?>
    {
        public abstract ConstantValueTypeDiscriminator Discriminator { get; }

        public virtual string? StringValue => throw new InvalidOperationException();
        internal virtual Rope? RopeValue => throw new InvalidOperationException();

        public virtual bool BooleanValue => throw new InvalidOperationException();

        public virtual long Int64Value => throw new InvalidOperationException();

        public virtual double DoubleValue => throw new InvalidOperationException();

        // returns true if value is in its default (zero-inited) form.
        public virtual bool IsDefaultValue => false;

        // NOTE: We do not have IsNumericZero. 
        //       The reason is that integral zeroes are same as default values
        //       and singles, floats and decimals have multiple zero values. 
        //       It appears that in all cases so far we considered isDefaultValue, and not about value being 
        //       arithmetic zero (especially when definition is ambiguous).

        public const ConstantValue NotAvailable = null;

        public static ConstantValue Bad => ConstantValueBad.Instance;
        public static ConstantValue Nil => ConstantValueNil.Instance;
        // Nil and Unset are all ConstantValueNull. Nil represents the nil constant in Lua.
        // Unset indicates an uninitialized ConstantValue.
        public static ConstantValue Unset => ConstantValueNil.Uninitialized;

        public static ConstantValue True => ConstantValueOne.Boolean;
        public static ConstantValue False => ConstantValueDefault.Boolean;

        public static ConstantValue Create(string? value)
        {
            if (value == null)
            {
                return Nil;
            }

            return new ConstantValueString(value);
        }

        internal static ConstantValue CreateFromRope(Rope value)
        {
            RoslynDebug.Assert(value != null);
            return new ConstantValueString(value);
        }

        public static ConstantValue Create(long value)
        {
            if (value == 0)
            {
                return ConstantValueDefault.Int64;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Int64;
            }

            return new ConstantValueI64(value);
        }

        public static ConstantValue Create(bool value) =>
            value ? ConstantValueOne.Boolean : ConstantValueDefault.Boolean;

        public static ConstantValue Create(double value)
        {
            if (BitConverter.DoubleToInt64Bits(value) == 0)
            {
                return ConstantValueDefault.Double;
            }
            else if (value == 1)
            {
                return ConstantValueOne.Double;
            }

            return new ConstantValueDouble(value);
        }

        public static ConstantValue Create(object value, ConstantValueTypeDiscriminator discriminator)
        {
            Debug.Assert(BitConverter.IsLittleEndian);

            return discriminator switch
            {
                ConstantValueTypeDiscriminator.Nil => Nil,
                ConstantValueTypeDiscriminator.Int64 => Create((long) value),
                ConstantValueTypeDiscriminator.Boolean => Create((bool) value),
                ConstantValueTypeDiscriminator.Double => Create((double) value),
                ConstantValueTypeDiscriminator.String => Create((string) value),
                _ => throw new InvalidOperationException(),//Not using ExceptionUtilities.UnexpectedValue() because this failure path is tested.
            };
        }

        public static ConstantValue Default(ConstantValueTypeDiscriminator discriminator)
        {
            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.Bad: return Bad;

                case ConstantValueTypeDiscriminator.Int64: return ConstantValueDefault.Int64;
                case ConstantValueTypeDiscriminator.Boolean: return ConstantValueDefault.Boolean;
                case ConstantValueTypeDiscriminator.Double: return ConstantValueDefault.Double;

                case ConstantValueTypeDiscriminator.Nil:
                case ConstantValueTypeDiscriminator.String: return Nil;
                default:
                    break;
            }

            throw ExceptionUtilities.UnexpectedValue(discriminator);
        }

        public object? Value => Discriminator switch
        {
            ConstantValueTypeDiscriminator.Bad => null,
            ConstantValueTypeDiscriminator.Nil => null,
            ConstantValueTypeDiscriminator.Int64 => Boxes.Box(Int64Value),
            ConstantValueTypeDiscriminator.Boolean => Boxes.Box(BooleanValue),
            ConstantValueTypeDiscriminator.Double => Boxes.Box(DoubleValue),
            ConstantValueTypeDiscriminator.String => StringValue,
            _ => throw ExceptionUtilities.UnexpectedValue(Discriminator),
        };

        public static bool IsIntegralType(ConstantValueTypeDiscriminator discriminator) =>
            discriminator is ConstantValueTypeDiscriminator.Int64;

        public bool IsIntegral => IsIntegralType(Discriminator);

        public bool IsNegativeNumeric => Discriminator switch
        {
            ConstantValueTypeDiscriminator.Int64 => Int64Value < 0,
            ConstantValueTypeDiscriminator.Double => DoubleValue < 0,
            _ => false,
        };

        public bool IsNumeric =>
            Discriminator is ConstantValueTypeDiscriminator.Int64 or ConstantValueTypeDiscriminator.Double;

        public static bool IsBooleanType(ConstantValueTypeDiscriminator discriminator) =>
            discriminator == ConstantValueTypeDiscriminator.Boolean;

        public bool IsBoolean => Discriminator == ConstantValueTypeDiscriminator.Boolean;

        public static bool IsStringType(ConstantValueTypeDiscriminator discriminator) => discriminator == ConstantValueTypeDiscriminator.String;

        [MemberNotNullWhen(true, nameof(StringValue))]
        public bool IsString => Discriminator == ConstantValueTypeDiscriminator.String;

        public static bool IsFloatingType(ConstantValueTypeDiscriminator discriminator) =>
            discriminator is ConstantValueTypeDiscriminator.Double;

        public bool IsFloating => Discriminator is ConstantValueTypeDiscriminator.Double;

        public bool IsBad => Discriminator == ConstantValueTypeDiscriminator.Bad;

        public bool IsNull => ReferenceEquals(this, Nil);

        public void Serialize(BlobBuilder writer)
        {
            switch (Discriminator)
            {
                case ConstantValueTypeDiscriminator.Boolean:
                    writer.WriteBoolean(BooleanValue);
                    break;

                case ConstantValueTypeDiscriminator.Double:
                    writer.WriteDouble(DoubleValue);
                    break;

                case ConstantValueTypeDiscriminator.Int64:
                    writer.WriteInt64(Int64Value);
                    break;

                default: throw ExceptionUtilities.UnexpectedValue(Discriminator);
            }
        }

        public override string ToString()
        {
            var valueToDisplay = GetValueToDisplay();
            return string.Format("{0}({1}: {2})", GetType().Name, valueToDisplay, Discriminator);
        }

        internal virtual string? GetValueToDisplay() => Value?.ToString();

        // equal constants must have matching discriminators
        // derived types override this if equivalence is more than just discriminators match. 
        // singletons also override this since they only need a reference compare.
        public virtual bool Equals(ConstantValue? other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return Discriminator == other.Discriminator;
        }

        public static bool operator ==(ConstantValue? left, ConstantValue? right)
        {
            if (ReferenceEquals(right, left))
            {
                return true;
            }

            if (left is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(ConstantValue? left, ConstantValue? right) => !(left == right);

        public override int GetHashCode() => Discriminator.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as ConstantValue);
    }
}
