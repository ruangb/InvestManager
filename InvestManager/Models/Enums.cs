using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace InvestManager.Models
{
    public static class Enums
    {
        public enum OperationType
        {
            [Description("Compra")]
            Purchase = 1,
            [Description("Venda")]
            Sale = 2
        }

        public enum Month
        {
            [Description("Janeiro")]
            January = 1,
            [Description("Fevereiro")]
            February = 2,
            [Description("Março")]
            March = 3,
            [Description("Abril")]
            April = 4,
            [Description("Maio")]
            May = 5,
            [Description("Junho")]
            June = 6,
            [Description("Julho")]
            July = 7,
            [Description("Agosto")]
            August = 8,
            [Description("Setembro")]
            September = 9,
            [Description("Outubro")]
            October = 10,
            [Description("Novembro")]
            November = 11,
            [Description("Dezembro")]
            December = 12
        }

        public static IEnumerable<string> GetDescriptions<T>()
        {
            var attributes = typeof(T).GetMembers()
                .SelectMany(member => member.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>())
                .ToList();

            return attributes.Select(x => x.Description);
        }

        public static string GetDescription<T>(this T enumValue)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                return null;

            var description = enumValue.ToString();
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            if (fieldInfo != null)
            {
                var attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    description = ((DescriptionAttribute)attrs[0]).Description;
                }

                //var value = fieldInfo.GetValue(enumValue);
                //var number = fieldInfo.GetRawConstantValue();
            }

            return description;
        }

        public static int? GetIndexByDescription<T>(string description)
        {
            if (!typeof(T).IsEnum)
                return null;

            Array EnumValues = Enum.GetValues(typeof(T));

            foreach (var item in EnumValues)
            {
                var fieldInfo = typeof(T).GetType().GetField(item.ToString());

                if (fieldInfo != null)
                {
                    var attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (((DescriptionAttribute)attrs[0]).Description == description)
                            return (int?)fieldInfo.GetRawConstantValue();
                    }
                }
            }

            return null;
        }
    }
}
