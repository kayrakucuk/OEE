namespace Oee.Domain.Tests;

public class ShiftResolverTests
{
    // Turkey dropped daylight saving in 2016 and sits on a permanent UTC+3, which makes it
    // the right zone for the ordinary cases and the wrong one for the DST cases below.
    private static readonly TimeZoneInfo Istanbul =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    private static readonly ShiftDefinition Morning =
        new(1, new TimeOnly(6, 0), TimeSpan.FromHours(8), WeekDays.Weekdays);

    private static readonly ShiftDefinition Afternoon =
        new(2, new TimeOnly(14, 0), TimeSpan.FromHours(8), WeekDays.Weekdays);

    private static readonly ShiftDefinition Night =
        new(3, new TimeOnly(22, 0), TimeSpan.FromHours(8), WeekDays.Weekdays);

    private static ShiftResolver ThreeShifts() =>
        new([Morning, Afternoon, Night], Istanbul);

    /// <summary>Builds a UTC instant from an Istanbul wall-clock time (UTC+3).</summary>
    private static DateTimeOffset Local(int year, int month, int day, int hour, int minute = 0) =>
        new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.FromHours(3))
            .ToUniversalTime();

    [Fact]
    public void Resolves_an_instant_inside_the_morning_shift()
    {
        // Friday 2026-07-31, 09:00 local.
        ShiftAssignment? result = ThreeShifts().Resolve(Local(2026, 7, 31, 9));

        result.Should().NotBeNull();
        result!.Value.ShiftId.Should().Be(1);
        result.Value.ShiftDate.Should().Be(new DateOnly(2026, 7, 31));
        result.Value.ActualLength.Should().Be(TimeSpan.FromHours(8));
    }

    [Fact]
    public void Resolves_the_afternoon_shift()
    {
        ShiftAssignment? result = ThreeShifts().Resolve(Local(2026, 7, 31, 15));

        result!.Value.ShiftId.Should().Be(2);
        result.Value.ShiftDate.Should().Be(new DateOnly(2026, 7, 31));
    }

    [Fact]
    public void A_shift_boundary_belongs_to_the_shift_starting()
    {
        // 14:00 exactly: morning ends, afternoon begins. Half-open intervals mean the
        // instant belongs to the afternoon.
        ShiftAssignment? result = ThreeShifts().Resolve(Local(2026, 7, 31, 14));

        result!.Value.ShiftId.Should().Be(2);
    }

    [Fact]
    public void A_night_shift_before_midnight_belongs_to_the_starting_date()
    {
        // Friday 23:00 — the night shift began at 22:00 on Friday.
        ShiftAssignment? result = ThreeShifts().Resolve(Local(2026, 7, 31, 23));

        result!.Value.ShiftId.Should().Be(3);
        result.Value.ShiftDate.Should().Be(new DateOnly(2026, 7, 31));
    }

    [Fact]
    public void A_night_shift_after_midnight_still_belongs_to_the_previous_date()
    {
        // Saturday 02:00 — still the night shift that started Friday 22:00. This is the
        // case that makes shift date a real concept rather than just the calendar date.
        ShiftAssignment? result = ThreeShifts().Resolve(Local(2026, 8, 1, 2));

        result.Should().NotBeNull();
        result!.Value.ShiftId.Should().Be(3);
        result.Value.ShiftDate.Should().Be(new DateOnly(2026, 7, 31));
    }

    [Fact]
    public void A_night_shift_does_not_run_when_its_start_date_is_excluded()
    {
        // Sunday 02:00 would be the tail of a Saturday night shift, but the pattern is
        // weekdays only and Saturday is not one.
        ShiftAssignment? result = ThreeShifts().Resolve(Local(2026, 8, 2, 2));

        result.Should().BeNull();
    }

    [Fact]
    public void An_instant_outside_every_pattern_resolves_to_nothing()
    {
        // Sunday midday: no weekday pattern covers it.
        ThreeShifts().Resolve(Local(2026, 8, 2, 12)).Should().BeNull();
    }

    [Fact]
    public void A_gap_between_shifts_resolves_to_nothing()
    {
        var resolver = new ShiftResolver(
            [new ShiftDefinition(1, new TimeOnly(6, 0), TimeSpan.FromHours(8), WeekDays.All)],
            Istanbul);

        resolver.Resolve(Local(2026, 7, 31, 15)).Should().BeNull();
    }

    [Fact]
    public void Start_and_end_are_returned_in_utc()
    {
        ShiftAssignment? result = ThreeShifts().Resolve(Local(2026, 7, 31, 9));

        // 06:00 Istanbul is 03:00 UTC.
        result!.Value.StartUtc.Should().Be(new DateTimeOffset(2026, 7, 31, 3, 0, 0, TimeSpan.Zero));
        result.Value.EndUtc.Should().Be(new DateTimeOffset(2026, 7, 31, 11, 0, 0, TimeSpan.Zero));
        result.Value.StartUtc.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Overlapping_patterns_resolve_to_the_one_that_started_most_recently()
    {
        var resolver = new ShiftResolver(
            [
                new ShiftDefinition(1, new TimeOnly(6, 0), TimeSpan.FromHours(12), WeekDays.All),
                new ShiftDefinition(2, new TimeOnly(10, 0), TimeSpan.FromHours(8), WeekDays.All)
            ],
            Istanbul);

        resolver.Resolve(Local(2026, 7, 31, 11))!.Value.ShiftId.Should().Be(2);
    }

    // ------------------------------------------------------------ daylight saving

    private static readonly TimeZoneInfo Berlin =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    [Fact]
    public void A_shift_spanning_spring_forward_is_an_hour_short()
    {
        // Germany springs forward at 02:00 on 2026-03-29. A 22:00 + 8h night shift that
        // starts on the 28th therefore covers only seven real hours.
        var resolver = new ShiftResolver(
            [new ShiftDefinition(1, new TimeOnly(22, 0), TimeSpan.FromHours(8), WeekDays.All)],
            Berlin);

        DateTimeOffset instant = new DateTimeOffset(2026, 3, 28, 23, 0, 0, TimeSpan.FromHours(1))
            .ToUniversalTime();

        ShiftAssignment? result = resolver.Resolve(instant);

        result.Should().NotBeNull();
        result!.Value.ShiftDate.Should().Be(new DateOnly(2026, 3, 28));
        result.Value.ActualLength.Should().Be(TimeSpan.FromHours(7),
            "the wall clock jumped from 02:00 to 03:00 during the shift");
    }

    [Fact]
    public void A_shift_spanning_fall_back_is_an_hour_long()
    {
        // Germany falls back at 03:00 on 2026-10-25, so the same pattern covers nine hours.
        var resolver = new ShiftResolver(
            [new ShiftDefinition(1, new TimeOnly(22, 0), TimeSpan.FromHours(8), WeekDays.All)],
            Berlin);

        DateTimeOffset instant = new DateTimeOffset(2026, 10, 24, 23, 0, 0, TimeSpan.FromHours(2))
            .ToUniversalTime();

        ShiftAssignment? result = resolver.Resolve(instant);

        result.Should().NotBeNull();
        result!.Value.ActualLength.Should().Be(TimeSpan.FromHours(9),
            "the wall clock repeated the 02:00 hour during the shift");
    }

    [Fact]
    public void A_shift_starting_inside_the_spring_forward_gap_still_resolves()
    {
        // 02:30 never existed on 2026-03-29 in Berlin. The shift begins when the clock
        // jumped to 03:30 rather than failing to exist.
        var resolver = new ShiftResolver(
            [new ShiftDefinition(1, new TimeOnly(2, 30), TimeSpan.FromHours(8), WeekDays.All)],
            Berlin);

        DateTimeOffset instant = new DateTimeOffset(2026, 3, 29, 6, 0, 0, TimeSpan.FromHours(2))
            .ToUniversalTime();

        ShiftAssignment? result = resolver.Resolve(instant);

        result.Should().NotBeNull();
        result!.Value.StartUtc.Should().Be(new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero));
    }

    // -------------------------------------------------------------- construction

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(24)]
    [InlineData(25)]
    public void A_shift_duration_must_be_positive_and_under_a_day(int hours)
    {
        Action act = () => _ = new ShiftResolver(
            [new ShiftDefinition(1, new TimeOnly(6, 0), TimeSpan.FromHours(hours), WeekDays.All)],
            Istanbul);

        act.Should().Throw<ArgumentException>().WithParameterName("shifts");
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        Action shifts = () => _ = new ShiftResolver(null!, Istanbul);
        Action zone = () => _ = new ShiftResolver([], null!);

        shifts.Should().Throw<ArgumentNullException>().WithParameterName("shifts");
        zone.Should().Throw<ArgumentNullException>().WithParameterName("plantTimeZone");
    }

    [Fact]
    public void No_patterns_resolves_to_nothing()
    {
        new ShiftResolver([], Istanbul).Resolve(Local(2026, 7, 31, 9)).Should().BeNull();
    }

    // ------------------------------------------------------------------ weekdays

    [Theory]
    [InlineData(2026, 7, 31, WeekDays.Friday)]
    [InlineData(2026, 8, 1, WeekDays.Saturday)]
    [InlineData(2026, 8, 2, WeekDays.Sunday)]
    [InlineData(2026, 8, 3, WeekDays.Monday)]
    public void Dates_map_to_their_day_flag(int year, int month, int day, WeekDays expected)
    {
        var date = new DateOnly(year, month, day);

        date.DayOfWeek.ToFlag().Should().Be(expected);
        WeekDays.All.Includes(date).Should().BeTrue();
    }

    [Fact]
    public void Weekday_and_weekend_sets_are_complementary()
    {
        (WeekDays.Weekdays | WeekDays.Weekend).Should().Be(WeekDays.All);
        (WeekDays.Weekdays & WeekDays.Weekend).Should().Be(WeekDays.None);
    }
}
