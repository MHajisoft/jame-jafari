/** Central API path helpers — keeps views free of scattered route strings. */
export const ApiPaths = {
  accounts: '/accounts',
  account: (id) => `/accounts/${id}`,
  costTypes: '/cost-types',
  costType: (id) => `/cost-types/${id}`,
  generalTypes: '/general-types',
  generalType: (id) => `/general-types/${id}`,
  incomeTransactions: '/income-transactions',
  incomeTransaction: (id) => `/income-transactions/${id}`,
  incomeTransactionAttachment: (id, attachmentId) => `/income-transactions/${id}/attachments/${attachmentId}`,
  costTransactions: '/cost-transactions',
  costTransaction: (id) => `/cost-transactions/${id}`,
  costTransactionAttachment: (id, attachmentId) => `/cost-transactions/${id}/attachments/${attachmentId}`,
  persons: '/persons',
  person: (id) => `/persons/${id}`,
  users: '/users',
  user: (id) => `/users/${id}`,
  food: '/food',
  foodItem: (id) => `/food/${id}`,
  foodRecommendations: '/food/recommendations',
  reports: {
    accountBalances: '/reports/account-balances',
    costTypes: '/reports/cost-types',
    summary: '/reports/summary',
    personIncome: '/reports/person-income',
    foodCosts: '/reports/food-costs',
    deathAnniversaries: '/reports/death-anniversaries'
  },
  lookups: {
    accounts: '/lookups/accounts',
    costTypes: '/lookups/cost-types',
    generalTypes: '/lookups/general-types',
    persons: '/lookups/persons'
  }
}
