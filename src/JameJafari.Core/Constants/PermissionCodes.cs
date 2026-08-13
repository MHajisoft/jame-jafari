namespace JameJafari.Core.Constants;

public static class PermissionCodes
{
    public const string AccountsView = "accounts.view";
    public const string AccountsCreate = "accounts.create";
    public const string AccountsUpdate = "accounts.update";
    public const string AccountsDelete = "accounts.delete";

    public const string IncomeView = "income.view";
    public const string IncomeCreate = "income.create";
    public const string IncomeUpdate = "income.update";
    public const string IncomeDelete = "income.delete";

    public const string CostView = "cost.view";
    public const string CostCreate = "cost.create";
    public const string CostUpdate = "cost.update";
    public const string CostDelete = "cost.delete";

    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string UsersChangePassword = "users.changepassword";

    public const string PersonsView = "persons.view";
    public const string PersonsCreate = "persons.create";
    public const string PersonsUpdate = "persons.update";
    public const string PersonsDelete = "persons.delete";

    public const string CostTypesView = "costtypes.view";
    public const string CostTypesCreate = "costtypes.create";
    public const string CostTypesUpdate = "costtypes.update";
    public const string CostTypesDelete = "costtypes.delete";

    public const string FoodView = "food.view";
    public const string FoodCreate = "food.create";
    public const string FoodUpdate = "food.update";
    public const string FoodDelete = "food.delete";

    public const string ReportsView = "reports.view";

    public const string DeathAnniversariesView = "deathanniversaries.view";

    public const string GeneralTypesView = "generaltypes.view";
    public const string GeneralTypesCreate = "generaltypes.create";
    public const string GeneralTypesUpdate = "generaltypes.update";
    public const string GeneralTypesDelete = "generaltypes.delete";

    /// <summary>Show attachments in lists/forms and open previews (no menu).</summary>
    public const string AttachmentsView = "attachments.view";
    /// <summary>Upload new attachment files on income/cost create/update.</summary>
    public const string AttachmentsAdd = "attachments.add";
    /// <summary>Delete an existing attachment from a transaction.</summary>
    public const string AttachmentsDelete = "attachments.delete";

    public static readonly string[] All =
    [
        AccountsView, AccountsCreate, AccountsUpdate, AccountsDelete,
        IncomeView, IncomeCreate, IncomeUpdate, IncomeDelete,
        CostView, CostCreate, CostUpdate, CostDelete,
        UsersView, UsersCreate, UsersUpdate, UsersDelete, UsersChangePassword,
        PersonsView, PersonsCreate, PersonsUpdate, PersonsDelete,
        CostTypesView, CostTypesCreate, CostTypesUpdate, CostTypesDelete,
        FoodView, FoodCreate, FoodUpdate, FoodDelete,
        ReportsView,
        DeathAnniversariesView,
        GeneralTypesView, GeneralTypesCreate, GeneralTypesUpdate, GeneralTypesDelete,
        AttachmentsView, AttachmentsAdd, AttachmentsDelete
    ];
}
