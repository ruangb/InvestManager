﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestManager.Models
{
    public class Operation
    {
        public int Id { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Asset{ get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Value{ get; set; }

        public int Quantity { get; set; }

        public DateTime Date { get; set; }

        [Column(TypeName = "varchar(6)")]
        public string Type { get; set; }

        public Operation()
        {
        }

        public Operation(int id, string asset, decimal value, int quantity, DateTime date, string type)
        {
            Id = id;
            Asset = asset;
            Value = value;
            Quantity = quantity;
            Date = date;
            Type = type;
        }
    }
}
