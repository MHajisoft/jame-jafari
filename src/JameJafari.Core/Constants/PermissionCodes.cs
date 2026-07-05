namespace JameJafari.Core.Constants;

public static class PermissionCodes
{
    public const string AccountsView = "accounts.view";
    public const string AccountsManage = "accounts.manage";
    public const string IncomeView = "income.view";
    public const string IncomeCreate = "income.create";
    public const string IncomeDelete = "income.delete";
    public const string CostView = "cost.view";
    public const string CostCreate = "cost.create";
    public const string CostDelete = "cost.delete";
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string PersonsView = "persons.view";
    public const string PersonsManage = "persons.manage";
    public const string CostTypesView = "costtypes.view";
    public const string CostTypesManage = "costtypes.manage";
    public const string FoodView = "food.view";
    public const string FoodManage = "food.manage";
    public const string ReportsView = "reports.view";
    public const string GeneralTypesManage = "generaltypes.manage";

    public static readonly string[] All =
    [
        AccountsView, AccountsManage,
        IncomeView, IncomeCreate, IncomeDelete,
        CostView, CostCreate, CostDelete,
        UsersView, UsersManage,
        PersonsView, PersonsManage,
        CostTypesView, CostTypesManage,
        FoodView, FoodManage,
        ReportsView, GeneralTypesManage
    ];
}
