namespace Backend.Services;

public record AppDateInfo(DateOnly ActiveDate, DateOnly RealDate, bool IsTestMode);

public class AppDateService
{
    private static DateOnly? _testDate;

    public DateOnly Today => _testDate ?? DateOnly.FromDateTime(DateTime.Today);

    public DateTime Now => Today.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));

    public AppDateInfo GetInfo()
    {
        return new AppDateInfo(Today, DateOnly.FromDateTime(DateTime.Today), _testDate.HasValue);
    }

    public AppDateInfo Set(DateOnly date)
    {
        _testDate = date;
        return GetInfo();
    }

    public AppDateInfo Reset()
    {
        _testDate = null;
        return GetInfo();
    }
}
