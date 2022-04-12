// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// jquery modal 사용 시
// <link rel="stylesheet" href="~/lib/jquery.modal-master/css/jquery.modal.css" />
// <script src="~/lib/jquery.modal-master/js/jquery.modal.js"></script>
/*function alertModal(title, message, close = true, callbackMethod) {
	modal({
		type: 'primary',
		title: title,
		text: message,
		buttons: [{
			text: '확인', //Button Text
			val: true, //Button Value
			eKey: true, //Enter Keypress
			addClass: null, //Button Classes (btn-large | btn-small | btn-green | btn-light-green | btn-purple | btn-orange | btn-pink | btn-turquoise | btn-blue | btn-light-blue | btn-light-red | btn-red | btn-yellow | btn-white | btn-black | btn-rounded | btn-circle | btn-square | btn-disabled)
			onClick: function (argument) {
				if (callbackMethod) {
					callbackMethod();
				}
				return true;
			}
		},],
		template: '<div class="modal-box"><div class="modal-inner"><div class="modal-title">' + (close ? '<a class= "modal-close-btn" ></a >' : '') + '</div><div class="modal-text"></div><div class="modal-buttons"></div></div></div>',
		_classes: {
			box: '.modal-box',
			boxInner: ".modal-inner",
			title: '.modal-title',
			content: '.modal-text',
			buttons: '.modal-buttons',
			closebtn: '.modal-close-btn'
		}
	});
}*/

function alertModal(title, message, close, callbackMethod) {
	$.showAlert({
		title: title,
		body: message,
		textTrue: "확인",
		close: close !== undefined ? close: true,
		onDispose: function () {
			if (callbackMethod) {
				callbackMethod();
            }
        }
	});
}

function alertConfirm(title, message, btnTitleTrue, btnTitleFalse, callbackMethod) {
	$.showConfirm({
		title: title, body: message, textTrue: btnTitleTrue, textFalse: btnTitleFalse,
		onSubmit: function (result) {
			if (result) {
				if (callbackMethod) {
					callbackMethod();
				}
			}
		},
		onDispose: function () {
		}
	})
}

/*-----------------------------------------------------------------------------------
 * Method : gfn_isNull
 * parameter  value
 * Desc   : value 값이 Null 값 유무를 가져온다.
 *----------------------------------------------------------------------------------*/
function gfn_isNull(sValue) {
	if (sValue == null)
		return true;
	if (new String(sValue).valueOf() == "undefined")
		return true;
	if (sValue.length == 0)
		return true;
	return false;
}

/*-----------------------------------------------------------------------------------
* Method : gfn_comma
* param :  str
* Desc   : 콤마찍기
*----------------------------------------------------------------------------------*/
function gfn_comma(str) {

	if (gfn_isNull(str)) return '';
	str = String(str);
	return str.replace(/(\d)(?=(?:\d{3})+(?!\d))/g, '$1,');
}

function gfn_grdListCnt(cnt) {
	if (gfn_isNull(cnt)) return "";
	if (cnt == 0) return "";

	return "( " + gfn_comma(cnt) + '건 )';
}