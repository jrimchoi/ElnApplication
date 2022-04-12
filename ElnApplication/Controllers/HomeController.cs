using ElnApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using ElnApplication.Controllers.Apis;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Http;
using Biovitrum.ElectronicLabBook.Common.Model;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ElnApplication.Controllers.Exceptions;
using System.Text.RegularExpressions;
using SessionLogin.Controllers;

namespace ElnApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _appSetting;
        private readonly Constants.ViewMode DebugViewMode = Constants.ViewMode.Control;

        public HomeController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<HomeController> logger)
        {
            this._appSetting = configuration;
            this._httpContextAccessor = httpContextAccessor;
            this._logger = logger;
        }

        #region 뷰 컨트롤
        [BaseFilter]
        public IActionResult Index()
        {
            try
            {
                //string userId = "201100539"; // 초기값으로 테스트
                //string experimentNumber = "EXP-21-AA0215";

                string userId = "201100545"; // 초기값으로 테스트
                string experimentNumber = "EXP-22-AA0556";
                

                if (HttpContext.Request.Host.Host != "localhost")
                {
                    experimentNumber = null;

                    // -------------------- GET PARAMETER 정보 GET START --------------------
                    if (HttpContext.Request.Query.ContainsKey("experimentnumber"))
                        experimentNumber = HttpContext.Request.Query["experimentnumber"];
                    // -------------------- GET PARAMETER 정보 GET END --------------------

                    // -------------------- SESSION 정보 GET START --------------------
                    string cookie = HttpContext.Request.Cookies["AuthorizationEln"];
                    ElnToken elnToken = ElnToken.Parse(cookie);

                    userId = elnToken.Username;
                }

                ViewData["ViewMode"] = !string.IsNullOrEmpty(experimentNumber) ? Constants.ViewMode.Control : Constants.ViewMode.View;
                if (HttpContext.Request.Host.Host == "localhost")
                    ViewData["ViewMode"] = DebugViewMode;
                HttpContext.SetSession(Constants.SESSION_KEY_VIEW_MODE, ViewData["ViewMode"].ToString());
                HttpContext.SetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER, experimentNumber);
                HttpContext.SetSession(Constants.SESSION_KEY_USER_ID, userId);
                // -------------------- SESSION 정보 GET END --------------------

                // -------------------- 유저별 레벨 부여 START --------------------
                List<Elncm03> lstLevel = OracleDbConn.SelectToList<Elncm03>(Constants.OracleConnectionStringElnIf,
                    "SELECT CODE, CODE_NM FROM TB_ELNCM03 WHERE CODE_ID = 'CENTER' AND ETC01 = " + "'" + HttpContext.GetSession(Constants.SESSION_KEY_USER_ID) + "'");
                if (lstLevel.Count > 0)
                {
                    if (lstLevel.Any(x => x.CODE == "X00000")) // 연구소장 코드
                        HttpContext.SetSession(Constants.SESSION_KEY_USER_LEVEL, Constants.USER_LEVEL_CTO);
                    else
                        HttpContext.SetSession(Constants.SESSION_KEY_USER_LEVEL, Constants.USER_LEVEL_CENTER_DIRECTOR);
                }
                else
                    HttpContext.SetSession(Constants.SESSION_KEY_USER_LEVEL, Constants.USER_LEVEL_RESEARCHER);
                if (userId == "201100545" || userId == "rnddt") // 레벨 예외처리
                {
                    HttpContext.SetSession(Constants.SESSION_KEY_USER_LEVEL, Constants.USER_LEVEL_CTO);
                }
                // -------------------- 유저별 레벨 부여 END --------------------

                // -------------------- 유저센터정보 GET START --------------------
                List<ElnUser> lstUserCenterCode = OracleDbConn.SelectToList<ElnUser>(Constants.OracleConnectionStringElnIf,
                    @"SELECT DEPT_CD FROM V_ELN_USER WHERE USERNAME = " + "'" + HttpContext.GetSession(Constants.SESSION_KEY_USER_ID) + "'");
                if (HttpContext.GetSession(Constants.SESSION_KEY_USER_LEVEL) != Constants.USER_LEVEL_CTO && lstUserCenterCode.Count == 0) throw new ProcessException("해당 유저의 센터정보가 없습니다.");
                HttpContext.SetSession(Constants.SESSION_KEY_USER_CENTER_CODE, lstUserCenterCode.Count > 0 ? lstUserCenterCode[0].DEPT_CD : null);
                // -------------------- 유저센터정보 GET END --------------------

                // -------------------- 센터정보 GET START --------------------
                string queryCenter = @"SELECT DEPT_CD, DEPT_NAME FROM V_ELNDEPT WHERE 1 = 1";
                if (HttpContext.GetSession(Constants.SESSION_KEY_USER_LEVEL) == Constants.USER_LEVEL_CENTER_DIRECTOR || HttpContext.GetSession(Constants.SESSION_KEY_USER_LEVEL) == Constants.USER_LEVEL_RESEARCHER)
                {
                    queryCenter += " AND DEPT_CD = " + "'" + HttpContext.GetSession(Constants.SESSION_KEY_USER_CENTER_CODE) + "'";
                }
                List<ElnDept> lstCenter = OracleDbConn.SelectToList<ElnDept>(Constants.OracleConnectionStringElnIf,
                    queryCenter);
                // -------------------- 센터정보 GET END --------------------

                ViewData["UserCenterCode"] = HttpContext.GetSession(Constants.SESSION_KEY_USER_CENTER_CODE);
                ViewData["ExperimentNumber"] = HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER);
                ViewData["UserId"] = HttpContext.GetSession(Constants.SESSION_KEY_USER_ID);
                ViewData["CenterList"] = lstCenter;
            }
            catch (ProcessException pe)
            {
                return RedirectToAction("ErrorAlert", new { title = "에러", message = pe.Message });
            }
            catch (Exception e)
            {
                return RedirectToAction("ErrorAlert", new { title = "에러", message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM Index : " + "\n" + e.Message });
            }

            return View();
        }

        [BaseFilter]
        public IActionResult AddSample()
        {
            ViewBag.title = "샘플 등록";

            if (!ChkSessionEdit)
                return RedirectToAction("ErrorAlert", new { title = "에러", message = "세션정보가 없습니다. 페이지를 새로고침해주세요." });

            try
            {
                // GET으로 받는 데이터이기 때문에 해당 연구노트가 유효한지 체크
                List<ElnDocument> lstElnDocument = OracleDbConn.SelectToList<ElnDocument>(Constants.OracleConnectionStringEln,
                     @"SELECT DOCUMENTID FROM DOCUMENT WHERE EXPERIMENTNUMBER = " + "'" + HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER) + "'");
                if(lstElnDocument.Count <= 0) throw new ProcessException("해당 연구노트의 문서가 없습니다.");

                // -------------------- 샘플번호(자동채번) GET START --------------------
                List<Elnprj06> lstElnprj06ForSampleNumber = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                selectGetSampleNumber(new Elnprj06() { CENTER_CODE = HttpContext.GetSession(Constants.SESSION_KEY_USER_CENTER_CODE) }));
                string centerAlias = _appSetting.GetValue<string>("Centers" + ":" + HttpContext.GetSession(Constants.SESSION_KEY_USER_CENTER_CODE) + ":" + "Alias");
                if(string.IsNullOrEmpty(centerAlias)) throw new ProcessException("환경설정에 센터 별칭이 없습니다.\n관리자에게 문의해주세요.");
                string sampleNumber = centerAlias + "-" + lstElnprj06ForSampleNumber[0].SAMPLE_NUMBER; // 센터별칭-00001-001
                // -------------------- 샘플번호(자동채번) GET END --------------------

                // 연구노트에 실 사용중인 시약정보 GET
                List<ReagentModel> lstOrigReagent = GetRealReagentList(HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER));

                // 연구노트에 실 사용중인 장비정보 GET
                List<EquipmentModel> lstOrigEquipment = GetRealEquipmentList(HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER));

                ViewData["UserId"] = HttpContext.GetSession(Constants.SESSION_KEY_USER_ID);
                ViewData["ExperimentNumber"] = HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER);
                ViewData["DocumentId"] = lstElnDocument[0].DOCUMENTID;
                ViewData["SampleNumber"] = sampleNumber;
                ViewData["CenterCode"] = HttpContext.GetSession(Constants.SESSION_KEY_USER_CENTER_CODE);
                ViewData["Reagent"] = lstOrigReagent;
                ViewData["Equipment"] = lstOrigEquipment;
            }
            catch(ProcessException pe)
            {
                return RedirectToAction("ErrorAlert", new { title = "에러", message = pe.Message });
            }
            catch(Exception e)
            {
                return RedirectToAction("ErrorAlert", new { title = "에러", message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM AddSample : " + "\n" + e.Message });
            }

            return View();
        }

        [BaseFilter]
        public IActionResult ModifySample(int eai_seq)
        {
            ViewBag.title = "샘플 수정";

            if (eai_seq == 0)
                return RedirectToAction("ErrorAlert", new { title = "에러", message = "필수 인자가 없습니다." });
            if (!ChkSessionEdit) 
                return RedirectToAction("ErrorAlert", new { title = "에러", message = "세션정보가 없습니다. 페이지를 새로고침해주세요." });

            try
            {
                // -------------------- 샘플 정보 GET START --------------------
                Elnprj06 elnprj06 = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                        @"SELECT EAI_SEQ, SAMPLE_NAME, SAMPLE_NUMBER, ATTRIBUTE_NUMBER, EXPERIMENT_NUMBER, CONTENT, REAGENT_DATA, EQUIPMENT_DATA, ANAL_REQ_YN FROM TB_ELNPRJ06 WHERE EAI_SEQ = " + eai_seq)[0];

                // ---------- 샘플 정보에서 시약정보 GET START ----------
                List<ReagentModel> lstSampleReagent = new List<ReagentModel>();
                if (elnprj06.REAGENT_DATA != null)
                {
                    JObject jObjReagent = JObject.Parse(elnprj06.REAGENT_DATA);
                    JArray jArrReagent = (JArray)jObjReagent["data"];

                    // 시약정보 리스트 생성
                    foreach (JObject item in jArrReagent)
                    {
                        bool check = (bool)item.GetValue("CHECK");
                        string barcode = item.GetValue("BARCODE").ToString();
                        string prodName = item.GetValue("PRODUCT_NAME").ToString();
                        decimal usage = (decimal)item.GetValue("USAGE");
                        string unit = item.GetValue("UNIT").ToString();
                        bool custom = barcode.Equals("Custom") ? true : false;
                        lstSampleReagent.Add(new ReagentModel() { BARCODE = barcode, PRODUCT_NAME = prodName, USAGE = usage, UNIT = unit, CHECK = check, CUSTOM = custom });
                    }
                }
                // -------------------- 샘플 정보 GET END --------------------

                // 연구노트에 실 사용중인 시약정보 GET
                List<ReagentModel> lstOrigReagent = GetRealReagentList(elnprj06.EXPERIMENT_NUMBER);

                // -------------------- 시약정보 병합 START --------------------
                // 조건. 양쪽 모두 추가되는 경우가 있음(연구노트에서 물질정보를 추가하거나, 샘플 등록 시 커스텀 추가)
                // 주의. 상품명 중복 가능하므로 바코드로 비교
                foreach (ReagentModel item in lstOrigReagent)
                {
                    ReagentModel findItem = lstSampleReagent.Find(x => x.BARCODE.Equals(item.BARCODE));
                    if(findItem != null) // 기존 입력 정보 삽입
                    {
                        item.USAGE = findItem.USAGE;
                        item.UNIT = findItem.UNIT;
                        item.CHECK = findItem.CHECK;
                    }
                }
                // 샘플 커스텀 데이터 삽입
                lstOrigReagent.AddRange(lstSampleReagent.FindAll(x => x.CUSTOM));
                // -------------------- 시약정보 병합 END --------------------

                // 연구노트에 실 사용중인 장비정보 GET
                List<EquipmentModel> lstOrigEquipment = GetRealEquipmentList(elnprj06.EXPERIMENT_NUMBER);

                ViewData["Object"] = elnprj06;
                ViewData["Reagent"] = lstOrigReagent;
                ViewData["Equipment"] = lstOrigEquipment;
            }
            catch(Exception e)
            {
                return RedirectToAction("ErrorAlert", new { title = "에러", message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM ModifySample : " + "\n" + e.Message });
            }
            
            return View();
        }

        [BaseFilter]
        public IActionResult CopySample(int copy_eai_seq)
        {
            ViewBag.title = "샘플 복사";

            if(copy_eai_seq == 0)
                return RedirectToAction("ErrorAlert", new { title = "에러", message = "필수 인자가 없습니다." });
            if (!ChkSessionEdit)
                return RedirectToAction("ErrorAlert", new { title = "에러", message = "세션정보가 없습니다. 페이지를 새로고침해주세요." });

            try
            {
                // GET으로 받는 데이터이기 때문에 해당 연구노트가 유효한지 체크
                List<ElnDocument> lstElnDocument = OracleDbConn.SelectToList<ElnDocument>(Constants.OracleConnectionStringEln,
                     @"SELECT DOCUMENTID FROM DOCUMENT WHERE EXPERIMENTNUMBER = " + "'" + HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER) + "'");
                if (lstElnDocument.Count <= 0) throw new ProcessException("해당 연구노트의 문서가 없습니다.");

                // -------------------- 복사 샘플 정보 GET START --------------------
                Elnprj06 elnprj06Copy = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                        @"SELECT EAI_SEQ, SAMPLE_NAME, SAMPLE_NUMBER, ATTRIBUTE_NUMBER, CENTER_CODE, EXPERIMENT_NUMBER, CONTENT, REAGENT_DATA, EQUIPMENT_DATA, ANAL_REQ_YN FROM TB_ELNPRJ06 WHERE EAI_SEQ = " + copy_eai_seq)[0];

                // ---------- 샘플 정보에서 시약정보 GET START ----------
                List<ReagentModel> lstSampleReagent = new List<ReagentModel>();
                if (elnprj06Copy.REAGENT_DATA != null)
                {
                    JObject jObjReagent = JObject.Parse(elnprj06Copy.REAGENT_DATA);
                    JArray jArrReagent = (JArray)jObjReagent["data"];

                    // 시약정보 리스트 생성
                    foreach (JObject item in jArrReagent)
                    {
                        bool check = (bool)item.GetValue("CHECK");
                        string barcode = item.GetValue("BARCODE").ToString();
                        string prodName = item.GetValue("PRODUCT_NAME").ToString();
                        decimal usage = (decimal)item.GetValue("USAGE");
                        string unit = item.GetValue("UNIT").ToString();
                        bool custom = barcode.Equals("Custom") ? true : false;
                        lstSampleReagent.Add(new ReagentModel() { BARCODE = barcode, PRODUCT_NAME = prodName, USAGE = usage, UNIT = unit, CHECK = check, CUSTOM = custom });
                    }
                }
                // -------------------- 복사 샘플 정보 GET END --------------------

                // -------------------- 샘플번호(자동채번) GET START --------------------
                string[] arrSampleNumber = elnprj06Copy.SAMPLE_NUMBER.Split("-");
                string sampleNumber = arrSampleNumber[0] + "-" + arrSampleNumber[1];
                List<Elnprj06> lstElnprj06ForSampleNumber = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                selectGetSampleCopyNumber(new Elnprj06() { SAMPLE_NUMBER = sampleNumber, CENTER_CODE = elnprj06Copy.CENTER_CODE }));
                elnprj06Copy.SAMPLE_NUMBER = arrSampleNumber[0] + "-" + arrSampleNumber[1] + "-" + lstElnprj06ForSampleNumber[0].SAMPLE_NUMBER; // 센터별칭-00001-001
                // -------------------- 샘플번호(자동채번) GET END --------------------

                // 연구노트에 실 사용중인 시약정보 GET
                List<ReagentModel> lstOrigReagent = GetRealReagentList(HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER));

                // -------------------- 시약정보 병합 START --------------------
                // 주의. 상품명 중복 가능하므로 바코드로 비교
                // 조건1. 복사 대상의 시약정보 중 체크상태인 시약 LIST
                // 조건2. 커스텀 여부 체크 : 현재 연구노트의 시약정보와 비교해서 같은 시약이 있으면 default 시약, 없으면 custom 시약
                lstSampleReagent.RemoveAll(x => !x.CHECK); // 조건1 처리
                foreach (ReagentModel item in lstSampleReagent)
                {
                    ReagentModel findItem = lstOrigReagent.Find(x => x.BARCODE.Equals(item.BARCODE));
                    item.USAGE = 0;
                    item.UNIT = string.Empty;
                    item.CUSTOM = findItem != null ? false : true; // 조건2 처리
                }
                // -------------------- 시약정보 병합 END --------------------

                // 연구노트에 실 사용중인 장비정보 GET
                List<EquipmentModel> lstOrigEquipment = GetRealEquipmentList(HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER));

                ViewData["UserId"] = HttpContext.GetSession(Constants.SESSION_KEY_USER_ID);
                ViewData["ExperimentNumber"] = HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER);
                ViewData["DocumentId"] = lstElnDocument[0].DOCUMENTID;
                ViewData["CenterCode"] = HttpContext.GetSession(Constants.SESSION_KEY_USER_CENTER_CODE);

                ViewData["Object"] = elnprj06Copy;
                ViewData["Reagent"] = lstSampleReagent;
                ViewData["Equipment"] = lstOrigEquipment;
            }
            catch (ProcessException pe)
            {
                return RedirectToAction("ErrorAlert", new { title = "에러", message = pe.Message });
            }
            catch (Exception e)
            {
                return RedirectToAction("ErrorAlert", new { title = "에러", message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM CopySample : " + "\n" + e.Message });
            }

            return View();
        }

        [BaseFilter]
        public IActionResult PartialViewSample(Elnprj06 elnprj06)
        {
            ViewData["Object"] = elnprj06;

            List<ReagentModel> lstReagent = new List<ReagentModel>();
            if (elnprj06.REAGENT_DATA != null)
            {
                JObject jObjReagent = JObject.Parse(elnprj06.REAGENT_DATA);
                JArray jArrReagent = (JArray)jObjReagent["data"];

                // 시약정보 리스트 생성
                foreach (JObject item in jArrReagent)
                {
                    string prodName = item.GetValue("PRODUCT_NAME").ToString();
                    bool check = (bool)item.GetValue("CHECK");
                    decimal usage = (decimal)item.GetValue("USAGE");
                    string unit = item.GetValue("UNIT").ToString();
                    bool custom = item.GetValue("BARCODE").ToString().Equals("Custom") ? true : false;
                    lstReagent.Add(new ReagentModel() { PRODUCT_NAME = prodName, USAGE = usage, UNIT = unit, CHECK = check, CUSTOM = custom });
                }
            }
            ViewData["ReagentList"] = lstReagent;

            return PartialView("~/Views/Home/ViewSample.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult ErrorAlert(string title, string message)
        {
            ViewData["title"] = title;
            ViewData["message"] = message;
            return View();
        }
        #endregion

        #region RestApi
        [HttpPost]
        public JsonResult RegistSample(Elnprj06 elnprj06)
        {
            try
            {
                if (elnprj06 == null) throw new Exception(ConstantsApi.MESSAGE_ERROR_PARAMETER);
                if (!ChkSessionEdit) throw new Exception(ConstantsApi.MESSAGE_ERROR_NO_SESSION);

                if (elnprj06.EAI_SEQ == 0)
                {
                    // -------------------- 샘플번호(자동채번) RECHECK START --------------------
                    if(elnprj06.SAMPLE_NUMBER.Split("-").Length <= 2) // 새로 등록
                    {
                        List<Elnprj06> lstElnprj06ForSampleNumber = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                        selectGetSampleNumber(new Elnprj06() { CENTER_CODE = elnprj06.CENTER_CODE }));
                        string centerAlias = _appSetting.GetValue<string>("Centers" + ":" + elnprj06.CENTER_CODE + ":" + "Alias");
                        if (string.IsNullOrEmpty(centerAlias)) throw new Exception("환경설정에 센터 별칭이 없습니다.\n관리자에게 문의해주세요.");
                        elnprj06.SAMPLE_NUMBER = centerAlias + "-" + lstElnprj06ForSampleNumber[0].SAMPLE_NUMBER; // 센터별칭-001-20211028
                    }
                    else // 복사
                    {
                        string[] arrSampleNumber = elnprj06.SAMPLE_NUMBER.Split("-");
                        string sampleNumber = arrSampleNumber[0] + "-" + arrSampleNumber[1];
                        List<Elnprj06> lstElnprj06ForSampleNumber = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                        selectGetSampleCopyNumber(new Elnprj06() { SAMPLE_NUMBER = sampleNumber, CENTER_CODE = elnprj06.CENTER_CODE }));
                        elnprj06.SAMPLE_NUMBER = arrSampleNumber[0] + "-" + arrSampleNumber[1] + "-" + lstElnprj06ForSampleNumber[0].SAMPLE_NUMBER; // 센터별칭-00001-001
                    }
                    // -------------------- 샘플번호(자동채번) RECHECK END --------------------

                    OracleDbConn.Update(Constants.OracleConnectionStringElnIf,
                    @"INSERT INTO TB_ELNPRJ06(SAMPLE_NUMBER, SAMPLE_NAME, ATTRIBUTE_NUMBER, CENTER_CODE, DOCUMENTID, EXPERIMENT_NUMBER, USER_ID, CONTENT, REAGENT_DATA, EQUIPMENT_DATA, ANAL_REQ_YN) 
                    VALUES(:SAMPLE_NUMBER, :SAMPLE_NAME, :ATTRIBUTE_NUMBER, :CENTER_CODE, :DOCUMENTID, :EXPERIMENT_NUMBER, :USER_ID, :CONTENT, :REAGENT_DATA, :EQUIPMENT_DATA, :ANAL_REQ_YN)"
                    , elnprj06);
                }
                else
                {
                    OracleDbConn.Update(Constants.OracleConnectionStringElnIf,
                    @"UPDATE TB_ELNPRJ06 SET ATTRIBUTE_NUMBER = :ATTRIBUTE_NUMBER, CONTENT = :CONTENT, REAGENT_DATA = :REAGENT_DATA, EQUIPMENT_DATA = :EQUIPMENT_DATA, ANAL_REQ_YN = :ANAL_REQ_YN, UPDATE_DATE = SYSDATE
                    WHERE EAI_SEQ = :EAI_SEQ"
                    , elnprj06);
                }
            }
            catch(Exception e)
            {
                return Json(new { Code = ConstantsApi.CODE_ERROR_DB_QUERY, Message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM RegistSample : " + e.Message });
            }

            return Json(new { Code = ConstantsApi.CODE_DATABASE_SUCCESS_REGIST, Message = ConstantsApi.MESSAGE_DATABASE_SUCCESS_REGIST, Etc01 = elnprj06.SAMPLE_NUMBER });
        }

        [HttpPost]
        public JsonResult DeleteSample(String eaiseqs)
        {
            try
            {
                if (eaiseqs == null) throw new Exception(ConstantsApi.MESSAGE_ERROR_PARAMETER);
                if (!ChkSessionEdit) throw new Exception(ConstantsApi.MESSAGE_ERROR_NO_SESSION);

                OracleDbConn.Update(Constants.OracleConnectionStringElnIf,
                       @"DELETE FROM TB_ELNPRJ06
                        WHERE EAI_SEQ in ("+eaiseqs+")"
                       , "");
            }
            catch(Exception e)
            {
                return Json(new { Code = ConstantsApi.CODE_ERROR_DB_QUERY, Message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM DeleteSample : " + e.Message });
            }

            return Json(new { Code = ConstantsApi.CODE_DATABASE_SUCCESS_DELETE, Message = ConstantsApi.MESSAGE_DATABASE_SUCCESS_DELETE });
        }

        [HttpGet]
        public JsonResult SearchSampleInfo(Elnprj06 elnprj06, string create_date_start = null, string create_date_end = null, string my_user_id = null, bool my_check = false, string my_experiment_number = null, Constants.ViewMode mode = Constants.ViewMode.Control)
        {
            /**
             * (중요)조회 권한에 따른 처리
             * 연구소장(레벨1)은 모든 조건 조회
             * 센터장(레벨2)은 센터가 고정되어있으므로 센터명에서 필터
             * 연구원(레벨3)은 센터가 고정되어있으므로 센터명에서 필터 + 제외된 조건 - 소속과제를 PP API에서 조회하여 해당 과제만 필터
             **/
            try
            {
                if (elnprj06 == null) throw new Exception(ConstantsApi.MESSAGE_ERROR_PARAMETER);

                string querySample = @"SELECT EAI_SEQ, SAMPLE_NUMBER, SAMPLE_NAME, ATTRIBUTE_NUMBER, CENTER_CODE, DOCUMENTID, EXPERIMENT_NUMBER, USER_ID, CONTENT, REAGENT_DATA, EQUIPMENT_DATA, ANAL_REQ_YN, CREATE_DATE, UPDATE_DATE,
                                (SELECT DEPT_NAME FROM V_ELNDEPT WHERE DEPT_CD = a.CENTER_CODE) AS CENTER_NAME, 
                                (SELECT FULLNAME FROM V_ELN_USER WHERE USERNAME = a.USER_ID) AS USER_NAME
                                FROM TB_ELNPRJ06 a";
                querySample += " WHERE 1 = 1";

                if (!string.IsNullOrEmpty(my_experiment_number)) // 연구노트번호 기준으로 조회할 때
                {
                    querySample += " AND EXPERIMENT_NUMBER = " + "'" + my_experiment_number + "'";
                }
                if (!string.IsNullOrEmpty(elnprj06.SAMPLE_NUMBER)) // 샘플번호
                {
                    querySample += " AND SAMPLE_NUMBER LIKE " + "'%" + elnprj06.SAMPLE_NUMBER + "%'";
                }
                if (!string.IsNullOrEmpty(elnprj06.CENTER_CODE)) // 센터코드
                {
                    querySample += " AND CENTER_CODE = " + "'" + elnprj06.CENTER_CODE + "'";
                }
                if (!string.IsNullOrEmpty(elnprj06.PROJECT_NAME)) // 과제명
                {
                    //string query = @"SELECT DOCUMENTID FROM DOCUMENT WHERE REGEXP_LIKE(LOWER(SUBSTR(PROJECTREFERENCES, 0, INSTR(PROJECTREFERENCES, '[') - 2)), " + "'" + elnprj06.PROJECT_NAME.ToLower() + "'" + ")";
                    string query = @"SELECT DOCUMENTID FROM DOCUMENT WHERE PROJECTREFERENCES = " + "'" + elnprj06.PROJECT_NAME + "'";
                    List<ElnDocument> lstResult = OracleDbConn.SelectToList<ElnDocument>(Constants.OracleConnectionStringEln,
                               query);
                    if (lstResult.Count > 0)
                    {
                        querySample += " AND DOCUMENTID IN (";
                        for (int i = 0; i < lstResult.Count; i++)
                        {
                            querySample += lstResult[i].DOCUMENTID;
                            if (i < lstResult.Count - 1)
                                querySample += ", ";
                        }
                        querySample += ")";
                    }
                }
                if (!string.IsNullOrEmpty(elnprj06.EXPERIMENT_NAME)) // 연구노트명
                {
                    string query = @"SELECT DOCUMENTID FROM METADATA WHERE METADATATYPEID = 0 AND LOWER(VALUE) LIKE " + "'%" + elnprj06.EXPERIMENT_NAME.ToLower() + "%'";
                    List<ElnDocument> lstResult = OracleDbConn.SelectToList<ElnDocument>(Constants.OracleConnectionStringEln,
                               query);
                    if(lstResult.Count > 0)
                    {
                        querySample += " AND DOCUMENTID IN (";
                        for (int i = 0; i < lstResult.Count; i++)
                        {
                            querySample += lstResult[i].DOCUMENTID;
                            if (i < lstResult.Count - 1)
                                querySample += ", ";
                        }
                        querySample += ")";
                    }
                    else // 검색은 있는데 데이터가 없는 경우 없게 출력되어야 하므로(하지않으면 전체 데이터가 출력됨)
                        querySample += " AND DOCUMENTID = 0";
                }
                if (!string.IsNullOrEmpty(elnprj06.USER_NAME) && !my_check) // 담당자명
                {
                    string query = @"SELECT USERNAME FROM V_ELN_USER WHERE LOWER(FULLNAME) LIKE " + "'%" + elnprj06.USER_NAME.ToLower() + "%'";
                    List<ElnUser> lstResult = OracleDbConn.SelectToList<ElnUser>(Constants.OracleConnectionStringElnIf,
                               query);
                    if (lstResult.Count > 0)
                    {
                        querySample += " AND USER_ID IN (";
                        for (int i = 0; i < lstResult.Count; i++)
                        {
                            querySample += "'" + lstResult[i].USERNAME + "'";
                            if (i < lstResult.Count - 1)
                                querySample += ", ";
                        }
                        querySample += ")";
                    }
                    else // 검색은 있는데 데이터가 없는 경우 없게 출력되어야 하므로(하지않으면 전체 데이터가 출력됨)
                        querySample += " AND USER_ID = null";
                }
                if (!string.IsNullOrEmpty(elnprj06.CONTENT))
                {
                    querySample += " AND LOWER(CONTENT) LIKE " + "'%" + elnprj06.CONTENT.ToLower() + "%'";
                }
                if (!string.IsNullOrEmpty(create_date_start) && !string.IsNullOrEmpty(create_date_end)) // 날짜 조회
                {
                    querySample += " AND TO_CHAR(CREATE_DATE, 'YYYY-MM-DD') BETWEEN " + "'" + create_date_start + "'" + " AND " + "'" + create_date_end + "'";
                }
                if (my_check && !string.IsNullOrEmpty(my_user_id))
                {
                    querySample += " AND USER_ID = " + "'" + my_user_id + "'";
                }

                querySample += " ORDER BY EAI_SEQ DESC";

                List<Elnprj06> lstGridData = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                               querySample);

                // --------------------- 그리드 추가 데이터 START --------------------
                List<Elnprj06> lstDupDocumentId = lstGridData.GroupBy(x => x.DOCUMENTID).Select(x => x.First()).ToList(); // DOCUMENTID 중복제거
                List<ElnDocument> lstDupProjectInfo = new List<ElnDocument>();
                if(lstDupDocumentId.Count > 0)
                {
                    string queryDocument = @"SELECT DOCUMENTID, SUBSTR(PROJECTREFERENCES, 0, INSTR(PROJECTREFERENCES, '[') - 2) AS PROJECT_NAME,
                                SUBSTR(PROJECTREFERENCES, INSTR(PROJECTREFERENCES, '[') + 1, (INSTR(PROJECTREFERENCES, ']') - INSTR(PROJECTREFERENCES, '[')) - 1) AS PROJECT_CODE
                                FROM DOCUMENT WHERE DOCUMENTID IN (";
                    for (int i = 0; i < lstDupDocumentId.Count; i++)
                    {
                        queryDocument += lstDupDocumentId[i].DOCUMENTID;
                        if (i < lstDupDocumentId.Count - 1)
                            queryDocument += ", ";
                    }
                    queryDocument += ")";
                    lstDupProjectInfo = OracleDbConn.SelectToList<ElnDocument>(Constants.OracleConnectionStringEln,
                                queryDocument);
                }

                // 샘플리스트의 프로젝트명 세팅
                foreach (Elnprj06 item in lstGridData)
                {
                    ElnDocument document = lstDupProjectInfo.Find(x => x.DOCUMENTID == item.DOCUMENTID);
                    item.PROJECT_NAME = document != null ? document.PROJECT_NAME : null;
                }

                // Control모드와 View모드의 조회조건이 현재까지 같지만 추후 조회 조건(ELN 적용 여부 및 SUBMISSION 적용 여부 등)이 변경될 수 있으므로 구분
                if (mode == Constants.ViewMode.Control)
                {
                    foreach (Elnprj06 item in lstDupDocumentId)
                    {
                        // ASCIICONTENT 예시 : 샘플번호 관리번호 사용시약 사용량 분석장비 샘플정보 촉매기술연구센터-001-20211123 A-0002 정보2 촉매기술연구센터-001-20211122 A-0001 정보1 
                        // ASCIICONTENT가 1개 이상 나올 수 있음(submission된 섹션 포함)
                        string query = @"SELECT a.SUBMITTED, b.ASCIICONTENT FROM SECTION a
                                        LEFT JOIN PARAGRAPH b ON b.INDEXINSECTION = 1 AND b.SECTIONID = a.SECTIONID
                                        WHERE a.DOCUMENTID = " + item.DOCUMENTID + " AND a.TITLE LIKE '샘플정보%'";
                        List<ElnSection> lstResult = OracleDbConn.SelectToList<ElnSection>(Constants.OracleConnectionStringEln,
                                   query);

                        List<Elnprj06> lstFindGridData = lstGridData.FindAll(x => x.DOCUMENTID == item.DOCUMENTID); // 부하 줄이기 위함. 같은 문서번호끼리만 비교
                        foreach (ElnSection item2 in lstResult)
                        {
                            string asciiContent = item2.ASCIICONTENT;
                            Regex pattern = new Regex(@"[a-zA-Z0-9가-힣]*-[0-9]{5}-[0-9]{3}|[a-zA-Z0-9가-힣]*-[0-9]{5}");
                            MatchCollection matchCollection = pattern.Matches(asciiContent);
                            foreach (Elnprj06 item3 in lstFindGridData)
                            {
                                if (matchCollection.Cast<Match>().Any(m => m.Value == item3.SAMPLE_NUMBER))
                                {
                                    item3.ELN_YN = "Y";
                                    item3.SUBMISSION_YN = item2.SUBMITTED == 1 ? "Y" : "N";
                                }
                            }
                        }
                    }
                    lstGridData = lstGridData.OrderByDescending(x => x.ELN_YN).ThenByDescending(x => x.SUBMISSION_YN).ToList();
                }
                if (mode == Constants.ViewMode.View)
                {
                    // 주석처리된 부분 = submission만 조회
                    //List<Elnprj06> lstNewGridData = new List<Elnprj06>();
                    foreach (Elnprj06 item in lstDupDocumentId)
                    {
                        string query = @"SELECT a.SUBMITTED, b.ASCIICONTENT FROM SECTION a
                                        LEFT JOIN PARAGRAPH b ON b.INDEXINSECTION = 1 AND b.SECTIONID = a.SECTIONID
                                        WHERE a.DOCUMENTID = " + item.DOCUMENTID + " AND a.TITLE LIKE '샘플정보%'";
                        /*string query = @"SELECT b.ASCIICONTENT FROM SECTION a
                                        LEFT JOIN PARAGRAPH b ON b.INDEXINSECTION = 1 AND b.SECTIONID = a.SECTIONID
                                    WHERE a.DOCUMENTID = " + item.DOCUMENTID + " AND a.TITLE = '샘플정보' AND a.SUBMITTED = 1";*/
                        List<ElnSection> lstResult = OracleDbConn.SelectToList<ElnSection>(Constants.OracleConnectionStringEln,
                                   query);

                        List<Elnprj06> lstFindGridData = lstGridData.FindAll(x => x.DOCUMENTID == item.DOCUMENTID); // 부하 줄이기 위함. 같은 문서번호끼리만 비교
                        
                        foreach (ElnSection item2 in lstResult)
                        {
                            string asciiContent = item2.ASCIICONTENT;
                            Regex pattern = new Regex(@"[a-zA-Z0-9가-힣]*-[0-9]{5}-[0-9]{3}|[a-zA-Z0-9가-힣]*-[0-9]{5}");
                            MatchCollection matchCollection = pattern.Matches(asciiContent);
                            foreach (Elnprj06 item3 in lstFindGridData)
                            {
                                if(matchCollection.Cast<Match>().Any(m => m.Value == item3.SAMPLE_NUMBER))
                                {
                                    item3.ELN_YN = "Y";
                                    item3.SUBMISSION_YN = item2.SUBMITTED == 1 ? "Y" : "N";
                                    // lstNewGridData.Add(item3); // if문안에 한줄만 사용
                                }
                            }
                        }
                    }
                    //lstGridData = lstNewGridData;
                    lstGridData = lstGridData.OrderByDescending(x => x.ELN_YN).ThenByDescending(x => x.SUBMISSION_YN).ToList(); // ELN 'Y'순, submittion 'Y'순으로 정렬
                }
                // --------------------- 그리드 추가 데이터 END --------------------

                return Json(new { Code = ConstantsApi.CODE_SUCESS, Message = ConstantsApi.MESSAGE_SUCCESS, Data = lstGridData });
            }
            catch(Exception e)
            {
                return Json(new { Code = ConstantsApi.CODE_ERROR_DB_QUERY, Message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM SearchSampleInfo : " + e.Message });
            }
        }

        [HttpGet]
        public JsonResult GetProjectListForCenter(Elnprj06 elnprj06)
        {
            try
            {
                if (elnprj06 == null) throw new Exception(ConstantsApi.MESSAGE_ERROR_PARAMETER);

                string querySample = "SELECT DOCUMENTID FROM TB_ELNPRJ06 WHERE 1 = 1";
                if (elnprj06.CENTER_CODE != null)
                {
                    querySample += " AND CENTER_CODE = " + "'" + elnprj06.CENTER_CODE + "'";
                }
                querySample += " GROUP BY DOCUMENTID";

                List<Elnprj06> lstSample = OracleDbConn.SelectToList<Elnprj06>(Constants.OracleConnectionStringElnIf,
                    querySample);

                List<ElnDocument> lstElnDocument = new List<ElnDocument>();
                if (lstSample.Count > 0)
                {
                    string queryDocument = @"SELECT PROJECTREFERENCES, SUBSTR(PROJECTREFERENCES, 0, INSTR(PROJECTREFERENCES, '[') - 2) AS PROJECT_NAME, 
                                    SUBSTR(PROJECTREFERENCES, INSTR(PROJECTREFERENCES, '[') + 1, (INSTR(PROJECTREFERENCES, ']') - INSTR(PROJECTREFERENCES, '[')) - 1) AS PROJECT_CODE
                                    FROM DOCUMENT WHERE DOCUMENTID IN (";
                    for (int i = 0; i < lstSample.Count; i++)
                    {
                        queryDocument += lstSample[i].DOCUMENTID;
                        if (i < lstSample.Count - 1)
                            queryDocument += ", ";
                    }
                    queryDocument += ") AND PROJECTREFERENCES IS NOT NULL GROUP BY PROJECTREFERENCES";

                    lstElnDocument = OracleDbConn.SelectToList<ElnDocument>(Constants.OracleConnectionStringEln,
                     queryDocument);
                }

                return Json(new { Code = ConstantsApi.CODE_SUCESS, Message = ConstantsApi.MESSAGE_SUCCESS, Data = lstElnDocument });
            }
            catch(Exception e)
            {
                return Json(new { Code = ConstantsApi.CODE_ERROR_DB_QUERY, Message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM GetProjectListForCenter : " + e.Message });
            }
        }

        [HttpPost]
        public JsonResult SetExportSampleData(Elnprj06Export elnprj06Export)
        {
            try
            {
                if (elnprj06Export == null) throw new Exception(ConstantsApi.MESSAGE_ERROR_PARAMETER);
                if (!ChkSessionEdit) throw new Exception(ConstantsApi.MESSAGE_ERROR_NO_SESSION);

                OracleDbConn.Update(Constants.OracleConnectionStringElnIf,
                    @"INSERT INTO TB_ELNPRJ06_EXPORT(JSON_DATA) 
                    VALUES(:JSON_DATA)"
                    , elnprj06Export);
                List<Elnprj06Export> lstSample = OracleDbConn.SelectToList<Elnprj06Export>(Constants.OracleConnectionStringElnIf,
                    "SELECT MAX(EAI_SEQ) AS EAI_SEQ FROM TB_ELNPRJ06_EXPORT");

                return Json(new { Code = ConstantsApi.CODE_SUCESS, Message = ConstantsApi.MESSAGE_SUCCESS, Data = lstSample[0].EAI_SEQ });
            }
            catch (Exception e)
            {
                return Json(new { Code = ConstantsApi.CODE_ERROR_DB_QUERY, Message = ConstantsApi.MESSAGE_ERROR_DB_QUERY + " FROM SetExportSampleData : " + e.Message });
            }
        }
        #endregion

        #region 공통 테이블 쿼리
        public string selectGetSampleNumber(Elnprj06 param) // 촉매-00001-001 형식 중 중간 number 추출
        {
            string query = @"SELECT
                            NVL(
                            LPAD(
                            MAX(SUBSTR(SAMPLE_NUMBER, INSTR(SAMPLE_NUMBER, '-', 1, 1) + 1, 5)) + 1
                            , '5', '0')
                            , '00001')
                            AS SAMPLE_NUMBER
                            FROM TB_ELNPRJ06
                            WHERE CENTER_CODE = '" + param.CENTER_CODE + "'";
            return query;
        }

        public string selectGetSampleCopyNumber(Elnprj06 param) // 촉매-00001-001 형식 중 마지막 copy number 추출
        {
            string query = @"SELECT
                            NVL(
                            LPAD(
                            MAX(SUBSTR(SAMPLE_NUMBER, INSTR(SAMPLE_NUMBER,'-', 1, 2) + 1, 3)) + 1, 
                            '3', '0')
                            , '001')
                            AS SAMPLE_NUMBER
                            FROM TB_ELNPRJ06 
                            WHERE CENTER_CODE = '" + param.CENTER_CODE + "'" + " AND " + "SAMPLE_NUMBER LIKE '" + param.SAMPLE_NUMBER +  "-%'"; // param.SAMPLE_NUMBER는 촉매-00001-001(복사)이면 촉매-00001 형식으로 만들어야 함
            return query;
        }

        public string selectGetSampleNumber2(Elnprj06 param) // 촉매기술연구센터-001-20211125 형식
        {
            string query = @"SELECT NVL2(a.NUM,
                            b.DEPT_NAME || '-' || a.NUM || '-' || TO_CHAR(SYSDATE, 'YYYYMMDD'),
                            b.DEPT_NAME || '-' || '001' || '-' || TO_CHAR(SYSDATE, 'YYYYMMDD')
                            ) AS SAMPLE_NUMBER
                           FROM (";
            string subQuery = @"SELECT LPAD(MAX(SUBSTR(
                                SUBSTR(SAMPLE_NUMBER, INSTR(SAMPLE_NUMBER,'-', 1, 1) + 1, INSTR(SAMPLE_NUMBER, '-', 1, 2) - INSTR(SAMPLE_NUMBER, '-', 1, 1) - 1)
                                , 3) + 1), '3', '0') AS NUM
                                FROM TB_ELNPRJ06 WHERE 1 = 1
                                AND TO_CHAR(SYSDATE, 'yyyymmdd') = TO_CHAR(CREATE_DATE, 'yyyymmdd')
                                AND CENTER_CODE = '" + param.CENTER_CODE + "'";
            query += subQuery;
            query += @") a
                    LEFT JOIN V_ELNDEPT b ON b.DEPT_CD = '" + param.CENTER_CODE + "'";

            return query;
        }
        #endregion

        #region 기타
        /**
         * 실제 ELN 섹션에서 사용 중인 시약정보 추출
         * ELN_IF DATABASE TB_ELNCM01 테이블은 섹션 추가 시 동기화는 가능하나, 섹션 삭제 시 동기화는 하지 않음
         * ELN DATABASE SECTION 테이블에 존재하는 SECTIONID는 실제 연구노트에 포함되어있음을 의미
         * 두 개의 테이블을 SECTIONID로 비교하여 존재여부를 파악하면 실제 사용 중인 시약정보 추출이 가능함
         * */
        public List<ReagentModel> GetRealReagentList(string experimentNumber)
        {
            // -------------------- 연구노트 시약정보 GET START --------------------
            string queryReagent = @"SELECT SECTIONID, JSON_DATA, SECTION_TYPE FROM TB_ELNCM01
                            WHERE EXPERIMENTNUMBER = " + "'" + experimentNumber + "'" + " AND SECTION_TYPE = 402";

            List<Elncm01> lstElncm01ForReagent = OracleDbConn.SelectToList<Elncm01>(Constants.OracleConnectionStringElnIf,
            queryReagent);

            List<ReagentModel> lstReagent = new List<ReagentModel>();
            if (lstElncm01ForReagent.Count > 0)
            {
                // ---------- 섹션 삭제 체크 -> DOCUMENT 테이블에서 실 사용 중인 섹션 데이터 추출(TB_ELNCM01은 삭제 동기화 안됨) START ----------
                string querySection = @"SELECT SECTIONID FROM SECTION WHERE SECTIONID IN (";
                for (int i = 0; i < lstElncm01ForReagent.Count; i++)
                {
                    querySection += lstElncm01ForReagent[i].SECTIONID;
                    if (i < lstElncm01ForReagent.Count - 1)
                        querySection += ", ";
                }
                querySection += ")";

                List<ElnSection> lstSection = OracleDbConn.SelectToList<ElnSection>(Constants.OracleConnectionStringEln,
                       querySection);
                lstElncm01ForReagent = lstElncm01ForReagent.Where(x => lstSection.Any(y => y.SECTIONID == x.SECTIONID)).ToList();
                // ---------- 섹션 삭제 체크 -> 실 세션 데이터 추출(TB_ELNCM01은 삭제 동기화 안됨) END ----------

                // ---------- 실 사용 중인 시약정보 JSON 파싱 START ----------
                foreach (Elncm01 item in lstElncm01ForReagent)
                {

                    JObject jObjReagent = JObject.Parse(lstElncm01ForReagent[0].JSON_DATA);
                    JArray jArrReagent = (JArray)jObjReagent["data"];

                    // 시약정보 리스트 생성
                    foreach (JObject item2 in jArrReagent)
                    {
                        string barcode = item2.GetValue("Barcode").ToString();
                        string prodName = item2.GetValue("ProductName").ToString();
                        lstReagent.Add(new ReagentModel() { BARCODE = barcode, PRODUCT_NAME = prodName });
                    }
                }
                // ---------- 실 사용 중인 시약정보 JSON 파싱 END ----------
                lstReagent = lstReagent.GroupBy(x => x.BARCODE).Select(x => x.First()).ToList(); // 한 연구노트 내에 여러개의 섹션이 생성될 수 있고 같은 항목으로 입력가능하므로 샘플에서는 중복 제거
            }
            // -------------------- 연구노트 시약정보 GET END --------------------

            return lstReagent;
        }

        /**
         * 실제 ELN 섹션에서 사용 중인 장비정보 추출
         * ELN_IF DATABASE TB_ELNCM01 테이블은 섹션 추가 시 동기화는 가능하나, 섹션 삭제 시 동기화는 하지 않음
         * ELN DATABASE SECTION 테이블에 존재하는 SECTIONID는 실제 연구노트에 포함되어있음을 의미
         * 두 개의 테이블을 SECTIONID로 비교하여 존재여부를 파악하면 실제 사용 중인 장비정보 추출이 가능함
         * */
        public List<EquipmentModel> GetRealEquipmentList(string experimentNumber)
        {
            // -------------------- 연구노트 장비정보 GET START --------------------
            string queryEquipment = @"SELECT SECTIONID, JSON_DATA, SECTION_TYPE FROM TB_ELNCM01
                            WHERE EXPERIMENTNUMBER = " + "'" + experimentNumber + "'" + " AND SECTION_TYPE = 401";
            List<Elncm01> lstElncm01ForEquipment = OracleDbConn.SelectToList<Elncm01>(Constants.OracleConnectionStringElnIf,
                queryEquipment);

            List<EquipmentModel> lstEquipment = new List<EquipmentModel>();
            if (lstElncm01ForEquipment.Count > 0)
            {
                // ---------- 섹션 삭제 체크 -> 실제 섹션 데이터 추출(TB_ELNCM01은 삭제 동기화 안됨) START ----------
                string querySection = @"SELECT SECTIONID FROM SECTION WHERE SECTIONID IN (";
                for (int i = 0; i < lstElncm01ForEquipment.Count; i++)
                {
                    querySection += lstElncm01ForEquipment[i].SECTIONID;
                    if (i < lstElncm01ForEquipment.Count - 1)
                        querySection += ", ";
                }
                querySection += ")";

                List<ElnSection> lstSection = OracleDbConn.SelectToList<ElnSection>(Constants.OracleConnectionStringEln,
                       querySection);
                lstElncm01ForEquipment = lstElncm01ForEquipment.Where(x => lstSection.Any(y => y.SECTIONID == x.SECTIONID)).ToList();
                // ---------- 섹션 삭제 체크 -> 실제 섹션 데이터 추출(TB_ELNCM01은 삭제 동기화 안됨) END ----------

                // ---------- 실제 사용 중인 장비정보 JSON 파싱 START ----------
                foreach (Elncm01 item in lstElncm01ForEquipment)
                {
                    JObject jObjEquipment = JObject.Parse(item.JSON_DATA);
                    JArray jArrEquipment = (JArray)jObjEquipment["data"];

                    // 장비정보 리스트 생성
                    foreach (JObject item2 in jArrEquipment)
                    {
                        lstEquipment.Add(new EquipmentModel() { ASSET_ID = item2.GetValue("ASSET_ID").ToString(), ASSET_NAME = item2.GetValue("ASSET_NAME").ToString() });
                    }
                }
                // ---------- 실제 사용 중인 장비정보 JSON 파싱 END ----------
                lstEquipment = lstEquipment.GroupBy(x => x.ASSET_ID).Select(x => x.First()).ToList(); // 한 연구노트 내에 여러개의 섹션이 생성될 수 있고 같은 항목으로 입력가능하므로 중복 제거
            }
            // -------------------- 연구노트 장비정보 GET END --------------------

            return lstEquipment;
        }

        /// <summary>
        /// 필수 세션 체크
        /// </summary>
        /// <returns></returns>
        public bool ChkSessionPublic
        {
            get
            {
                string userLevel = HttpContext.GetSession(Constants.SESSION_KEY_USER_LEVEL);
                string userId = HttpContext.GetSession(Constants.SESSION_KEY_USER_ID);
                string viewMode = HttpContext.GetSession(Constants.SESSION_KEY_VIEW_MODE);
                if (string.IsNullOrEmpty(userLevel) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(viewMode))
                    return false;
                return true;
            }
        }

        public bool ChkSessionEdit
        {
            get
            {
                string userLevel = HttpContext.GetSession(Constants.SESSION_KEY_USER_LEVEL);
                string userId = HttpContext.GetSession(Constants.SESSION_KEY_USER_ID);
                string viewMode = HttpContext.GetSession(Constants.SESSION_KEY_VIEW_MODE);
                string userCenterCode = HttpContext.GetSession(Constants.SESSION_KEY_USER_CENTER_CODE);
                string experimentNumber = HttpContext.GetSession(Constants.SESSION_KEY_EXPERIMENT_NUMBER);
                if (string.IsNullOrEmpty(userLevel) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(viewMode)
                    || string.IsNullOrEmpty(userCenterCode) || string.IsNullOrEmpty(experimentNumber))
                    return false;
                return true;
            }
        }
        #endregion
    }
}
