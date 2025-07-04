﻿namespace CarInsuranceSales
{
    public class Config
    {
        public string BotToken { get; set; }
        public Messages Messages { get; set; }
        public Mindee Mindee { get; set; }
        public OpenRouterAPI OpenRouterAPI { get; set; }
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
        public string GeneratedInsurancePolicyDocumentMessage { get; set; }
        public string ErrorMessage { get; set; }
        public string WrongMessage { get; set; }
    }

    public class Mindee
    {
        public string ApiKey { get; set; }
        public string AccountName { get; set; }
        public string EndpointName { get; set; }
        public string Version { get; set; }
    }

    public class OpenRouterAPI
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public string BaseAdress { get; set; }
        public string ApiKey { get; set; }
    }
}