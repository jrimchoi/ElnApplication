using Microsoft.Extensions.Configuration;

namespace ElnApplication
{
    public class Constants
    {
        public enum ViewMode
        {
            View,
            Control,
        }
        public static string PPPort = "9943";
        public static string PPSamplePath = "/protocols/Hanwha_Project/Actions/Experiment%20add%20sample/Experiment%20add%20sample";
        public static string MsSqlConnectionString = @"Data Source=DESKTOP-CFQCI1N\MSSQL;Initial Catalog=testdb;User ID=testuser;Password=qwer1234;";
        public static string OracleConnectionStringElnIf = @"Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = 10.151.149.246)(PORT = 1521)))(CONNECT_DATA=(SID=elndev))); User Id = ELN_IF; Password = Eln2021; Connection Timeout = 3;";
        public static string OracleConnectionStringEln = @"Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = 10.151.149.246)(PORT = 1521)))(CONNECT_DATA=(SID=elndev))); User Id = eln; Password = eln123; Connection Timeout = 3;";

        public static string SESSION_KEY_USER_LEVEL = "user_level";
        public static string SESSION_KEY_USER_ID = "user_id";
        public static string SESSION_KEY_USER_CENTER_CODE = "user_center_code";
        public static string SESSION_KEY_EXPERIMENT_NUMBER = "experiment_number";
        public static string SESSION_KEY_VIEW_MODE = "view_mode";

        public static string USER_LEVEL_CTO = "1";
        public static string USER_LEVEL_CENTER_DIRECTOR = "2";
        public static string USER_LEVEL_RESEARCHER = "3";
    }

    public class ConstantsApi
    {
        public const int CODE_SUCESS = 100;
        public const int CODE_ERROR_UNKNOWN = 101;
        public const int CODE_ERROR_PARAMETER = 102;
        public const int CODE_ERROR_DB_CONNECT = 103;
        public const int CODE_ERROR_DB_QUERY = 104;

        public const int CODE_DATABASE_SUCCESS_REGIST = 201;
        public const int CODE_DATABASE_SUCCESS_DELETE = 202;

        public const string MESSAGE_SUCCESS = "성공";
        public const string MESSAGE_ERROR_UNKNOWN = "알 수 없는 에러";
        public const string MESSAGE_ERROR_PARAMETER = "인자 없음";
        public const string MESSAGE_ERROR_DB_CONNECT = "데이터베이스 연결 에러";
        public const string MESSAGE_ERROR_DB_QUERY = "데이터베이스 쿼리 에러";
        public const string MESSAGE_ERROR_NO_SESSION = "세션정보 없음";

        public const string MESSAGE_DATABASE_SUCCESS_REGIST = "성공적으로 등록되었습니다.";
        public const string MESSAGE_DATABASE_SUCCESS_DELETE = "성공적으로 삭제되었습니다.";
    }
}
