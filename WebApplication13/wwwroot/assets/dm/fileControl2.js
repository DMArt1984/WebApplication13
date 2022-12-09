// Получить файлы
//alert('hello!')
//alert(GetInfo('category'))
//alert(GetInfo('categoryId'))

getFiles('', GetInfo('category'), GetInfo('categoryId'), '')

// innerText
function GetInfo(Id) {
    return document.getElementById(Id).innerText;
}

function SetInfo(Id, Value) {
    document.getElementById(Id).innerText = Value;
}

// value
function GetInfoValue(Id) {
    return document.getElementById(Id).value;
}

function SetInfoValue(Id, Value) {
    document.getElementById(Id).value = Value;
}

// url
function GetPathHost() {
    var host = document.getElementById('host').innerText;
    return host;
}

// Путь к сайту
function GetPathBase() {
    var base = document.getElementById('base').innerText;
    return base;
}

// Загрузить файл
async function uploadFile2(category, categoryId, description) {
    let formData = new FormData();
    formData.append("file", fileupload.files[0]);
    formData.append("category", category);
    formData.append("categoryId", categoryId);
    formData.append("description", description);

    SetInfoValue('filedesc', '');

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
            var Ids = GetInfoValue("newfiles")
            if (Ids != "") {
                Ids = Ids.concat(';')
            }
            Ids = Ids.concat(data.Id)

            SetInfoValue("newfiles", Ids)

            document.getElementById("message").innerText = ""
            getFiles(Ids, GetInfo('category'), GetInfo('categoryId'), GetInfoValue('delfiles'))
        },
        error: function (jqXHR, textStatus, errorThrown) {
            // handle error
            ShowId("listfiles")
            ShowId("uplfiles")
            //alert("jqXHR.status "+ jqXHR.status)
            //alert("etAllResponseHeaders() "+ jqXHR.getAllResponseHeaders())
            //alert("jqXHR.responseText "+ jqXHR.responseText)
            //alert("textStatus " + textStatus)
            //alert("errorThrown " + errorThrown)

            let title = "";
            let parser = new DOMParser();
            const doc = parser.parseFromString(jqXHR.responseText, 'text/html');
            links = doc.getElementsByTagName('title');
            for (var item of links) {
                title = item.innerText;
            }

            document.getElementById("message").innerText = `Ошибка загрузки файла: ${textStatus} [${jqXHR.status}] ${title} ${errorThrown}`;
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
    DeleteFile(GetInfo('delfileId'))
}

// Удалить файл
function DeleteFile(Id) {
    var Ids = GetInfoValue("delfiles")
    if (Ids != "") {
        Ids = Ids.concat(';')
    }
    Ids = Ids.concat(Id)

    SetInfoValue("delfiles", Ids)

    getFiles(GetInfoValue('newfiles'), GetInfo('category'), GetInfo('categoryId'), GetInfoValue('delfiles'))
}


// Получение списка файлов с сервера
async function getFiles(Ids, category, categoryId, DelIds) {
    let formData = new FormData();
    formData.append("Ids", Ids);
    formData.append("category", category);
    formData.append("categoryId", categoryId);
    formData.append("DelIds", DelIds);

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
                document.getElementById("message").innerText =""
                BuildFiles(data)
            } else {
                document.getElementById("message").innerText = `<span class="badge bg-danger">Ошибка получения файлов! ${data.Error}</span>`;
            }
            //alert('The file has been uploaded successfully.');
        },
        error: function (error) {
            // handle error
            document.getElementById("mesage").innerText = `<span class="badge bg-danger">Ошибка получения файлов! ${error}</span>`;
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
        if (fileSize == null) {
            fileSize = ""
        } else if (fileSize < 1) {
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

        if (element.Description == undefined) {
            element.Description = "";
        }

        line.innerHTML = `<div class="row align-items-between">
                            <div class="col-4">
                                <button type="button" class="btn btn-s btn-outline-danger waves-effect waves-light" data-bs-toggle="modal" data-bs-target="#myModalDelFile" onclick="SetInfo('delfileId',${element.Id})">
                                <i class="fas fa-trash-alt px-1"></i></button> <i class='${GetIcon(element.Name)} text-secondary px-1'></i> 
                                <a class="px-1" target="_blank" href="${GetPathBase()}${element.Path}">${element.Name}</a> <span> ${fileSize}</span>
                            </div>
                            <div class="col-8">
                                <div class="input-group mb-2">
                                    <div class="input-group-prepend">
                                        <div class="input-group-text">Описание файла</div>
                                    </div>
                                    <input type="text" class="form-control" name="DescFiles" value="${element.Description}" placeholder="" />
                                </div>
                            </div>
                           </div>`
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