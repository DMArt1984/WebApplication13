// Получить файлы
//alert('hello!')
//alert(GetInfo('category'))
//alert(GetInfo('categoryId'))

getFiles('', GetInfo('category'), GetInfo('categoryId'))

function GetInfo(Id) {
    return document.getElementById(Id).innerText;
}

function GetInfoValue(Id) {
    return document.getElementById(Id).value;
}

function SetInfo(Id, Value) {
    document.getElementById(Id).innerText = Value;
}

function GetPathHost() {
    var host = document.getElementById('host').innerText;
    return host;
}

function GetPathBase() {
    var base = document.getElementById('base').innerText;
    return base;
}

//function Hello() {
//    $.ajax({
//        type: 'get',
//        url: '/Business/Test1',
//        data: { 'data': '12345' },
//        success: function (result) {

//            //document.getElementById("a_Text").textContent = result;
//            document.getElementById("a_Text").innerHTML = result;
//        },
//        failure: function () {
//            alert("failure");
//        },
//        beforeSend: function () {
//            document.getElementById("a_Work").innerHTML = "Loading...";
//        },
//        complete: function () {
//            document.getElementById("a_Work").innerHTML = "OK";
//        }
//    });
//}

//async function uploadFile() {
//      let formData = new FormData();
//      formData.append("file", fileupload.files[0]);
//      await fetch('/Business/GetFiles', {
//        method: "POST",
//        body: formData
//      });
//      alert('The file has been uploaded successfully.');
//}

// Загрузить файл
async function uploadFile2(category, categoryId, description) {
    let formData = new FormData();
    formData.append("file", fileupload.files[0]);
    formData.append("category", category);
    formData.append("categoryId", categoryId);
    formData.append("description", description);

    $.ajax({
        type: "POST",
        url: `${GetPathBase()}/Business/AddFile_forJS`,
        xhr: function () {
            var myXhr = $.ajaxSettings.xhr();
            if (myXhr.upload) {
                myXhr.upload.onprogress = function (evt) {
                    if (evt.lengthComputable) {
                        var percentComplete = parseInt((evt.loaded / evt.total) * 100);
                        console.log("Загрузка: " + percentComplete + "% выполнено")
                        document.getElementById("a_Work").innerHTML = "Загрузка: " + percentComplete + "% выполнено";
                    }
                };
            }
            return myXhr;
        },
        success: function (data) {
            // your callback here
            //console.log(data.FileName);
            //console.log(data.description);
            //console.log(data.category);
            //console.log(data.categoryId);
            //data.Info.forEach(element => console.log(">"+element));
            //alert('The file has been uploaded successfully.');
            document.getElementById("message").innerHTML = ""
            getFiles('', GetInfo('category'), GetInfo('categoryId'))
        },
        error: function (error) {
            // handle error
            ShowId("listfiles")
            ShowId("uplfiles")
            document.getElementById("message").innerHTML = "Ошибка загрузки файла!";
        },
        async: true,
        data: formData,
        cache: false,
        contentType: false,
        processData: false,
        beforeSend: function () {
            HideId("listfiles")
            HideId("uplfiles")
            document.getElementById("a_Work").innerHTML = "Загрузка файла...";
        },
        complete: function () {
            document.getElementById("a_Work").innerHTML = "";
            $("#fileupload").val('');
        },
        timeout: 60000
    });

}

function SelectDelFile() {
    DeleteFile(GetInfo('delfileId'), GetInfo('category'), GetInfo('categoryId'))
}

// Удалить файл
function DeleteFile(Id, category, categoryId) {
    $.ajax({
        type: 'get',
        url: `${GetPathBase()}/Business/DeleteFile_forJS`,
        data: { 'Id': Id, 'category': category, 'categoryId': categoryId },
        success: function (result) {
            getFiles('', GetInfo('category'), GetInfo('categoryId'))
        },
        failure: function () {
            alert("failure");
        },
        beforeSend: function () {
            HideId("listfiles")
            HideId("uplfiles")
            document.getElementById("a_Work").innerHTML = "Удаление файла...";
        },
        complete: function () {
            document.getElementById("a_Work").innerHTML = "";
        }
    });
}

