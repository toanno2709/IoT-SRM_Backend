using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackend.BusinessObjects.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__760965CCE3CEA0C6", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "Rubrics",
                columns: table => new
                {
                    rubric_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    criteria_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    max_score = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Rubrics__A1FB3B3A54CA5B29", x => x.rubric_id);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    semester_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    year = table.Column<int>(type: "int", nullable: true),
                    term = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Semester__CBC81B01BEC31556", x => x.semester_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    role_id = table.Column<int>(type: "int", nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__B9BE370F77954EFA", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id");
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    announcement_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    admin_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    target_audience = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Announce__C640A82D80ABE06F", x => x.announcement_id);
                    table.ForeignKey(
                        name: "FK_Announcements_Admin",
                        column: x => x.admin_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    class_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    instructor_id = table.Column<int>(type: "int", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    semester_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Classes__FDF47986C6BBE5E5", x => x.class_id);
                    table.ForeignKey(
                        name: "FK_Classes_Instructor",
                        column: x => x.instructor_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Classes_Semester",
                        column: x => x.semester_id,
                        principalTable: "Semesters",
                        principalColumn: "semester_id");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    message_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sender_id = table.Column<int>(type: "int", nullable: true),
                    receiver_id = table.Column<int>(type: "int", nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sent_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Messages__0BBF6EE61FA26B94", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_Messages_Receiver",
                        column: x => x.receiver_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Messages_Sender",
                        column: x => x.sender_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__E059842F65AEF100", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_Notifications_User",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Class_Enrollments",
                columns: table => new
                {
                    enrollment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_id = table.Column<int>(type: "int", nullable: true),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    enrolled_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Class_En__6D24AA7A09B7DB62", x => x.enrollment_id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Class",
                        column: x => x.class_id,
                        principalTable: "Classes",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK_Enrollments_Student",
                        column: x => x.student_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    class_id = table.Column<int>(type: "int", nullable: true),
                    leader_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Projects__BC799E1F4515E28E", x => x.project_id);
                    table.ForeignKey(
                        name: "FK_Projects_Class",
                        column: x => x.class_id,
                        principalTable: "Classes",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK_Projects_Leader",
                        column: x => x.leader_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Rubric_Weights",
                columns: table => new
                {
                    weight_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    rubric_id = table.Column<int>(type: "int", nullable: false),
                    weight_ratio = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Rubric_W__453932ACE56F44E9", x => x.weight_id);
                    table.ForeignKey(
                        name: "FK_RubricWeights_Class",
                        column: x => x.class_id,
                        principalTable: "Classes",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK_RubricWeights_Rubric",
                        column: x => x.rubric_id,
                        principalTable: "Rubrics",
                        principalColumn: "rubric_id");
                });

            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    evaluation_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    instructor_id = table.Column<int>(type: "int", nullable: true),
                    total_score = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Evaluati__827C592D4FC39243", x => x.evaluation_id);
                    table.ForeignKey(
                        name: "FK_Evaluations_Instructor",
                        column: x => x.instructor_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Evaluations_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "Hall_of_Fame",
                columns: table => new
                {
                    hof_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    nominated_by = table.Column<int>(type: "int", nullable: true),
                    nominated_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    semester_id = table.Column<int>(type: "int", nullable: true),
                    rank = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Hall_of___A7FA0EFE9BDCBBC9", x => x.hof_id);
                    table.ForeignKey(
                        name: "FK_HOF_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_HOF_Semester",
                        column: x => x.semester_id,
                        principalTable: "Semesters",
                        principalColumn: "semester_id");
                });

            migrationBuilder.CreateTable(
                name: "Project_Assets",
                columns: table => new
                {
                    asset_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    asset_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    file_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    video_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mime_type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    file_ext = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    is_primary = table.Column<bool>(type: "bit", nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: true),
                    uploaded_by = table.Column<int>(type: "int", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    visibility = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Project___D28B561D5D4D7819", x => x.asset_id);
                    table.ForeignKey(
                        name: "FK_Project_Assets_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "Project_Members",
                columns: table => new
                {
                    member_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    role_in_project = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Project___B29B85342BF23F4F", x => x.member_id);
                    table.ForeignKey(
                        name: "FK_Project_Members_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_Project_Members_User",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Project_Milestones",
                columns: table => new
                {
                    milestone_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Project___67592EB7BCF1E706", x => x.milestone_id);
                    table.ForeignKey(
                        name: "FK_Project_Milestones_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "Project_Submissions",
                columns: table => new
                {
                    submission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    submitted_by = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Project___9B53559522FF4404", x => x.submission_id);
                    table.ForeignKey(
                        name: "FK_Submissions_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_Submissions_User",
                        column: x => x.submitted_by,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "sensors",
                columns: table => new
                {
                    sensor_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sensors__1A8E906028A273BE", x => x.sensor_id);
                    table.ForeignKey(
                        name: "FK_sensors_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "Evaluation_Details",
                columns: table => new
                {
                    detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    evaluation_id = table.Column<int>(type: "int", nullable: true),
                    rubric_id = table.Column<int>(type: "int", nullable: true),
                    score = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Evaluati__38E9A224263B5411", x => x.detail_id);
                    table.ForeignKey(
                        name: "FK_EvalDetails_Evaluation",
                        column: x => x.evaluation_id,
                        principalTable: "Evaluations",
                        principalColumn: "evaluation_id");
                    table.ForeignKey(
                        name: "FK_EvalDetails_Rubric",
                        column: x => x.rubric_id,
                        principalTable: "Rubrics",
                        principalColumn: "rubric_id");
                });

            migrationBuilder.CreateTable(
                name: "Project_Approval_History",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    submission_id = table.Column<int>(type: "int", nullable: false),
                    reviewer_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    acted_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Project___096AA2E9E3530770", x => x.history_id);
                    table.ForeignKey(
                        name: "FK_ApprovalHistory_Reviewer",
                        column: x => x.reviewer_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_ApprovalHistory_Submission",
                        column: x => x.submission_id,
                        principalTable: "Project_Submissions",
                        principalColumn: "submission_id");
                });

            migrationBuilder.CreateTable(
                name: "Live_Demo",
                columns: table => new
                {
                    demo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    protocol = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    demo_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ended_at = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    sensor_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Live_Dem__A77EA3F0ACFB453A", x => x.demo_id);
                    table.ForeignKey(
                        name: "FK_LiveDemo_Project",
                        column: x => x.project_id,
                        principalTable: "Projects",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_LiveDemo_Sensor",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "sensor_id");
                });

            migrationBuilder.CreateTable(
                name: "sensor_data",
                columns: table => new
                {
                    data_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sensor_id = table.Column<int>(type: "int", nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    value = table.Column<double>(type: "float", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    raw_payload = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sensor_d__F5A76B3B81CAD6A5", x => x.data_id);
                    table.ForeignKey(
                        name: "FK_sensor_data_Sensor",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "sensor_id");
                });

            migrationBuilder.CreateTable(
                name: "Live_Demo_Sensors",
                columns: table => new
                {
                    lds_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    demo_id = table.Column<int>(type: "int", nullable: false),
                    sensor_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Live_Dem__A3A7250B504BC41A", x => x.lds_id);
                    table.ForeignKey(
                        name: "FK_LDS_Demo",
                        column: x => x.demo_id,
                        principalTable: "Live_Demo",
                        principalColumn: "demo_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LDS_Sensor",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "sensor_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_admin_id",
                table: "Announcements",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_Class_Enrollments_student_id",
                table: "Class_Enrollments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "uq_class_student",
                table: "Class_Enrollments",
                columns: new[] { "class_id", "student_id" },
                unique: true,
                filter: "[class_id] IS NOT NULL AND [student_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_instructor_id",
                table: "Classes",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_semester_id",
                table: "Classes",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluation_Details_rubric_id",
                table: "Evaluation_Details",
                column: "rubric_id");

            migrationBuilder.CreateIndex(
                name: "uq_eval_rubric",
                table: "Evaluation_Details",
                columns: new[] { "evaluation_id", "rubric_id" },
                unique: true,
                filter: "[evaluation_id] IS NOT NULL AND [rubric_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_instructor_id",
                table: "Evaluations",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_project_id",
                table: "Evaluations",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_Hall_of_Fame_semester_id",
                table: "Hall_of_Fame",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "uq_project_semester",
                table: "Hall_of_Fame",
                columns: new[] { "project_id", "semester_id" },
                unique: true,
                filter: "[project_id] IS NOT NULL AND [semester_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Live_Demo_project_id",
                table: "Live_Demo",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_Live_Demo_sensor_id",
                table: "Live_Demo",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Live_Demo_Sensors_sensor_id",
                table: "Live_Demo_Sensors",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "uq_demo_sensor",
                table: "Live_Demo_Sensors",
                columns: new[] { "demo_id", "sensor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_receiver_id",
                table: "Messages",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_sender_id",
                table: "Messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_user_id",
                table: "Notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Project_Approval_History_reviewer_id",
                table: "Project_Approval_History",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "IX_Project_Approval_History_submission_id",
                table: "Project_Approval_History",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "idx_assets_project",
                table: "Project_Assets",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_assets_type",
                table: "Project_Assets",
                columns: new[] { "asset_type", "visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_Project_Members_user_id",
                table: "Project_Members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_project_user",
                table: "Project_Members",
                columns: new[] { "project_id", "user_id" },
                unique: true,
                filter: "[project_id] IS NOT NULL AND [user_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Project_Milestones_Project",
                table: "Project_Milestones",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_Project_Submissions_project_id",
                table: "Project_Submissions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_Project_Submissions_submitted_by",
                table: "Project_Submissions",
                column: "submitted_by");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_class_id",
                table: "Projects",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_leader_id",
                table: "Projects",
                column: "leader_id");

            migrationBuilder.CreateIndex(
                name: "IX_Rubric_Weights_rubric_id",
                table: "Rubric_Weights",
                column: "rubric_id");

            migrationBuilder.CreateIndex(
                name: "uq_class_rubric",
                table: "Rubric_Weights",
                columns: new[] { "class_id", "rubric_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Semester__357D4CF91CF41FDF",
                table: "Semesters",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sensor_ts",
                table: "sensor_data",
                columns: new[] { "sensor_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_sensors_project_id",
                table: "sensors",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_role_id",
                table: "Users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__AB6E6164D0B4B9B9",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "Class_Enrollments");

            migrationBuilder.DropTable(
                name: "Evaluation_Details");

            migrationBuilder.DropTable(
                name: "Hall_of_Fame");

            migrationBuilder.DropTable(
                name: "Live_Demo_Sensors");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Project_Approval_History");

            migrationBuilder.DropTable(
                name: "Project_Assets");

            migrationBuilder.DropTable(
                name: "Project_Members");

            migrationBuilder.DropTable(
                name: "Project_Milestones");

            migrationBuilder.DropTable(
                name: "Rubric_Weights");

            migrationBuilder.DropTable(
                name: "sensor_data");

            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "Live_Demo");

            migrationBuilder.DropTable(
                name: "Project_Submissions");

            migrationBuilder.DropTable(
                name: "Rubrics");

            migrationBuilder.DropTable(
                name: "sensors");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
