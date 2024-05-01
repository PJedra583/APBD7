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
               reader = await command.ExecuteReaderAsync();
               if (!reader.HasRows)
               {
                  return BadRequest("No such order in DB");
               }
               await reader.CloseAsync();

               var orderid = reader.GetInt32(0);

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
               var d = DateTime.Now;
               command.CommandText = "UPDATE \"order\" SET Fullfilledat = " + d +
                                     " WHERE IdProduct = @product_id and amount = @amount and IdWarehouse = @ware_id";
               if (await command.ExecuteNonQueryAsync() < 1)
               {
                  return BadRequest("Some DB Error");
               }

               command.CommandText = "Select count(*) from Product_warehouse";
               reader = await command.ExecuteReaderAsync();
               var idProductWarehouse = reader.GetInt32(0);
               idProductWarehouse++;
               await reader.CloseAsync();

               command.CommandText = "Select * from Product where idproduct = @product_id";
               reader = await command.ExecuteReaderAsync();
               var pric = reader.GetDouble(3);
               pric *= ware.Amount;
               await reader.CloseAsync();

               //Wstawienie zamówienia
               command.CommandText = "INSERT INTO Product_Warehouse values " +
                                     "(@idProductWare, @ware_id, @product_id, @idOrder, @amount, @price, d)";
               command.Parameters.AddWithValue("@price", pric);
               command.Parameters.AddWithValue("@idProductWare", idProductWarehouse);
               command.Parameters.AddWithValue("@idOrder", orderid);

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