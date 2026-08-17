using Microsoft.EntityFrameworkCore;
using Oee.Domain;
using Oee.Domain.Entities;

namespace Oee.Persistence;

/// <summary>
/// Master data baked into the migrations via <c>HasData</c>.
/// </summary>
/// <remarks>
/// Primary keys are hard-coded because <c>HasData</c> requires stable literals — EF
/// compares them across migrations to work out what changed. The trade-off is that
/// changing the seed means generating a new migration rather than just restarting the app.
/// Acceptable for master data that changes a handful of times a year.
/// </remarks>
internal static class SeedData
{
    private static class Plants
    {
        public const int Istanbul = 1;
    }

    private static class Lines
    {
        public const int A = 1;
        public const int B = 2;
    }

    private static class Shifts
    {
        public const int Morning = 1;
        public const int Afternoon = 2;
        public const int Night = 3;
    }

    private static class Reasons
    {
        public const int Break = 1;
        public const int PreventiveMaintenance = 2;
        public const int NoDemand = 3;
        public const int MechanicalFailure = 4;
        public const int ElectricalFailure = 5;
        public const int Changeover = 6;
        public const int ToolChange = 7;
        public const int MaterialStarvation = 8;
        public const int DownstreamBlockage = 9;
        public const int ReducedSpeed = 10;
        public const int ProcessDefect = 11;
        public const int StartupScrap = 12;
    }

    public static void Apply(ModelBuilder modelBuilder)
    {
        SeedPlantsAndLines(modelBuilder);
        SeedMachines(modelBuilder);
        SeedProducts(modelBuilder);
        SeedReasonCodes(modelBuilder);
        SeedShifts(modelBuilder);
        SeedPlannedDowntime(modelBuilder);
    }

    private static void SeedPlantsAndLines(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plant>().HasData(new Plant
        {
            Id = Plants.Istanbul,
            Code = "IST-01",
            Name = "Istanbul Plant",
            TimeZoneId = "Europe/Istanbul"
        });

        modelBuilder.Entity<Line>().HasData(
            new Line { Id = Lines.A, PlantId = Plants.Istanbul, Code = "LINE-A", Name = "Housing Assembly" },
            new Line { Id = Lines.B, PlantId = Plants.Istanbul, Code = "LINE-B", Name = "Bracket Fabrication" });
    }

    private static void SeedMachines(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Machine>().HasData(
            // Line A — the assembler is the constraint.
            new Machine { Id = 1, LineId = Lines.A, Code = "M-A1", Name = "Injection Moulder", SequenceInLine = 1, IsBottleneck = false },
            new Machine { Id = 2, LineId = Lines.A, Code = "M-A2", Name = "Assembly Station", SequenceInLine = 2, IsBottleneck = true },
            new Machine { Id = 3, LineId = Lines.A, Code = "M-A3", Name = "Carton Packer", SequenceInLine = 3, IsBottleneck = false },

            // Line B — the welder is the constraint.
            new Machine { Id = 4, LineId = Lines.B, Code = "M-B1", Name = "CNC Mill", SequenceInLine = 1, IsBottleneck = false },
            new Machine { Id = 5, LineId = Lines.B, Code = "M-B2", Name = "Robot Welder", SequenceInLine = 2, IsBottleneck = true },
            new Machine { Id = 6, LineId = Lines.B, Code = "M-B3", Name = "Powder Coating Booth", SequenceInLine = 3, IsBottleneck = false });
    }

