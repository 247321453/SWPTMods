using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        private static bool IsNumber(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            char[] cs = str.ToCharArray();
            Console.WriteLine("0="+((int)'0')+", 9="+((int)'9'));
            foreach (char c in cs)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
                Console.WriteLine((int)c);
            }
            return true;
        }

        static void Main(string[] args)
        {
            Console.WriteLine(IsNumber("你好"));
            Console.WriteLine(IsNumber("1V2"));
            Console.WriteLine(IsNumber("12s34"));
            Console.WriteLine(IsNumber("122234"));
            Console.ReadKey();
        }
    }
}
