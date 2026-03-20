using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PageNumber",
                table: "ocr_jobs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "ocr_jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "media_store",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceId",
                table: "media_store",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_projects_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_projects_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_members",
                columns: table => new
                {
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_members", x => new { x.WorkspaceId, x.UserId });
                    table.ForeignKey(
                        name: "FK_workspace_members_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workspace_members_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_vocabularies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    WordText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContextMeaning = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_vocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_vocabularies_AspNetUsers_AddedBy",
                        column: x => x.AddedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_vocabularies_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ocr_jobs_ProjectId",
                table: "ocr_jobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_media_store_WorkspaceId",
                table: "media_store",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_project_vocabularies_AddedBy",
                table: "project_vocabularies",
                column: "AddedBy");

            migrationBuilder.CreateIndex(
                name: "IX_project_vocabularies_ProjectId",
                table: "project_vocabularies",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CreatedByUserId",
                table: "projects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_WorkspaceId",
                table: "projects",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_UserId",
                table: "workspace_members",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_media_store_workspaces_WorkspaceId",
                table: "media_store",
                column: "WorkspaceId",
                principalTable: "workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ocr_jobs_projects_ProjectId",
                table: "ocr_jobs",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_media_store_workspaces_WorkspaceId",
                table: "media_store");

            migrationBuilder.DropForeignKey(
                name: "FK_ocr_jobs_projects_ProjectId",
                table: "ocr_jobs");

            migrationBuilder.DropTable(
                name: "project_vocabularies");

            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropIndex(
                name: "IX_ocr_jobs_ProjectId",
                table: "ocr_jobs");

            migrationBuilder.DropIndex(
                name: "IX_media_store_WorkspaceId",
                table: "media_store");

            migrationBuilder.DropColumn(
                name: "PageNumber",
                table: "ocr_jobs");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ocr_jobs");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "media_store");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "media_store",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
