//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace My_Sync
{
    using System;
    using System.Collections.Generic;
    
    public partial class History
    {
        public long id { get; set; }
        public string timestamp { get; set; }
        public string eventType { get; set; }
        public Nullable<decimal> isFolder { get; set; }
        public string serverName { get; set; }
        public string fileName { get; set; }
    }
}