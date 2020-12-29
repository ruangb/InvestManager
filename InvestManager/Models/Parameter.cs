using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestManager.Models
{
    public class Parameter
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "{0} required")]
        [DisplayFormat(DataFormatString = "{0:F5}")]
        [Column(TypeName = "decimal(5,5)")]
        [Display(Name = "Taxa de Negociação")]
        public decimal TradingFee{ get; set; }

        [Required(ErrorMessage = "{0} required")]
        [DisplayFormat(DataFormatString = "{0:F4}")]
        [Column(TypeName = "decimal(4,4)")]
        [Display(Name = "Taxa de Liquidez")]
        public decimal LiquidityFee { get; set; }

        public Parameter()
        {
        }

        public Parameter(int id, decimal tradingFee, decimal liquidityFee)
        {
            Id = id;
            TradingFee = tradingFee;
            LiquidityFee = liquidityFee;
        }
    }
}
