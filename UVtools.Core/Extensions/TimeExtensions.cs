﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */


using System;

namespace UVtools.Core.Extensions
{
    public static class TimeExtensions
    {
        /// <summary>
        /// Converts seconds to milliseconds
        /// </summary>
        /// <param name="value"></param>
        /// <param name="rounding"></param>
        /// <returns></returns>
        public static float SecondsToMilliseconds(float value, byte rounding = 2) => (float)Math.Round(value * 1000f, rounding);

        /// <summary>
        /// Converts seconds to milliseconds
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static uint SecondsToMillisecondsUint(float value) => (uint)value * 1000;

        /// <summary>
        /// Converts milliseconds to seconds
        /// </summary>
        /// <param name="value"></param>
        /// <param name="rounding"></param>
        /// <returns></returns>
        public static float MillisecondsToSeconds(float value, byte rounding = 2) => (float)Math.Round(value / 1000f, rounding);
    }
}
