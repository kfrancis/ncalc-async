﻿using System;

namespace NCalcAsync
{
    // Summary:
    //     Provides enumerated values to use to set evaluation options.
    [Flags]
    public enum EvaluateOptions
    {
        // Summary:
        //     Specifies that no options are set.
        None = 1,
        //
        // Summary:
        //     Specifies case-insensitive matching.
        IgnoreCase = 2,
        //
        // Summary:
        //     No-cache mode. Ingores any pre-compiled expression in the cache.
        NoCache = 4,
        //
        // Summary:
        //     Treats parameters as arrays and result a set of results.
        IterateParameters = 8,
        //
        // Summary:
        //     When using Round(), if a number is halfway between two others, it is rounded toward the nearest number that is away from zero. 
        RoundAwayFromZero = 16,
        //
        // Summary:
        //     Ignore case on string compare
        MatchStringsWithIgnoreCase = 1 << 5,
        //
        // Summary:
        //     Use ordinal culture on string compare
        MatchStringsOrdinal = 1 << 6,
        //
        // Summary:
        //     Use checked math
        OverflowProtection = 1 << 7,

        /// <summary>
        ///     Allow calculation with boolean values.
        /// </summary>
        BooleanCalculation = 1 << 8,

        /// <summary>
        ///     When using Abs(), return a double instead of a decimal.
        /// </summary>
        UseDoubleForAbsFunction = 1 << 9,

        /// <summary>
        /// Defines a "null" parameter and allows comparison of values to null.
        /// </summary>
        AllowNullParameter = 1 << 10
    }
}
