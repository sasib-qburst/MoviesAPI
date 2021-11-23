using Microsoft.EntityFrameworkCore.Migrations;

namespace MoviesAPI.Migrations
{
    public partial class AdminRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Insert into [dbo].[AspNetRoles](Id, Name , NormalizedName)
                            values('50ef1b91-554f-4ab8-9759-fc3cda5455d0','Admin','Admin')");

            migrationBuilder.Sql(@"Insert into [dbo].[AspNetRoles](Id, Name , NormalizedName)
                            values('106e6e42-3d87-41c3-b415-a8007458bc00','Actor','Actor')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"delete [dbo].[AspNetRoles]
                                where Id='50ef1b91-554f-4ab8-9759-fc3cda5455d0'");
            migrationBuilder.Sql(@"delete [dbo].[AspNetRoles]
                                where Id='106e6e42-3d87-41c3-b415-a8007458bc00'");
        }
    }
}
