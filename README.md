# File Storage

## Bucket

### 1) [Post] /Bucket/Add Добавление нового Bucket или списка Buckets

Добавить Buckets:

```json
[
    {
        "name": "bucket1",
        "default": false
    },
    {
        "name": "bucket2",
        "default": false
    }
]
```

Добавляем Buckets и делаем один из них по умолчанию:

```json
[
    {
        "name": "bucket1",
        "default": false
    },
    {
        "name": "bucket2",
        "default": true
    }
]
```

### 2) [Get] /Bucket/Info/{name} Информация о Bucket по его имени

Параметры:  
name - string

### 3) [Get] /Bucket/Info/All Информация о всех Bucket системы

### 4) [Put] /Bucket/СhangeDefault Назначаем Bucket по уполчанию (Default == true) или снимаем значение по умолчанию c Bucket (Default == false)

Назначаем Bucket по умолчанию:

```json
{
    "name": "bucket",
    "default": true
}
```

Снимаем с Bucket значение "по умолчанию":

```json
{
    "name": "bucket",
    "default": false
}
```

### 5) [Delete] /Bucket/Delete Удаляем Bucket или список Buckets

Удаляем список Buckets:

```json
[
    {
        "name": "bucket1"
    },
    {
        "name": "bucket2"
    }
]
```

## File

### 1) [Post] /File/Add Добавление нового файла или группы файлов

Параметры:  

files - файлы который необходимо загрузить (FormFile)

encription - необходимо ли шифровать файл (по умолчанию файл не шифруется [encription == false])

bucket - указывает в какой bucket будет сохранен файл (если ничего не указать то берется bucket по умолчанию)

### 2) [Get] /File/Get/{id} Получить файл по его id

Параметры:  

id - GUID

### 3) [Get] /File/Info/{id} Информация о файле по его id

Параметры:  

id - GUID

### 4) [Get] /File/Info/All Информация о файлах

Параметры:  

Информация о файлах не находящихся в корзине [trash == false]

Информация о файлах находящихся в корзиен [trash == true]

(По умолчанию false)

### 5) [Put] /File/MoveInTrash Перемецаем файлы в корзину

Перемещаем файлы в корзину:

```json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    },
    {
        "id": "46d334c7-7cd6-494d-b3b2-b5b216c3eef6"
    }
]
```

### 6) [Put] /File/MoveOutTrash Убираем файлы из корзины

Убираем файлы из корзины:

```json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    },
    {
        "id": "46d334c7-7cd6-494d-b3b2-b5b216c3eef6"
    }
]
```

### 7) [Delete] /File/Delete Удаляем файлы

Удаляем файлы:

(Удалить можно только те файлы что находятся в корзине)

```json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    },
    {
        "id": "46d334c7-7cd6-494d-b3b2-b5b216c3eef6"
    }
]
```
