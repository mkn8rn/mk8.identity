using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mk8.identity.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActivationDates = table.Column<string>(type: "jsonb", nullable: false),
                    DeactivationDates = table.Column<string>(type: "jsonb", nullable: false),
                    IsInGracePeriod = table.Column<bool>(type: "boolean", nullable: false),
                    GracePeriodStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GracePeriodMonthsEarned = table.Column<int>(type: "integer", nullable: false),
                    GracePeriodMonthsUsed = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContributionPeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContributionPeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidatedByMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contributions_Memberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contributions_Memberships_SubmittedByMembershipId",
                        column: x => x.SubmittedByMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contributions_Memberships_ValidatedByMembershipId",
                        column: x => x.ValidatedByMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActionRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActionCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ActionCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RelatedMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedToMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    MinimumRoleRequired = table.Column<int>(type: "integer", nullable: false),
                    GracePeriodMonth = table.Column<int>(type: "integer", nullable: true),
                    GracePeriodMonthsRemaining = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Memberships_AssignedToMembershipId",
                        column: x => x.AssignedToMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Memberships_RelatedMembershipId",
                        column: x => x.RelatedMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Privileges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotingRights = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Privileges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Privileges_Memberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatrixAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    DisabledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrivilegesId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisabledByMembershipId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatrixAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatrixAccounts_Memberships_CreatedByMembershipId",
                        column: x => x.CreatedByMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatrixAccounts_Memberships_DisabledByMembershipId",
                        column: x => x.DisabledByMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatrixAccounts_Privileges_PrivilegesId",
                        column: x => x.PrivilegesId,
                        principalTable: "Privileges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SenderMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DesiredMatrixUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HandledByMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TemporaryPassword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedMatrixAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_MatrixAccounts_CreatedMatrixAccountId",
                        column: x => x.CreatedMatrixAccountId,
                        principalTable: "MatrixAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Messages_Memberships_HandledByMembershipId",
                        column: x => x.HandledByMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Memberships_SenderMembershipId",
                        column: x => x.SenderMembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_MembershipId_Month_Year_Type",
                table: "Contributions",
                columns: new[] { "MembershipId", "Month", "Year", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_SubmittedByMembershipId",
                table: "Contributions",
                column: "SubmittedByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_ValidatedByMembershipId",
                table: "Contributions",
                column: "ValidatedByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_MatrixAccounts_AccountId",
                table: "MatrixAccounts",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatrixAccounts_CreatedByMembershipId",
                table: "MatrixAccounts",
                column: "CreatedByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_MatrixAccounts_DisabledByMembershipId",
                table: "MatrixAccounts",
                column: "DisabledByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_MatrixAccounts_PrivilegesId",
                table: "MatrixAccounts",
                column: "PrivilegesId");

            migrationBuilder.CreateIndex(
                name: "IX_MatrixAccounts_Username",
                table: "MatrixAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserId",
                table: "Memberships",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedMatrixAccountId",
                table: "Messages",
                column: "CreatedMatrixAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_HandledByMembershipId",
                table: "Messages",
                column: "HandledByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderMembershipId",
                table: "Messages",
                column: "SenderMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Type_Status",
                table: "Messages",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AssignedToMembershipId",
                table: "Notifications",
                column: "AssignedToMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_MinimumRoleRequired_IsRead",
                table: "Notifications",
                columns: new[] { "MinimumRoleRequired", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RelatedMembershipId",
                table: "Notifications",
                column: "RelatedMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Privileges_MembershipId",
                table: "Privileges",
                column: "MembershipId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contributions");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "MatrixAccounts");

            migrationBuilder.DropTable(
                name: "Privileges");

            migrationBuilder.DropTable(
                name: "Memberships");
        }
    }
}
