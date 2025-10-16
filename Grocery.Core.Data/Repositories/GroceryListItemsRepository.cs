using Grocery.Core.Data.Helpers;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    public class GroceryListItemsRepository : DatabaseConnection, IGroceryListItemsRepository
    {
        private readonly List<GroceryListItem> groceryListItems = [];

        public GroceryListItemsRepository()
        {
            //ISO 8601 format: date.ToString("o", CultureInfo.InvariantCulture)
            CreateTable(@"CREATE TABLE IF NOT EXISTS GroceryListItems (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [ProductId] INTEGER NOT NULL,
                            [GroceryListId] INTEGER NOT NULL,
                            [Amount] INTEGER NOT NULL)");
            List<string> insertQueries = [@"INSERT OR IGNORE INTO GroceryList(ProductId, GroceryListId, Amount) VALUES(1,1,2)",
                                          @"INSERT OR IGNORE INTO GroceryList(ProductId, GroceryListId, Amount) VALUES(2,1,3)",
                                          @"INSERT OR IGNORE INTO GroceryList(ProductId, GroceryListId, Amount) VALUES(3,1,1)"];
            InsertMultipleWithTransaction(insertQueries);
            GetAll();
        }

        public List<GroceryListItem> GetAll()
        {
            groceryListItems.Clear();
            string selectQuery = "SELECT Id, ProductId, GroceryListId, Amount FROM GroceryListItems";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int productId = reader.GetInt32(1);
                    int groceryListId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    groceryListItems.Add(new(id, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return groceryListItems;
        }

        public List<GroceryListItem> GetAllOnGroceryListId(int id)
        {
            groceryListItems.Clear();
            string selectQuery = $"SELECT Id, ProductId, GroceryListId, Amount FROM GroceryListItems WHERE GroceryListId = {id}";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int itemId = reader.GetInt32(0);
                    int productId = reader.GetInt32(1);
                    int groceryListId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    groceryListItems.Add(new(itemId, groceryListId, productId, amount));
                }
            }
            return groceryListItems;    
        }

        public GroceryListItem Add(GroceryListItem item)
        {
            int recordsAffected;
            string insertQuery = $"INSERT INTO GroceryListItems(ProductId, GroceryListId, Amount) VALUES(@ProductId, @GroceryListId, @Amount) Returning RowId;";
            OpenConnection();
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("Amount", item.Amount);

                item.Id = Convert.ToInt32(command.ExecuteScalar());
            }
            CloseConnection();
            return item;
        }

        public GroceryListItem? Delete(GroceryListItem item)
        {
            string deleteQuery = $"DELETE FROM GroceryListItems WHERE Id = {item.Id};";
            OpenConnection();
            Connection.ExecuteNonQuery(deleteQuery);
            CloseConnection();
            return item;
        }

        public GroceryListItem? Get(int id)
        {
            string selectQuery = $"SELECT Id, ProductId, GroceryListId, Amount FROM GroceryListItems WHERE Id = {id}";
            GroceryListItem? listItem = null;
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int itemId = reader.GetInt32(0);
                    int productId = reader.GetInt32(1);
                    int groceryListId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    listItem = new(itemId, groceryListId, productId, amount);
                }
            }
            CloseConnection();
            return listItem;
        }

        public GroceryListItem? Update(GroceryListItem item)
        {
            int recordsAffected;
            
            string updateQuery = $"UPDATE GroceryListItems SET ProductId = @ProductId, GroceryListId = @GroceryListId, Amount = @Amount WHERE Id = {item.Id};";
            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("Amount", item.Amount);

                recordsAffected = command.ExecuteNonQuery();
            }
            CloseConnection();
            return item;
        }
    }
}
