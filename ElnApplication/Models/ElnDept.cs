using System;

namespace ElnApplication.Models
{
    public class ElnDept
    {
        // SELECT
        public decimal EAI_SEQ { get; set; }
        public string DEPT_CD { get; set; }
        public string DEPT_NAME { get; set; }
        public string ACTIVE { get; set; }
        public string ELN_APLY_YN { get; set; }
        public DateTime ELN_APPLY_DATE { get; set; }
        public DateTime CREATE_DATE { get; set; }
    }
}
