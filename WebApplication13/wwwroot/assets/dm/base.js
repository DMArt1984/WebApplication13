

function GetInfo(Id) {
    return document.getElementById(Id).innerText;
}

function GetInfoValue(Id) {
    return document.getElementById(Id).value;
}

function SetInfo(Id, Value) {
    document.getElementById(Id).innerText = Value;
}

// Show Hide for ID
function HideId(id) {
    document.getElementById(id).style.display = 'none'; // скрыть
}
function ShowId(id) {
    document.getElementById(id).style.display = ''; // показать
}