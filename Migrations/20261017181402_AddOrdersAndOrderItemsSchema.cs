using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediGuard.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndOrderItemsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_DeliverymanId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Pharmacies_PharmacyId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_Orders_OrderId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Pharmacies_PharmacyId",
                table: "Prescriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Prescriptions",
                table: "Prescriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PharmacyId",
                table: "Orders");

            migrationBuilder.RenameTable(
                name: "Prescriptions",
                newName: "prescriptions");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "orders");

            migrationBuilder.RenameIndex(
                name: "IX_Prescriptions_PharmacyId",
                table: "prescriptions",
                newName: "IX_prescriptions_PharmacyId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_DeliverymanId",
                table: "orders",
                newName: "IX_orders_DeliverymanId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "prescriptions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "PharmacyId",
                table: "prescriptions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "prescriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "prescriptions",
                type: "TIMESTAMPTZ",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "prescriptions",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "prescriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "prescriptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "jpg");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "prescriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedById",
                table: "prescriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "orders",
                type: "TIMESTAMPTZ",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "orders",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "orders",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrderStatus",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "POS");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "CASH");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "UNPAID");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "orders",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "orders",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "orders",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assigned_deliveryman_id",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_status",
                table: "orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NOT_REQUIRED");

            migrationBuilder.AddColumn<bool>(
                name: "is_b2b_order",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_pharmacy_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_prescriptions",
                table: "prescriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orders",
                table: "orders",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliverymanId = table.Column<string>(type: "text", nullable: false),
                    DeliveryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "B2C"),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "TIMESTAMPTZ", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "TIMESTAMPTZ", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deliveries_AspNetUsers_DeliverymanId",
                        column: x => x.DeliverymanId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_deliveries_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMPTZ", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_CustomerId",
                table: "prescriptions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_OrderId",
                table: "prescriptions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_VerifiedById",
                table: "prescriptions",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "idx_orders_delivery",
                table: "orders",
                columns: new[] { "assigned_deliveryman_id", "delivery_status" });

            migrationBuilder.CreateIndex(
                name: "idx_orders_order_number_unique",
                table: "orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_orders_pharmacy_date",
                table: "orders",
                columns: new[] { "PharmacyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_CustomerId",
                table: "orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_supplier_pharmacy_id",
                table: "orders",
                column: "supplier_pharmacy_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId",
                table: "orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_deliveries_order_status",
                table: "deliveries",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_DeliverymanId",
                table: "deliveries",
                column: "DeliverymanId");

            migrationBuilder.CreateIndex(
                name: "idx_orderitems_order_medicine",
                table: "order_items",
                columns: new[] { "OrderId", "MedicineId" });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_BatchId",
                table: "order_items",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_MedicineId",
                table: "order_items",
                column: "MedicineId");


            migrationBuilder.AddForeignKey(
                name: "FK_orders_AspNetUsers_CustomerId",
                table: "orders",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_AspNetUsers_DeliverymanId",
                table: "orders",
                column: "DeliverymanId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_AspNetUsers_UserId",
                table: "orders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_AspNetUsers_assigned_deliveryman_id",
                table: "orders",
                column: "assigned_deliveryman_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_Pharmacies_PharmacyId",
                table: "orders",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_Pharmacies_supplier_pharmacy_id",
                table: "orders",
                column: "supplier_pharmacy_id",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_AspNetUsers_CustomerId",
                table: "prescriptions",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_AspNetUsers_VerifiedById",
                table: "prescriptions",
                column: "VerifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_Pharmacies_PharmacyId",
                table: "prescriptions",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_orders_OrderId",
                table: "prescriptions",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_AspNetUsers_CustomerId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_AspNetUsers_DeliverymanId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_AspNetUsers_UserId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_AspNetUsers_assigned_deliveryman_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_Pharmacies_PharmacyId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_Pharmacies_supplier_pharmacy_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_AspNetUsers_CustomerId",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_AspNetUsers_VerifiedById",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_Pharmacies_PharmacyId",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_orders_OrderId",
                table: "prescriptions");

            migrationBuilder.DropTable(
                name: "deliveries");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_prescriptions",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_CustomerId",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_OrderId",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_VerifiedById",
                table: "prescriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_delivery",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_order_number_unique",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_pharmacy_date",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_CustomerId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_supplier_pharmacy_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_UserId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrderStatus",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "assigned_deliveryman_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_b2b_order",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "supplier_pharmacy_id",
                table: "orders");

            migrationBuilder.RenameTable(
                name: "prescriptions",
                newName: "Prescriptions");

            migrationBuilder.RenameTable(
                name: "orders",
                newName: "Orders");

            migrationBuilder.RenameIndex(
                name: "IX_prescriptions_PharmacyId",
                table: "Prescriptions",
                newName: "IX_Prescriptions_PharmacyId");

            migrationBuilder.RenameIndex(
                name: "IX_orders_DeliverymanId",
                table: "Orders",
                newName: "IX_Orders_DeliverymanId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<Guid>(
                name: "PharmacyId",
                table: "Prescriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "Prescriptions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Prescriptions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMPTZ",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Prescriptions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMPTZ",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Orders",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Prescriptions",
                table: "Prescriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PharmacyId",
                table: "Orders",
                column: "PharmacyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_DeliverymanId",
                table: "Orders",
                column: "DeliverymanId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Pharmacies_PharmacyId",
                table: "Orders",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_Orders_OrderId",
                table: "payments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Pharmacies_PharmacyId",
                table: "Prescriptions",
                column: "PharmacyId",
                principalTable: "Pharmacies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
