namespace ODSaleOrder.API.Models
{
    //public class NotificationRequestModel
    //{
    //    public string title { get; set; }
    //    public string notificationBody { get; set; }
    //    public int type { get; set; }
    //    public string deviceToken { get; set; }
    //    public string purpose { get; set; }
    //    public bool isUrgent { get; set; }
    //    public string priority { get; set; }
    //    public string navigatePath { get; set; }
    //    public string dataId { get; set; }
    //    public string syncCode { get; set; }
    //    public string status { get; set; }
    //    public string action { get; set; }
    //    public string notificationType { get; set; }
    //    public string templateData { get; set; }
    //    public string receiver { get; set; }
    //}

    public class NotificationRequestModel
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public int Type { get; set; }
        public string Purpose { get; set; }
        public bool IsUrgent { get; set; }
        public string Priority { get; set; }
        public string NavigatePath { get; set; }
        public string DataId { get; set; }
        public string SyncCode { get; set; }
        public string Status { get; set; }
        public string Action { get; set; }
        public string NotificationType { get; set; }
        public string TemplateData { get; set; }
        public string EmployeeCode { get; set; }
    }
}
