//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Stork_Future_TaoLi.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class CC_TAOLI_TABLE
    {
        public System.Guid ID { get; set; }
        public string CC_CODE { get; set; }
        public string CC_TYPE { get; set; }
        public string CC_DIRECTION { get; set; }
        public Nullable<int> CC_AMOUNT { get; set; }
        public Nullable<double> CC_BUY_PRICE { get; set; }
        public string CC_USER { get; set; }
        public Nullable<int> CC_OFFSETFLAG { get; set; }
    }
}
