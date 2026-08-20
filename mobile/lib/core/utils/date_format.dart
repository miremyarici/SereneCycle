/// Backend `DateOnly` bekliyor: yyyy-MM-dd. Hem istek gövdeleri hem route
/// yolları aynı biçimi kullandığı için tek yerde.
String toIsoDate(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-'
    '${date.month.toString().padLeft(2, '0')}-'
    '${date.day.toString().padLeft(2, '0')}';
