using System.Data;
using APBD7.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD7.Controllers;

[ApiController]
[Route("api/warehouse")]
public class WarehouseController : ControllerBase
{
   private readonly IConfiguration _configuration;

   public WarehouseController(IConfiguration configuration)
   {
      _configuration = configuration;
   }

   [HttpPost]
   public  async Task<IActionResult> PlaceOrder(Ware ware)
   {
      try
      {
         using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
         {
            await connection.OpenAsync();
            using (SqlCommand command = new SqlCommand())
            {
               command.Connection = connection;
               
               //Sprawdzenie czy istnieje dany magazyn
               command.CommandText = "Select 1 from Warehouse where IdWarehouse = @ware_id";
               command.Parameters.AddWithValue("@ware_id", ware.IdWarehouse);

               var reader = await command.ExecuteReaderAsync();
               if (!reader.HasRows)
               {
                  return BadRequest("No such Warehouse");
               }

               await reader.CloseAsync();
               //Sprawdzenie czy istnieje dany produkt
               command.CommandText = "Select 1 from Product where IdProduct = @product_id";
               command.Parameters.AddWithValue("@product_id", ware.IdProduct);
               reader = await command.ExecuteReaderAsync();
               if (!reader.HasRows)
               {
                  return BadRequest("No such Product");
               }

               if (ware.Amount <= 0)
               {
                  return BadRequest("Invalid ammount");
               }
               await reader.CloseAsync();

               //czy isnieje rekord w bazie 
               command.CommandText = "Select * from \"order\" where IdProduct = @product_id and amount = @amount and " +
                                     "Createdat < @date ";
               
               command.Parameters.AddWithValue("@amount", ware.Amount);
               command.Parameters.AddWithValue("@date", ware.CreatedAt);
               
               //odczytanie idorder
               reader = await command.ExecuteReaderAsync();
               var orderid = -1;
               if (!reader.HasRows)
               {
                  return BadRequest("No such order in DB" + command.CommandText);
               }
               else
               {
                  while (reader.Read())
                  {
                     orderid = reader.GetInt32(0);
                  }
               }
               await reader.CloseAsync();
               
               //Sprawdzenie czy zamowienie zostało już zrealizowane
               command.CommandText = "Select 1 from \"order\" where IdProduct = @product_id and amount = @amount and " +
                                     "idorder = @idOrder and fulfilledat is null " ;
               command.Parameters.AddWithValue("@idOrder", orderid);
               reader = await command.ExecuteReaderAsync();
               if (!reader.HasRows)
               {
                  return BadRequest("Order was Fulfilled!");
               }
               await reader.CloseAsync();
               
               //Sprawdzenie czy zamowienie jest w tabeli product_warehouse
               command.CommandText = "Select 1 from Product_warehouse where IdProduct = @product_id and amount = @amount and " +
                                     "IdWarehouse = @ware_id";
               reader = await command.ExecuteReaderAsync();
               if (reader.HasRows)
               {
                  return BadRequest("Same order was actually placed!");
               }
               await reader.CloseAsync();
               
              


               //Wykonanie zamówienia
               var date_now = DateTime.Now;
               command.CommandText = "UPDATE \"order\" SET Fulfilledat = @Date_now" +
                                     " WHERE IdProduct = @product_id and amount = @amount and idorder = @idOrder";
               command.Parameters.AddWithValue("@Date_now", date_now);
               if (await command.ExecuteNonQueryAsync() < 1)
               {
                  return BadRequest("Some DB Error");
               }

               command.CommandText = "Select * from Product where idproduct = @product_id";
               reader = await command.ExecuteReaderAsync();
               Decimal pric;
               if (reader.Read())
               {
                  pric = reader.GetDecimal(3);
                  pric *= ware.Amount;
                  command.Parameters.AddWithValue("@price", pric);
               }
               await reader.CloseAsync();

               //Wstawienie zamówienia
               command.CommandText = "INSERT INTO Product_Warehouse values " +
                                     "(@ware_id, @product_id, @idOrder, @amount, @price, @Date_now)";
               if (await command.ExecuteNonQueryAsync() < 1)
               {
                  command.CommandText = "rollback";
                  await command.ExecuteNonQueryAsync();
                  return BadRequest("Failed to insert");
               }
               
               //odczytanie klucza głównego: 
               command.CommandText =
                  "Select idproductwarehouse from Product_Warehouse where idWarehouse = @ware_id and " +
                  " idproduct = @product_id and idorder = @idOrder";
               reader = await command.ExecuteReaderAsync();
               reader.Read();
               var idProductWarehouse = reader.GetInt32(0);
               await reader.CloseAsync();
               return Ok("Order placed successfully. Primary key: " + idProductWarehouse);
            }
         }
      }
      catch (Exception e)
      {
         return StatusCode(500, "An error occured: " + e);
      }
   }
}