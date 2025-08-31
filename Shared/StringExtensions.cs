using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class StringExtensions
    {
        public static string? ToShort(this string? str, int n = 4) 
        {
            var rlen = str?.Length ?? 0;
            return rlen > 40 ? $"{str?.Substring(0, n)}...{str?.Substring(rlen - 4)}" : str;
        }
    }
}
