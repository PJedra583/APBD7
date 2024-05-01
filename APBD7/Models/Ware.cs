using System.ComponentModel.DataAnnotations;

namespace APBD7.Models;

public class Ware
{
    public Ware(int idProduct, int idWarehouse, int amount, DateTime createdAt)
    {
        IdProduct = idProduct;
        IdWarehouse = idWarehouse;
        Amount = amount;
        CreatedAt = createdAt;
    }

    [Required]
    public int IdProduct { get; set; }
    [Required]
    public int IdWarehouse { get; set; }
    [Required]
    public int Amount { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
}