// Получение списка файлов с сервера
async function getFiles(Ids, category, categoryId) {
    let formData = new FormData();
    formData.append("Ids", Ids);
    formData.append("category", category);
    formData.append("categoryId", categoryId);

    $.ajax({
        type: "POST",
        url: `${GetPathBase()}/Business/GetFiles_forJS`,
        xhr: function () {
            var myXhr = $.ajaxSettings.xhr();
            if (myXhr.upload) {
                myXhr.upload.onprogress = function (evt) {
                    if (evt.lengthComputable) {
                        var percentComplete = parseInt((evt.loaded / evt.total) * 100);
                        console.log("Получение: " + percentComplete + "% выполнено")
                        document.getElementById("a_Work").innerHTML = "Получение: " + percentComplete + "% выполнено";
                    }
                };
            }
            return myXhr;
        },
        success: function (data) {
            // your callback here
            if (data.Error == undefined) {
                document.getElementById("message").innerHTML =""
                BuildFiles(data)
            } else {
                document.getElementById("message").innerHTML = `<span class="badge bg-danger">Ошибка получения файлов! ${data.Error}</span>`;
            }
            //alert('The file has been uploaded successfully.');
        },
        error: function (error) {
            // handle error
            document.getElementById("mesage").innerHTML = `<span class="badge bg-danger">Ошибка получения файлов! ${error}</span>`;
        },
        async: true,
        data: formData,
        cache: false,
        contentType: false,
        processData: false,
        beforeSend: function () {
            document.getElementById("a_Work").innerHTML = "Получение списка файлов...";
        },
        complete: function () {
            document.getElementById("a_Work").innerHTML = "";
        },
        timeout: 60000
    });
}

// Вывод списка файлов
function BuildFiles(data) {
    const group = document.getElementById('listfiles');
    group.innerHTML = ""

    var req = new XMLHttpRequest();

    for (const element of data.Files) {
        var line = document.createElement('p')

        req.open("GET", `${GetPathBase()}${element.Path}`, false);
        req.send();
        var fileSize = req.getResponseHeader('content-length') // байт; 
        if (fileSize < 1) {
            fileSize = "1 байт"
        } else if (fileSize < 1024) {
            fileSize = fileSize + " байт"
        } else if (fileSize < 1024 * 1024) {
            fileSize = Math.ceil(fileSize / 1024) + " Кб"
        } else {
            fileSize = Math.ceil(fileSize / 1024 / 1024) + " Мб"
        }
        // <button type="button" class="btn btn-outline-light waves-effect">Light</button>
        // <button type="button" class="btn btn-outline-danger waves-effect waves-light">Danger</button>
        line.innerHTML = `<button type="button" class="btn btn-s btn-outline-danger waves-effect waves-light" data-bs-toggle="modal" data-bs-target="#myModalDelFile" onclick="SetInfo('delfileId',${element.Id})"><i class="fas fa-trash-alt px-1"></i></button> <i class='${GetIcon(element.Name)} text-secondary px-1'></i> <a class="px-1" target="_blank" href="${GetPathBase()}${element.Path}">${element.Name}</a> <span> ~${fileSize}</span>`
        group.appendChild(line)
    }

    ShowId("listfiles")
    ShowId("uplfiles")
}

// Иконка для файла
function GetIcon(filename) {
    var Icon = "fas fa-file"; // "ri-file-text-fill";
    var Ex = filename.split(".").pop()
    switch (Ex) {
        case "jpg":
        case "png":
        case "gif":
        case "wmf":
            Icon = "fas fa-file-image";
            break;

        case "pdf":
        case "txt":
            Icon = "fas fa-file-alt";
            break;

        case "doc":
            Icon = "fas fa-file-word"; // mdi mdi-microsoft-word
            break;

        case "xls":
            Icon = "fas fa-file-excel"; // mdi mdi-microsoft-excel
            break;

        case "avi":
        case "mov":
        case "mpg":
            Icon = "fas fa-file-video";
            break;

        case "mp3":
        case "wav":
            Icon = "fas fa-file-audio";
            break;

        case "zip":
        case "rar":
            Icon = "fas fa-file-archive";
            break;
    }
    return Icon
}

// Show Hide for ID
function HideId(id) {
    document.getElementById(id).style.display = 'none'; // скрыть
}
function ShowId(id) {
    document.getElementById(id).style.display = ''; // показать
}