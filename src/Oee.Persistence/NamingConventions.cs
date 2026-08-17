using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Oee.Persistence;

/// <summary>
/// Rewrites every generated database identifier to snake_case.
/// </summary>
/// <remarks>
/// PostgreSQL folds unquoted identifiers to lower case, so an EF-default
/// <c>PlannedDowntimes</c> table can only ever be reached as <c>"PlannedDowntimes"</c> —
/// quoted, every time, in every hand-written query and every psql session. Renaming once
/// here costs nothing and saves that friction forever.
/// <para>
/// Applied last in <see cref="OeeDbContext.OnModelCreating"/> so it also catches names
/// that the entity configurations set explicitly.
/// </para>
/// </remarks>
internal static class NamingConventions
{
    public static void UseSnakeCase(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            string? tableName = entity.GetTableName();
            if (tableName is not null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            foreach (IMutableProperty property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (IMutableKey key in entity.GetKeys())
            {
                string? name = key.GetName();
                if (name is not null)
                {
                    key.SetName(ToSnakeCase(name));
                }
            }

            foreach (IMutableForeignKey foreignKey in entity.GetForeignKeys())
            {
                string? name = foreignKey.GetConstraintName();
                if (name is not null)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(name));
                }
            }

            foreach (IMutableIndex index in entity.GetIndexes())
            {
                string? name = index.GetDatabaseName();
                if (name is not null)
                {
                    index.SetDatabaseName(ToSnakeCase(name));
                }
            }
        }
    }

    /// <summary>
    /// <c>PlannedDowntimes</c> becomes <c>planned_downtimes</c>; <c>IX_Lines_PlantId</c>
    /// becomes <c>ix_lines_plant_id</c>.
    /// </summary>
    private static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);

        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];

            if (current == '_')
            {
                builder.Append('_');
                continue;
            }

            bool startsNewWord =
                i > 0
                && char.IsUpper(current)
                && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));

            if (startsNewWord && builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
