using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EKvarovi.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fault_priorities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    default_resolution_hours = table.Column<int>(type: "integer", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fault_priorities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fault_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_closed_state = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fault_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fault_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fault_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "intervention_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intervention_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "location_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "material_units",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    location_type_id = table.Column<int>(type: "integer", nullable: false),
                    contact_person = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_locations_location_types_location_type_id",
                        column: x => x.location_type_id,
                        principalTable: "location_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "materials",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    material_unit_id = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.id);
                    table.CheckConstraint("ck_materials_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "fk_materials_material_units_material_unit_id",
                        column: x => x.material_unit_id,
                        principalTable: "material_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    specialization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    home_location_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_locations_home_location_id",
                        column: x => x.home_location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fault_reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    location_id = table.Column<int>(type: "integer", nullable: false),
                    fault_type_id = table.Column<int>(type: "integer", nullable: true),
                    fault_priority_id = table.Column<int>(type: "integer", nullable: true),
                    fault_status_id = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reported_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closing_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fault_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_fault_reports_fault_priorities_fault_priority_id",
                        column: x => x.fault_priority_id,
                        principalTable: "fault_priorities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fault_reports_fault_statuses_fault_status_id",
                        column: x => x.fault_status_id,
                        principalTable: "fault_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fault_reports_fault_types_fault_type_id",
                        column: x => x.fault_type_id,
                        principalTable: "fault_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fault_reports_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fault_reports_users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fault_reports_users_reported_by_user_id",
                        column: x => x.reported_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fault_reports_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fault_report_histories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fault_report_id = table.Column<int>(type: "integer", nullable: false),
                    change_type = table.Column<short>(type: "smallint", nullable: false),
                    old_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    new_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fault_report_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_fault_report_histories_fault_reports_fault_report_id",
                        column: x => x.fault_report_id,
                        principalTable: "fault_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fault_report_histories_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fault_report_id = table.Column<int>(type: "integer", nullable: false),
                    technician_user_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    unassigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reassign_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_assignments", x => x.id);
                    table.CheckConstraint("ck_work_assignments_unassigned", "is_active = false OR unassigned_at IS NULL");
                    table.ForeignKey(
                        name: "fk_work_assignments_fault_reports_fault_report_id",
                        column: x => x.fault_report_id,
                        principalTable: "fault_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_assignments_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_assignments_users_technician_user_id",
                        column: x => x.technician_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interventions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    work_assignment_id = table.Column<int>(type: "integer", nullable: false),
                    fault_report_id = table.Column<int>(type: "integer", nullable: false),
                    technician_user_id = table.Column<int>(type: "integer", nullable: false),
                    intervention_status_id = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interventions", x => x.id);
                    table.CheckConstraint("ck_interventions_times", "finished_at IS NULL OR started_at IS NULL OR finished_at >= started_at");
                    table.ForeignKey(
                        name: "fk_interventions_fault_reports_fault_report_id",
                        column: x => x.fault_report_id,
                        principalTable: "fault_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_interventions_intervention_statuses_intervention_status_id",
                        column: x => x.intervention_status_id,
                        principalTable: "intervention_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_interventions_users_technician_user_id",
                        column: x => x.technician_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_interventions_work_assignments_work_assignment_id",
                        column: x => x.work_assignment_id,
                        principalTable: "work_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purpose = table.Column<short>(type: "smallint", nullable: false),
                    fault_report_id = table.Column<int>(type: "integer", nullable: true),
                    intervention_id = table.Column<int>(type: "integer", nullable: true),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stored_file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    relative_path = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
                    table.CheckConstraint("ck_attachments_purpose_range", "purpose BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_attachments_purpose_target", "(purpose IN (1, 3) AND fault_report_id IS NOT NULL AND intervention_id IS NULL)\r\nOR\r\n(purpose = 2 AND intervention_id IS NOT NULL AND fault_report_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_attachments_fault_reports_fault_report_id",
                        column: x => x.fault_report_id,
                        principalTable: "fault_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attachments_interventions_intervention_id",
                        column: x => x.intervention_id,
                        principalTable: "interventions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attachments_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intervention_materials",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    intervention_id = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    unit_price_snapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intervention_materials", x => x.id);
                    table.CheckConstraint("ck_intervention_materials_quantity", "quantity > 0 AND quantity <= 99999.99");
                    table.ForeignKey(
                        name: "fk_intervention_materials_interventions_intervention_id",
                        column: x => x.intervention_id,
                        principalTable: "interventions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_intervention_materials_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "fault_priorities",
                columns: new[] { "id", "color_hex", "default_resolution_hours", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, "#6c757d", 168, true, "Nizak", 1 },
                    { 2, "#0d6efd", 72, true, "Srednji", 2 },
                    { 3, "#fd7e14", 24, true, "Visok", 3 },
                    { 4, "#dc3545", 4, true, "Kritičan", 4 }
                });

            migrationBuilder.InsertData(
                table: "fault_statuses",
                columns: new[] { "id", "is_active", "is_closed_state", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, true, false, "Zaprimljeno", 1 },
                    { 2, true, false, "Pregledano", 2 },
                    { 3, true, false, "Dodijeljeno", 3 },
                    { 4, true, false, "U radu", 4 },
                    { 5, true, false, "Riješeno", 5 },
                    { 6, true, true, "Zatvoreno", 6 }
                });

            migrationBuilder.InsertData(
                table: "fault_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, true, "Elektrika", 1 },
                    { 2, true, "Voda", 2 },
                    { 3, true, "Grijanje", 3 },
                    { 4, true, "Mreža", 4 },
                    { 5, true, "Građevinski radovi", 5 },
                    { 6, true, "Ostalo", 6 }
                });

            migrationBuilder.InsertData(
                table: "intervention_statuses",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, true, "Planirana", 1 },
                    { 2, true, "U tijeku", 2 },
                    { 3, true, "Završena", 3 },
                    { 4, true, "Neuspješna", 4 }
                });

            migrationBuilder.InsertData(
                table: "location_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, true, "Upravna zgrada", 1 },
                    { 2, true, "Škola", 2 },
                    { 3, true, "Zdravstvena ustanova", 3 },
                    { 4, true, "Skladište", 4 }
                });

            migrationBuilder.InsertData(
                table: "material_units",
                columns: new[] { "id", "abbreviation", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, "kom", true, "Komad", 1 },
                    { 2, "m", true, "Metar", 2 },
                    { 3, "l", true, "Litra", 3 },
                    { 4, "kg", true, "Kilogram", 4 },
                    { 5, "pak", true, "Paket", 5 }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "description", "name" },
                values: new object[,]
                {
                    { 1, "Admin", "Pun pristup svim podacima i postavkama", "Administrator" },
                    { 2, "Manager", "Pregled svih prijava, kategorizacija, dodjela i zatvaranje", "Upravitelj" },
                    { 3, "Technician", "Rad na vlastitim nalozima i intervencijama", "Izvršitelj" },
                    { 4, "Reporter", "Prijava kvarova i pregled vlastitih prijava", "Prijavitelj" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_attachments_fault_report_id",
                table: "attachments",
                column: "fault_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_intervention_id",
                table: "attachments",
                column: "intervention_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_uploaded_by_user_id",
                table: "attachments",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fault_priorities_name",
                table: "fault_priorities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fault_report_histories_changed_by_user_id",
                table: "fault_report_histories",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_histories_report_changed",
                table: "fault_report_histories",
                columns: new[] { "fault_report_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_closed_by_user_id",
                table: "fault_reports",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_due_date",
                table: "fault_reports",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_fault_priority_id",
                table: "fault_reports",
                column: "fault_priority_id");

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_fault_status_id",
                table: "fault_reports",
                column: "fault_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_fault_type_id",
                table: "fault_reports",
                column: "fault_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_location_id",
                table: "fault_reports",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_reported_at",
                table: "fault_reports",
                column: "reported_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_reported_by_user_id",
                table: "fault_reports",
                column: "reported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_reviewed_by_user_id",
                table: "fault_reports",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_fault_reports_report_number",
                table: "fault_reports",
                column: "report_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fault_statuses_name",
                table: "fault_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fault_types_name",
                table: "fault_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_intervention_materials_material_id",
                table: "intervention_materials",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ux_intervention_materials_unique_item",
                table: "intervention_materials",
                columns: new[] { "intervention_id", "material_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_intervention_statuses_name",
                table: "intervention_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_interventions_fault_report_id",
                table: "interventions",
                column: "fault_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_interventions_intervention_status_id",
                table: "interventions",
                column: "intervention_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_interventions_started_at",
                table: "interventions",
                column: "started_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_interventions_technician_user_id",
                table: "interventions",
                column: "technician_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_interventions_work_assignment_id",
                table: "interventions",
                column: "work_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_location_types_name",
                table: "location_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_locations_location_type_id",
                table: "locations",
                column: "location_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_locations_code",
                table: "locations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_units_name",
                table: "material_units",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_materials_material_unit_id",
                table: "materials",
                column: "material_unit_id");

            migrationBuilder.CreateIndex(
                name: "ux_materials_code",
                table: "materials",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_home_location_id",
                table: "users",
                column: "home_location_id");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_assignments_assigned_by_user_id",
                table: "work_assignments",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_assignments_technician",
                table: "work_assignments",
                columns: new[] { "technician_user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_work_assignments_active_per_report",
                table: "work_assignments",
                column: "fault_report_id",
                unique: true,
                filter: "is_active AND NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: "fault_report_histories");

            migrationBuilder.DropTable(
                name: "intervention_materials");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "interventions");

            migrationBuilder.DropTable(
                name: "materials");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "intervention_statuses");

            migrationBuilder.DropTable(
                name: "work_assignments");

            migrationBuilder.DropTable(
                name: "material_units");

            migrationBuilder.DropTable(
                name: "fault_reports");

            migrationBuilder.DropTable(
                name: "fault_priorities");

            migrationBuilder.DropTable(
                name: "fault_statuses");

            migrationBuilder.DropTable(
                name: "fault_types");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "location_types");
        }
    }
}
