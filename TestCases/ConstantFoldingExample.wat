// Пример для демонстрации constant folding
// Эти выражения должны быть свернуты во время компиляции

routine main()
is
    // Арифметические константы: 2 + 3 -> 5
    var x : integer is 2 + 3;

    // Вложенные константы: (1 + 2) * (4 - 1) -> 9
    var y : integer is (1 + 2) * (4 - 1);

    // Вещественные константы: 1.5 * 2.0 -> 3.0
    var z : real is 1.5 * 2.0;

    // Булевы константы: true and false -> false
    var b : boolean is true and false;

    // Сравнения констант: 5 < 10 -> true
    var c : boolean is 5 < 10;

    // Унарные операции: -(-5) -> 5
    var w : integer is -(-5);

    // Логическое отрицание: not(true) -> false
    var d : boolean is not(true);

    // Смешанные операции: 2 + 3 * 4 -> 14
    var e : integer is 2 + 3 * 4;

    print x, y, z, b, c, w, d, e;
end