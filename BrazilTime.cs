namespace MuranoApp
{
    public static class BrazilTime
    {
        private static readonly string[] TimeZoneCandidates =
        {
        "America/Sao_Paulo",
        "E. South America Standard Time"
    };

        public static TimeZoneInfo GetTimeZone()
        {
            foreach (var timeZoneId in TimeZoneCandidates)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch
                {
                }
            }

            throw new InvalidOperationException(
                "Não foi possível localizar o fuso horário do Brasil (São Paulo) no ambiente atual.");
        }

        public static DateTimeOffset ConvertBrazilLocalToUtc(DateTime localDateTime)
        {
            var brazilTimeZone = GetTimeZone();

            var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, brazilTimeZone);

            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        public static DateTime ConvertUtcToBrazilLocal(DateTimeOffset utcDateTime)
        {
            var brazilTimeZone = GetTimeZone();
            return TimeZoneInfo.ConvertTime(utcDateTime, brazilTimeZone).DateTime;
        }
    }
}
