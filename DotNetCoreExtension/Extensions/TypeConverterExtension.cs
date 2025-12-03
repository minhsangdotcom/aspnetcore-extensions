using System.Collections;
using deniszykov.TypeConversion;
using DotNetCoreExtension.Extensions.Reflections;

namespace DotNetCoreExtension.Extensions;

public static class TypeConverterExtension
{
    /// <summary>
    /// convert only string object to specific type
    /// </summary>
    /// <param name="input"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public static object? ConvertTo(this object? input, Type targetType)
    {
        if (input is null)
        {
            return null;
        }

        Type inputType = input.GetType();

        if (
            targetType.IsAssignableFrom(inputType)
            || inputType.IsUserDefineType()
            || inputType.IsArrayGenericType()
            || (inputType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(inputType))
        )
        {
            return input;
        }

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // ======== ULID ========
        if (underlying == typeof(Ulid))
        {
            return Ulid.Parse(input.ToString()!);
        }

        // ======== DATEONLY ========
        if (underlying == typeof(DateOnly))
        {
            return ConvertToDateOnly(input);
        }

        // ======== DATETIME ========
        if (underlying == typeof(DateTime))
        {
            return ConvertToDateTime(input);
        }

        // ======== DATETIMEOFFSET ========
        if (underlying == typeof(DateTimeOffset))
        {
            return ConvertToDateTimeOffset(input);
        }

        // ======== FALLBACK ========
        var conversionProvider = new TypeConversionProvider();
        return conversionProvider.Convert(typeof(object), targetType, input);
    }

    private static DateOnly ConvertToDateOnly(object input)
    {
        // direct type
        if (input is DateOnly d)
        {
            return d;
        }

        if (input is DateTime dt)
        {
            return DateOnly.FromDateTime(dt);
        }

        if (input is DateTimeOffset dto)
        {
            return DateOnly.FromDateTime(dto.Date);
        }

        if (input is long l)
        {
            return DateOnly.FromDateTime(ParseUnix(l).UtcDateTime);
        }

        if (input is int i)
        {
            return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(i).UtcDateTime);
        }

        if (input is string s)
        {
            s = s.Trim();

            if (DateOnly.TryParse(s, out var dOnly))
            {
                return dOnly;
            }

            if (DateTimeOffset.TryParse(s, out var dto2))
            {
                return DateOnly.FromDateTime(dto2.Date);
            }

            if (DateTime.TryParse(s, out var dt2))
            {
                return DateOnly.FromDateTime(dt2);
            }

            if (long.TryParse(s, out var unix))
            {
                return DateOnly.FromDateTime(ParseUnix(unix).UtcDateTime);
            }

            throw new InvalidCastException($"Cannot convert '{s}' to DateOnly.");
        }

        throw new InvalidCastException($"Cannot convert '{input.GetType()}' to DateOnly.");
    }

    private static DateTime ConvertToDateTime(object input)
    {
        if (input is DateTime dt)
        {
            return dt;
        }

        if (input is DateTimeOffset dto)
        {
            return dto.UtcDateTime;
        }

        if (input is DateOnly dOnly)
        {
            return dOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        }

        if (input is long l)
        {
            return ParseUnix(l).UtcDateTime;
        }

        if (input is int i)
        {
            return DateTimeOffset.FromUnixTimeSeconds(i).UtcDateTime;
        }

        if (input is string s)
        {
            if (long.TryParse(s, out long unix))
            {
                return ParseUnix(unix).UtcDateTime;
            }

            if (DateTime.TryParse(s, out var dt2))
            {
                return dt2;
            }

            if (DateTimeOffset.TryParse(s, out var dto2))
            {
                return dto2.UtcDateTime;
            }

            if (DateOnly.TryParse(s, out var dol))
            {
                return dol.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
            }

            throw new InvalidCastException($"Cannot convert '{s}' to DateTime.");
        }

        throw new InvalidCastException($"Cannot convert '{input.GetType()}' to DateTime.");
    }

    private static DateTimeOffset ConvertToDateTimeOffset(object input)
    {
        if (input is DateTimeOffset dto)
        {
            return dto;
        }

        if (input is DateTime dt)
        {
            return new DateTimeOffset(dt);
        }

        if (input is DateOnly dOnly)
        {
            return new DateTimeOffset(dOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local));
        }

        if (input is long l)
        {
            return ParseUnix(l);
        }

        if (input is int i)
        {
            return DateTimeOffset.FromUnixTimeSeconds(i);
        }

        if (input is string s)
        {
            // unix timestamps
            if (long.TryParse(s, out long unix))
            {
                return ParseUnix(unix);
            }

            // ISO-8601
            if (DateTimeOffset.TryParse(s, out var dto2))
            {
                return dto2;
            }

            // Normal datetime
            if (DateTime.TryParse(s, out var dt2))
            {
                return new DateTimeOffset(dt2);
            }

            if (DateOnly.TryParse(s, out var dOnly2))
            {
                return new DateTimeOffset(dOnly2.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local));
            }

            throw new InvalidCastException($"Cannot convert string '{s}' to DateTimeOffset.");
        }

        throw new InvalidCastException($"Cannot convert '{input.GetType()}' to DateTimeOffset.");
    }

    private static DateTimeOffset ParseUnix(long unix)
    {
        return unix >= 1_000_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
            : DateTimeOffset.FromUnixTimeSeconds(unix);
    }
}
