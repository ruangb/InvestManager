using System;
using System.Collections.Generic;

namespace InvestManager
{
    public static class ToolKit
    {
        public static IList<string> GetPastYears()
        {
            IList<string> years = new List<string>();

            string year = string.Empty;

            for (int i = 0; year != "2010"; i++)
            {
                year = DateTime.Now.AddYears(-i).Year.ToString();

                years.Add(year);
            }

            return years;
        }
    }
}
