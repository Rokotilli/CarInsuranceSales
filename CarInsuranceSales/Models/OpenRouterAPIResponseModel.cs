namespace CarInsuranceSales.Models
{
    public class OpenRouterAPIResponseModel
    {
        public string Id { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }       
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public string Finish_Reason { get; set; }
        public string Native_Finish_Reason { get; set; }
        public string Index { get; set; }
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
