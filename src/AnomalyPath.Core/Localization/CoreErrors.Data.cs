#nullable enable
namespace AnomalyPath.Core.Localization
{
    /// <summary>Часть класса <see cref="CoreErrors"/>: ошибки данных.</summary>
    public static partial class CoreErrors
    {
        /// <summary>Пустой путь к файлу.</summary>
        public const string EmptyPath = "error.empty_path";

        /// <summary>Пакет данных повреждён или имеет неверную структуру.</summary>
        public const string DataPackInvalid = "error.data_pack_invalid";

        /// <summary>Версия пакета данных не поддерживается.</summary>
        public const string DataPackVersion = "error.data_pack_version";

        /// <summary>Обязательное строковое поле не задано.</summary>
        public const string RequiredField = "error.required_field";

        /// <summary>Обязательный список пуст.</summary>
        public const string EmptyCollection = "error.empty_collection";

        /// <summary>Числовое поле меньше единицы.</summary>
        public const string MinimumOne = "error.minimum_one";

        /// <summary>Числовое поле отрицательное.</summary>
        public const string NonNegative = "error.non_negative";

        /// <summary>Значение должно быть в диапазоне 0..1.</summary>
        public const string ChanceRange = "error.chance_range";

        /// <summary>Индекс вне диапазона.</summary>
        public const string IndexOutOfRange = "error.index_out_of_range";

        /// <summary>Ник длиннее допустимого.</summary>
        public const string NicknameLength = "error.nickname_length";

        /// <summary>Верхняя граница меньше нижней.</summary>
        public const string RangeOrder = "error.range_order";

        /// <summary>Шаг должен быть положительным.</summary>
        public const string StepPositive = "error.step_positive";

        /// <summary>Значение должно быть больше нуля.</summary>
        public const string PositiveValue = "error.positive_value";

        /// <summary>Путь выходит за корень данных.</summary>
        public const string PathOutsideRoot = "error.path_outside_root";

        /// <summary>Файл не найден.</summary>
        public const string FileNotFound = "error.file_not_found";

        /// <summary>Отсутствует обязательное строковое поле.</summary>
        public const string MissingStringField = "error.missing_string_field";

        /// <summary>Отсутствует обязательное числовое поле.</summary>
        public const string MissingNumberField = "error.missing_number_field";

        /// <summary>Отсутствует обязательный массив.</summary>
        public const string MissingArrayField = "error.missing_array_field";

        /// <summary>Элемент массива должен быть строкой.</summary>
        public const string ArrayItemNotString = "error.array_item_not_string";

        /// <summary>Ожидалась пара чисел.</summary>
        public const string NumberPairExpected = "error.number_pair_expected";

        /// <summary>Файл сущности подключён дважды.</summary>
        public const string DuplicateEntity = "error.duplicate_entity";

        /// <summary>Коллекция подключена дважды.</summary>
        public const string DuplicateCollection = "error.duplicate_collection";

        /// <summary>Дублирующийся id внутри вида.</summary>
        public const string DuplicateId = "error.duplicate_id";

        /// <summary>Цикл наследования.</summary>
        public const string InheritanceCycle = "error.inheritance_cycle";

        /// <summary>Слишком глубокая цепочка наследования.</summary>
        public const string InheritanceDepth = "error.inheritance_depth";

        /// <summary>Наследование между разными видами.</summary>
        public const string KindMismatch = "error.kind_mismatch";

        /// <summary>Категория weapon без блока weapon.</summary>
        public const string WeaponWithoutBlock = "error.weapon_without_block";

        /// <summary>Вместимость магазина оружия меньше одного патрона.</summary>
        public const string WeaponMagazineInvalid = "error.weapon_magazine_invalid";

        /// <summary>Предмет не найден в репозитории.</summary>
        public const string ItemNotFound = "error.item_not_found";

        /// <summary>Дублирующийся id в репозитории.</summary>
        public const string DuplicateRepositoryId = "error.duplicate_repository_id";

        /// <summary>Сервис не зарегистрирован в контейнере.</summary>
        public const string ServiceNotRegistered = "error.service_not_registered";

        /// <summary>Сервис уже зарегистрирован.</summary>
        public const string ServiceAlreadyRegistered = "error.service_already_registered";

        /// <summary>Предмет не помещается в контейнер.</summary>
        public const string ItemDoesNotFit = "error.item_does_not_fit";

        /// <summary>Прибавка к весу задана предмету, который не является рюкзаком.</summary>
        public const string ExtraWeightOnCategory = "error.extra_weight_on_category";

