using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomProvider.Model
{
    public class OpportunityHistory
    {
        public string Topic { get; set; }
        public DateTime? SnapshotDate { get; set; }

        public Guid Id { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public Entity ToEntity()
        {
            var e = new Entity("ppp_opportunityhistory");
            e["ppp_name"] = Topic;
            e["ppp_estimatedrevenue"] = EstimatedRevenue;
            e["ppp_opportunityhistoryid"] = Id;
            e["ppp_snapshotdate"] = SnapshotDate;
            return e;
        }
    }
}
