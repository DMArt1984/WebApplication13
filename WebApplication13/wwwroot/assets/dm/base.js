
// Получить текст элемента по ID
function GetInfo(Id) {
    return document.getElementById(Id).innerText;
}

// Установить текст для ID
function SetInfo(Id, Value) {
    document.getElementById(Id).innerText = Value;
}

// Получить значение элемента по ID
function GetInfoValue(Id) {
    return document.getElementById(Id).value;
}

// Установить значение элемента по ID
function SetInfoValue(Id, Value) {
    document.getElementById(Id).value = Value;
}

// Показать или скрыть элемент по ID
function HideId(id) {
    document.getElementById(id).style.display = 'none'; // скрыть
}
function ShowId(id) {
    document.getElementById(id).style.display = ''; // показать
}