        /// <summary>У артефакта не указан тип.</summary>
        public const string ArtifactWithoutType = "error.artifact_without_type";

        /// <summary>Неизвестный материал брони или шлема.</summary>
        public const string UnknownMaterial = "error.unknown_material";

        /// <summary>Неизвестная категория предмета.</summary>
        public const string UnknownItemCategory = "error.unknown_item_category";

        /// <summary>Неизвестный режим цены.</summary>
        public const string UnknownPriceMode = "error.unknown_price_mode";

        /// <summary>Неизвестный тип урона в блоке защиты.</summary>
        public const string UnknownDamageType = "error.unknown_damage_type";

        /// <summary>Блок защиты задан предмету, который её не даёт.</summary>
        public const string ProtectionOnWrongCategory = "error.protection_on_category";

        /// <summary>Бронебойность патрона вне диапазона 0..1.</summary>
        public const string PenetrationRange = "error.penetration_range";

        /// <summary>Бронебойность задана предмету, который не является патроном.</summary>
        public const string PenetrationOnCategory = "error.penetration_on_category";

        /// <summary>Проверка данных пройдена.</summary>
        public const string ValidationOk = "validation.ok";

        /// <summary>Проверка данных не пройдена.</summary>
        public const string ValidationFailed = "validation.failed";

        /// <summary>В наборе переводов не хватает ключа.</summary>
        public const string TranslationMissing = "validation.translation_missing";

        /// <summary>Каталог текстов игры отсутствует.</summary>
        public const string GameTextsMissing = "validation.game_texts_missing";

        /// <summary>Заголовок экрана ошибки данных.</summary>
        public const string DataErrorTitle = "validation.data_error_title";

        /// <summary>Данные не загружены.</summary>
        public const string DataErrorMissing = "validation.data_missing";

        /// <summary>Кнопка выхода на экране ошибки данных.</summary>
        public const string DataErrorQuit = "validation.data_quit";

        /// <summary>Строка отчёта: путь и сообщение.</summary>
        public const string ReportLine = "log.report_line";

        /// <summary>Не удалось сохранить exit_save.</summary>
        public const string ExitSaveFailed = "log.exit_save_failed";

        /// <summary>Данные не прошли проверку.</summary>
        public const string DataCheckFailed = "log.data_check_failed";

        /// <summary>Файл не используется ни каталогом, ни коллекциями.</summary>
        public const string UnreachableFile = "validation.unreachable";

        /// <summary>Дублирующийся id с указанием первого файла.</summary>
        public const string DuplicateIdFiles = "error.duplicate_id_files";

        /// <summary>Отсутствует обязательный объект.</summary>
        public const string MissingObjectField = "error.missing_object_field";

        /// <summary>Число записей редкости не совпадает с числом ступеней.</summary>
        public const string RarityRecordCount = "error.rarity_record_count";

        /// <summary>Порядок редкости вне диапазона ступеней.</summary>
        public const string RarityOrderRange = "error.rarity_order_range";

        /// <summary>Порядок редкости повторяется.</summary>
        public const string RarityOrderDuplicate = "error.rarity_order_duplicate";

        /// <summary>Нет записи редкости для ступени.</summary>
        public const string RarityMissing = "error.rarity_missing";

        /// <summary>Модификаторы заданы категории, у которой их нет.</summary>
        public const string ModifiersOnWrongCategory = "error.modifiers_on_category";

        /// <summary>У артефакта задан верхний блок защиты вместо модификатора.</summary>
        public const string ArtifactProtectionBlock = "error.artifact_protection_block";

        /// <summary>Тип урона задан модификатору, который его не использует.</summary>
        public const string ModifierDamageType = "error.modifier_damage_type";

        /// <summary>Значение модификатора равно нулю.</summary>
        public const string NonZero = "error.non_zero";

        /// <summary>Неизвестная характеристика модификатора.</summary>
        public const string UnknownStat = "error.unknown_stat";

        /// <summary>Неизвестный режим сложения модификатора.</summary>
        public const string UnknownModifierMode = "error.unknown_modifier_mode";

        /// <summary>Одинаковый модификатор повторяется в одном списке.</summary>
        public const string DuplicateModifier = "error.duplicate_modifier";

        /// <summary>Сумма шансов редкости артефакта не равна единице.</summary>
        public const string ArtifactRarityChances = "error.artifact_rarity_chances";

        /// <summary>Id настройки артефактов не совпадает с ожидаемым.</summary>
        public const string ArtifactSettingsId = "error.artifact_settings_id";
    }
}
