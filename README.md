# SQLParser

For the purpose of simplifying database delivery or migration, all files are collected into one SQL file. This is very convenient for manual execution: just take the file and run it. If there is an error, you can click on it in MS SQL Studio and go directly to it. However, if this is to be delivered in CLI mode or in a CI/CD process, it is very difficult to find the error in a semantic block between two GO statements.

Simply splitting the file by GO statements is not possible, because there are block comments and single lines in which there are statements, which causes problems and false positives. Cleaning the code is difficult and can always add more of the same.

That's why I wrote a mini-utility for semantic parsing through the SMO library, and output the problematic code section to the console.

This allowed me to create a CI process for adding SQL code to the repository and testing the final script.

TODO
- [ ] add building via Github Actions
- [ ] publishing artifacts release
- [ ] test project by sqlserver in linux docker



Для упрощения доставки БД или миграции БД, все файлы собираются в один  sql.
Что для ручного запуска очень удобно: файл взял и запустил. Если есть ошибка, то кликаешь мышкой на ошибки в MS SQL Studio и попадаешь на нее. Но если это доставлять в режиме cli, или CI/CD процессе то полога искать ошибку в 3 строке, какого то семантического блока между двумя операторами GO, очень сложно

Просто дробить файл по операторам GO не возможно, т. к. есть блочные комментарии, одиночные строки  в который есть операторы, что вызываем проблемы и ложные сработывания. Чистить код сложно и всегда могу добавить еще такого же.

Поэтому я написал мини утилиту для семантического разбора через библиотеку SMO, и вывод проблемного участка кода в консоль.

Это позволило создать CI процесс на добавления SQL кода в репозиторий, и тестирования итогового скрипта