#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Ключи видимых текстов интерфейса. Код ресурса — <c>ui.</c> + имя члена в нижнем регистре,
    /// сами тексты лежат в <c>strings.{ru,en}.json</c>. В префабах хранится код
    /// (<see cref="UiText.Code"/>), а не номер члена, поэтому порядок членов можно менять
    /// без переноса подписей.
    /// </summary>
    public enum UiTextKey
    {
        /// <summary>Кнопка «Назад».</summary>
        Common_Back,

        /// <summary>Кнопка «Отмена».</summary>
        Common_Cancel,

        /// <summary>Кнопка «Выход».</summary>
        Common_Quit,

        /// <summary>Кнопка «Удалить».</summary>
        Common_Delete,

        /// <summary>Заголовок подтверждения.</summary>
        Common_Confirm_Title,

        /// <summary>Сообщение «нет активной игры».</summary>
        Common_No_Game,

        /// <summary>Пустое значение слота.</summary>
        Common_Dash,

        /// <summary>Крестик закрытия или удаления.</summary>
        Common_Close,

        /// <summary>Метка выбранного элемента.</summary>
        Common_Selected,

        /// <summary>Деньги с подписью.</summary>
        Common_Money,

        /// <summary>Уровень строчными буквами.</summary>
        Common_Level,

        /// <summary>Уровень с заглавной буквы.</summary>
        Common_Level_Cap,

        /// <summary>Строка «день, часы» строчными.</summary>
        Common_Day_Hours,

        /// <summary>Строка «День, часы» с заглавной.</summary>
        Common_Day_Hours_Tile,

        /// <summary>Счётчик занятых клеток.</summary>
        Common_Count_Of,

        /// <summary>Строка переполнения набора.</summary>
        Common_Not_Placed,

        /// <summary>Название игры.</summary>
        MainMenu_Game_Title,

        /// <summary>Подзаголовок меню.</summary>
        MainMenu_Subtitle,

        /// <summary>Кнопка новой игры.</summary>
        MainMenu_New_Game,

        /// <summary>Кнопка загрузки.</summary>
        MainMenu_Load,

        /// <summary>Кнопка «Об игре».</summary>
        MainMenu_About,

        /// <summary>Кнопка «Авторы».</summary>
        MainMenu_Authors,

        /// <summary>Кнопка «Настройки».</summary>
        MainMenu_Settings,

        /// <summary>Текст «Об игре».</summary>
        MainMenu_About_Text,

        /// <summary>Версия игры в правом нижнем углу меню: номер из сборки и подпись демо.</summary>
        MainMenu_Version,

        /// <summary>Заголовок новой игры.</summary>
        NewGame_Title,

        /// <summary>Подпись поля имени.</summary>
        NewGame_Name_Label,

        /// <summary>Подсказка поля имени.</summary>
        NewGame_Name_Placeholder,

        /// <summary>Кнопка случайного имени.</summary>
        NewGame_Random_Name,

        /// <summary>Подпись выбора набора.</summary>
        NewGame_Loadout_Label,

        /// <summary>Подпись коэффициента страховки надетых вещей.</summary>
        NewGame_Insurance_Label,

        /// <summary>Подпись влияния ночи на точность.</summary>
        NewGame_Night_Accuracy_Label,

        /// <summary>Подпись усложнения торговли.</summary>
        NewGame_Trade_Label,

        /// <summary>Кнопка «Начать».</summary>
        NewGame_Start,

        /// <summary>Нет доступных наборов (подпись).</summary>
        NewGame_No_Loadouts,

        /// <summary>Ошибка предпросмотра набора.</summary>
        NewGame_Preview_Failed,

        /// <summary>Статус: нет доступных наборов.</summary>
        NewGame_Status_No_Sets,

        /// <summary>Статус: введите имя.</summary>
        NewGame_Status_Enter_Name,

        /// <summary>Статус: имя слишком длинное.</summary>
        NewGame_Status_Name_Too_Long,

        /// <summary>Статус: ошибка старта.</summary>
        NewGame_Status_Error,

        /// <summary>Заголовок окна инвентаря набора.</summary>
        NewGame_Preview_Title,

        /// <summary>Окно инвентаря пусто.</summary>
        NewGame_Preview_Empty,

        /// <summary>Заголовок экрана загрузки.</summary>
        Load_Title,

        /// <summary>Нет сохранений.</summary>
        Load_Empty,

        /// <summary>Кнопка загрузки сохранения в строке списка.</summary>
        Load_Load,

        /// <summary>Заголовок удаления сохранения.</summary>
        Load_Delete_Title,

        /// <summary>Вопрос удаления сохранения.</summary>
        Load_Delete_Question,

        /// <summary>Ошибка удаления.</summary>
        Load_Delete_Error,

        /// <summary>Сохранение удалено.</summary>
        Load_Deleted,

        /// <summary>Ошибка загрузки.</summary>
        Load_Load_Error,

        /// <summary>Заголовок экрана сохранения.</summary>
        SaveGame_Title,

        /// <summary>Нет активной игры на экране сохранения.</summary>
        SaveGame_Status_No_Game,

        /// <summary>Сохранение записано.</summary>
        SaveGame_Toast_Saved,

        /// <summary>Ошибка сохранения.</summary>
        SaveGame_Status_Error,

        /// <summary>Кнопка сохранения в строке списка.</summary>
        SaveGame_Save,

        /// <summary>Подсказка поля названия сохранения.</summary>
        SaveGame_Name_Placeholder,

        /// <summary>Заголовок подтверждения перезаписи сохранения.</summary>
        SaveGame_Overwrite_Title,

        /// <summary>Вопрос подтверждения перезаписи сохранения.</summary>
        SaveGame_Overwrite_Question,

        /// <summary>Заголовок диалога выхода.</summary>
        Exit_Title,

        /// <summary>Текст диалога выхода.</summary>
        Exit_Body,

        /// <summary>Кнопка «В меню».</summary>
        Exit_To_Menu,

        /// <summary>Кнопка «На рабочий стол».</summary>
        Exit_To_Desktop,

        /// <summary>Кнопка «Сохранить игру».</summary>
        Game_Save,

        /// <summary>Кнопка «Загрузить игру».</summary>
        Game_Load,

        /// <summary>Кнопка «Выйти».</summary>
        Game_Exit,

        /// <summary>Игра начата.</summary>
        Game_Started,

        /// <summary>Сводка состояния игрока.</summary>
        Game_Status,

        /// <summary>Заголовок меню игры.</summary>
        GameMenu_Title,

        /// <summary>Кнопка «Продолжить» в меню игры.</summary>
        GameMenu_Continue,

        /// <summary>Счётчик смертей за прохождение.</summary>
        GameMenu_Deaths,

        /// <summary>Заголовок панели действий.</summary>
        Game_Actions,

        /// <summary>Кнопка перехода с названием локации.</summary>
        Game_Travel,

        /// <summary>Нет доступных действий.</summary>
        Game_No_Actions,

        /// <summary>В локации идёт бой.</summary>
        Game_In_Combat,

        /// <summary>Подсказка области объекта.</summary>
        Game_Object_Hint,

        /// <summary>Заголовок области объекта.</summary>
        Game_Object,

        /// <summary>Действие осмотра объекта лута с его названием.</summary>
        Game_Object_Action,

        /// <summary>Объект лута без лута: действие недоступно.</summary>
        Game_Object_Empty,

        /// <summary>Номер дня и время на часах основного экрана.</summary>
        Game_Clock,

        /// <summary>Сведения о локации на основном экране.</summary>
        Game_Location_Summary,

        /// <summary>Уровень игрока в верхней полосе: «Уровень {0}».</summary>
        Game_Level,

        /// <summary>Опыт текущего уровня: «{0} / {1} опыта».</summary>
        Game_Xp_Line,

        /// <summary>Роль локации: хаб.</summary>
        Game_Node_Hub,

        /// <summary>Роль локации: дорога.</summary>
        Game_Node_Route,

        /// <summary>Роль локации: точка интереса.</summary>
        Game_Node_Site,

        /// <summary>Переход выполнен.</summary>
        Game_Travel_Success,

        /// <summary>Ребро перехода не найдено.</summary>
        Game_Travel_EdgeNotFound,

        /// <summary>Переход начинается не из текущей локации.</summary>
        Game_Travel_WrongLocation,

        /// <summary>Переход запрещён во время боя.</summary>
        Game_Travel_InCombat,

        /// <summary>Ошибка перехода с текстом ошибки.</summary>
        Game_Travel_Error,

        /// <summary>Заголовок экрана персонажа.</summary>
        Character_Title,

        /// <summary>Пункт меню «Снять».</summary>
        Character_Menu_Unequip,

        /// <summary>Пункт меню «Одеть».</summary>
        Character_Menu_Equip,

        /// <summary>Пункт меню «Разрядить».</summary>
        Character_Menu_Unload,

        /// <summary>Пункт меню «Зарядить».</summary>
        Character_Menu_Load,

        /// <summary>Пункт меню «Разобрать».</summary>
        Character_Menu_Disassemble,

        /// <summary>Пункт меню «Выбросить».</summary>
        Character_Menu_Discard,

        /// <summary>Пункт меню «Применить» для расходника.</summary>
        Character_Menu_Use,

        /// <summary>Заголовок инвентаря.</summary>
        Character_Inventory_Title,

        /// <summary>Строка переполнения инвентаря.</summary>
        Character_Overflow,

        /// <summary>Подпись веса инвентаря: текущий и предельный вес.</summary>
        Character_Weight,

        /// <summary>Подпись слота шлема.</summary>
        Character_Slot_Helmet,

        /// <summary>Подпись слота брони.</summary>
        Character_Slot_Armor,

        /// <summary>Подпись слота рюкзака.</summary>
        Character_Slot_Backpack,

        /// <summary>Подпись слота пояса.</summary>
        Character_Slot_Belt,

        /// <summary>Подпись слота дополнительного оружия.</summary>
        Character_Slot_Secondary,

        /// <summary>Подпись слота основного оружия.</summary>
        Character_Slot_Main,

        /// <summary>Подпись характеристики здоровья.</summary>
        Character_Stat_Health,

        /// <summary>Подпись характеристики еды.</summary>
        Character_Stat_Food,

        /// <summary>Подпись характеристики воды.</summary>
        Character_Stat_Water,

        /// <summary>Подпись характеристики выносливости.</summary>
        Character_Stat_Stamina,

        /// <summary>Заголовок раздела «Защита» на панели статов персонажа.</summary>
        Character_Stats_Defense,

        /// <summary>Сообщение «надето».</summary>
        Character_Action_Equipped,

        /// <summary>Сообщение «снято».</summary>
        Character_Action_Unequipped,

        /// <summary>Сообщение «артефакт надет».</summary>
        Character_Action_Artifact_Equipped,

        /// <summary>Сообщение о разрядке магазина.</summary>
        Character_Action_Unloaded,

        /// <summary>Сообщение о зарядке магазина.</summary>
        Character_Action_Loaded,

        /// <summary>Сообщение о разборе предмета.</summary>
        Character_Action_Disassembled,

        /// <summary>Сообщение о выбросе предмета.</summary>
        Character_Action_Discarded,

        /// <summary>Сообщение о применении расходника.</summary>
        Character_Action_Used,

        /// <summary>Результат «готово».</summary>
        Character_Result_Ok,

        /// <summary>Результат: предмет не найден.</summary>
        Character_Result_Item_Missing,

        /// <summary>Результат: неверный слот.</summary>
        Character_Result_Wrong_Slot,

        /// <summary>Результат: нет места.</summary>
        Character_Result_No_Room,

        /// <summary>Результат: не артефакт.</summary>
        Character_Result_Not_Artifact,

        /// <summary>Результат: нет свободного слота артефакта.</summary>
        Character_Result_No_Free_Artifact_Slot,

        /// <summary>Результат: не оружие.</summary>
        Character_Result_Not_A_Weapon,

        /// <summary>Результат: магазин пуст.</summary>
        Character_Result_Nothing_To_Unload,

        /// <summary>Результат: магазин уже полон.</summary>
        Character_Result_Magazine_Full,

        /// <summary>Результат: предмет не является патронами.</summary>
        Character_Result_Not_Ammo,

        /// <summary>Результат: калибр патронов не подходит к оружию.</summary>
        Character_Result_Ammo_Caliber_Mismatch,

        /// <summary>Результат: в магазине патроны другого вида.</summary>
        Character_Result_Ammo_Type_Mismatch,

        /// <summary>Результат: в инвентаре нет подходящих патронов.</summary>
        Character_Result_No_Ammo,

        /// <summary>Результат: нет патронов.</summary>
        Character_Result_Ammo_Missing,

        /// <summary>Результат: расходник применять нельзя.</summary>
        Character_Result_Not_Usable,

        /// <summary>Результат: восполнять нечего, предмет не потрачен.</summary>
        Character_Result_Nothing_To_Restore,

        /// <summary>Кнопка ремонта у торговца-механика.</summary>
        Trader_Repair,

        /// <summary>Кнопка ремонта со стоимостью.</summary>
        Trader_Repair_Cost,

        /// <summary>Сообщение об успешном ремонте.</summary>
        Trader_Repair_Done,

        /// <summary>Результат: чинить нечего.</summary>
        Trader_Result_Nothing_To_Repair,

        /// <summary>Результат: не получилось.</summary>
        Character_Result_Failed,

        /// <summary>Название нового сохранения.</summary>
        SaveSlot_New,

        /// <summary>Имя нового сохранения с номером.</summary>
        SaveSlot_New_Name,

        /// <summary>Подпись пустой ячейки.</summary>
        SaveSlot_Cell,

        /// <summary>Пустая ячейка.</summary>
        SaveSlot_Empty,

        /// <summary>Повреждённый слот.</summary>
        SaveSlot_Corrupted,

        /// <summary>Строка повреждённого слота.</summary>
        SaveSlot_Corrupted_Line,

        /// <summary>Строка прогресса слота.</summary>
        SaveSlot_Progress,

        /// <summary>Подпись страниц.</summary>
        SaveSlot_Pager,

        /// <summary>Ошибка данных: заголовок.</summary>
        Data_Error_Title,

        /// <summary>Ошибка данных: нет данных.</summary>
        Data_Error_Missing,

        /// <summary>Кнопка возврата в меню со старта игры.</summary>
        NewGame_Back,

        /// <summary>Маркер выбранного набора снаряжения.</summary>
        NewGame_Selected_Marker,

        /// <summary>Подпись режима постоянной смерти.</summary>
        NewGame_Permadeath_Label,

        /// <summary>Действие перехода к торговцу с его именем.</summary>
        Game_Trade,

        /// <summary>Кнопка «Купить».</summary>
        Trader_Buy,

        /// <summary>Кнопка «Продать».</summary>
        Trader_Sell,

        /// <summary>Остаток товара на складе.</summary>
        Trader_Stock,

        /// <summary>Цена товара.</summary>
        Trader_Price,

        /// <summary>Сообщение об успешной покупке.</summary>
        Trader_Bought,

        /// <summary>Сообщение об успешной продаже.</summary>
        Trader_Sold,

        /// <summary>Торговец недоступен.</summary>
        Trader_Unavailable,

        /// <summary>Заголовок выпадашки выбора торговца.</summary>
        Trader_Traders,

        /// <summary>Нет спрайта портрета торговца.</summary>
        Trader_Portrait_Missing,

        /// <summary>Результат: торговец недоступен.</summary>
        Trader_Result_Unavailable,

        /// <summary>Результат: не хватает денег.</summary>
        Trader_Result_Not_Enough_Money,

        /// <summary>Результат: товар распродан.</summary>
        Trader_Result_No_Stock,

        /// <summary>Результат: нет места в инвентаре.</summary>
        Trader_Result_No_Room,

        /// <summary>Результат: товара нет в ассортименте.</summary>
        Trader_Result_Offer_Missing,

        /// <summary>Результат: предмет не найден.</summary>
        Trader_Result_Item_Missing,

        /// <summary>Результат: предмет не покупают.</summary>
        Trader_Result_Not_Sellable,

        /// <summary>Результат: неверное количество.</summary>
        Trader_Result_Invalid_Count,

        /// <summary>Результат: сделка не состоялась.</summary>
        Trader_Result_Failed,

        /// <summary>Заголовок колонки товаров торговца в окне торговли.</summary>
        Trade_Trader_Items,

        /// <summary>Подпись поля количества предметов.</summary>
        Trade_Quantity,

        /// <summary>Кнопка «Макс»: выбрать максимально возможное количество.</summary>
        Trade_Max,

        /// <summary>Подсказка, когда предмет ещё не выбран.</summary>
        Trade_Select,

        /// <summary>Цена одной штуки: «{0} руб / шт».</summary>
        Trade_Unit_Price,

        /// <summary>Кнопка покупки с итоговой ценой: «Купить · {0} руб».</summary>
        Trade_Buy_Total,

        /// <summary>Кнопка продажи с итоговой ценой: «Продать · {0} руб».</summary>
        Trade_Sell_Total,

        /// <summary>Действие «Задания» на основном экране.</summary>
        Game_Quests,

        /// <summary>Действие «Доска объявлений» в хабе.</summary>
        Game_Quest_Board,

        /// <summary>Действие «Зачистить локацию» на целевом экземпляре.</summary>
        Game_Clear_Location,

        /// <summary>Локация зачищена.</summary>
        Game_Cleared,

        /// <summary>Заголовок журнала заданий.</summary>
        Quest_Title,

        /// <summary>Заголовок доски объявлений с названием хаба.</summary>
        Quest_Board_Title,

        /// <summary>Заголовок списка слотов доски.</summary>
        Quest_Section_Board,

        /// <summary>Заголовок списка активных заданий.</summary>
        Quest_Section_Active,

        /// <summary>Пустой список активных заданий.</summary>
        Quest_Empty_Active,

        /// <summary>Пустой список доступных заданий.</summary>
        Quest_Empty_Offers,

        /// <summary>Доска доступна только в хабе.</summary>
        Quest_No_Board,

        /// <summary>Кнопка «Взять».</summary>
        Quest_Take,

        /// <summary>Кнопка «Сдать».</summary>
        Quest_Turn_In,

        /// <summary>Кнопка «Отменить».</summary>
        Quest_Cancel,

        /// <summary>Кнопка «Отказаться».</summary>
        Quest_Decline,

        /// <summary>Отказ от задания.</summary>
        Quest_Offer_Declined,

        /// <summary>Активный квест отменён.</summary>
        Quest_Cancelled,

        /// <summary>Слот обновится через N часов.</summary>
        Quest_Refresh_In,

        /// <summary>Сложность задания.</summary>
        Quest_Difficulty,

        /// <summary>Простая сложность.</summary>
        Quest_Difficulty_Easy,

        /// <summary>Средняя сложность.</summary>
        Quest_Difficulty_Medium,

        /// <summary>Сложная сложность.</summary>
        Quest_Difficulty_Hard,

        /// <summary>Награда задания.</summary>
        Quest_Reward,

        /// <summary>Строка прогресса.</summary>
        Quest_Progress,

        /// <summary>Готово к сдаче.</summary>
        Quest_Ready,

        /// <summary>Срок выполнения задания: сколько часов осталось.</summary>
        Quest_Fail_Time,

        /// <summary>Условие провала задания: смерть игрока.</summary>
        Quest_Fail_Death,

        /// <summary>Задание взято.</summary>
        Quest_Accepted,

        /// <summary>Задание сдано.</summary>
        Quest_Completed,

        /// <summary>Задание на откате.</summary>
        Quest_Cooldown,

        /// <summary>Целевая локация.</summary>
        Quest_Target_Location,

        /// <summary>Целевые актёры.</summary>
        Quest_Target_Actors,

        /// <summary>Целевые предметы.</summary>
        Quest_Target_Items,

        /// <summary>Прогресс по NPC.</summary>
        Quest_Npc_Progress,

        /// <summary>Задание не найдено.</summary>
        Quest_Result_Not_Found,

        /// <summary>Задание не повторяемое.</summary>
        Quest_Result_Not_Repeatable,

        /// <summary>В слоте нет такого предложения.</summary>
        Quest_Result_Not_Offered,

        /// <summary>Слот закрыт таймером.</summary>
        Quest_Result_Slot_Locked,

        /// <summary>Слот пуст.</summary>
        Quest_Result_Slot_Empty,

        /// <summary>Действие только в хабе.</summary>
        Quest_Result_Wrong_Location,

        /// <summary>Доска недоступна.</summary>
        Quest_Result_Unavailable,

        /// <summary>Условия не выполнены.</summary>
        Quest_Result_Requirements,

        /// <summary>Задание уже взято.</summary>
        Quest_Result_Already_Active,

        /// <summary>Слишком много активных заданий.</summary>
        Quest_Result_Active_Limit,

        /// <summary>Задание на откате.</summary>
        Quest_Result_Cooldown,

        /// <summary>Нет подходящих целей.</summary>
        Quest_Result_No_Targets,

        /// <summary>Цель ещё не выполнена.</summary>
        Quest_Result_Not_Ready,

        /// <summary>Задание взято на другой доске.</summary>
        Quest_Result_Wrong_Board,

        /// <summary>Нет места для награды.</summary>
        Quest_Result_No_Room,

        /// <summary>Действие недоступно.</summary>
        Quest_Result_Invalid,

        /// <summary>Заголовок экрана талантов.</summary>
        Talent_Title,

        /// <summary>Кнопка открытия дерева талантов.</summary>
        Talent_Open,

        /// <summary>Свободные очки талантов.</summary>
        Talent_Points,

        /// <summary>Сколько очков вложено в дерево.</summary>
        Talent_Spent,

        /// <summary>Ранг таланта: текущий и предельный.</summary>
        Talent_Rank,

        /// <summary>Кнопка изучения таланта.</summary>
        Talent_Learn,

        /// <summary>Талант изучен.</summary>
        Talent_Learned,

        /// <summary>Талант закрыт порогом вложенных очков.</summary>
        Talent_Locked,

        /// <summary>Талант изучен полностью.</summary>
        Talent_Max,

        /// <summary>Подсказка: нажатие на талант изучает его.</summary>
        Talent_Tooltip_Click,

        /// <summary>Дерево талантов пусто.</summary>
        Talent_Empty,

        /// <summary>Прибавка эффекта за один ранг.</summary>
        Talent_Effect_Per_Rank,

        /// <summary>Эффект: максимум ОД.</summary>
        Talent_Effect_Ap_Max,

        /// <summary>Эффект: максимум здоровья.</summary>
        Talent_Effect_Health,

        /// <summary>Эффект: меткость.</summary>
        Talent_Effect_Accuracy,

        /// <summary>Эффект: максимум выносливости.</summary>
        Talent_Effect_Stamina,

        /// <summary>Эффект: восстановление выносливости за час отдыха.</summary>
        Talent_Effect_Stamina_Recovery,

        /// <summary>Эффект: максимум еды.</summary>
        Talent_Effect_Max_Food,

        /// <summary>Эффект: максимум воды.</summary>
        Talent_Effect_Max_Water,

        /// <summary>Талант не найден.</summary>
        Talent_Result_Unknown,

        /// <summary>Талант уже изучен полностью.</summary>
        Talent_Result_Max,

        /// <summary>Талант ещё закрыт.</summary>
        Talent_Result_Locked,

        /// <summary>Нет свободных очков.</summary>
        Talent_Result_No_Points,

        /// <summary>Действие с талантом недоступно.</summary>
        Talent_Result_Invalid,

        /// <summary>Кнопка «Закрыть» подтверждения на экране добычи.</summary>
        Loot_Close,

        /// <summary>Инвентарь полон, часть лута ушла в переполнение.</summary>
        Loot_Full,

        /// <summary>Предмет не влезает по весу: показаны текущий вес и предел.</summary>
        Loot_Weight_Full,

        /// <summary>Предмет не влезает: в сетке инвентаря нет свободного места.</summary>
        Loot_No_Room,

        /// <summary>Заголовок экрана наград.</summary>
        Rewards_Title,

        /// <summary>Кнопка «Взять всё» на экране наград.</summary>
        Rewards_Take_All,

        /// <summary>Подпись левой колонки: трофеи противника.</summary>
        Rewards_Source_Victory,

        /// <summary>Подпись левой колонки: содержимое тайника.</summary>
        Rewards_Source_Stash,

        /// <summary>Подпись левой колонки: лут объекта или аномалии.</summary>
        Rewards_Source_Loot,

        /// <summary>Подпись левой колонки: награда за задание.</summary>
        Rewards_Source_Quest,

        /// <summary>Строка денег награды.</summary>
        Rewards_Money,

        /// <summary>Предупреждение: оставшийся лут пропадёт.</summary>
        Rewards_Lost,

        // Ключи ниже когда-то были вставлены в середину перечисления, пока номер члена
        // сериализовался в `LocalizedText`. Теперь префабы хранят код ключа строкой,
        // порядок членов ни на что не влияет.

        /// <summary>Подпись коэффициента потери опыта при смерти.</summary>
        NewGame_XpLoss_Label,

        /// <summary>Название тайника на локации.</summary>
        Game_Stash,

        /// <summary>Подсказка вскрыть тайник.</summary>
        Game_Stash_Hint,

        /// <summary>Сообщение о срабатывании джекпота.</summary>
        Game_Jackpot,

        /// <summary>Тайник уже вскрыт.</summary>
        Game_Stash_Opened,

        /// <summary>На локации нет тайника.</summary>
        Game_Stash_None,

        /// <summary>Подпись подтипа «путь».</summary>
        Game_Subtype_Path,

        /// <summary>Подпись подтипа «здание».</summary>
        Game_Subtype_Building,

        /// <summary>Подпись подтипа «местность».</summary>
        Game_Subtype_Terrain,

        /// <summary>Действие «В хаб» на локации пути.</summary>
        Game_Return_Hub,

        /// <summary>В хаб можно вернуться только с пути.</summary>
        Game_Travel_HubUnavailable,

        /// <summary>Не хватает выносливости на переход по маршруту.</summary>
        Game_Travel_NotEnoughStamina,

        /// <summary>Не хватает выносливости на возврат в хаб.</summary>
        Game_Travel_NotEnoughStamina_Return,

        /// <summary>Строка маршрута: номер шага и цена возврата в хаб.</summary>
        Game_Route_Summary,

        /// <summary>Действие «Покинуть хаб»: строит маршрут и уводит на первую дорогу.</summary>
        Game_Leave_Hub,

        /// <summary>Действие «Отдых» на основном экране.</summary>
        Game_Rest,

        /// <summary>Заголовок диалога отдыха.</summary>
        Rest_Title,

        /// <summary>Строка текущей и максимальной выносливости в диалоге отдыха.</summary>
        Rest_Stamina,

        /// <summary>Выбранное число часов отдыха.</summary>
        Rest_Hours,

        /// <summary>Сколько выносливости восстановится за выбранные часы.</summary>
        Rest_Recovery,

        /// <summary>Шанс нападения за выбранные часы отдыха вне хаба.</summary>
        Rest_Risk,

        /// <summary>В хабе нападений не бывает.</summary>
        Rest_Safe,

        /// <summary>Кнопка «Отдыхать» в диалоге отдыха.</summary>
        Rest_Confirm,

        /// <summary>Сообщение об окончании отдыха.</summary>
        Rest_Done,

        /// <summary>Тело сообщения об окончании отдыха без нападения.</summary>
        Rest_Success,

        /// <summary>Сообщение о нападении во время отдыха.</summary>
        Rest_Encounter,

        /// <summary>В бою отдыхать нельзя.</summary>
        Rest_InCombat,

        /// <summary>Выносливость полна, отдых не нужен.</summary>
        Rest_FullStamina,

        /// <summary>Правила отдыха недоступны.</summary>
        Rest_Unavailable,

        /// <summary>Выбрано часов больше предела отдыха.</summary>
        Rest_InvalidHours,

        /// <summary>Ошибка выполнения отдыха.</summary>
        Rest_Error,

        /// <summary>Потеря здоровья от жажды за переход или ход.</summary>
        Game_Thirst_Hit,

        /// <summary>Смерть от жажды.</summary>
        Game_Thirst_Death,

        /// <summary>Заголовок экрана карты мира.</summary>
        Map_Title,

        /// <summary>Тир хаба: «Тир {0}».</summary>
        Map_Tier,

        /// <summary>Отметка текущего хаба.</summary>
        Map_Here,

        /// <summary>Переход между хабами возможен только из хаба.</summary>
        Map_NotInHub,

        /// <summary>Других хабов на карте нет.</summary>
        Map_NoTargets,

        /// <summary>Действие «Идти самому».</summary>
        Map_Walk,

        /// <summary>Действие «Через проводника».</summary>
        Map_Guide,

        /// <summary>Цена пешего перехода: часы, выносливость и риск встречи.</summary>
        Map_Walk_Price,

        /// <summary>Цена перехода с проводником: часы и деньги.</summary>
        Map_Guide_Price,

        /// <summary>Игрок уже в этом хабе.</summary>
        Map_Same,

        /// <summary>Не хватает выносливости на пеший переход.</summary>
        Map_NotEnoughStamina,

        /// <summary>Не хватает денег на проводника.</summary>
        Map_NotEnoughMoney,

        /// <summary>Переход сейчас недоступен.</summary>
        Map_Unavailable,

        /// <summary>Сообщение о прибытии в хаб: «Прибытие: {0}».</summary>
        Map_Arrived,

        /// <summary>Сообщение о встрече на дороге: «На дороге: {0}».</summary>
        Map_Encounter,

        /// <summary>Заголовок простого боевого экрана.</summary>
        Combat_Title,

        /// <summary>Строка противника: имя, здоровье и позиция.</summary>
        Combat_Enemy_Line,

        /// <summary>Строка игрока: здоровье, ОД, оружие, патроны и позиция.</summary>
        Combat_Player_Line,

        /// <summary>Заголовок журнала боя; он же подпись кнопки журнала.</summary>
        Combat_Log_Title,

        /// <summary>Кнопка «Атака».</summary>
        Combat_Attack,

        /// <summary>Кнопка «Переход»: открывает список шагов по своей половине.</summary>
        Combat_Move,

        /// <summary>Кнопка «Скиллы»: открывает список скиллов, перезарядки и смены оружия.</summary>
        Combat_Skills,

        /// <summary>Здоровье бойца: «{0} / {1}».</summary>
        Combat_Hp,

        /// <summary>Патроны в магазине: «{0} / {1}».</summary>
        Combat_Ammo,

        /// <summary>Полоса опыта: «Ур. {0} · {1} / {2}».</summary>
        Combat_Xp,

        /// <summary>Кнопка «Перезарядка».</summary>
        Combat_Reload,

        /// <summary>Кнопка «Шаг к центру».</summary>
        Combat_Step_Toward,

        /// <summary>Кнопка «Шаг от центра».</summary>
        Combat_Step_Away,

        /// <summary>Кнопка «Сменить оружие».</summary>
        Combat_Swap,

        /// <summary>Кнопка «Конец хода».</summary>
        Combat_End_Turn,

        /// <summary>Кнопка «Вернуться» после конца боя.</summary>
        Combat_Back,

        /// <summary>Сообщение «Боя нет».</summary>
        Combat_No_Combat,

        /// <summary>Позиция «ближняя».</summary>
        Combat_Position_Near,

        /// <summary>Позиция «средняя».</summary>
        Combat_Position_Mid,

        /// <summary>Позиция «дальняя».</summary>
        Combat_Position_Far,

        /// <summary>Нет оружия в активном слоте.</summary>
        Combat_No_Weapon,

        /// <summary>Кнопка скилла оружия: название, ОД и патроны.</summary>
        Combat_Skill_Button,

        /// <summary>Кнопка скилла без расхода патронов: название и ОД.</summary>
        Combat_Skill_Button_No_Ammo,

        /// <summary>Кнопка скилла с причиной недоступности.</summary>
        Combat_Skill_Reason,

        /// <summary>Скилл недоступен: не хватает ОД.</summary>
        Combat_Skill_No_Ap,

        /// <summary>Скилл недоступен: не хватает патронов.</summary>
        Combat_Skill_No_Ammo,

        /// <summary>Скилл недоступен: не та позиция.</summary>
        Combat_Skill_Wrong_Position,

        /// <summary>Скилл недоступен: оружие сломано.</summary>
        Combat_Skill_Broken_Weapon,

        /// <summary>Скилл недоступен: скилл не найден в данных.</summary>
        Combat_Skill_Missing,

        /// <summary>Скилл недоступен: бой завершён.</summary>
        Combat_Skill_Finished,

        /// <summary>Скилл не применён: название скилла.</summary>
        Combat_Skill_Unavailable,

        /// <summary>Журнал: попадание скиллом — название и урон.</summary>
        Combat_Log_Skill_Hit,

        /// <summary>Журнал: промах скиллом — название.</summary>
        Combat_Log_Skill_Miss,

        /// <summary>Журнал: скилл без выстрелов применён — название.</summary>
        Combat_Log_Skill_Used,

        /// <summary>Журнал: попадание и урон.</summary>
        Combat_Log_Hit,

        /// <summary>Журнал: промах.</summary>
        Combat_Log_Miss,

        /// <summary>Всплывающее число урона над фигурой.</summary>
        Combat_Floating_Damage,

        /// <summary>Всплывающее слово «Промах» над фигурой.</summary>
        Combat_Floating_Miss,

        /// <summary>Шанс попадания скилла в подсказке.</summary>
        Combat_Hit_Chance,

        /// <summary>Ожидаемый урон скилла в подсказке.</summary>
        Combat_Damage_Expected,

        /// <summary>Журнал: перезарядка.</summary>
        Combat_Log_Reload,

        /// <summary>Журнал: шаг по половине.</summary>
        Combat_Log_Move,

        /// <summary>Журнал: смена оружия.</summary>
        Combat_Log_Swap,

        /// <summary>Журнал: противник попал и нанёс урон.</summary>
        Combat_Log_Enemy_Hit,

        /// <summary>Журнал: противник промахнулся.</summary>
        Combat_Log_Enemy_Miss,

        /// <summary>Журнал: шаг противника по его половине.</summary>
        Combat_Log_Enemy_Move,

        /// <summary>Журнал: перезарядка противника.</summary>
        Combat_Log_Enemy_Reload,

        /// <summary>Журнал: противник применил аптечку и вылечил здоровье.</summary>
        Combat_Log_Enemy_Medkit,

        /// <summary>Результат боя: победа.</summary>
        Combat_Result_Victory,

        /// <summary>Результат боя: поражение.</summary>
        Combat_Result_Defeat,

        /// <summary>Кнопка «Выйти из боя».</summary>
        Combat_Flee,

        /// <summary>Подтверждение побега: вопрос «Вы уверены, что хотите сбежать из боя?».</summary>
        Combat_Flee_Confirm,

        /// <summary>Зелёная кнопка подтверждения побега.</summary>
        Combat_Flee_Confirm_Button,

        /// <summary>Журнал: побег из боя состоялся.</summary>
        Combat_Log_Flee,

        /// <summary>Журнал: побег не удался, противник убил игрока.</summary>
        Combat_Log_Flee_Failed,

        /// <summary>Сообщение после успешного побега.</summary>
        Combat_Result_Escaped,

        /// <summary>Действие «Обыскать» у аномалии.</summary>
        Game_Anomaly_Search,

        /// <summary>Сообщение об уроне от аномалии.</summary>
        Game_Anomaly_Hit,

        /// <summary>Сообщение, что аномалия не задела.</summary>
        Game_Anomaly_No_Effect,

        /// <summary>Сообщение о смерти от аномалии.</summary>
        Game_Anomaly_Death,

        /// <summary>Действие «Войти в бой» с названием противника локации.</summary>
        Game_Enemy_Action,

        /// <summary>Бой с противником локации не удалось начать.</summary>
        Game_Enemy_Failed,

        /// <summary>Действие «Поговорить» с именем персонажа.</summary>
        Game_Talk_Action,

        /// <summary>Разговор не удался.</summary>
        Dialog_Failed,

        /// <summary>Сообщение, что квест взят.</summary>
        Dialog_Quest_Taken,

        /// <summary>Сообщение, что квест выполнен.</summary>
        Dialog_Quest_Completed,

        /// <summary>Полоса меню диалога торговца: «Поговорить».</summary>
        Dialog_Talk,

        /// <summary>Полоса меню диалога торговца: «Поторгуем?».</summary>
        Dialog_Trade,

        /// <summary>Полоса меню диалога торговца: «Прощай».</summary>
        Dialog_Farewell,

        /// <summary>Заголовок раздела сюжетных заданий в журнале.</summary>
        Quest_Section_Story,

        /// <summary>Строка прогресса сюжетного квеста.</summary>
        Quest_Objective_Done,

        /// <summary>Заголовок отдельного экрана инвентаря.</summary>
        Inventory_Title,

        /// <summary>Кнопка «Инвентарь» в нижней полосе игрового экрана.</summary>
        Inventory_Open,

        /// <summary>Фильтр инвентаря «Все».</summary>
        Inventory_Filter_All,

        /// <summary>Фильтр инвентаря «Оружие».</summary>
        Inventory_Filter_Weapons,

        /// <summary>Фильтр инвентаря «Броня».</summary>
        Inventory_Filter_Armor,

        /// <summary>Фильтр инвентаря «Расходники».</summary>
        Inventory_Filter_Consumables,

        /// <summary>Фильтр инвентаря «Прочее».</summary>
        Inventory_Filter_Other,

        /// <summary>Заголовок быстрых слотов на боевом экране.</summary>
        Combat_Quick_Title,

        /// <summary>Подпись пустого быстрого слота.</summary>
        Combat_Quick_Empty,

        /// <summary>Журнал: игрок применил медицину и сколько вылечил.</summary>
        Combat_Log_Medicine,

        /// <summary>Заголовок строки травм игрока на боевом экране.</summary>
        Combat_Injuries_Title,

        /// <summary>Сообщение, что травм у игрока нет.</summary>
        Combat_No_Injuries,

        /// <summary>Строка одной травмы в бою: название и остаток.</summary>
        Combat_Injury_Line,

        /// <summary>Остаток травмы в ходах.</summary>
        Combat_Injury_Turns,

        /// <summary>Остаток травмы в часах.</summary>
        Combat_Injury_Hours,

        /// <summary>Травма держится до лечения.</summary>
        Combat_Injury_Until_Healed,

        /// <summary>Подпись выбора разрешения окна.</summary>
        Settings_Resolution,

        /// <summary>Подпись громкости звука.</summary>
        Settings_Volume,

        /// <summary>Подпись чувствительности мыши.</summary>
        Settings_Mouse_Sensitivity,

        /// <summary>Кнопка перехода к настройке клавиш.</summary>
        Settings_Keys,

        /// <summary>Заголовок раздела клавиш «Игра».</summary>
        Keys_Section_Game,

        /// <summary>Кнопка сохранения привязок клавиш.</summary>
        Keys_Save,

        /// <summary>Ожидание нажатия клавиши.</summary>
        Keys_Press,

        /// <summary>Заголовок подтверждения при несохранённых клавишах.</summary>
        Keys_Unsaved_Title,

        /// <summary>Вопрос при несохранённых клавишах.</summary>
        Keys_Unsaved_Question,

        /// <summary>Кнопка сброса несохранённых привязок.</summary>
        Keys_Unsaved_Discard,

        /// <summary>Сообщение о сохранении привязок.</summary>
        Keys_Saved,

        /// <summary>Прочерк у действия без клавиши.</summary>
        Keys_Unbound,

        /// <summary>Название действия «быстрое сохранение».</summary>
        Action_QuickSave,

        /// <summary>Название действия «быстрая загрузка».</summary>
        Action_QuickLoad,

        /// <summary>Название действия «инвентарь».</summary>
        Action_Inventory,

        /// <summary>Название действия «персонаж».</summary>
        Action_Character,

        /// <summary>Название действия «карта».</summary>
        Action_Map,

        /// <summary>Название действия «задания».</summary>
        Action_Quests,

        /// <summary>Название действия «таланты».</summary>
        Action_Talents,

        /// <summary>Название быстрого сохранения в списке сохранений.</summary>
        Quick_Save_Title,

        /// <summary>Сообщение об успешном быстром сохранении.</summary>
        Quick_Save_Done,

        /// <summary>Ошибка быстрого сохранения.</summary>
        Quick_Save_Error,

        /// <summary>Быстрого сохранения нет.</summary>
        Quick_Load_None,

        /// <summary>Сообщение о загрузке быстрого сохранения.</summary>
        Quick_Load_Done,

        /// <summary>Ошибка быстрой загрузки.</summary>
        Quick_Load_Error,

        /// <summary>Название объекта «карта мира» на экране хаба.</summary>
        Hub_Map,

        /// <summary>Дверь комнаты торговца: возврат в большой хаб.</summary>
        Hub_Back,

        /// <summary>Заголовок журнала заданий.</summary>
        Quest_Journal_Title,

        /// <summary>Категория журнала «сюжетные».</summary>
        Quest_Category_Story,

        /// <summary>Категория журнала «побочные».</summary>
        Quest_Category_Side,

        /// <summary>Категория журнала «повторяемые».</summary>
        Quest_Category_Repeatable,

        /// <summary>В выбранной категории журнала нет заданий.</summary>
        Quest_Empty_Category,

        /// <summary>Подсказка журнала, пока задание не выбрано.</summary>
        Quest_Details_Empty,

        /// <summary>Заголовок блока условий в описании задания.</summary>
        Quest_Details_Conditions,

        /// <summary>Подсказка сюжетного задания: сдача происходит в диалоге.</summary>
        Quest_Story_Turn_In,

        /// <summary>Тип цели задания «зачистить локацию».</summary>
        Quest_Type_Clear_Location,

        /// <summary>Тип цели задания «убить мутантов».</summary>
        Quest_Type_Kill_Mutants,

        /// <summary>Тип цели задания «собрать артефакты».</summary>
        Quest_Type_Gather_Artifacts,

        /// <summary>Тип цели задания «найти предметы».</summary>
        Quest_Type_Find_Items,

        /// <summary>Очки талантов вложены и применены.</summary>
        Talent_Saved,

        /// <summary>Подтверждение выхода с несохранёнными очками талантов.</summary>
        Talent_Unsaved,

        /// <summary>Кнопка подтверждения выхода с несохранёнными очками талантов.</summary>
        Talent_Unsaved_Confirm,

        /// <summary>Текст титров на экране авторов.</summary>
        Authors_Credits,

        /// <summary>Кнопка выхода с экрана авторов в стартовое меню.</summary>
        Authors_Back,

        /// <summary>Заголовок окна взлома тайника.</summary>
        Lock_Title,

        /// <summary>Остаток попыток взлома: осталось / всего.</summary>
        Lock_Attempts,

        /// <summary>Длина кода и размер алфавита.</summary>
        Lock_Code,

        /// <summary>Кнопка очистки введённого кода.</summary>
        Lock_Clear,

        /// <summary>Кнопка проверки кода.</summary>
        Lock_Submit,

        /// <summary>Кнопка выхода из окна взлома.</summary>
        Lock_Leave,

        /// <summary>Код введён не полностью.</summary>
        Lock_Invalid,

        /// <summary>Ответ на неверный код: на месте и не там.</summary>
        Lock_Wrong,

        /// <summary>Замок вскрыт верным кодом.</summary>
        Lock_Solved,

        /// <summary>Попытки кончились, тайник потерян.</summary>
        Lock_Failed,

        /// <summary>Кодовый замок не настроен в данных.</summary>
        Lock_Not_Configured,

        /// <summary>Заголовок окна поиска артефакта.</summary>
        Radar_Title,

        /// <summary>Остаток попыток обыска: осталось / всего.</summary>
        Radar_Attempts,

        /// <summary>Здоровье игрока в окне радара.</summary>
        Radar_Health,

        /// <summary>Остаток сканов: осталось / всего.</summary>
        Radar_Scans,

        /// <summary>Кнопка сканирования выбранной клетки.</summary>
        Radar_Scan,

        /// <summary>Кнопка обыска выбранной клетки.</summary>
        Radar_Search,

        /// <summary>Кнопка выхода из окна радара.</summary>
        Radar_Leave,

        /// <summary>Заголовок шкалы силы сигнала.</summary>
        Radar_Signal,

        /// <summary>Сигнала нет: артефактов в аномалии не осталось.</summary>
        Radar_No_Signal,

        /// <summary>Выбранная клетка поля.</summary>
        Radar_Selected,

        /// <summary>Сила сигнала в отсканированной клетке.</summary>
        Radar_Scanned,

        /// <summary>Сканы кончились.</summary>
        Radar_No_Scans,

        /// <summary>Клетка уже обыскана.</summary>
        Radar_Already,

        /// <summary>Артефакт найден и лежит в аномалии.</summary>
        Radar_Found,

        /// <summary>В клетке ничего нет.</summary>
        Radar_Empty,

        /// <summary>Радар не настроен в данных.</summary>
        Radar_Not_Configured,

        /// <summary>Попытки обыска кончились.</summary>
        Radar_No_Attempts,

        /// <summary>Строка журнала сканов: клетка и сила сигнала.</summary>
        Radar_Log,

        /// <summary>Подсказка предмета: вес одной единицы.</summary>
        Item_Weight,

        /// <summary>Подсказка предмета: базовый урон оружия.</summary>
        Item_Damage,

        /// <summary>Подсказка предмета: вместимость магазина.</summary>
        Item_Magazine,

        /// <summary>Подсказка предмета: калибр оружия или патронов.</summary>
        Item_Caliber,

        /// <summary>Подсказка предмета: бронебойность патрона.</summary>
        Item_Penetration,

        /// <summary>Подсказка предмета: текущая и максимальная прочность.</summary>
        Item_Durability,

        /// <summary>Подсказка предмета: защита от типа урона.</summary>
        Item_Protection,

        /// <summary>Подсказка предмета: прибавка рюкзака к лимиту веса.</summary>
        Item_Extra_Weight,

        /// <summary>Подсказка предмета: число слотов артефактов.</summary>
        Item_Artifact_Slots,

        /// <summary>Подсказка артефакта: прибавка к максимуму здоровья.</summary>
        Item_Health,

        /// <summary>Подсказка артефакта или расходника: прибавка к меткости.</summary>
        Item_Accuracy,

        /// <summary>Подсказка артефакта: прибавка к максимуму выносливости.</summary>
        Item_Stamina,

        /// <summary>Подсказка расходника: сколько здоровья лечит.</summary>
        Item_Heal,

        /// <summary>Подсказка расходника: сколько очков действия восстанавливает.</summary>
        Item_Ap_Restore,

        /// <summary>Подсказка расходника: сколько еды восполняет.</summary>
        Item_Food,

        /// <summary>Подсказка расходника: сколько воды восполняет.</summary>
        Item_Water,

        /// <summary>Подсказка расходника: остаток применений.</summary>
        Item_Charges,

        /// <summary>Подсказка расходника: сколько травм снимает.</summary>
        Item_Heals_Injuries,

        /// <summary>Подсказка расходника: прибавка к цене действия в ОД.</summary>
        Item_Action_Ap,

        /// <summary>Подсказка расходника: прибавка к цене шага в ОД.</summary>
        Item_Move_Ap,

        /// <summary>Подсказка расходника: сопротивление толчку.</summary>
        Item_Push_Resistance,

        /// <summary>Заголовок столбца быстрых слотов окна «Инвентарь».</summary>
        Character_Quick_Title,

        /// <summary>Подсказка под заголовком быстрых слотов: что и как туда кладётся.</summary>
        Character_Quick_Hint,

        /// <summary>Сообщение: предмет положен в быстрый слот.</summary>
        Character_Action_Quick_Assigned,

        /// <summary>Сообщение: предмет убран из быстрого слота.</summary>
        Character_Action_Quick_Removed,

        /// <summary>Отказ: предмет нельзя положить в быстрый слот.</summary>
        Character_Result_Not_Quick_Item,

        /// <summary>Строка прототипа похода: номер экрана, локация и пройденные метры.</summary>
        Game_Walk_Hud,

        /// <summary>Кнопка прототипа похода: идти вперёд.</summary>
        Game_Walk_Go,
        Game_Walk_Run,

        /// <summary>Заголовок конца дороги в прототипе похода.</summary>
        Game_Walk_End,

        /// <summary>Подсказка выбора дальнейшего пути в прототипе похода.</summary>
        Game_Walk_End_Hint,

        /// <summary>Выбор «дальше по дороге» в прототипе похода.</summary>
        Game_Walk_Next,

        /// <summary>Выбор «домой в хаб» в прототипе похода.</summary>
        Game_Walk_Home,

        /// <summary>Выбор «другая локация» в прототипе похода.</summary>
        Game_Walk_Other,

        /// <summary>Сообщение о возвращении в хаб в прототипе похода.</summary>
        Game_Walk_Hub,

        /// <summary>Кнопка «снова в путь» в прототипе похода.</summary>
        Game_Walk_Again,

        /// <summary>Сообщение об осмотренном объекте в прототипе похода.</summary>
        Game_Walk_Used,

        /// <summary>Сообщение о встреченном противнике в прототипе похода.</summary>
        Game_Walk_Fight,

        /// <summary>Сообщение о неизвестной локации в прототипе похода.</summary>
        Game_Walk_No_Location,

        /// <summary>Заголовок окна добычи в прототипе похода.</summary>
        Game_Walk_Loot_Title,

        /// <summary>Кнопка «взять всё» в окне добычи.</summary>
        Game_Walk_Take_All,

        /// <summary>Кнопка закрытия окна прототипа.</summary>
        Game_Walk_Close,

        /// <summary>Заголовок боя в прототипе похода.</summary>
        Game_Walk_Combat_Title,

        /// <summary>Кнопка «продолжить» после боя.</summary>
        Game_Walk_Continue,

        /// <summary>Строка здоровья и очков действия в прототипе боя.</summary>
        Game_Walk_Stats,

        /// <summary>Строка оружия и патронов в прототипе боя.</summary>
        Game_Walk_Weapon,

        /// <summary>Строка раунда в прототипе боя.</summary>
        Game_Walk_Round,

        /// <summary>Пометка «ваш ход» в прототипе боя.</summary>
        Game_Walk_Player_Turn,

        /// <summary>Пометка «ход противника» в прототипе боя.</summary>
        Game_Walk_Enemy_Turn,

        /// <summary>Сообщение о пустой добыче.</summary>
        Game_Walk_Empty,

        /// <summary>Сообщение о полном инвентаре.</summary>
        Game_Walk_Inventory_Full,

        /// <summary>Сообщение о взятых предметах.</summary>
        Game_Walk_Taken,

        /// <summary>Подпись полоски прогресса пути.</summary>
        Game_Walk_Path,

        /// <summary>Строка еды и воды в режиме похода.</summary>
        Game_Walk_Needs,

        /// <summary>Кнопка входа в ответвление.</summary>
        Game_Walk_Up,

        /// <summary>Заголовок экрана-заглушки «игра в стадии разработки».</summary>
        Development_Stub_Title,

        /// <summary>Текст экрана-заглушки: дальнейший контент будет позже.</summary>
        Development_Stub_Message,

        /// <summary>Кнопка закрытия экрана-заглушки.</summary>
        Development_Stub_Close
    }
}