    private static void SeedProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Code = "PRD-100", Name = "Standard Housing", IdealCycleTimeSec = 1.0 },
            new Product { Id = 2, Code = "PRD-200", Name = "Reinforced Housing", IdealCycleTimeSec = 1.5 },
            new Product { Id = 3, Code = "PRD-300", Name = "Compact Bracket", IdealCycleTimeSec = 0.75 },
            new Product { Id = 4, Code = "PRD-400", Name = "Heavy-Duty Bracket", IdealCycleTimeSec = 2.5 });
    }

    /// <summary>
    /// Three planned reasons plus at least one for each of the Six Big Losses, so every
    /// slice of a Pareto chart has a code that can produce it.
    /// </summary>
    private static void SeedReasonCodes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReasonCode>().HasData(
            // Planned — subtracted before OEE, so no loss category.
            Planned(Reasons.Break, "BRK", "Scheduled operator break"),
            Planned(Reasons.PreventiveMaintenance, "PM", "Planned preventive maintenance"),
            Planned(Reasons.NoDemand, "NO-DEMAND", "No production scheduled"),

            // 1 — Breakdowns
            Unplanned(Reasons.MechanicalFailure, "MECH", "Mechanical failure", LossCategory.Breakdowns),
            Unplanned(Reasons.ElectricalFailure, "ELEC", "Electrical or control failure", LossCategory.Breakdowns),

            // 2 — Setup and adjustments
            Unplanned(Reasons.Changeover, "CO", "Product changeover", LossCategory.SetupAndAdjustments),
            Unplanned(Reasons.ToolChange, "TOOL", "Tool or die change", LossCategory.SetupAndAdjustments),

            // 3 — Idling and minor stops
            Unplanned(Reasons.MaterialStarvation, "STARVE", "Starved of material", LossCategory.IdlingAndMinorStops),
            Unplanned(Reasons.DownstreamBlockage, "BLOCK", "Blocked by downstream station", LossCategory.IdlingAndMinorStops),

            // 4 — Reduced speed
            Unplanned(Reasons.ReducedSpeed, "SPEED", "Running below rated speed", LossCategory.ReducedSpeed),

            // 5 — Process defects
            Unplanned(Reasons.ProcessDefect, "DEFECT", "In-process quality defect", LossCategory.ProcessDefects),

            // 6 — Startup rejects
            Unplanned(Reasons.StartupScrap, "STARTUP", "Scrap produced before process stabilised", LossCategory.StartupRejects));
    }

    private static ReasonCode Planned(int id, string code, string description) => new()
    {
        Id = id,
        Code = code,
        Description = description,
        IsPlanned = true,
        SixBigLossCategory = null
    };

    private static ReasonCode Unplanned(int id, string code, string description, LossCategory category) => new()
    {
        Id = id,
        Code = code,
        Description = description,
        IsPlanned = false,
        SixBigLossCategory = category
    };

    /// <summary>
    /// A three-shift weekday pattern. Note these are seeded against LINE-A only — see the
    /// note in the README about line B having no schedule.
    /// </summary>
    private static void SeedShifts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shift>().HasData(
            new Shift
            {
                Id = Shifts.Morning,
                LineId = Lines.A,
                Code = "A",
                Name = "Morning",
                StartLocal = new TimeOnly(6, 0),
                Duration = TimeSpan.FromHours(8),
                Days = WeekDays.Weekdays
            },
            new Shift
            {
                Id = Shifts.Afternoon,
                LineId = Lines.A,
                Code = "B",
                Name = "Afternoon",
                StartLocal = new TimeOnly(14, 0),
                Duration = TimeSpan.FromHours(8),
                Days = WeekDays.Weekdays
            },
            new Shift
            {
                Id = Shifts.Night,
                LineId = Lines.A,
                Code = "C",
                Name = "Night",
                StartLocal = new TimeOnly(22, 0),
                Duration = TimeSpan.FromHours(8),
                Days = WeekDays.Weekdays
            });
    }

    /// <summary>
    /// One 30-minute meal break per shift, two hours in. This is what makes Planned
    /// Production Time differ from shift length in the seeded data — without it every
    /// seeded shift would be a full 480 minutes and the distinction would never be
    /// exercised.
    /// </summary>
    private static void SeedPlannedDowntime(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlannedDowntime>().HasData(
            MealBreak(1, Shifts.Morning, new TimeOnly(10, 0)),
            MealBreak(2, Shifts.Afternoon, new TimeOnly(18, 0)),
            MealBreak(3, Shifts.Night, new TimeOnly(2, 0)));
    }

    private static PlannedDowntime MealBreak(int id, int shiftId, TimeOnly start) => new()
    {
        Id = id,
        ShiftId = shiftId,
        MachineId = null,
        ReasonCodeId = Reasons.Break,
        StartLocal = start,
        Duration = TimeSpan.FromMinutes(30),
        Days = WeekDays.Weekdays,
        EffectiveFrom = null,
        EffectiveTo = null
    };
}
