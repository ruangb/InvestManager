using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestManager.Models
{
    public class Share
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "{0} required")]
        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Ativo")]
        public string Asset { get; set; }

        [Required(ErrorMessage = "{0} required")]
        [Range(0, double.MaxValue, ErrorMessage = "{0} deve ser superior a {1}")]
        [DisplayFormat(DataFormatString = "{0:F2}")]
        [Display(Name = "Preço")]
        public double Price { get; set; }

        [Required(ErrorMessage = "{0} required")]
        [Range(0, int.MaxValue, ErrorMessage = "{0} deve ser superior a {1}")]
        [Display(Name = "Quantidade")]
        public int Quantity { get; set; }

        public Share()
        {
        }

        public Share(int id, string asset, double price, int quantity)
        {
            Id = id;
            Asset = asset;
            Price = price;
            Quantity = quantity;
        }
    }
}
