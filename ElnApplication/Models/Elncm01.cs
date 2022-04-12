namespace ElnApplication.Models
{
    public class Elncm01
    {
        // SELECT
        public decimal DOCUMENTID { get; set; }
        public decimal SECTIONID { get; set; }
        public string JSON_DATA { get; set; }
        public decimal SECTION_TYPE { get; set; }
        public string KEY_DATA { get; set; }
        public string EXPERIMENTNUMBER { get; set; }
        public string CREATE_DATE { get; set; }
        public string CREATOR { get; set; }
        public string MODIFIED_DATE { get; set; }
        public string MODIFIER { get; set; }
    }
}
