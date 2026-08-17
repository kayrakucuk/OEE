using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Oee.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ideal_cycle_time_sec = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_ideal_cycle_time_positive", "ideal_cycle_time_sec > 0");
                });

            migrationBuilder.CreateTable(
                name: "reason_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    six_big_loss_category = table.Column<int>(type: "integer", nullable: true),
                    is_planned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reason_codes", x => x.id);
                    table.CheckConstraint("ck_reason_codes_category_matches_planned_flag", "(is_planned AND six_big_loss_category IS NULL)\n              OR (NOT is_planned AND six_big_loss_category IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plant_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_lines_plants_plant_id",
                        column: x => x.plant_id,
                        principalTable: "plants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    line_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sequence_in_line = table.Column<int>(type: "integer", nullable: false),
                    is_bottleneck = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machines", x => x.id);
                    table.ForeignKey(
                        name: "fk_machines_lines_line_id",
                        column: x => x.line_id,
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    line_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    start_local = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shifts", x => x.id);
                    table.CheckConstraint("ck_shifts_duration_under_a_day", "duration > interval '0' AND duration < interval '24 hours'");
                    table.ForeignKey(
                        name: "fk_shifts_lines_line_id",
                        column: x => x.line_id,
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "planned_downtimes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shift_id = table.Column<int>(type: "integer", nullable: false),
                    machine_id = table.Column<int>(type: "integer", nullable: true),
                    reason_code_id = table.Column<int>(type: "integer", nullable: false),
                    start_local = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planned_downtimes", x => x.id);
                    table.CheckConstraint("ck_planned_downtimes_duration_positive", "duration > interval '0'");
                    table.CheckConstraint("ck_planned_downtimes_effective_window_ordered", "effective_from IS NULL OR effective_to IS NULL OR effective_from <= effective_to");
                    table.ForeignKey(
                        name: "fk_planned_downtimes_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_planned_downtimes_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalTable: "reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_planned_downtimes_shifts_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "plants",
                columns: new[] { "id", "code", "name", "time_zone_id" },
                values: new object[] { 1, "IST-01", "Istanbul Plant", "Europe/Istanbul" });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "id", "code", "ideal_cycle_time_sec", "name" },
                values: new object[,]
                {
                    { 1, "PRD-100", 1.0, "Standard Housing" },
                    { 2, "PRD-200", 1.5, "Reinforced Housing" },
                    { 3, "PRD-300", 0.75, "Compact Bracket" },
                    { 4, "PRD-400", 2.5, "Heavy-Duty Bracket" }
                });

            migrationBuilder.InsertData(
                table: "reason_codes",
                columns: new[] { "id", "code", "description", "is_planned", "six_big_loss_category" },
                values: new object[,]
                {
                    { 1, "BRK", "Scheduled operator break", true, null },
                    { 2, "PM", "Planned preventive maintenance", true, null },
                    { 3, "NO-DEMAND", "No production scheduled", true, null },
                    { 4, "MECH", "Mechanical failure", false, 1 },
                    { 5, "ELEC", "Electrical or control failure", false, 1 },
                    { 6, "CO", "Product changeover", false, 2 },
                    { 7, "TOOL", "Tool or die change", false, 2 },
                    { 8, "STARVE", "Starved of material", false, 3 },
                    { 9, "BLOCK", "Blocked by downstream station", false, 3 },
                    { 10, "SPEED", "Running below rated speed", false, 4 },
                    { 11, "DEFECT", "In-process quality defect", false, 5 },
                    { 12, "STARTUP", "Scrap produced before process stabilised", false, 6 }
                });

            migrationBuilder.InsertData(
                table: "lines",
                columns: new[] { "id", "code", "name", "plant_id" },
                values: new object[,]
                {
                    { 1, "LINE-A", "Housing Assembly", 1 },
                    { 2, "LINE-B", "Bracket Fabrication", 1 }
                });

            migrationBuilder.InsertData(
                table: "machines",
                columns: new[] { "id", "code", "is_bottleneck", "line_id", "name", "sequence_in_line" },
                values: new object[,]
                {
                    { 1, "M-A1", false, 1, "Injection Moulder", 1 },
                    { 2, "M-A2", true, 1, "Assembly Station", 2 },
                    { 3, "M-A3", false, 1, "Carton Packer", 3 },
                    { 4, "M-B1", false, 2, "CNC Mill", 1 },
                    { 5, "M-B2", true, 2, "Robot Welder", 2 },
                    { 6, "M-B3", false, 2, "Powder Coating Booth", 3 }
                });

            migrationBuilder.InsertData(
                table: "shifts",
                columns: new[] { "id", "code", "days", "duration", "line_id", "name", "start_local" },
                values: new object[,]
                {
                    { 1, "A", 31, new TimeSpan(0, 8, 0, 0, 0), 1, "Morning", new TimeOnly(6, 0, 0) },
                    { 2, "B", 31, new TimeSpan(0, 8, 0, 0, 0), 1, "Afternoon", new TimeOnly(14, 0, 0) },
                    { 3, "C", 31, new TimeSpan(0, 8, 0, 0, 0), 1, "Night", new TimeOnly(22, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "planned_downtimes",
                columns: new[] { "id", "days", "duration", "effective_from", "effective_to", "machine_id", "reason_code_id", "shift_id", "start_local" },
                values: new object[,]
                {
                    { 1, 31, new TimeSpan(0, 0, 30, 0, 0), null, null, null, 1, 1, new TimeOnly(10, 0, 0) },
                    { 2, 31, new TimeSpan(0, 0, 30, 0, 0), null, null, null, 1, 2, new TimeOnly(18, 0, 0) },
                    { 3, 31, new TimeSpan(0, 0, 30, 0, 0), null, null, null, 1, 3, new TimeOnly(2, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_lines_plant_id_code",
                table: "lines",
                columns: new[] { "plant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_machines_line_id_code",
                table: "machines",
                columns: new[] { "line_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_machines_line_id_sequence_in_line",
                table: "machines",
                columns: new[] { "line_id", "sequence_in_line" });

            migrationBuilder.CreateIndex(
                name: "ux_machines_one_bottleneck_per_line",
                table: "machines",
                column: "line_id",
                unique: true,
                filter: "is_bottleneck");

            migrationBuilder.CreateIndex(
                name: "ix_planned_downtimes_machine_id",
                table: "planned_downtimes",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_planned_downtimes_reason_code_id",
                table: "planned_downtimes",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_planned_downtimes_shift_id",
                table: "planned_downtimes",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_plants_code",
                table: "plants",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_code",
                table: "products",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reason_codes_code",
                table: "reason_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shifts_line_id_code",
                table: "shifts",
                columns: new[] { "line_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planned_downtimes");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "reason_codes");

            migrationBuilder.DropTable(
                name: "shifts");

            migrationBuilder.DropTable(
                name: "lines");

            migrationBuilder.DropTable(
                name: "plants");
        }
    }
}
