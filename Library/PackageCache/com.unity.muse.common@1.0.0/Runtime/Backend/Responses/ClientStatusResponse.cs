using System;
using System.Globalization;

namespace Unity.Muse.Common
{
    [Serializable]
    class ClientStatusResponse : Response
    {
        public string status;
        public string obsolete_date;
        public string msg;

        public bool IsDeprecated => status == "Deprecated" && (string.IsNullOrEmpty(obsolete_date) || DateTime.Now >= ObsoleteDate);
        public bool WillBeDeprecated => status == "Deprecated" && DateTime.Now < ObsoleteDate;
        public bool IsLatest => status == "Latest" || string.IsNullOrEmpty(status);
        public bool NeedsUpdate => status == "Update";
        public DateTime ObsoleteDate => Convert.ToDateTime(obsolete_date, new CultureInfo("en-US")).ToLocalTime();
    }
}
