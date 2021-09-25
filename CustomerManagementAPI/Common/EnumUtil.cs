﻿using System;

namespace CustomerManagementAPI.Common
{
    public static class EnumUtil
    {
        public static T ParseEnum<T>(string value)
        {
            return (T) Enum.Parse(typeof(T), value, true);
        }
    }
}