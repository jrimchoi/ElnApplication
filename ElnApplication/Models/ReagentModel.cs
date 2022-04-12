namespace ElnApplication.Models
{
    public class ReagentModel
    {
        public string BARCODE { get; set; }                 // 바코드(식별자, 시약명은 같을 수 있어서 혼동있을 수 있음)
        public string CASNO { get; set; }                   // 화학물질 고유번호
        public string PRODUCT_NAME { get; set; }            // 시약명
        public decimal USAGE { get; set; }                  // 사용량
        public string UNIT { get; set; }                    // 단위

        // 그리드 관련
        public bool CHECK { get; set; } = false;            // 체크
        public bool CUSTOM { get; set; } = false;           // 커스텀
    }
}
