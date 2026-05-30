namespace heijunka.Models
{
    public class Item
    {
        public string Code { get; set; }
        public string[] Classifications { get; set; }
            = new string[7];

        // 안전재고
        public int SafetyStock { get; set; } = 0;

        public Item(string code)
        {
            Code = code;
        }
    }
}