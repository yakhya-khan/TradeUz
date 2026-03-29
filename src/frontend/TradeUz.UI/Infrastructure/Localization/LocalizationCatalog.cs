using System.Collections.Generic;

namespace TradeUz.UI.Infrastructure.Localization;

internal static class LocalizationCatalog
{
    // Центральный каталог строк интерфейса.
    // Один и тот же ключ используется и в XAML, и во viewmodel.
    private static readonly IReadOnlyDictionary<string, (string UzbekLatin, string Russian, string English)> Strings =
        new Dictionary<string, (string UzbekLatin, string Russian, string English)>
        {
            ["ShellNavHome"] = ("Bosh sahifa", "Главная", "Home"),
            ["ShellNavSupply"] = ("Yetkazib berish", "Доставка", "Delivery"),
            ["ShellNavRetailSales"] = ("Chakana savdo", "Розничные продажи", "Retail Sales"),
            ["ShellNavSales"] = ("Savdo bo'limi", "Отдел продаж", "Sales Department"),
            ["ShellNavOrders"] = ("Buyurtmalar", "Заказы", "Orders"),
            ["ShellThemeTooltipLight"] = ("Yorug' mavzu. Qorong'u mavzuga o'tish.", "Светлая тема. Переключить на тёмную.", "Light theme. Switch to dark."),
            ["ShellThemeTooltipDark"] = ("Qorong'u mavzu. Yorug' mavzuga o'tish.", "Тёмная тема. Переключить на светлую.", "Dark theme. Switch to light."),
            ["ShellLanguageTooltip"] = ("Tilni almashtirish", "Сменить язык", "Switch language"),
            ["RetailSalesTitle"] = ("Chakana savdo", "Розничные продажи", "Retail Sales"),
            ["RetailSalesNewSale"] = ("Yangi savdo", "Новая продажа", "New Sale"),
            ["RetailSalesSearchWatermark"] = ("Mahsulot nomi / kodi", "Название / код товара", "Product name / code"),
            ["RetailSalesColItem"] = ("Mahsulot", "Товар", "Item"),
            ["RetailSalesColQty"] = ("Soni", "Кол-во", "Qty"),
            ["RetailSalesColPrice"] = ("Narxi", "Цена", "Price"),
            ["RetailSalesColTotal"] = ("Jami", "Сумма", "Total"),
            ["RetailSalesSummaryPlaceholder"] = ("Jami savdo, chegirmalar va QQS ma'lumotlari shu yerda bo'ladi", "Здесь будет информация по общей продаже, скидкам и НДС", "Sales totals, discounts, and tax details will appear here"),
            ["RetailSalesPaymentPlaceholder"] = ("To'lov ma'lumotlari, to'lov usullari va boshqa ma'lumotlar shu yerda bo'ladi", "Здесь будут данные об оплате, способах оплаты и другая информация", "Payment details, payment methods, and related information will appear here"),
            ["RetailSalesNumpad"] = ("Raqamlar paneli", "Нумпад", "Numpad"),
            ["OrdersPageTitle"] = ("Buyurtmalar sahifasi", "Страница заказов", "Orders Page"),
            ["DashboardWelcomeMessage"] = ("Assalomu alaykum", "Здравствуйте", "Hello"),
            ["DashboardDescription"] = ("Bu dashboard sahifasi", "Это страница дашборда", "This is the dashboard page"),
            ["ValidationDateIncomplete"] = ("Sana to'liq kiritilmagan", "Дата введена не полностью", "The date is incomplete"),
            ["ValidationDateInvalidMonth"] = ("Oy xato kiritilgan", "Неверно указан месяц", "The month is invalid"),
            ["ValidationDateInvalidDay"] = ("Kun xato kiritilgan", "Неверно указан день", "The day is invalid"),
            ["ValidationDateBeforeMin"] = ("Sana ruxsat etilgan qiymatdan kichikroq", "Дата меньше допустимого значения", "The date is earlier than the allowed minimum"),
            ["ValidationDateAfterMax"] = ("Sana ruxsat etilgan qiymatdan kattaroq", "Дата больше допустимого значения", "The date is later than the allowed maximum")
        };

    public static string Get(AppLanguage language, string key)
    {
        // Если перевод не найден, возвращаем ключ, чтобы проблему было видно сразу.
        if (!Strings.TryGetValue(key, out var value))
            return key;

        return language switch
        {
            AppLanguage.UzbekLatin => value.UzbekLatin,
            AppLanguage.Russian => value.Russian,
            _ => value.English
        };
    }

    public static string GetLanguageCode(AppLanguage language) =>
        language switch
        {
            // Короткий код нужен для компактной кнопки выбора языка в shell.
            AppLanguage.UzbekLatin => "UZ",
            AppLanguage.Russian => "RU",
            _ => "EN"
        };
}
