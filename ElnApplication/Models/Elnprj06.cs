using System;

namespace ElnApplication.Models
{
    public class Elnprj06
    {
        public decimal EAI_SEQ { get; set; }
        public string SAMPLE_NUMBER { get; set; }
        public string SAMPLE_NAME { get; set; }
        public string ATTRIBUTE_NUMBER { get; set; }
        public string CENTER_CODE { get; set; }
        public decimal DOCUMENTID { get; set; }
        public string EXPERIMENT_NUMBER { get; set; }
        public string USER_ID { get; set; }
        public string CONTENT { get; set; }
        public string REAGENT_DATA { get; set; }
        public string EQUIPMENT_DATA { get; set; }
        public string ANAL_REQ_YN { get; set; }
        
        public DateTime CREATE_DATE { get; set; }
        public DateTime UPDATE_DATE { get; set; }

        // 그리드 관련
        public string CENTER_NAME { get; set; }
        public string USER_NAME { get; set; }
        public string PROJECT_NAME { get; set; }
        public string EXPERIMENT_NAME { get; set; }

        public string ELN_YN { get; set; } = "N";
        public string SUBMISSION_YN { get; set; } = "N";
    }
}
