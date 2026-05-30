using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace heijunka.Models
{
    public class Order
    {
        public string ItemCode { get; set; }
        public int Qty { get; set; }
        public string[] Classifications { get; set; }
            = new string[7]; // 분류A~G

        public Order(string itemCode, int qty, string[] classifications)
        {
            ItemCode = itemCode;
            Qty = qty;
            Classifications = classifications;
        }
    }
}