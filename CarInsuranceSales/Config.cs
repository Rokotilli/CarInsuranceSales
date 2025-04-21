namespace CarInsuranceSales
{
    public class Config
    {
        public string BotToken { get; set; }
        public Messages Messages { get; set; }
    }

    public class Messages
    {
        public string StartMessage { get; set; }
        public string IncorrectPhotoMessagge { get; set; }
        public string PassportShouldBeSentInOneMessage { get; set; }
        public string TechnicalPassportShouldBeSentInOneMessage { get; set; }
        public string PassportSubmittedMessage { get; set; }
        public string DataSubmittedMessage { get; set; }
        public string DataRejectedMessage { get; set; }
        public string CostDisagreedMessage { get; set; }
        public string WeProcessedYourPhotoMessage { get; set; }
        public string WeSavedYourDataMessage { get; set; }
    }
}