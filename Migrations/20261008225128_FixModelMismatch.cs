using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediGuard.Migrations
{
    /// <inheritdoc />
    public partial class FixModelMismatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_batches_PharmacyId",
                table: "batches");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Prescriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "Prescriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufacturingDate",
                table: "batches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "MedicineId",
                table: "batches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "batches",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RemainingQuantity",
                table: "batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SellingPrice",
                table: "batches",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "medicines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PharmacyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    BrandName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    GenericName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RequiresPrescription = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Tablet"),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMPTZ", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medicines_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_medicines_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_medicines_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PharmacyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MfgDate = table.Column<DateTime>(type: "DATE", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "DATE", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ACTIVE"),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMPTZ", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_batches", x => x.Id);
                    table.CheckConstraint("CK_inventory_batches_quantity_non_negative", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_inventory_batches_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_batches_medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_batches_fefo_lookup",
                table: "batches",
                columns: new[] { "PharmacyId", "MedicineId", "Status", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_batches_MedicineId",
                table: "batches",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "idx_batches_fefo",
                table: "inventory_batches",
                columns: new[] { "PharmacyId", "MedicineId", "Status", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_batches_MedicineId",
                table: "inventory_batches",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "idx_medicines_barcode",
                table: "medicines",
                columns: new[] { "PharmacyId", "Barcode" });

            migrationBuilder.CreateIndex(
                name: "idx_medicines_pharmacy_id_name",
                table: "medicines",
                columns: new[] { "PharmacyId", "BrandName" });

            migrationBuilder.CreateIndex(
                name: "IX_medicines_CategoryId",
                table: "medicines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_medicines_ManufacturerId",
                table: "medicines",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_batches_medicines_MedicineId",
                table: "batches",
                column: "MedicineId",
                principalTable: "medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batches_medicines_MedicineId",
                table: "batches");

            migrationBuilder.DropTable(
                name: "inventory_batches");

            migrationBuilder.DropTable(
                name: "medicines");

            migrationBuilder.DropIndex(
                name: "idx_batches_fefo_lookup",
                table: "batches");

            migrationBuilder.DropIndex(
                name: "IX_batches_MedicineId",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "ManufacturingDate",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "MedicineId",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "SellingPrice",
                table: "batches");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_batches_PharmacyId",
                table: "batches",
                column: "PharmacyId");
        }
    }
}
