// Установить фильтр 
function UseFilter(nameCol, id1, id2, condition) {
    var date1 = document.getElementById(id1).value;
    var date2 = document.getElementById(id2).value;

    var filter = FilterQueryByName(nameCol);
    var items = ItemsFilter(filter);

    var arraySelect = ConditionsItems(items, condition, date1, date2);
    //console.log(arraySelect);
    if (arraySelect == null || arraySelect.length == 0) {
        //console.log('>');
        $('#noFilters').show();
        //console.log('>');
    } else {
        $('#noFilters').hide();
    }
    FilterOnlySelect(filter, arraySelect);
}

// Установить первое и последнее значение для фильтра
function FirstLastForColumn(nameCol, idFirst, idLast) {
    var items = GetItemsFilter(nameCol);
    if (items.length > 0) {
        var first = items[0];
        var last = items[items.length - 1];
        $('#' + idFirst).val(first);
        $('#' + idLast).val(last);
    }
}

// Установить первое и последнее значение для фильтра
function FirstLastForColumn(nameCol, id) {
    var items = GetItemsFilter(nameCol);
    if (items.length > 0) {
        var first = items[0];
        var last = items[items.length - 1];
        $('#' + id + 'a').val(first);
        $('#' + id + 'b').val(last);
    }
}

// Список условий
function ArrayConditions() {
    return ['empty', 'notempty', 'equals', 'notequals', 'gt', 'gte', 'lt', 'lte', 'contains', 'notcontains', 'between', 'notbetween'];
}
function ArrayTextConditions() {
    return ['пусто', 'не пусто', 'равно', 'не равно', 'больше', 'больше или равно', 'меньше', 'меньше или равно', 'содержит', 'не содержит', 'диапазон', 'вне диапазона'];
}

// ==============================================================

// Создать SELECT
function CreateSelect(idElement, values, contents = null) {
    var select = document.getElementById(idElement);
    select.style.visibility = 'visible';
    select.length = 0;
    if (contents == null) {
        contents = values;
    }
    if (select != null) {
        for (var i = 0; i < values.length; i++) {
            var val = values[i];
            var cont = contents[i];
            var el = document.createElement("option");
            el.value = val;
            el.textContent = cont;
            select.appendChild(el);
        }
    }
}

// Событие для SELECT
function ChangeSelCond(idSel, id1, id2) {
    var ifname = $('#' + idSel).val();
    //console.log(ifname);

    switch (ifname) {
        case 'empty':
        case 'notempty':
            $('#' + id1).hide();
            $('#' + id2).hide();
            break;

        case 'equals':
        case 'notequals':
        case 'gt':
        case 'gte':
        case 'lt':
        case 'lte':
        case 'contains':
        case 'notcontains':
            $('#' + id1).show();
            $('#' + id2).hide();
            break;

        case 'between':
        case 'notbetween':
            $('#' + id1).show();
            $('#' + id2).show();
            break;

        default:
            $('#' + id1).hide();
            $('#' + id2).hide();
            break;
    }

}

// ==============================================================

// Условия для набора (списка значений)
function ConditionsItems(items, ifname, value1 = null, value2 = null) {
    var arrNames = [];
    items.forEach(function (value, index, array) {
        if (Conditions(value, ifname, value1, value2)) {
            arrNames.push(value);
        }
    });
    return arrNames;
}

// Получение результата условий
function Conditions(value, ifname, value1 = null, value2 = null) {
    switch (ifname) {
        case 'empty':
            return Empty(value);
            break;

        case 'notempty':
            return NotEmpty(value);
            break;

        case 'equals':
            return Equals(value, value1);
            break;

        case 'notequals':
            return NotEquals(value, value1);
            break;

        case 'gt':
            return GT(value, value1);
            break;

        case 'gte':
            return GTE(value, value1);
            break;

        case 'lt':
            return LT(value, value1);
            break;

        case 'lte':
            return LTE(value, value1);
            break;

        case 'contains':
            return Contains(value, value1);
            break;

        case 'notcontains':
            return NotContains(value, value1);
            break;

        case 'between':
            return Between(value, value1, value2);
            break;

        case 'notbetween':
            return NotBetween(value, value1, value2);
            break;

        default:
            return false;
            break;
    }
}

