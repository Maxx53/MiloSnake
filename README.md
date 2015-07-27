# MiloSnake
Simple snake game, created for WinForms using GDI+

***

Простая змейка, написанная на WinForms с использованием GDI+

###Описание
Преимущества, отличающие именно эту змейку от тысячи других змеек на гитхабе:
- Код с подробнейшими комментариями. Разобраться сможет даже начинающий программист.
- Возможность масштабирования графики. Оптимизировано под разрешение 800х600.
- Геймплейная фишка: активная еда - курица, которая убегает от змейки.
- Поддержка уровней с препятствиями в виде камней.
- Все ресуры игры вынесены в отдельную папку (data), что позволяет модифицировать игру без пересборки бинарного файла.

###Управление
- WASD или Стрелки - изменить направление движения змейки;
- Space (пробел) - ускорение;
- R (в игре или после смерти) - запуск случайного уровня;
- Esc (после смерти) - вернуться в главное меню.


###Формат уровня
Текстовый, 40х30 символов.
```
0 - пустая клетка
# - камень
@ - стартовая позиция змейки
```
###Загрузить
[MiloSnake.rar](https://dl.dropboxusercontent.com/u/1288526/milosnake.rar)

###Скриншот
![Gameplay](http://images.illuzor.com/uploads/chickscr.png)
