﻿using System;
using System.Globalization;
using GParse;
using GParse.Utilities;
using Loretta.CodeAnalysis.Text;
using Loretta.ThirdParty.FParsec;
using Loretta.Utilities;

namespace Loretta.CodeAnalysis.Syntax
{
    internal sealed partial class Lexer
    {
        private Int32 SkipDecimalDigits ( )
        {
            Char digit;
            var digits = 0;
            while ( LoCharUtils.IsDecimal ( digit = this._reader.Peek ( ).GetValueOrDefault ( ) ) || digit == '_' )
            {
                this._reader.Advance ( 1 );
                if ( digit != '_' ) digits++;
            }
            return digits;
        }

        private Double ParseBinaryNumber ( )
        {
            var num = 0L;
            var digits = 0;
            Char digit;
            while ( CharUtils.IsInRange ( '0', digit = this._reader.Peek ( ).GetValueOrDefault ( ), '1' ) || digit == '_' )
            {
                this._reader.Advance ( 1 );
                if ( digit == '_' ) continue;
                num = ( num << 1 ) | ( digit - ( Int64 ) '0' );
                digits++;
            }

            var span = TextSpan.FromBounds ( this._start, this._reader.Position );
            var location = new TextLocation ( this._text, span );
            if ( !this._luaOptions.AcceptBinaryNumbers )
                this.Diagnostics.ReportBinaryLiteralNotSupportedInVersion ( location );
            if ( digits < 1 )
                this.Diagnostics.ReportInvalidNumber ( location );
            if ( digits > 64 )
                this.Diagnostics.ReportNumericLiteralTooLarge ( location );

            return num;
        }

        private Double ParseOctalNumber ( )
        {
            var num = 0L;
            var digits = 0;
            Char digit;
            while ( CharUtils.IsInRange ( '0', digit = this._reader.Peek ( ).GetValueOrDefault ( ), '7' ) || digit == '_' )
            {
                this._reader.Advance ( 1 );
                if ( digit == '_' ) continue;
                num = ( num << 3 ) | ( digit - ( Int64 ) '0' );
                digits++;
            }

            var span = TextSpan.FromBounds ( this._start, this._reader.Position );
            var location = new TextLocation ( this._text, span );
            if ( !this._luaOptions.AcceptOctalNumbers )
                this.Diagnostics.ReportOctalLiteralNotSupportedInVersion ( location );
            if ( digits < 1 )
                this.Diagnostics.ReportInvalidNumber ( location );
            if ( digits > 21 )
                this.Diagnostics.ReportNumericLiteralTooLarge ( location );

            return num;
        }

        private Double ParseDecimalNumber ( )
        {
            var numStart = this._reader.Position;

            this.SkipDecimalDigits ( );
            if ( this._reader.IsNext ( '.' ) )
            {
                this._reader.Advance ( 1 );
                this.SkipDecimalDigits ( );
                if ( CharUtils.AsciiLowerCase ( this._reader.Peek ( ).GetValueOrDefault ( ) ) == 'e' )
                {
                    this._reader.Advance ( 1 );
                    if ( this._reader.IsNext ( '+' ) || this._reader.IsNext ( '-' ) )
                        this._reader.Advance ( 1 );
                    this.SkipDecimalDigits ( );
                }
            }

            var numEnd = this._reader.Position;
            var numLength = numEnd - numStart;

            this._reader.Restore ( numStart );
            if ( numLength < 255 )
            {
                ReadOnlySpan<Char> rawNum = this._reader.ReadSpan ( numLength );
                Span<Char> buff = stackalloc Char[numLength];

                var buffIdx = 0;
                for ( var rawNumIdx = 0; rawNumIdx < numLength; rawNumIdx++ )
                {
                    var ch = rawNum[rawNumIdx];
                    if ( ch == '_' ) continue;
                    buff[buffIdx] = ch;
                    buffIdx++;
                }

                return Double.Parse (
                    buff.Slice ( 0, buffIdx ),
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture );
            }
            else
            {
                var rawNum = this._reader.ReadString ( numLength )!;
                return Double.Parse (
                    rawNum.Replace ( "_", "" ),
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture );
            }
        }

        private Double ParseHexadecimalNumber ( )
        {
            var numStart = this._reader.Position;

            skipHexDigits ( );
            if ( this._reader.IsNext ( '.' ) )
            {
                this._reader.Advance ( 1 );
                skipHexDigits ( );
                if ( CharUtils.AsciiLowerCase ( this._reader.Peek ( ).GetValueOrDefault ( ) ) == 'p' )
                {
                    this._reader.Advance ( 1 );
                    if ( this._reader.IsNext ( '+' ) || this._reader.IsNext ( '-' ) )
                        this._reader.Advance ( 1 );
                    this.SkipDecimalDigits ( );
                }

                if ( !this._luaOptions.AcceptHexFloatLiterals )
                {
                    var span = TextSpan.FromBounds ( this._start, this._reader.Position );
                    var location = new TextLocation ( this._text, span );
                    this.Diagnostics.ReportHexFloatLiteralNotSupportedInVersion ( location );
                }
            }

            var numEnd = this._reader.Position;
            var numLength = numEnd - numStart;

            this._reader.Restore ( numStart );
            if ( numLength < 255 )
            {
                ReadOnlySpan<Char> rawNum = this._reader.ReadSpan ( numLength );
                Span<Char> buff = stackalloc Char[numLength];

                var buffIdx = 0;
                for ( var rawNumIdx = 0; rawNumIdx < numLength; rawNumIdx++ )
                {
                    var ch = rawNum[rawNumIdx];
                    if ( ch == '_' ) continue;
                    buff[buffIdx] = ch;
                    buffIdx++;
                }

                return HexFloat.DoubleFromHexString ( buff.Slice ( 0, buffIdx ) );
            }
            else
            {
                var rawNum = this._reader.ReadString ( numLength )!;
                return HexFloat.DoubleFromHexString ( rawNum.Replace ( "_", "" ) );
            }

            void skipHexDigits ( )
            {
                Char digit;
                while ( LoCharUtils.IsHexadecimal ( digit = this._reader.Peek ( ).GetValueOrDefault ( ) ) || digit == '_' )
                    this._reader.Advance ( 1 );
            }
        }
    }
}