// Условия сравнения:
// пусто
function Empty(value) {
    return value == null;
}
// не пусто
function NotEmpty(value) {
    return value != null;
}
// равно
function Equals(left, right) {
    return left == right;
}
// не равно
function NotEquals(left, right) {
    return left != right;
}
// больше
function GT(left, right) {
    return left > right;
}
// больше или равно
function GTE(left, right) {
    return left >= right;
}
// меньше
function LT(left, right) {
    return left < right;
}
// меньше или равно
function LTE(left, right) {
    return left <= right;
}
// содержит
function Contains(value, subString) {
    return value.includes(subString) == true;
    //return value.indexOf(subString) !== -1;
}
// не содержит
function NotContains(value, subString) {
    return value.includes(subString) == false;
    //return value.indexOf(subString) == -1;
}
// диапазон
function Between(value, min, max) {
    return value >= min && value <= max;
}
// вне диапазона
function NotBetween(value, min, max) {
    return value < min || value > max;
}

// ===========================================================

// Снять весь выбор из фильтра
function FilterDeselectAll(filter) {
    if (filter != null) {
        filter.rows().deselect();
    } else {
        console.log('FilterDeselectAll == ERR');
    }
}

// Снять выбор из фильтра
function FilterDeselect(filter) {
    if (filter != null) {
        //filter.rows().every(function (rowIdx, tableLoop, rowLoop) {
        //    if (jQuery.inArray(this.data().display, arraySelect) !== -1) {
        //        this.deselect();
        //    }
        //});

        filter.rows(function (idx, data, node) {
            return (jQuery.inArray(data.display, arraySelect) !== -1) ?
                true : false;
        }).deselect();
    } else {
        console.log('FilterDeselect == ERR');
    }
}

// Добавить выбор в фильтре
function FilterSelect(filter, arraySelect) {
    if (filter != null) {

        //filter.rows().every(function (rowIdx, tableLoop, rowLoop) {
        //    if (jQuery.inArray(this.data().display, arraySelect) !== -1) {
        //        this.select();
        //    }
        //});

        //filter.rows().select();

        filter.rows(function (idx, data, node) {
            return (jQuery.inArray(data.display, arraySelect) !== -1) ?
                true : false;
        }).select();

        //console.log("OK SELECT");

    } else {
        console.log('FilterSelect == ERR');
    }
}

// Сделать выбор в фильтре
function FilterOnlySelect(filter, arraySelect) {
    FilterDeselectAll(filter);
    FilterSelect(filter, arraySelect);
}


// Получить набор (список элементов) фильтра по имени столбца
function GetItemsFilter(nameCol) {
    //var filters = FilterNames(); // имена фильтров (столбцов)
    //console.log(filters);
    //var indexPane = GetIndexArray(filters, nameCol); // индекс фильтра
    //console.log(indexPane);
    //console.log('next >');
    //var filter = FilterQuery(indexPane); // фильтр
    //console.log('filter:');
    //console.log(filter);
    //console.log('items:');
    var items = ItemsFilter(FilterQueryByName(nameCol));
    return items;
}

// Получить фильтр по имени
function FilterQueryByName(nameCol) {
    var filters = FilterNames(); // имена фильтров (столбцов)
    if (filters.length >= 1) {
        var indexPane = GetIndexArray(filters, nameCol); // индекс фильтра
        var filter = FilterQuery(indexPane); // фильтр
        return filter;
    } else {
        return null;
    }
}

// Получить имена столбцов для фильтров
function FilterNames() {
    var titles = document.querySelectorAll('.dtsp-paneInputButton');
    return QueryToArray(titles, 'placeholder');
}

// Получить индекс фильтра по имени столбца
function FilterIndex(nameCol) {
    var titles = document.querySelectorAll('.dtsp-paneInputButton');
    var array = QueryToArray(titles, 'placeholder');
    return GetIndexArray(array, nameCol);
}

// Получить фильтр
function FilterQuery(indexPane) {
    if (indexPane <= -1) {
        return null;
    }
    var panes = document.querySelectorAll('.dtsp-searchPanes table[id^="DataTables_Table_"].dataTable');
    //console.log('FilterQuery >');
    //console.log(panes.length);
    if (indexPane >= panes.length) {
        return null;
    }
    //console.log('FilterQuery #r');
    return $(panes[indexPane]).DataTable();
}

// Получить набор фильтра
function ItemsFilter(filter) {
    var arrNames = [];
    filter.rows().every(function (rowIdx, tableLoop, rowLoop) {
        arrNames.push(this.data().display);
    });
    return arrNames;
}

// Конвертация запроса в массив
function QueryToArray(nodes, nameKey) {
    var arrNames = [];
    Object.keys(nodes).forEach(function (key) {
        var val = nodes[key][nameKey];
        arrNames.push(val);
    });
    return arrNames;
}

// Получить индекс массива по значению
function GetIndexArray(array, nameValue) {
    return array.indexOf(nameValue);
}