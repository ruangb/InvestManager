using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestManager.Models
{
    public class Operation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "{0} obrigatório")]
        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Ativo")]
        public string Asset { get; set; }

        [Required(ErrorMessage = "{0} obrigatório")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(7,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Preço")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "{0} obrigatório")]
        [Range(0, int.MaxValue, ErrorMessage = "{0} deve ser superior a {1}")]
        [Display(Name = "Quantidade")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "{0} obrigatório")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "Data")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "{0} obrigatório")]
        [Column(TypeName = "varchar(6)")]
        [Display(Name = "Tipo")]
        public string Type { get; set; }

        [Required(ErrorMessage = "{0} obrigatório")]
        [Column(TypeName ="decimal(1)")]
        public decimal Status { get; set; }

        [NotMapped]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(7,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Valor Investido")]
        public decimal InvestValue { get; set; }

        [NotMapped]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(7,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Rentabilidade R$")]
        public decimal RentabilityValue { get; set; }

        [NotMapped]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        [Column(TypeName = "decimal(7,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Rentabilidade %")]
        public decimal RentabilityPercentage { get; set; }

        [NotMapped]
        public IList<string> ListType { get; set; }

        [NotMapped]
        public IEnumerable<string> Months { get; set; }

        [NotMapped]
        [ReadOnly(true)]
        [Required(ErrorMessage = "{0} obrigatório")]
        [Display(Name = "Mês")]
        public string ReferenceMonth { get; set; }

        [NotMapped]
        public IEnumerable<string> Years { get; set; }

        [NotMapped]
        [ReadOnly(true)]
        [Required(ErrorMessage = "{0} obrigatório")]
        [Display(Name = "Ano")]
        public string ReferenceYear { get; set; }

        [NotMapped]
        public IList<Operation> Operations { get; set; }

        public Operation()
        {
        }

        public Operation(int id, string asset, decimal price, int quantity, DateTime date, string type)
        {
            Id = id;
            Asset = asset;
            Price = price;
            Quantity = quantity;
            Date = date;
            Type = type;
        }
    }
}
