﻿// Получить дату и время в зоне клиента
function UTC_To_LocalTime(DT) {

    if (DT.trim() == "")
        return "";

    var year = 0
    var month = 0
    var day = 0
    var hour = 0
    var minute = 0
    var second = 0

    //alert(DT)

    var datetime = DT.split(' ')
    if (datetime.length > 0) {
        var date = datetime[0];
        var YMD = date.split('.')
        if (YMD.length == 3) {
            year = YMD[0]
            month = YMD[1]
            day = YMD[2]
        }
    }

    if (datetime.length > 1) {
        var time = datetime[1]
        var HMS = time.split(':')
        if (HMS.length == 3) {
            hour = HMS[0]
            minute = HMS[1]
            second = HMS[2]
        }
    }

    var jdatetime = new Date(Date.UTC(year, month, day, hour, minute, second))
    //jdatetime.setTime(jdatetime.getTime() + numOfHours * 60 * 60 * 1000);
    var out = `${jdatetime.getDate().toString().padStart(2, '0')}.${jdatetime.getMonth().toString().padStart(2, '0')}.${jdatetime.getFullYear()} ${jdatetime.getHours().toString().padStart(2, '0')}:${jdatetime.getMinutes().toString().padStart(2, '0')}:${jdatetime.getSeconds().toString().padStart(2, '0')}`

    //alert(out)

    return out
}

// Получения временной зоны в минутах
function get_TimezoneOffset() {
    const d = new Date();
    let diff = d.getTimezoneOffset();
    return diff;
}

// Преобразование строки из YYYY.MM.DD HH:MM:SS в 2022-01-07T08:09
function get_CalendarDateTime(dt) {
    let out = dt.replaceAll('.', '-').replaceAll(' ', 'T');
    var fortim = out.split(':');
    if (fortim.length > 2) {
        out = fortim[0] + ":" + fortim[1];
       
    }
    //alert(out);
    return out;
}

// Преобразование даты и времени js в YYYY.MM.DD HH:MM
function jsDT_To_YMDHM(jdatetime) {
    return `${jdatetime.getFullYear().toString().padStart(2, '0')}.${(jdatetime.getMonth()+1).toString().padStart(2, '0')}.${jdatetime.getDate()} ${jdatetime.getHours().toString().padStart(2, '0')}:${jdatetime.getMinutes().toString().padStart(2, '0')}`;
}

// Преобразование из DD.MM.YYYY HH:MM:SS в YYYY.MM.DD HH:MM:SS
function DMY_To_YMD(dt) {
    if (dt.trim() == "")
        return "";

    var year = 0;
    var month = 0;
    var day = 0;
    var tm = "";

    var datetime = dt.split(' ')
    if (datetime.length > 0) {
        var date = datetime[0];
        var YMD = date.split('.')
        if (YMD.length == 3) {
            year = YMD[2]
            month = YMD[1]
            day = YMD[0]
        }
    }

    if (datetime.length > 1) {
        var time = datetime[1]
        var HMS = time.split(':')
        if (HMS.length == 3) {
            hour = HMS[0]
            minute = HMS[1]
            second = HMS[2]
        }
    }

    return `${year.toString().padStart(2, '0')}.${month.toString().padStart(2, '0')}.${day.toString().padStart(2, '0')} ${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;

}