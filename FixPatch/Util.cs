using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixPatch
{
    public static class Util
    {

        public static void AddOrUpdate<K, V>(Dictionary<K, V> dic, K k, V v)
        {
            if (dic.ContainsKey(k))
            {
                dic[k] = v;
            }
            else
            {
                dic.Add(k, v);
            }
        }
    }
}
