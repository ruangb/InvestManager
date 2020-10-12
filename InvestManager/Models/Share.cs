using System.ComponentModel.DataAnnotations.Schema;

namespace InvestManager.Models
{
    public class Share
    {
        public int Id { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Asset{ get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Value{ get; set; }

        public int Quantity { get; set; }

        public Share()
        {
        }

        public Share(int id, string asset, decimal value, int quantity)
        {
            Id = id;
            Asset = asset;
            Value = value;
            Quantity = quantity;
        }
    }
}
