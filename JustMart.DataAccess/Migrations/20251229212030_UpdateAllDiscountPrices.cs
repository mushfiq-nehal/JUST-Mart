using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustMart.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAllDiscountPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all products to set DiscountPrice = ListPrice where DiscountPrice is 0
            migrationBuilder.Sql(
                "UPDATE Products SET DiscountPrice = ListPrice WHERE DiscountPrice = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
