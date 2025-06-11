using System;
using System.Collections.Generic;

namespace SysAdmin.Models.SystemUrl
{
    public class SystemUrlModel
    {
        public Guid? Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public int? Apikind { get; set; }
        public int? InternetType { get; set; }
        public string Versions { get; set; }
        public string Ecrurl { get; set; }
        public string Ecrversion { get; set; }
    }

    public class SystemUrlListModel
    {
        public List<SystemUrlModel> Items { get; set; }
    }
}
