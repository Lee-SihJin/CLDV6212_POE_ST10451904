namespace ABCRetailersFunction.Models
{
    public class OrderMessage
    {
       
        public string Action { get; set; }

        public string OrderId { get; set; }       
        public string CustomerId { get; set; }    
        public string ProductId { get; set; }   
        public double TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        public int Quantity { get; set; }        
        public string Status { get; set; }       
    }
}