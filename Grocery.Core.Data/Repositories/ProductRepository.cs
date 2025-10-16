using System.Globalization;
using Grocery.Core.Data.Helpers;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    public class ProductRepository : DatabaseConnection, IProductRepository
    {
        private readonly List<Product> products = new();

        public ProductRepository()
        {
            CreateTable(@"
CREATE TABLE IF NOT EXISTS Products (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Name      TEXT    NOT NULL,
    Stock     INTEGER NOT NULL,
    ShelfLife TEXT    NOT NULL,         -- yyyy-MM-dd
    Price     TEXT    NOT NULL          -- decimaal als string met 2 decimalen
);");

            // Seed met volledige kolommen en THT als yyyy-MM-dd
            var inserts = new List<string>
            {
                @"INSERT OR IGNORE INTO Products(Name, Stock, ShelfLife, Price)
                  VALUES('Brood', 100, '2026-12-31', '1.50')",
                @"INSERT OR IGNORE INTO Products(Name, Stock, ShelfLife, Price)
                  VALUES('Melk', 100, '2025-12-31', '2.50')",
                @"INSERT OR IGNORE INTO Products(Name, Stock, ShelfLife, Price)
                  VALUES('Kaas', 100, '2026-06-30', '3.50')"
            };
            InsertMultipleWithTransaction(inserts);

            GetAll(); // cache vullen (optioneel)
        }

        private static string ToIsoDate(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static DateOnly ParseIsoDate(string s) => DateOnly.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string ToMoneyText(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
        private static decimal ParseMoneyText(string s) => decimal.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);

        public List<Product> GetAll()
        {
            products.Clear();
            const string sql = "SELECT Id, Name, Stock, ShelfLife, Price FROM Products";
            OpenConnection();
            using (var cmd = new SqliteCommand(sql, Connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int stock = reader.GetInt32(2);
                    string shelfStr = reader.GetString(3);
                    string priceStr = reader.GetString(4);

                    var shelfLife = ParseIsoDate(shelfStr);
                    var price = ParseMoneyText(priceStr);

                    products.Add(new Product(id, name, stock, shelfLife, price));
                }
            }
            CloseConnection();
            return products;
        }

        public Product? Get(int id)
        {
            const string sql = "SELECT Id, Name, Stock, ShelfLife, Price FROM Products WHERE Id = @Id";
            OpenConnection();
            using var cmd = new SqliteCommand(sql, Connection);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = cmd.ExecuteReader();

            Product? product = null;
            if (reader.Read())
            {
                int pid = reader.GetInt32(0);
                string name = reader.GetString(1);
                int stock = reader.GetInt32(2);
                string shelfStr = reader.GetString(3);
                string priceStr = reader.GetString(4);

                product = new Product(pid, name, stock, ParseIsoDate(shelfStr), ParseMoneyText(priceStr));
            }
            CloseConnection();
            return product;
        }

        public Product Add(Product item)
        {
            const string sql = @"
INSERT INTO Products (Name, Stock, ShelfLife, Price)
VALUES (@Name, @Stock, @ShelfLife, @Price)
RETURNING Id;";

            OpenConnection();
            using var cmd = new SqliteCommand(sql, Connection);
            cmd.Parameters.AddWithValue("@Name", item.Name);
            cmd.Parameters.AddWithValue("@Stock", item.Stock);
            cmd.Parameters.AddWithValue("@ShelfLife", ToIsoDate(item.ShelfLife));
            cmd.Parameters.AddWithValue("@Price", ToMoneyText(item.Price));

            item.Id = Convert.ToInt32(cmd.ExecuteScalar());
            CloseConnection();
            return item;
        }

        public Product? Update(Product item)
        {
            const string sql = @"
UPDATE Products
SET Name = @Name, Stock = @Stock, ShelfLife = @ShelfLife, Price = @Price
WHERE Id = @Id;";

            OpenConnection();
            using var cmd = new SqliteCommand(sql, Connection);
            cmd.Parameters.AddWithValue("@Name", item.Name);
            cmd.Parameters.AddWithValue("@Stock", item.Stock);
            cmd.Parameters.AddWithValue("@ShelfLife", ToIsoDate(item.ShelfLife));
            cmd.Parameters.AddWithValue("@Price", ToMoneyText(item.Price));
            cmd.Parameters.AddWithValue("@Id", item.Id);

            cmd.ExecuteNonQuery();
            CloseConnection();
            return item;
        }

        public Product? Delete(Product item)
        {
            const string sql = "DELETE FROM Products WHERE Id = @Id";
            OpenConnection();
            using var cmd = new SqliteCommand(sql, Connection);
            cmd.Parameters.AddWithValue("@Id", item.Id);
            cmd.ExecuteNonQuery();
            CloseConnection();
            return item;
        }
    }
}
