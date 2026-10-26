using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThuTasty.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdministratorModelSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove model seed definitions while preserving existing Identity data.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve existing Identity data when reverting this migration.
        }
    }
}
