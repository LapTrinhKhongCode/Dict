using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationB2B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'PersonalTier')
                    ALTER TABLE [AspNetUsers] ADD [PersonalTier] nvarchar(max) NOT NULL DEFAULT N'';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'PremiumExpiresAt')
                    ALTER TABLE [AspNetUsers] ADD [PremiumExpiresAt] datetime2 NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'StripeCustomerId')
                    ALTER TABLE [AspNetUsers] ADD [StripeCustomerId] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'StripeSubscriptionId')
                    ALTER TABLE [AspNetUsers] ADD [StripeSubscriptionId] nvarchar(max) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalTier",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PremiumExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "AspNetUsers");
        }
    }
}
