using System;
using System.Collections.Generic;
using System.Text;

namespace QueryAccelerator.Model
{
    public class OpportunityHistory
    {
        public string Topic { get; set; }
        public DateTime? SnapshotDate { get; set; }

        public Guid Id { get; set; }
        public decimal EstimatedRevenue { get; set; }

    }
}
